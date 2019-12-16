using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using Microsoft.Win32;

namespace TabletSwitchCore
{
    class Program
    {
        static System.Timers.Timer timer = null;
        static LastChange lastChange = null;
        static Thread workThread = null;

        static void Main(string[] args)
        {
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            //Check on startup, since we won't get a change event if starting late
            CheckMetrics();

            StartWorkThread();

            //The timer/watcher will run on its own thread. Sleep the main one to keep the app resident forever
            Thread.Sleep(Timeout.Infinite);
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
            var state = Win32.GetSystemMetrics(Win32.SM_CONVERTIBLESLATEMODE);
            if (state == (int)DeviceState.Tablet && lastChange?.DeviceState != DeviceState.Tablet)
            {
                //Tablet mode
                //This is 200% on surface devices
                ChangeDPI((int)Scaling.ScreenDefault);
                lastChange = new LastChange { DeviceState = DeviceState.Tablet, TimeOfChange = DateTime.Now };
            }
            else if (state == (int)DeviceState.Desktop && lastChange?.DeviceState != DeviceState.Desktop)
            {
                //Desktop mode
                ChangeDPI((int)Scaling.Screen150);
                lastChange = new LastChange { DeviceState = DeviceState.Desktop, TimeOfChange = DateTime.Now };
            }
        }

        private static void ChangeDPI(int dpi)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("Control Panel", true);

            key = key.OpenSubKey("Desktop", true);
            key = key.OpenSubKey("PerMonitorSettings", true);

            //TODO: Get your own monitor ID from the registry and put it here
            key = key.OpenSubKey("LGD0555600224_00_07E2_41^6DF395BF1D440664DC6515C277A800D6", true);

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
