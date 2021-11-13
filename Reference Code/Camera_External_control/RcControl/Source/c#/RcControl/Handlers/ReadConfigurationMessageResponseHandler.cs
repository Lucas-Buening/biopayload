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
    public class ReadConfigurationMessageResponseHandler : SysexMessageHandler<ReadConfigurationMessageResponse>
    {
        #region Variables
        private const byte CommandByte = RcCommands.READ_CONFIGURATION;

        protected new const string BaseExceptionMessage =
            "Error with the incoming byte. This is not a valid ReadConfigurationMessageResponse. ";

        private enum HandlerState
        {
            StartEnd,
            StartSysex,
            Command,
            Channels,
            MinPulseLSB,
            MinPulseMSB,
            MaxPulseLSB,
            MaxPulseMSB,
            FrameLengthLSB,
            FrameLengthLSB1,
            FrameLengthMSB,
            FrameLengthMSB1,
            CommandStatus,
            EndSysex
        }

        private HandlerState currentHandlerState;
        #endregion

        #region Ctor / Dtor
        public ReadConfigurationMessageResponseHandler(IMessageBroker messageBroker)
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

                case HandlerState.Channels:
                    return true;

                case HandlerState.MinPulseLSB:
                case HandlerState.MinPulseMSB:
                case HandlerState.MaxPulseLSB:
                case HandlerState.MaxPulseMSB:
                case HandlerState.FrameLengthLSB:
                case HandlerState.FrameLengthLSB1:
                case HandlerState.FrameLengthMSB:
                case HandlerState.FrameLengthMSB1:
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
                        currentHandlerState = HandlerState.Channels;
                    else
                    {
                        message.Channels = 0;
                        currentHandlerState = HandlerState.EndSysex;
                    }
                    return true;

                case HandlerState.Channels:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "Channel should be < 128");
                    }
                    message.Channels = messageByte;
                    currentHandlerState = HandlerState.MinPulseLSB;
                    return true;

                case HandlerState.MinPulseLSB:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "MinPulse LSB byte should be < 128");
                    }
                    message.MinPulse = messageByte;
                    currentHandlerState = HandlerState.MinPulseMSB;
                    return true;

                case HandlerState.MinPulseMSB:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "MinPulse MSB byte should be < 128");
                    }
                    message.MinPulse += (short)(messageByte << 7);
                    currentHandlerState = HandlerState.MaxPulseLSB;
                    return true;

                case HandlerState.MaxPulseLSB:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "MaxPulse LSB byte should be < 128");
                    }
                    message.MaxPulse = messageByte;
                    currentHandlerState = HandlerState.MaxPulseMSB;
                    return true;

                case HandlerState.MaxPulseMSB:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "MaxPulse MSB byte should be < 128");
                    }
                    message.MaxPulse += (short)(messageByte << 7);
                    currentHandlerState = HandlerState.FrameLengthLSB;
                    return true;

                case HandlerState.FrameLengthLSB:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "FrameLength LSB byte should be < 128");
                    }
                    message.FrameLength = messageByte;
                    currentHandlerState = HandlerState.FrameLengthLSB1;
                    return true;

                case HandlerState.FrameLengthLSB1:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "FrameLength LSB 1 byte should be < 128");
                    }
                    message.FrameLength += messageByte << 7;
                    currentHandlerState = HandlerState.FrameLengthMSB;
                    return true;

                case HandlerState.FrameLengthMSB:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "FrameLength MSB byte should be < 128");
                    }
                    message.FrameLength += messageByte << 14;
                    currentHandlerState = HandlerState.FrameLengthMSB1;
                    return true;

                case HandlerState.FrameLengthMSB1:
                    if (messageByte > 127)
                    {
                        currentHandlerState = HandlerState.StartEnd;
                        throw new MessageHandlerException(BaseExceptionMessage + "FrameLength MSB 1 byte should be < 128");
                    }
                    message.FrameLength += messageByte << 21;
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
