// -------------------------------------------------------------
// FirmataRC - Version 1.0 Copyright richard.prinz@min.at 2013
// This code is under Creative Commons License V3.0
// See:
// http://creativecommons.org/licenses/by/3.0/
// http://creativecommons.org/licenses/by/3.0/at/
//
// You are allowed to use and modify this code (private and 
// commercial) as long as you reference the origin of it in
// any end user documentation, EULA's etc.

#region Usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#endregion

namespace RcControl.Constants
{
    public static class RcCommands
    {
        public const byte SET_CONFIGURATION = 0x50;
        public const byte READ_CONFIGURATION = 0x51;
        public const byte SET_CHANNEL_VALUE = 0x52;
        public const byte READ_CHANNEL_VALUE = 0x53;
        public const byte READ_ALL_CHANNEL_VALUES = 0x54;
        public const byte SET_PTT = 0x55;
        public const byte RESET = 0x5F;
    }
}
