using System;
using System.Collections.Generic;
using System.Text;

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
