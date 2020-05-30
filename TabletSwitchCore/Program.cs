using System;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace TabletSwitchCore
{
    class Program
    {
        static System.Timers.Timer timer = null;
        static LastChange lastChange = null;
        static Thread workThread = null;
        public static IConfigurationRoot configuration;

        static void Main(string[] args)
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            try
            {
                //Check on startup, since we won't get a change event if starting late
                CheckMetrics();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            StartWorkThread();

            //The timer/watcher will run on its own thread. Sleep the main one to keep the app resident forever
            Thread.Sleep(Timeout.Infinite);
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Build configuration
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton<IConfigurationRoot>(configuration);
        }

        private static void StartWorkThread()
        {
            //The System.Management bits do not work on ARM native, so if we are an ARM process, fall back to polling
            //If running in emulation, this env variable will be "x86," and the watcher works fine in that environment.
            var arch = System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE").ToLower();
            if (arch == "arm64" || arch == "arm")
            {
                timer = new System.Timers.Timer();
                //Slowed down from 1000
                timer.Interval = 3000;
                timer.AutoReset = true;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            }
            else
            {
                workThread = new Thread(new ThreadStart(() =>
                {
                    var query = new WqlEventQuery("Win32_SystemConfigurationChangeEvent");
                    var watcher = new ManagementEventWatcher(query);
                    watcher.EventArrived += Watcher_EventArrived;
                    watcher.Start();
                }));
                workThread.Start();
            }
        }

        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    StartWorkThread();
                    break;
                case PowerModes.Suspend:
                    if (workThread != null)
                        workThread.Abort();
                    if (timer != null)
                        timer.Stop();
                    break;
            }
        }

        private static void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            //We get a spray of notifications when this event fires. 
            //If it hasn't been more than 5 seconds since we received, just ignore
            if (lastChange?.TimeOfChange > DateTime.Now.AddSeconds(-5))
                return;

            CheckMetrics();
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CheckMetrics();
        }

        private static void CheckMetrics()
        {
            //Two ways to do this: GetSystemMetrics detects the undocking of the machine, 
            //the registry method reads the "Tablet Mode" switch; SM_TABLET PC does not seem to work on Windows 10
            //and SM_CONVERTIBLESLATEMODE doesn't seem totally reliable

            var tabletMode = false;

            var detectType = configuration.GetSection("SwitchDetect").Value;
            if (detectType == "TabletMode")
            {
                var regTabletMode = (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell", "TabletMode", 0);
                if (regTabletMode == 1)
                    tabletMode = true;
            }
            else if (detectType == "SystemMetric")
            {
                var state = Win32.GetSystemMetrics(Win32.SM_CONVERTIBLESLATEMODE);
                if (state == (int)DeviceState.Tablet && lastChange?.DeviceState != DeviceState.Tablet)
                    tabletMode = true;
            }
            
            if (tabletMode)
            {
                //Tablet mode
                var tabletDpi = (Scaling)Enum.Parse(typeof(Scaling), configuration.GetSection("TabletDpi").Value);
                ChangeDPI((int)tabletDpi);
                lastChange = new LastChange { DeviceState = DeviceState.Tablet, TimeOfChange = DateTime.Now };
            }
            else
            {
                //Desktop mode
                var desktopDpi = (Scaling)Enum.Parse(typeof(Scaling), configuration.GetSection("DesktopDpi").Value);
                ChangeDPI((int)desktopDpi);
                lastChange = new LastChange { DeviceState = DeviceState.Desktop, TimeOfChange = DateTime.Now };
            }
        }

        private static void ChangeDPI(int dpi)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Control Panel", true);

            key = key.OpenSubKey("Desktop", true);
            key = key.OpenSubKey("PerMonitorSettings", true);

            var monitorId = configuration.GetSection("MonitorID").Value;

            key = key.OpenSubKey(monitorId, true);

            var current = Convert.ToInt32(key.GetValue("DpiValue"));
            if (current == dpi)
            {
                key.Close();
                return;
            }

            key.SetValue("DpiValue", dpi);
            key.Flush();

            //jog the resolution so Windows notices the dpi change
            //might be a better way, couldn't find one
            SetResolution(1920, 1280);
            SetResolution(2880, 1920);
        }

        private static void SetResolution(int w, int h)
        {
            Win32.DEVMODE dm = new Win32.DEVMODE();

            dm.dmSize = (short)Marshal.SizeOf(typeof(Win32.DEVMODE));

            dm.dmPelsWidth = w;
            dm.dmPelsHeight = h;

            dm.dmFields = Win32.DEVMODE.DM_PELSWIDTH | Win32.DEVMODE.DM_PELSHEIGHT;

            Win32.ChangeDisplaySettings(ref dm, 0);
        }
    }
}
