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
using Sharpduino.Constants;
using Sharpduino.Messages.Send;
#endregion

namespace RcControl.Messages.Send
{
    public class SetConfigurationMessage
    {
        public SetConfigurationMessage(byte Channels, short MinPulse, short MaxPulse, int FrameLength)
        {
            this.Channels = Channels;
            this.MinPulse = MinPulse;
            this.MaxPulse = MaxPulse;
            this.FrameLength = FrameLength;
        }

        public byte Channels { get; set; }
        public short MinPulse { get; set; }
        public short MaxPulse { get; set; }
        public int FrameLength { get; set; }
    }
}
