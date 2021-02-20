#region Usings
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Diagnostics;
using Sharpduino.SerialProviders;
using Sharpduino;
using Sharpduino.EventArguments;
using Sharpduino.Logging;
using System.Threading;
#endregion

namespace RcControl
{
    public partial class Form1 : Form
    {
        #region Constants
        private const int MAX_INIT_RETRIES = 10;
        #endregion

        #region Enums
        public enum SyncDirection
        {
            Pc2Arduino,
            Arduino2Pc
        }
        #endregion

        #region Variables
        private List<TrackBar> sliders;
        private List<Button> sliderReadBackButtons;
        private System.Windows.Forms.Timer ticker;
        private FirmataRC firmata;
        private bool firmataInitialized;
        private int initRetryCounter;
        #endregion

        #region Ctor / Dtor
        public Form1()
        {
            InitializeComponent();

            this.FormClosing +=
                new FormClosingEventHandler(Form1_FormClosing);

            LogManager.CurrentLogger = new DebugLogger();
        }
        #endregion

        #region Event Handler
        #region Form1_Load
        private void Form1_Load(object sender, EventArgs e)
        {
            InitGui();
            SetConfigMode();
            SetStatusText(String.Empty);

            ticker = new System.Windows.Forms.Timer()
            {
                // Interval lowered to increase rate of reading from targeting file
                Interval = 100
            };
            ticker.Tick +=
                new EventHandler(Ticker_Tick);
            ticker.Start();
        }
        #endregion
        #region Form1_FormClosing
        void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectFirmata();
        }
        #endregion

        #region btnOpen_Click
        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (cmbPort.SelectedItem == null)
                return;

            SetWaitMode();
            SetStatusText("Connecting to Firmata ");

