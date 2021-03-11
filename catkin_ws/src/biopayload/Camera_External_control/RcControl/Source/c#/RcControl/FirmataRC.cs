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
using Sharpduino.Base;
using Sharpduino.Constants;
using Sharpduino.EventArguments;
using Sharpduino.Messages;
using Sharpduino.Messages.Receive;
using Sharpduino.Messages.Send;
using Sharpduino.Messages.TwoWay;
using Sharpduino.SerialProviders;
using Sharpduino;
using RcControl.Base;
using RcControl.Messages.Receive;
using RcControl.Messages.Send;
#endregion

namespace RcControl
{
    public class FirmataRC : FirmataRCBase, IHandleAllMessages, IHandleRcMessages
    {
        #region Enums
        private enum InitializationStages
        {
            QueryProtocolVersion = 0,
            QueryFirmwareVersion,
            //QueryCapabilities,
            //QueryAnalogMappings,
            //QueryPinStates,
            //StartReports,
            FullyInitialized
        }
        #endregion

        #region Variables
        private InitializationStages currentInitState;
        #endregion

        #region Properties
        #region IsInitialized
        /// <summary>
        /// This is true if we have finished the first communications with the board
        /// to setup the main functionality. The EasyFirmata can be used when this is true
        /// </summary>
        public bool IsInitialized
        {
            get { return currentInitState == InitializationStages.FullyInitialized; }
        }
        #endregion
        #region Pins
        /// <summary>
        /// The pins available
        /// </summary>
        public List<Pin> Pins { get; private set; }
        #endregion
        #region AnalogPins
        /// <summary>
        /// The analog pins of the board
        /// </summary>
        public List<Pin> AnalogPins { get; private set; }
        #endregion
        #region ProtocolVersion
        /// <summary>
        /// The protocol version that the board uses to communicate
        /// </summary>
        public string ProtocolVersion { get; private set; }
        #endregion
        #region Firmware
        /// <summary>
        /// The firmware version that the board is running
        /// </summary>
        public string Firmware { get; private set; }
        #endregion
        #endregion

        #region Events
        #region Initialized
        /// <summary>
        /// This event marks the end of the initialization procedure
        /// The EasyFirmata is usable now
        /// </summary>
        public event EventHandler Initialized;
        private void OnInitialized()
        {
            var handler = Initialized;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }
        #endregion
        #region NewAnalogValue
        /// <summary>
        /// Event to notify about new analog values
        /// </summary>
        public event EventHandler<NewAnalogValueEventArgs> NewAnalogValue;
        private void OnNewAnalogValue(byte pin, int value)
        {
            var handler = NewAnalogValue;
            if (handler != null)
            {
                handler(this, new NewAnalogValueEventArgs() { AnalogPin = pin, NewValue = value });
            }
        }
        #endregion
        #region NewDigitalValue
        /// <summary>
        /// Event that is raised when a digital message is received 
        /// </summary>
        public event EventHandler<NewDigitalValueEventArgs> NewDigitalValue;
        private void OnNewDigitalValue(int port, bool[] pins)
        {
            var handler = NewDigitalValue;
            if (handler != null)
            {
                handler(this, new NewDigitalValueEventArgs() { Port = port, Pins = pins });
            }
        }
        #endregion
        #region NewStringMessage
        /// <summary>
        /// Event that is raised when a string message is received
        /// </summary>
        public event EventHandler<NewStringMessageEventArgs> NewStringMessage;
        private void OnNewStringMessage(string message)
        {
            var handler = NewStringMessage;
            if (handler != null)
            {
                handler(this, new NewStringMessageEventArgs() { Message = message });
            }
        }
        #endregion
        #region PinStateReceived
        /// <summary>
        /// Event that is raised when we receive a message about the state of a pin
        /// Usually in response to a PinStateQueryMessage
        /// </summary>
        public event EventHandler<PinStateEventArgs> PinStateReceived;
        private void OnPinStateReceived(PinStateMessage message)
        {
            var handler = PinStateReceived;
            if (handler != null)
            {
                handler(this, new PinStateEventArgs() { Pin = message.PinNo, Mode = message.Mode, Value = message.State });
            }
        }
        #endregion

        #region SetConfigurationResponse
        public event EventHandler<SetConfigurationMessageResponse> SetConfigurationResponse;
        private void OnSetConfigurationResponse(SetConfigurationMessageResponse message)
        {
            var handler = SetConfigurationResponse;
            if (handler != null)
                handler(this, message);
        }
        #endregion
        #region ReadConfigurationResponse
        public event EventHandler<ReadConfigurationMessageResponse> ReadConfigurationResponse;
        private void OnReadConfigurationResponse(ReadConfigurationMessageResponse message)
        {
            var handler = ReadConfigurationResponse;
            if (handler != null)
                handler(this, message);
        }
        #endregion

