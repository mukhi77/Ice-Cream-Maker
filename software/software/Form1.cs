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
        StreamWriter logWriter;
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
                }));
            }
        }

        //private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    int n = sp.BytesToRead;
        //    byte[] temp = new byte[n];
        //    sp.Read(temp, 0, n);
        //    buffer.AddRange(temp);

        //    // parse 3-byte frames: [255][MS5B][LS5B]
        //    while (buffer.Count >= 3)
        //    {
        //        if (buffer[0] != 0xFF)
        //        {
        //            buffer.RemoveAt(0);
        //            continue;
        //        }

        //        byte ms5 = buffer[1];
        //        byte ls5 = buffer[2];
        //        buffer.RemoveRange(0, 3);

        //        int adc10 = ((ms5 & 0x1F) << 5) | (ls5 & 0x1F);
        //        double t = (DateTime.Now - startTime).TotalSeconds;

        //        // --- Compute temperatures ---
        //        double tempCalibrated = CalculateTemperature(adc10, BETA);   // your MATLAB beta
        //        double tempManual = CalculateTemperature(adc10, 3435.0);      // manual beta
        //        double errBeta = tempManual - tempCalibrated;                 // model error (°C)

        //        // --- Apply user compensation to calibrated temp ---
        //        double adj = ApplyCompensation(tempCalibrated);

        //        // Evaluate Motor Speed
        //        EvaluateMotorControl(tempCalibrated);

        //        BeginInvoke(new Action(() =>
        //        {
        //            lblTempNow.Text = $"{adj:F2} °C";


        //            logWriter?.WriteLine($"{t:F3},{adc10},{tempCalibrated:F3},{adj:F3}");
        //        }));
        //    }
        //}


        // ==== Temperature calculation (Beta model) ====
        //private double CalculateTemperature(int adc, double beta)
        //{
        //    if (adc <= 0) adc = 1;
        //    if (adc >= ADC_MAX) adc = ADC_MAX - 1;

        //    double v = (adc / (double)ADC_MAX) * VREF;
        //    double Rth = RS * v / (VREF - v);

        //    double invT = (1.0 / T0) + (1.0 / beta) * Math.Log(Rth / R0);
        //    double TK = 1.0 / invT;
        //    return TK - 273.15;
        //}


        private double ApplyCompensation(double tempC)
        {
            double adj = (tempC);
            return adj;
        }

        private byte lastSpeed = 255;
        //private void EvaluateMotorControl(double tempC)
        //{
        //    byte speed;

        //    if (tempC >= T_hi)
        //    {
        //        speed = 10;
        //    }

        //    else if (tempC <= T_lo)
        //    {
        //        speed = 100;
        //    }

        //    else
        //    {
        //        speed = 50;
        //    }

        //    if (speed != lastSpeed) // only write when changed
        //    {
        //        byte[] data = new byte[] { directionByte, speed };
        //        sp.Write(data, 0, 2);
        //        UpdateSpeedLabel(speed);
        //        lastSpeed = speed;
        //    }
        //}

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (sp.IsOpen) sp.Write(new byte[] { (byte)'S' }, 0, 1);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (sp.IsOpen) sp.Write(new byte[] { (byte)'X' }, 0, 1);
        }

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
    }
}