            ConnectFirmata((string)cmbPort.SelectedItem, (int)cmbBaud.SelectedItem);
        }
        #endregion
        #region btnClose_Click
        private void btnClose_Click(object sender, EventArgs e)
        {
            SetConfigMode();
            SetStatusText("Firmata not connected");

            DisconnectFirmata();
        }
        #endregion
        #region btnAbout_Click
        private void btnAbout_Click(object sender, EventArgs e)
        {
            About a = new About();
            a.ShowDialog();
        }
        #endregion

        #region btnLowClick
        private void btnLow_Click(object sender, EventArgs e)
        {
            SetSliders(0);
        }
        #endregion
        #region btnMiddle_Click
        private void btnMiddle_Click(object sender, EventArgs e)
        {
            SetSliders(128);
        }
        #endregion
        #region btnHigh_Click
        private void btnHigh_Click(object sender, EventArgs e)
        {
            SetSliders(255);
        }
        #endregion
        #region btnHwReset_Click
        private void btnHwReset_Click(object sender, EventArgs e)
        {
            if (firmata != null)
                firmata.Reset();
        }
        #endregion
        #region btnReadConfig_Click
        private void btnReadConfig_Click(object sender, EventArgs e)
        {
            if (firmata != null)
                firmata.ReadConfiguration();
        }
        #endregion
        #region btnClearLog_Click
        private void btnClearLog_Click(object sender, EventArgs e)
        {
            if (firmata != null)
                firmata.FlushData();
            txtLog.Text = String.Empty;
        }
        #endregion

        #region btnSliderReadBack_Click
        void btnSliderReadBack_Click(object sender, EventArgs e)
        {
            Button b = sender as Button;
            if (b == null)
                return;

            if (!(b.Tag is byte))
                return;
            byte channel = (byte)b.Tag;

            if (firmata != null)
                firmata.ReadChannelValue(channel);
        }
        #endregion

        #region Slider_Changed
        void Slider_Changed(object sender, EventArgs e)
        {
            TrackBar t = sender as TrackBar;
            if (t == null)
                return;

            if (!(t.Tag is byte))
                return;
            byte channel = (byte)t.Tag;

            // This probably wont work the way I expect it to
             t.Value = 128;
            // We'll see

            Debug.WriteLine("Channel {0}: {1}",
                t.Tag, t.Value);

            if (firmata != null)
                // Comment out following line for targetting output file test
                firmata.SetChannelValue(channel, (byte)t.Value);
                return;
                

        }
        #endregion
        #region Ticker_Tick
        void Ticker_Tick(object sender, EventArgs e)
        {
            statusDateTime.Text = DateTime.Now.ToString("G");

            // String array to put read values into
            string[] lines;

            if (firmata != null)
            {
                // Read all the lines in the targeting output file
                try {
                    lines = System.IO.File.ReadAllLines(@"C:/Users/rtseg/Desktop/Turret/Out01.txt");
                    
                    // Set the channels that control vertical and horizontal movement to the values in lines
                    firmata.SetChannelValue(2, (byte)int.Parse(lines[1]));
                    firmata.SetChannelValue(3, (byte)int.Parse(lines[0]));
                } catch {
                    Console.WriteLine("File being accessed by something else");
                }

                // Set all other channels to midpoint
                firmata.SetChannelValue(0, (byte)128);
                firmata.SetChannelValue(1, (byte)128);
                firmata.SetChannelValue(4, (byte)128);
                firmata.SetChannelValue(5, (byte)128);

                if (!firmataInitialized)
                    if (firmata.IsInitialized)
                    {
                        SetProcessMode();
                        SetStatusText("Connected to {0} on port {1}",
                            firmata.Firmware, cmbPort.SelectedItem);

                        firmataInitialized = true;
                        SyncChannelsWithSliders();

                        firmata.SetConfigurationResponse += 
                            new EventHandler<Messages.Receive.SetConfigurationMessageResponse>(Firmata_SetConfigurationResponse);

                        firmata.ReadConfigurationResponse += 
                            new EventHandler<Messages.Receive.ReadConfigurationMessageResponse>(Firmata_ReadConfigurationResponse);

                        firmata.SetChannelValueResponse += 
                            new EventHandler<Messages.Receive.SetChannelValueMessageResponse>(Firmata_SetChannelValueResponse);

                        firmata.ReadChannelValueResponse +=
                            new EventHandler<Messages.Receive.ReadChannelValueMessageResponse>(Firmata_ReadChannelValueResponse);

                        firmata.ReadAllChannelValuesResponse += 
                            new EventHandler<Messages.Receive.ReadAllChannelValuesMessageResponse>(Firmata_ReadAllChannelValuesResponse);

                        firmata.SetPTTResponse += 
                            new EventHandler<Messages.Receive.SetPTTMessageResponse>(Firmata_SetPTTResponse);

                        firmata.ResetResponse +=
                            new EventHandler<Messages.Receive.ResetMessageResponse>(Firmata_ResetResponse);
                    }
                    else
                    {
                        initRetryCounter--;
                        SetStatusText(statusMessage.Text + " .");

                        if (initRetryCounter < 0)
                        {
                            SetConfigMode();
                            SetStatusText("Unable to connect to firmata");

                            DisconnectFirmata();
                        }
                    }
            }
        }
        #endregion

        #region chkPTT_CheckedChanged
        private void chkPTT_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox c = sender as CheckBox;
            if (c == null)
                return;

            if (firmata == null || !firmata.IsInitialized)
                return;

            firmata.SetPTT(c.Checked);
            c.Text = String.Format("PTT {0}", (c.Checked ? "*" : String.Empty));
        }
        #endregion

        #region Firmata_DataReceived
        private delegate void SetLogThreadSafeDelegate(object Sender, EventArgs e);

        void Firmata_DataReceived(object sender, EventArgs e)
        {
            if (firmata == null)
                return;

            //txtLog.SetPropertyThreadSafe(() => txtLog.Text, Tools.HexDump(firmata.Data, 8));

            if (txtLog.InvokeRequired)
                txtLog.Invoke(
                    new SetLogThreadSafeDelegate(Firmata_DataReceived),
                    new object[] { sender, e });
            else
            {
                txtLog.Text = Tools.HexDump(firmata.Data, 8);
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();

                //txtLog.AppendText(Tools.HexDump(firmata.Data, 8));
            }
        }
        #endregion

        #region Firmata_SetConfigurationResponse
        void Firmata_SetConfigurationResponse(object sender, Messages.Receive.SetConfigurationMessageResponse e)
        {
        }
        #endregion
        #region Firmata_ReadConfigurationResponse
        void Firmata_ReadConfigurationResponse(object sender, Messages.Receive.ReadConfigurationMessageResponse e)
        {
        }
        #endregion

        #region Firmata_ReadChannelValueResponse
        void Firmata_ReadChannelValueResponse(object sender, Messages.Receive.ReadChannelValueMessageResponse e)
        {
            TrackBar t = sliders[e.Channel];
            Tools.SetControlPropertyThreadSafe(t, "Value", (e.Value < t.Minimum ? t.Minimum : (e.Value > t.Maximum ? t.Maximum : e.Value)));
        }
        #endregion
        #region Firmata_SetChannelValueResponse
        void Firmata_SetChannelValueResponse(object sender, Messages.Receive.SetChannelValueMessageResponse e)
        {
        }
        #endregion
        #region Firmata_ReadAllChannelValuesResponse
        void Firmata_ReadAllChannelValuesResponse(object sender, Messages.Receive.ReadAllChannelValuesMessageResponse e)
        {
        }
        #endregion

        #region Firmata_SetPTTResponse
        void Firmata_SetPTTResponse(object sender, Messages.Receive.SetPTTMessageResponse e)
        {
        }
        #endregion
        #region Firmata_ResetResponse
        void Firmata_ResetResponse(object sender, Messages.Receive.ResetMessageResponse e)
        {
            SyncChannelsWithSliders(SyncDirection.Arduino2Pc);
        }
        #endregion
        #endregion

        #region Private Methods
        #region InitGui
        private void InitGui(byte Channels = 7)
        {
            // init port names combobox
            cmbPort.DataSource = SerialPort.GetPortNames();

            // init baud rates
            cmbBaud.DataSource = new int[] { 300, 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
            // default is 57600 baud
            cmbBaud.SelectedIndex = 7;

            // init channel sliders
            sliders = new List<TrackBar>();
            sliderReadBackButtons = new List<Button>();
            for (byte channel = 0; channel < Channels; channel++)
            {
                Label l = new Label()
                {
                    Text = String.Format("Channel {0}", channel + 1),
                    Margin = new Padding(3, 7, 3, 0)
                };

                TrackBar t = new TrackBar()
                {
                    Minimum = 0,
                    Maximum = 255,
                    SmallChange = 1,
                    LargeChange = 15,
                    Value = 0,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    Dock = DockStyle.Top,
                    Tag = (byte)channel
                };
                t.ValueChanged +=
                    new EventHandler(Slider_Changed);
                sliders.Add(t);

                Button b = new Button()
                {
                    Text = "Read",
                    Tag = (byte)channel
                };
                b.Click +=
                    new EventHandler(btnSliderReadBack_Click);
                sliderReadBackButtons.Add(b);

                AddRow(l, t, b);
            }
        }
        #endregion

        #region SetWaitMode
        private void SetWaitMode()
        {
            cmbPort.Enabled = false;
            cmbBaud.Enabled = false;
            btnOpen.Enabled = false;
            btnClose.Enabled = false;
            btnLow.Enabled = false;
            btnMiddle.Enabled = false;
            btnHigh.Enabled = false;
            btnHwReset.Enabled = false;
            btnReadConfig.Enabled = false;
            chkPTT.Enabled = false;

            EnableSliders(false);
        }
        #endregion
        #region SetConfigMode
        private void SetConfigMode()
        {
            cmbPort.Enabled = true;
            cmbBaud.Enabled = true;
            btnOpen.Enabled = true;
            btnClose.Enabled = false;
            btnLow.Enabled = false;
            btnMiddle.Enabled = false;
            btnHigh.Enabled = false;
            btnHwReset.Enabled = false;
            btnReadConfig.Enabled = false;
            chkPTT.Enabled = false;

            EnableSliders(false);
        }
        #endregion
        #region SetProcessMode
        private void SetProcessMode()
        {
            cmbPort.Enabled = false;
            cmbBaud.Enabled = false;
            btnOpen.Enabled = false;
            btnClose.Enabled = true;
            btnLow.Enabled = true;
            btnMiddle.Enabled = true;
            btnHigh.Enabled = true;
            btnHwReset.Enabled = true;
            btnReadConfig.Enabled = true;
            chkPTT.Enabled = true;

            EnableSliders(true);
        }
        #endregion

        #region SetSliders
        private void SetSliders(int Value = 0)
        {
            foreach (TrackBar t in sliders)
                t.Value = (Value < t.Minimum ? t.Minimum : (Value > t.Maximum ? t.Maximum : Value));
        }
        #endregion
        #region EnableSliders
        private void EnableSliders(bool Enabled)
        {
            foreach (TrackBar t in sliders)
                t.Enabled = Enabled;

            foreach (Button b in sliderReadBackButtons)
                b.Enabled = Enabled;
        }
        #endregion
        #region SyncChannelsWithSliders
        private void SyncChannelsWithSliders(SyncDirection SyncDirection = SyncDirection.Pc2Arduino)
        {
            if (firmata == null || !firmata.IsInitialized)
                return;

            if (SyncDirection == Form1.SyncDirection.Pc2Arduino)
            {
                foreach (TrackBar t in sliders)
                    if (t.Tag is byte)
                            // removed for target testing

                            firmata.SetChannelValue((byte)t.Tag, (byte)t.Value);

                            return;
            }
            else
            {
                foreach (TrackBar t in sliders)
                    if (t.Tag is byte)
                        firmata.ReadChannelValue((byte)t.Tag);
            }
        }
        #endregion

        #region AddRow
        private void AddRow(Control label, Control value, Button readBack)
        {
            int rowIndex = AddTableRow();

            if (label != null)
                tableLayoutSliders.Controls.Add(label, 0, rowIndex);

            if (value != null)
                tableLayoutSliders.Controls.Add(value, 1, rowIndex);

            if (readBack != null)
                tableLayoutSliders.Controls.Add(readBack, 2, rowIndex);
        }
        #endregion
        #region AddTableRow
        private int AddTableRow()
        {
            int index = tableLayoutSliders.RowCount++;
            RowStyle style = new RowStyle(SizeType.AutoSize);
            tableLayoutSliders.RowStyles.Add(style);
            return index;
        }
        #endregion

        #region SetStatusText
        private void SetStatusText(string Message, params object[] Params)
        {
            statusMessage.Text = String.Format(Message, Params);
        }
        #endregion

        #region ConnectFirmata
        private void ConnectFirmata(string Port, int Baud = 57600)
        {
            if (firmata != null)
                DisconnectFirmata();

            firmataInitialized = false;
            initRetryCounter = MAX_INIT_RETRIES;
            firmata = new FirmataRC(new ComPortProvider(Port, Baud));
            firmata.DataReceived +=
                new Base.FirmataRCBase.DataReceivedEventHandler(Firmata_DataReceived);
        }
        #endregion
        #region DisconnectFirmata
        private void DisconnectFirmata()
        {
            if (firmata != null)
            {
                firmata.Dispose();
                firmata = null;
            }

            firmataInitialized = false;
            txtLog.Text = String.Empty;
        }
        #endregion
        #endregion
    }
}