        #region SetChannelValueResponse
        public event EventHandler<SetChannelValueMessageResponse> SetChannelValueResponse;
        private void OnSetChannelValueResponse(SetChannelValueMessageResponse message)
        {
            var handler = SetChannelValueResponse;
            if (handler != null)
                handler(this, message);
        }
        #endregion
        #region ReadChannelValueResponse
        public event EventHandler<ReadChannelValueMessageResponse> ReadChannelValueResponse;
        private void OnReadChannelValueResponse(ReadChannelValueMessageResponse message)
        {
            var handler = ReadChannelValueResponse;
            if (handler != null)
                handler(this, message);
        }
        #endregion
        #region ReadAllChannelValuesResponse
        public event EventHandler<ReadAllChannelValuesMessageResponse> ReadAllChannelValuesResponse;
        private void OnReadAllChannelValuesResponse(ReadAllChannelValuesMessageResponse message)
        {
            var handler = ReadAllChannelValuesResponse;
            if (handler != null)
                handler(this, message);
        }
        #endregion

        #region SetPTTResponse
        public event EventHandler<SetPTTMessageResponse> SetPTTResponse;
        private void OnSetPTTResponse(SetPTTMessageResponse message)
        {
            var handler = SetPTTResponse;
            if (handler != null)
                handler(this, message);
        }
        #endregion
        #region ResetResponse
        public event EventHandler<ResetMessageResponse> ResetResponse;
        private void OnResetResponse(ResetMessageResponse message)
        {
            var handler = ResetResponse;
            if (handler != null)
                handler(this, message);
        }
        #endregion
        #endregion

        #region Ctor / Dtor
        public FirmataRC(ISerialProvider serialProvider)
            : base(serialProvider)
        {
            // Initialize the objects
            Pins = new List<Pin>();
            AnalogPins = new List<Pin>();

            // Subscribe ourselves to the message broker
            MessageBroker.Subscribe(this);

            // Start the init procedure
            ReInit();
        }
        #endregion

