using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        [DllImport("DpiHelper.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetDisplayDpi(int dpi);

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
                    //This doesn't work on Windows 11 any more. Leaving it here for posterity
                    //var monitorKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell");
                    //var hkey = monitorKey.Handle.DangerousGetHandle();
                    //int result = Win32.RegNotifyChangeKeyValue(hkey, true, Win32.REG_NOTIFY_CHANGE.LAST_SET, IntPtr.Zero, false);
                    CheckMetrics();
                    Thread.Sleep(3000);
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
                    {
                        // Replace Thread.Abort() with a safer approach using a cancellation mechanism
                        workThread.Interrupt();
                        workThread.Join(); // Wait for the thread to finish
                        workThread = null;
                    }
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
            if (detectType == "TabletMode") //This may not work any more
            {
                var regTabletMode = (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\ImmersiveShell", "TabletMode", 0);
                if (regTabletMode == 1)
                    tabletMode = true;
            }
            else if (detectType == "SystemMetric")
            {
                var state = Win32.GetSystemMetrics(Win32.SM_CONVERTIBLESLATEMODE);
                if (state == (int)DeviceState.Tablet)
                    tabletMode = true;
            }

            if (tabletMode && (lastChange == null || lastChange.DeviceState != DeviceState.Tablet))
            {
                //Tablet mode
                var tabletDpi = (Scaling)Enum.Parse(typeof(Scaling), configuration.GetSection("TabletDpi").Value);
                var result = SetDisplayDpi((int)tabletDpi); //C++ DLL callout
                lastChange = new LastChange { DeviceState = DeviceState.Tablet, TimeOfChange = DateTime.Now };
            }
            else if (tabletMode == false && (lastChange == null || lastChange.DeviceState != DeviceState.Desktop))
            {
                //Desktop mode
                var desktopDpi = (Scaling)Enum.Parse(typeof(Scaling), configuration.GetSection("DesktopDpi").Value);
                var result = SetDisplayDpi((int)desktopDpi); //C++ DLL callout
                lastChange = new LastChange { DeviceState = DeviceState.Desktop, TimeOfChange = DateTime.Now };
            }
        }
    }
}
