using System;
using System.Collections.Generic;
using System.Text;

namespace TabletSwitchCore
{
    enum Scaling
    {
        Larger3 = 3,
        Larger2 = 2,
        Larger1 = 1,
        ScreenDefault = 0,
        Smaller1 = -1,
        Smaller2 = -2,
        Smaller3 = -3,
        Smaller4 = -4
    }

    enum DeviceState
    {
        Tablet = 0,
        Desktop = 1
    }

    class LastChange
    {
        public DeviceState DeviceState { get; set; }
        public DateTime TimeOfChange { get; set; }
    }
}
