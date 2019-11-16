using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace TabletSwitchCore
{
    enum Scaling
    {
        Screen300 = 3,
        Screen250 = 2,
        Screen225 = 1,
        ScreenDefault = 0,
        Screen175 = -1,
        Screen150 = -2,
        Screen125 = -3,
        Screen100 = -4
    }

    class Program
    {
        static System.Timers.Timer timer = new System.Timers.Timer();

        static void Main(string[] args)
        {
            timer.Interval = 1000;
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            //The timer will run on its own thread. Sleep the main one to keep the app resident forever
            Thread.Sleep(Timeout.Infinite);
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var state = Win32.GetSystemMetrics(Win32.SM_CONVERTIBLESLATEMODE);
            if (state == 0)
            {
                //Tablet mode
                //This is 200% on surface devices
                ChangeDPI((int)Scaling.ScreenDefault);
            }
            else
            {
                //Desktop mode
                ChangeDPI((int)Scaling.Screen150);
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
                return;

            key.SetValue("DpiValue", dpi);

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
