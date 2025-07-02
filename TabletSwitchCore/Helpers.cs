using System;
using System.Collections.Generic;
using System.Text;

namespace TabletSwitchCore
{
    enum Scaling
    {
        Larger3 = 200,
        Larger2 = 175,
        Larger1 = 150,
        ScreenDefault = 125
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
