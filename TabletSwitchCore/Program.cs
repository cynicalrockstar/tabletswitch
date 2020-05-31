using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace TabletSwitchCore
{
    class Program
    {
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

            //The watcher will run on its own thread. Sleep the main one to keep the app resident forever
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
            //Switched from the timer method to this win32 registry monitor API. Seems to work on all architectures
            workThread = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    var monitorKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell");
                    var hkey = monitorKey.Handle.DangerousGetHandle();
                    int result = Win32.RegNotifyChangeKeyValue(hkey, true, Win32.REG_NOTIFY_CHANGE.LAST_SET, IntPtr.Zero, false);
                    CheckMetrics();
                }
            }));
            workThread.Start();
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
                    break;
            }
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

            GetResolution(out int w, out int h);

            SetResolution(w, h + 1);
            SetResolution(w, h);
        }

        private static void GetResolution(out int w, out int h)
        {
            Win32.DEVMODE dm = new Win32.DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(Win32.DEVMODE));
            Win32.EnumDisplaySettings(null, -1, ref dm);
            w = dm.dmPelsWidth;
            h = dm.dmPelsHeight;
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
