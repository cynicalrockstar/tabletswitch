using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace TabletSwitchCore
{
    internal static class Win32
    {
        public static readonly int SM_CONVERTIBLESLATEMODE = 0x2003;
        public static readonly int SM_TABLETPC = 0x56;
        public static readonly int WM_SETTINGCHANGE = 0x1A;
        public static readonly int SMTO_ABORTIFHUNG = 0x0002;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "GetSystemMetrics")]
        public static extern int GetSystemMetrics(int nIndex);
    }
}
