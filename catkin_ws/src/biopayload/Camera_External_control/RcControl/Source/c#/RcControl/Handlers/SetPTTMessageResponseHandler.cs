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
using Sharpduino.Base;
using Sharpduino.Constants;
using Sharpduino.Exceptions;
using Sharpduino.Messages;
using Sharpduino.Messages.Receive;
using Sharpduino.Handlers;
using RcControl.Constants;
using RcControl.Messages.Receive;
#endregion

namespace RcControl.Handlers
{
    public class SetPTTMessageResponseHandler : SysexMessageHandler<SetPTTMessageResponse>
    {
        #region Variables
        private const byte CommandByte = RcCommands.SET_PTT;

        protected new const string BaseExceptionMessage =
            "Error with the incoming byte. This is not a valid SetPTTMessageResponse. ";

        private enum HandlerState
        {
            StartEnd,
            StartSysex,
            Command,
            CommandStatus,
            PTT,
            EndSysex
        }

        private HandlerState currentHandlerState;
        #endregion

        #region Ctor / Dtor
        public SetPTTMessageResponseHandler(IMessageBroker messageBroker)
            : base(messageBroker)
        { }
        #endregion

        #region Public Methods
        #region CanHandle
        public override bool CanHandle(byte firstByte)
        {
            switch (currentHandlerState)
            {
                case HandlerState.StartEnd:
                    return firstByte == START_MESSAGE;

                case HandlerState.StartSysex:
                    return firstByte == CommandByte;

                case HandlerState.Command:
                case HandlerState.CommandStatus:
                case HandlerState.PTT:
                case HandlerState.EndSysex:
                    return true;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        #endregion
        #endregion

        #region Protected Methods
        #region OnResetHandlerState
        protected override void OnResetHandlerState()
        {
            currentHandlerState = HandlerState.StartEnd;
        }
        #endregion
        #region HandleByte
        protected override bool HandleByte(byte messageByte)
        {
            switch (currentHandlerState)
            {
                case HandlerState.StartEnd:
                    currentHandlerState = HandlerState.StartSysex;
                    return true;

                case HandlerState.StartSysex:
                    if (messageByte != RcCommands.SET_PTT)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage +
                            String.Format("Command byte not {0:X}", RcCommands.SET_PTT));
                    }
                    currentHandlerState = HandlerState.CommandStatus;
                    return true;

                case HandlerState.CommandStatus:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "Command status should be < 128");
                    }
                    message.Status = (RcCommandStatus)messageByte;
                    currentHandlerState = HandlerState.PTT;
                    return true;

                case HandlerState.PTT:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "PTT should be < 128");
                    }
                    message.Value = messageByte;
                    currentHandlerState = HandlerState.EndSysex;
                    return true;

                case HandlerState.EndSysex:
                    if (messageByte == MessageConstants.SYSEX_END)
                    {
                        messageBroker.CreateEvent(message);
                        Reset();
                        return false;
                    }
                    return true;

                default:
                    throw new MessageHandlerException("Unknown ResetMessageResponse handler state");
            }
        }
        #endregion
        #endregion
    }
}