        #region Public Methods
        #region ReInit
        public void ReInit()
        {
            currentInitState = 0;
            AdvanceInitialization();
        }
        #endregion
        #region Handle
        #region Handle ProtocolVersionMessage
        /// <summary>
        /// Handle the Protocol Message. Contains info about the protocol that the board is using to communicate
        /// </summary>
        public void Handle(ProtocolVersionMessage message)
        {
            if (IsInitialized)
                return;
            ProtocolVersion = string.Format("{0}.{1}", message.MajorVersion, message.MinorVersion);

            // Go to the next state
            currentInitState++;

            AdvanceInitialization();
        }
        #endregion
        #region Handle SysexFirmwareMessage
        /// <summary>
        /// Handle the Firmware Message. Contains info about the firmware running in the board
        /// </summary>
        public void Handle(SysexFirmwareMessage message)
        {
            if (IsInitialized)
                return;
            Firmware = string.Format("{0}:{1}.{2}", message.FirmwareName, message.MajorVersion, message.MinorVersion);

            // Go to the next state
            currentInitState++;

            AdvanceInitialization();
        }
        #endregion
        #region Handle CapabilityMessage
        /// <summary>
        /// Handle the capability messages. There will be one such message for each pin
        /// </summary>
        public void Handle(CapabilityMessage message)
        {
            var pin = new Pin();
            foreach (var mes in message.Modes)
                pin.Capabilities[mes.Key] = mes.Value;

            // Add it to the collection
            Pins.Add(pin);
        }
        #endregion
        #region Handle CapabilitiesFinishedMessage
        /// <summary>
        /// Handle the Capabilities Finished Message. This is used to advance to the next step of
        /// the initialization after the capabilities
        /// </summary>
        public void Handle(CapabilitiesFinishedMessage message)
        {
            // If we haven't initialized then do the next thing in the init procedure
            if (!IsInitialized)
            {
                // Go to the next state
                currentInitState++;

                AdvanceInitialization();
            }
            // Otherwise this message conveys no information
        }
        #endregion
        #region Handle AnalogMappingMessage
        /// <summary>
        /// Handle the Analog Mapping Message. This is used to find out which pins have 
        /// analog input capabilities and fill the AnalogPins list
        /// </summary>
        public void Handle(AnalogMappingMessage message)
        {
            if (IsInitialized) return;

            for (int i = 0; i < message.PinMappings.Count; i++)
            {
                // If we have an analog pin
                if (message.PinMappings[i] != 127)
                {
                    // Put the corresponding pin to the analog pins dictionary
                    // this is a reference, so any changes to the primary object
                    // will be reflected here too.
                    AnalogPins.Add(Pins[i]);
                }
            }

            // Go to the next state
            currentInitState++;

            AdvanceInitialization();
        }
        #endregion
        #region Handle PinStateMessage
        /// <summary>
        /// Handler the Pin State Message. Get more information about each pin.
        /// This is called multiple times and we advance to the next step, only after
        /// we have received information about the last pin
        /// </summary>
        public void Handle(PinStateMessage message)
        {
            Pin currentPin = Pins[message.PinNo];
            currentPin.CurrentMode = message.Mode;
            currentPin.CurrentValue = message.State;

            if (IsInitialized)
                return;

            // Notify others only when we are fully initialized
            OnPinStateReceived(message);

            // here we check to see if we have finished with the PinState Messages
            // and advance to the next step. Test the following:
            if (message.PinNo == Pins.Count - 1)
            {
                // Go to the next state
                currentInitState++;

                AdvanceInitialization();
            }
        }
        #endregion
        #region Handle AnalogMessage
        /// <summary>
        /// Handle the Analog Messsage. Update the value for the pin and raise a
        /// NewAnalogValue event
        /// </summary>
        public void Handle(AnalogMessage message)
        {
            // Here we are in the twilight zone
            //if (currentInitState <= InitializationStages.QueryPinStates)
            //    return;

            // First save the value in the Pins and AnalogPins lists
            AnalogPins[message.Pin].CurrentValue = message.Value;

            OnNewAnalogValue(message.Pin, message.Value);
        }
        #endregion
        #region Handle DigitalMessage
        /// <summary>
        /// Handle the Digital Message. Update the values for the pins of the port
        /// and raise a NewDigitalValue event
        /// </summary>
        /// <param name="message"></param>
        public void Handle(DigitalMessage message)
        {
            var pinStart = (byte)(8 * message.Port);
            for (byte i = 0; i < 8; i++)
            {
                Pins[i + pinStart].CurrentValue = message.PinStates[i] ? 1 : 0;
            }

            OnNewDigitalValue(message.Port, message.PinStates);
        }
        #endregion
        #region Handle SysexStringMessage
        /// <summary>
        /// Handle the Sysex String Message. Raise a NewStringMessage event
        /// </summary>
        /// <param name="message"></param>
        public void Handle(SysexStringMessage message)
        {
            OnNewStringMessage(message.Message);
        }
        #endregion
        #region Handle I2CResponseMessage
        public void Handle(I2CResponseMessage message)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Handle SetConfigurationMessageResponse
        public void Handle(SetConfigurationMessageResponse message)
        {
            OnSetConfigurationResponse(message);
        }
        #endregion
        #region Handle ReadConfigurationMessageResponse
        public void Handle(ReadConfigurationMessageResponse message)
        {
            OnReadConfigurationResponse(message);
        }
        #endregion
        #region Handle SetChannelValueResponseMessage
        public void Handle(SetChannelValueMessageResponse message)
        {
            OnSetChannelValueResponse(message);
        }
        #endregion
        #region Handle ReadChannelValueMessageResponse
        public void Handle(ReadChannelValueMessageResponse message)
        {
            OnReadChannelValueResponse(message);
        }
        #endregion
        #region Handle ReadAllChannelValuesMessageResponse
        public void Handle(ReadAllChannelValuesMessageResponse message)
        {
            OnReadAllChannelValuesResponse(message);
        }
        #endregion
        #region Handle SetPTTMessage
        public void Handle(SetPTTMessageResponse message)
        {
            OnSetPTTResponse(message);
        }
        #endregion
        #region Handle ResetMessage
        public void Handle(ResetMessageResponse message)
        {
            OnResetResponse(message);
        }
        #endregion
        #endregion
        #region GetDigitalPortValues
        /// <summary>
        /// Get the current values for a digital port. It is useful for creating a DigitalMessage
        /// </summary>
        /// <param name="port">The port whose values we want</param>
        /// <returns>A bool array representing the current state of each pin</returns>
        public bool[] GetDigitalPortValues(int port)
        {
            var values = new bool[8];
            for (int i = 0; i < 8; i++)
            {
                // Even if we have analog values ie > 1 we put 0 as it doesn't matter
                // from the board side. They will be ignored anyway
                if (port * 8 + i < Pins.Count)
                    values[i] = Pins[port * 8 + i].CurrentValue == 1 ? true : false;
            }

            return values;
        }
        #endregion
        #region SendMessage
        public override void SendMessage<T>(T message)
        {
            if (!IsInitialized)
                return;
            base.SendMessage(message);
        }
        #endregion

