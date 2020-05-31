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

        public enum DMDO
        {
            DEFAULT = 0,
            D90 = 1,
            D180 = 2,
            D270 = 3
        }


        [Flags]
        public enum REG_NOTIFY_CHANGE : uint
        {
            /// <summary>
            /// Notify the caller if a subkey is added or deleted
            /// </summary>
            NAME = 0x1,
            /// <summary>
            /// Notify the caller of changes to the attributes of the key,
            /// such as the security descriptor information
            /// </summary>
            ATTRIBUTES = 0x2,
            /// <summary>
            /// Notify the caller of changes to a value of the key. This can
            /// include adding or deleting a value, or changing an existing value
            /// </summary>
            LAST_SET = 0x4,
            /// <summary>
            /// Notify the caller of changes to the security descriptor of the key
            /// </summary>
            SECURITY = 0x8
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DEVMODE
        {
            public const int DM_PELSWIDTH = 0x80000;
            public const int DM_PELSHEIGHT = 0x100000;
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;

            public int dmPositionX;
            public int dmPositionY;
            public DMDO dmDisplayOrientation;
            public int dmDisplayFixedOutput;

            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int ChangeDisplaySettings([In] ref DEVMODE lpDevMode, int dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int EnumDisplaySettings(byte[] lpszDeviceName, [param: MarshalAs(UnmanagedType.U4)] int iModeNum, [In, Out] ref DEVMODE lpDevMode);
        
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, EntryPoint = "GetSystemMetrics")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("Advapi32.dll")]
        public static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool watchSubtree, REG_NOTIFY_CHANGE notifyFilter, IntPtr hEvent, bool asynchronous);
    }
}
