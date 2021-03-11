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
    public class ReadChannelValueMessageResponseHandler : SysexMessageHandler<ReadChannelValueMessageResponse>
    {
        #region Variables
        private const byte CommandByte = RcCommands.READ_CHANNEL_VALUE;

        protected new const string BaseExceptionMessage =
            "Error with the incoming byte. This is not a valid ReadChannelValueMessageResponse. ";

        private enum HandlerState
        {
            StartEnd,
            StartSysex,
            Command,
            Channel,
            ValueLSB,
            ValueMSB,
            CommandStatus,
            EndSysex
        }

        private HandlerState currentHandlerState;
        #endregion

        #region Ctor / Dtor
        public ReadChannelValueMessageResponseHandler(IMessageBroker messageBroker)
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
                    currentHandlerState = HandlerState.StartSysex;
                    goto case HandlerState.StartSysex;

                case HandlerState.StartSysex:
                    return firstByte == START_MESSAGE;

                case HandlerState.Command:
                    return firstByte == CommandByte;

                case HandlerState.CommandStatus:
                    return true;

                case HandlerState.Channel:
                    return true;

                case HandlerState.ValueLSB:
                case HandlerState.ValueMSB:
                    return true;

                case HandlerState.EndSysex:
                    return firstByte == MessageConstants.SYSEX_END;

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
                case HandlerState.StartSysex:
                    if (messageByte != START_MESSAGE)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage +
                            String.Format("FirmataRC response messages must start with {0:X}", START_MESSAGE));
                    }
                    currentHandlerState = HandlerState.Command;
                    return true;

                case HandlerState.Command:
                    if (messageByte != CommandByte)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage +
                            String.Format("Command byte not {0:X}", CommandByte));
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

                    if (message.Status == RcCommandStatus.OK)
                        currentHandlerState = HandlerState.Channel;
                    else
                    {
                        message.Channel = 255;
                        currentHandlerState = HandlerState.EndSysex;
                    }
                    return true;

                case HandlerState.Channel:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "Channel should be < 128");
                    }
                    message.Channel = messageByte;
                    currentHandlerState = HandlerState.ValueLSB;
                    return true;

                case HandlerState.ValueLSB:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "Value LSB byte should be < 128");
                    }
                    message.Value = messageByte;
                    currentHandlerState = HandlerState.ValueMSB;
                    return true;

                case HandlerState.ValueMSB:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "Value MSB byte should be < 128");
                    }
                    int v = message.Value += (byte)(messageByte << 7);
                    message.Value = (byte)(v >= 255 ? 255 : v);
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
                    throw new MessageHandlerException("Unknown SetChannelValueResponseMessage handler state");
            }
        }
        #endregion
        #endregion
    }
}
