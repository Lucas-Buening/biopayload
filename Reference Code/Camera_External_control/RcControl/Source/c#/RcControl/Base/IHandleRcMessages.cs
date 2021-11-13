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
using Sharpduino.Messages.Receive;
using Sharpduino.Base;
using RcControl.Messages.Send;
using RcControl.Messages.Receive;
#endregion

namespace RcControl.Base
{
    public interface IHandleRcMessages :
        IHandle<SetConfigurationMessageResponse>,
        IHandle<ReadConfigurationMessageResponse>,
        IHandle<SetChannelValueMessageResponse>,
        IHandle<ReadChannelValueMessageResponse>,
        IHandle<ReadAllChannelValuesMessageResponse>,
        IHandle<SetPTTMessageResponse>,
        IHandle<ResetMessageResponse>
    {}
}