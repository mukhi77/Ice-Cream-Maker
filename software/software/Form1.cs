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

        // Logger
        private bool loggingEnabled = false;

        private int lastLoggedMinute = -1;
        private byte lastLoggedState = 255;

        private readonly object logLock = new object();


        public Form1()
        {
            InitializeComponent();


            // Fill COM list
            cboPorts.Items.AddRange(SerialPort.GetPortNames());
            if (cboPorts.Items.Count > 0) cboPorts.SelectedIndex = 0;

            // Serial event handler
            sp.DataReceived += Sp_DataReceived;

            trkSpeed.Minimum = 0;
            trkSpeed.Maximum = 170;

            ctrlTimer = new System.Windows.Forms.Timer();
            ctrlTimer.Interval = 200; // 5 Hz retries
            ctrlTimer.Tick += CtrlTimer_Tick;
            ctrlTimer.Start();

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

            // Frame: [0xFF][mixLo][mixHi][brineLo][brineHi][speed][state]
            while (buffer.Count >= 7)
            {
                int start = buffer.IndexOf(0xFF);
                if (start < 0) { buffer.Clear(); return; }
                if (start > 0) buffer.RemoveRange(0, start);
                if (buffer.Count < 7) return;

                byte mixLo = buffer[1];
                byte mixHi = buffer[2];
                byte brLo = buffer[3];
                byte brHi = buffer[4];
                byte speed = buffer[5];
                byte state = buffer[6];
                buffer.RemoveRange(0, 7);

                short mixX100 = (short)(mixLo | (mixHi << 8));
                short brX100 = (short)(brLo | (brHi << 8));

                double Tmix = mixX100 / 100.0;
                double Tbr = brX100 / 100.0;

                // ACK frame: [0xAC][cmdId][value]
                while (buffer.Count >= 3 && buffer[0] == 0xAC)
                {
                    byte cmdId = buffer[1];
                    byte val = buffer[2];
                    buffer.RemoveRange(0, 3);

                    HandleAck(cmdId, val);
                }


                BeginInvoke(new Action(() =>
                {
                    lblTempMix.Text = $"{Tmix:F2} °C";
                    lblTempBrine.Text = $"{Tbr:F2} °C";
                    lblSpeed.Text = $"Speed: {speed}";
                    lblState.Text = $"State: {GetUiStateName(state)}";
                }));

                double elapsedSeconds = loggingEnabled ? logSw.Elapsed.TotalSeconds : cycleSw.Elapsed.TotalSeconds;
                string mode = chkOpenLoop.Checked ? "OPEN" : "CLOSED";
                MaybeLog(elapsedSeconds, state, speed, Tmix, Tbr, mode);

            }
        }

        private readonly System.Diagnostics.Stopwatch cycleSw = new System.Diagnostics.Stopwatch();
        private System.Windows.Forms.Timer uiTimer;

        private volatile bool ackModeOpen = false;
        private volatile bool ackModeClosed = false;
        private volatile byte lastAckOpenSpeed = 255;

        private void HandleAck(byte cmdId, byte value)
        {
            // 3=Mode, value: 0 closed, 1 open
            if (cmdId == 3)
            {
                ackModeOpen = (value == 1);
                ackModeClosed = (value == 0);
                return;
            }

            // 4=OpenLoopSpeed
            if (cmdId == 4)
            {
                lastAckOpenSpeed = value;
                return;
            }
        }

        private byte openLoopSetpoint = 0;
        private byte lastFwSpeed = 0;      // from status packet
        private byte lastFwState = 0;
        private bool fwInOpenMode = false; // inferred from ACK or from checkbox+logic
        private System.Windows.Forms.Timer ctrlTimer;
        private readonly System.Diagnostics.Stopwatch logSw = new System.Diagnostics.Stopwatch();


        private void CtrlTimer_Tick(object sender, EventArgs e)
        {
            if (!sp.IsOpen) return;
            if (!chkOpenLoop.Checked) return;

            // We want firmware in open-loop mode
            // Send 'O' once until ACK says open
            if (!ackModeOpen)
            {
                sp.Write(new byte[] { (byte)'O' }, 0, 1);
            }

            // Now keep sending speed until firmware applied speed matches (within tolerance)
            const int tol = 2; // speed units tolerance
            int err = openLoopSetpoint - lastFwSpeed;

            if (Math.Abs(err) > tol)
            {
                // Send frame: 0xFE + speed setpoint
                byte[] frame = new byte[] { 0xFE, openLoopSetpoint };
                sp.Write(frame, 0, 2);

                
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
            if (sp.IsOpen)
            {
                sp.Write(new byte[] { (byte)'S' }, 0, 1);

                cycleSw.Reset();
                cycleSw.Start();

                if (uiTimer == null)
                {
                    uiTimer = new System.Windows.Forms.Timer();
                    uiTimer.Interval = 250;
                    uiTimer.Tick += (s, e2) =>
                    {
                        var t = loggingEnabled ? logSw.Elapsed : cycleSw.Elapsed;
                        lblElapsed.Text = $"Time Elapsed: {(int)t.TotalMinutes:00}:{t.Seconds:00}";
                    };
                }
                uiTimer.Start();
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (sp.IsOpen)
            {
                sp.Write(new byte[] { (byte)'X' }, 0, 1);
                cycleSw.Stop();
                uiTimer?.Stop();
                lblElapsed.Text = "Time Elapsed: 0";
                trkSpeed.Value = 0;
                openLoopSetpoint = 0;
                lblSpeedCmd.Text = $"Manual Setpoint: {openLoopSetpoint}";
            }
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

        private void chkOpenLoop_CheckedChanged(object sender, EventArgs e)
        {
            if (!sp.IsOpen) return;

            if (chkOpenLoop.Checked)
            {
                openLoopSetpoint = (byte)trkSpeed.Value;   
                ackModeOpen = false;  // force handshake
                sp.Write(new byte[] { (byte)'O' }, 0, 1);
                lblState.Text = "State: Open Loop";
            }
            else
            {
                ackModeClosed = false;
                sp.Write(new byte[] { (byte)'C' }, 0, 1);
            }
        }



        private void trkSpeed_Scroll(object sender, EventArgs e)
        {
            openLoopSetpoint = (byte)trkSpeed.Value;
            lblSpeedCmd.Text = $"Manual Setpoint: {openLoopSetpoint}";

            if (!sp.IsOpen) return;
            if (!chkOpenLoop.Checked) return;

            // Ensure firmware is in open-loop mode
            sp.Write(new byte[] { (byte)'O' }, 0, 1);

            // Send open-loop speed frame immediately
            byte[] frame = new byte[] { 0xFE, openLoopSetpoint };
            sp.Write(frame, 0, 2);
        }


        private void StartLogging()
        {
            if (loggingEnabled) return;

            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "MECH423_Logs"
            );
            Directory.CreateDirectory(folder);

            string file = Path.Combine(folder, $"run_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            lock (logLock)
            {
                logWriter = new StreamWriter(file, append: false);
                logWriter.AutoFlush = true;
                logWriter.WriteLine("timestamp_s,elapsed_mmss,state,speed,T_mix_C,T_brine_C,dT_C,mode,reason");
            }

            loggingEnabled = true;
            lastLoggedMinute = -1;
            lastLoggedState = 255;

            logSw.Reset();
            logSw.Start();

            if (uiTimer == null)
            {
                uiTimer = new System.Windows.Forms.Timer();
                uiTimer.Interval = 250;
                uiTimer.Tick += (s, e2) =>
                {
                    var t = loggingEnabled ? logSw.Elapsed : cycleSw.Elapsed;
                    lblElapsed.Text = $"Time Elapsed: {(int)t.TotalMinutes:00}:{t.Seconds:00}";
                };
            }
            uiTimer.Start();


        }

        private void StopLogging()
        {
            loggingEnabled = false;
            logSw.Stop();
            lock (logLock)
            {
                logWriter?.Flush();
                logWriter?.Dispose();
                logWriter = null;
            }
        }

        private void MaybeLog(double elapsedSeconds, byte state, byte speed, double Tmix, double Tbrine, string mode)
        {
            if (!loggingEnabled || logWriter == null) return;

            int minute = (int)(elapsedSeconds / 60.0);

            bool minuteChanged = minute != lastLoggedMinute;
            bool stateChanged = state != lastLoggedState;

            if (!minuteChanged && !stateChanged) return;

            string reason = stateChanged ? "state_change" : "minute";
            double dT = Tmix - Tbrine;

            string elapsedMMSS = $"{minute:00}:{(int)(elapsedSeconds % 60):00}";
            string line = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0:F1},{1},{2},{3},{4:F2},{5:F2},{6:F2},{7},{8}",
                elapsedSeconds, elapsedMMSS, StateName(state), speed, Tmix, Tbrine, dT, mode, reason
            );

            lock (logLock)
            {
                logWriter.WriteLine(line);
            }

            if (minuteChanged) lastLoggedMinute = minute;
            if (stateChanged) lastLoggedState = state;
        }

        private void btnLogging_Click(object sender, EventArgs e)
        {
            if (!loggingEnabled)
            {
                StartLogging();
                btnLogging.Text = "Stop Logging";
            }
            else
            {
                StopLogging();
                btnLogging.Text = "Start Logging";
            }
        }

        private string GetUiStateName(byte fwState)
        {
            if (chkOpenLoop.Checked) return "Open Loop";

            // else decode firmware state
            switch (fwState)
            {
                case 0: return "IDLE";
                case 1: return "CHURN";
                case 2: return "SETTLE";
                case 3: return "FINISH";
                case 4: return "FAULT";
                default: return "UNKNOWN";
            }
        }

    }
}
