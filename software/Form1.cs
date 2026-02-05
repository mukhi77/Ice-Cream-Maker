using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace software
{
    public partial class Form1 : Form
    {
        SerialPort sp = new SerialPort();
        StreamWriter outputFile;
        DateTime startTime;
        List<byte> buffer = new List<byte>();

        // ==== Thermistor constants ====
        const double VREF = 3.6;
        const int ADC_MAX = 1023;
        const double RS = 10000.0;
        const double R0 = 6759.776536312849;
        const double T0 = 309.35;
        const double BETA = 3510.321997999228;
        const double T_hi = 27.0;
        const double T_lo = 23.0;
        byte directionByte = 45;

        public Form1()
        {
            InitializeComponent();
            InitializeGUI();


            // Fill COM list
            cboPorts.Items.AddRange(SerialPort.GetPortNames());
            if (cboPorts.Items.Count > 0) cboPorts.SelectedIndex = 0;

            // Serial event handler
            sp.DataReceived += Sp_DataReceived;
        }

        // ==== CONNECT / DISCONNECT HANDLER ====
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!sp.IsOpen)
            {
                if (string.IsNullOrWhiteSpace(cboPorts.Text))
                {
                    MessageBox.Show("Please select a COM port.");
                    return;
                }

                try
                {
                    sp.PortName = cboPorts.Text.Trim();
                    sp.BaudRate = 9600;
                    sp.DataBits = 8;
                    sp.StopBits = StopBits.One;
                    sp.Parity = Parity.None;
                    sp.Handshake = Handshake.None;
                    sp.ReadTimeout = 500;
                    sp.WriteTimeout = 500;

                    sp.Open();
                    if (sp.IsOpen)
                    {
                        btnConnect.Text = "Disconnect Serial";
                        startTime = DateTime.Now;
                    }
                    else
                    {
                        MessageBox.Show("Port failed to open, sp.IsOpen = false");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not open port:\n" + ex.Message);
                }
            }
            else
            {
                try
                {
                    sp.Close();
                    btnConnect.Text = "Connect Serial";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error closing port:\n" + ex.Message);
                }
            }
        }

        // ==== Serial receive handler ====
        private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int n = sp.BytesToRead;
            byte[] temp = new byte[n];
            sp.Read(temp, 0, n);
            buffer.AddRange(temp);

            while (buffer.Count >= 5)
            {
                if (buffer[0] != 0xFF)
                {
                    buffer.RemoveAt(0);
                    continue;
                }

                byte tLo = buffer[1];
                byte tHi = buffer[2];
                byte speed = buffer[3];
                byte state = buffer[4];
                buffer.RemoveRange(0, 5);

                short tempX100 = (short)(tLo | (tHi << 8));
                double tempC = tempX100 / 100.0;

                BeginInvoke(new Action(() =>
                {
                    lblTempNow.Text = $"{tempC:F2} °C";
                    lblSpeed.Text = $"Speed: {speed}";
                    lblState.Text = $"State: {StateName(state)}";


                    if (checkBoxSave.Checked && outputFile != null)
                    {
                        string timeStamp = DateTime.Now.ToString("yyyy MMM dd HH:mm:ss");
                        outputFile.WriteLine($"{tempC},{speed},{timeStamp}");
                    }
                }));
            }
        }

        private double ApplyCompensation(double tempC)
        {
            double adj = (tempC);
            return adj;
        }

        private byte lastSpeed = 255;

        //private void btnStart_Click(object sender, EventArgs e)
        //{
        //    if (sp.IsOpen) sp.Write(new byte[] { (byte)'S' }, 0, 1);
        //}

        //private void btnStop_Click(object sender, EventArgs e)
        //{
        //    if (sp.IsOpen) sp.Write(new byte[] { (byte)'X' }, 0, 1);
        //}

        private string StateName(byte s)
        {
            // Must match firmware enum ordering (given below)
            switch (s)
            {
                case 0: return "IDLE";
                case 1: return "PRECOOL";
                case 2: return "CHURN_RAMP";
                case 3: return "CHURNING";
                case 4: return "ANTI_JAM";
                case 5: return "FINISH";
                case 6: return "FAULT";
                default: return $"UNKNOWN({s})";
            }
        }

        private void InitializeGUI()
        {
            trkSpeed.Minimum = -170;
            trkSpeed.Maximum = 170;
            trkSpeed.Value = 0;

            lblSpeed.Text = "Speed: 0";
        }

        private void UpdateSpeedLabel(byte speed)
        {
            if (lblSpeed.InvokeRequired)
            {
                lblSpeed.BeginInvoke(new Action(() =>
                {
                    lblSpeed.Text = $"Speed: {speed}";
                }));
            }
            else
            {
                lblSpeed.Text = $"Speed: {speed}";
            }
        }

        private void checkBoxSave_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSave.Checked)
            {
                outputFile = new StreamWriter(textBoxFileName.Text);
            }

            else
            {
                outputFile.Flush();
                outputFile.Close();
                outputFile.Dispose();
            }
        }

        private void buttonFileName_MouseClick(object sender, MouseEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Select a file name/location";
            saveFileDialog.DefaultExt = "csv";
            saveFileDialog.AddExtension = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                textBoxFileName.Text = saveFileDialog.FileName;
            }
        }

        // ======================================
        // SEND CONTINUOUS SPEED COMMAND (2 BYTES)
        // ======================================
        private void SendSpeedCommand(int value)
        {
            if (!sp.IsOpen)
                return;

            byte directionByte;
            byte magnitudeByte;

            if (value == 0)
            {
                directionByte = 43;      // '+'
                magnitudeByte = 0;
            }
            else if (value > 0)
            {
                directionByte = 43;      // '+'
                magnitudeByte = (byte)value;
            }
            else
            {
                directionByte = 45;      // '-'
                magnitudeByte = (byte)(-value);
            }

            byte[] data = new byte[] { directionByte, magnitudeByte };
            sp.Write(data, 0, 2);
        }

        // ======================================
        // TRACKBAR MOVEMENT
        // ======================================
        private void trkSpeed_Scroll(object sender, EventArgs e)
        {
            int value = trkSpeed.Value;
            lblSpeed.Text = "Speed: " + value;
            SendSpeedCommand(value);
        }

        private void button1_MouseClick(object sender, MouseEventArgs e)
        {
            lblSpeed.Text = "Speed: " + 0;
            trkSpeed.Value = 0;
            SendSpeedCommand(0);
        }
    }
}