        #region ReadConfiguration
        public void ReadConfiguration()
        {
            if (!IsInitialized)
                return;
            SendMessage<ReadConfigurationMessage>(new ReadConfigurationMessage());
        }
        #endregion
        #region SetChannelValue
        public void SetChannelValue(byte Channel, byte Value)
        {
            if (!IsInitialized)
                return;
            SendMessage<SetChannelValueMessage>(new SetChannelValueMessage(Channel, Value));
        }
        #endregion
        #region ReadChannelValue
        public void ReadChannelValue(byte Channel)
        {
            if (!IsInitialized)
                return;
            SendMessage<ReadChannelValueMessage>(new ReadChannelValueMessage(Channel));
        }
        #endregion
        #region SetPTT
        public void SetPTT(bool Value)
        {
            if (!IsInitialized)
                return;
            SendMessage<RcControl.Messages.Send.SetPTTMessage>(
                new RcControl.Messages.Send.SetPTTMessage((byte)(Value ? 1 : 0)));
        }
        #endregion
        #region Reset
        public void Reset()
        {
            if (!IsInitialized)
                return;
            SendMessage<RcControl.Messages.Send.ResetMessage>(new RcControl.Messages.Send.ResetMessage());
        }
        #endregion
        #endregion

        #region Protected Methods
        #region Dispose
        protected override void Dispose(bool shouldDispose)
        {
            StopReports();

            base.Dispose(shouldDispose);
        }
        #endregion
        #endregion

        #region Private Methods
        #region AdvanceInitialization
        /// <summary>
        /// Go through the initialization procedure
        /// </summary>
        private void AdvanceInitialization()
        {
            // Do nothing if we are initialized
            if (currentInitState == InitializationStages.FullyInitialized)
                return;

            switch (currentInitState)
            {
                case InitializationStages.QueryProtocolVersion:
                    // This is the first inistialization stage
                    // Stop any previous reports
                    StopReports();
                    base.SendMessage(new ProtocolVersionRequestMessage());
                    break;
                case InitializationStages.QueryFirmwareVersion:
                    base.SendMessage(new QueryFirmwareMessage());
                    break;
                /*
                case InitializationStages.QueryCapabilities:
                    // Clear the pins, as we will be receiving new ones
                    Pins.Clear();
                    AnalogPins.Clear();
                    // Send the message to get the capabilities
                    base.SendMessage(new QueryCapabilityMessage());
                    break;
                case InitializationStages.QueryAnalogMappings:
                    base.SendMessage(new AnalogMappingQueryMessage());
                    break;
                case InitializationStages.QueryPinStates:
                    for (int i = 0; i < Pins.Count; i++)
                    {
                        base.SendMessage(new PinStateQueryMessage{Pin = (byte) i});
                    }
                    break;
                case InitializationStages.StartReports:
                    var portsCount = (byte) Math.Ceiling(Pins.Count/8.0);
                    for (byte i = 0; i < portsCount; i++)
                    {
                        base.SendMessage(new ToggleDigitalReportMessage() { Port = i, ShouldBeEnabled = true });
                    }

                    for (byte i = 0; i < AnalogPins.Count; i++)
                    {
                        base.SendMessage(new ToggleAnalogReportMessage() { Pin = i, ShouldBeEnabled = true });
                    }

                    // There is no callback for the above messages so advance anyway                    
                    OnInitialized();
                    break;
                */
                case InitializationStages.FullyInitialized:
                    // Do nothing we are finished with the initialization
                    break;
                default:
                    throw new ArgumentOutOfRangeException("stage");
            }
        }
        #endregion
        #region StopReports
        /// <summary>
        /// Stop receiving reports.
        /// </summary>
        private void StopReports()
        {
            for (byte i = 0; i < MessageConstants.MAX_DIGITAL_PORTS; i++)
                base.SendMessage(new ToggleDigitalReportMessage()
                {
                    Port = i,
                    ShouldBeEnabled = false
                });

            for (byte i = 0; i < AnalogPins.Count; i++)
                base.SendMessage(new ToggleAnalogReportMessage()
                {
                    Pin = i,
                    ShouldBeEnabled = false
                });
        }
        #endregion
        #endregion
    }
}