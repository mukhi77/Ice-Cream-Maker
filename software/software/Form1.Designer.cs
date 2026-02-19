namespace software
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnStop = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.lblState = new System.Windows.Forms.Label();
            this.lblSpeed = new System.Windows.Forms.Label();
            this.lblTempMix = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.cboPorts = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblElapsed = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblSpeedCmd = new System.Windows.Forms.Label();
            this.chkOpenLoop = new System.Windows.Forms.CheckBox();
            this.trkSpeed = new System.Windows.Forms.TrackBar();
            this.lblTempBrine = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.checkBoxMilkshake = new System.Windows.Forms.CheckBox();
            this.checkBoxIceCream = new System.Windows.Forms.CheckBox();
            this.btnLogging = new System.Windows.Forms.Button();
            this.lblChurnPhase = new System.Windows.Forms.Label();
            this.buttonReduceSpeed = new System.Windows.Forms.Button();
            this.buttonClearReducedSpeed = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkSpeed)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(257, 29);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(256, 40);
            this.btnStop.TabIndex = 30;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(6, 29);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(245, 40);
            this.btnStart.TabIndex = 29;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // lblState
            // 
            this.lblState.AutoSize = true;
            this.lblState.Location = new System.Drawing.Point(364, 42);
            this.lblState.Name = "lblState";
            this.lblState.Size = new System.Drawing.Size(65, 20);
            this.lblState.TabIndex = 28;
            this.lblState.Text = "State: 0";
            // 
            // lblSpeed
            // 
            this.lblSpeed.AutoSize = true;
            this.lblSpeed.Location = new System.Drawing.Point(275, 42);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Size = new System.Drawing.Size(73, 20);
            this.lblSpeed.TabIndex = 27;
            this.lblSpeed.Text = "Speed: 0";
            // 
            // lblTempMix
            // 
            this.lblTempMix.AutoSize = true;
            this.lblTempMix.Location = new System.Drawing.Point(208, 42);
            this.lblTempMix.Name = "lblTempMix";
            this.lblTempMix.Size = new System.Drawing.Size(40, 20);
            this.lblTempMix.TabIndex = 23;
            this.lblTempMix.Text = "36.5";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 42);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(189, 20);
            this.label9.TabIndex = 22;
            this.label9.Text = "Mixture Temperature (°C):";
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(244, 4);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(290, 31);
            this.btnConnect.TabIndex = 26;
            this.btnConnect.Text = "Connect Serial";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // cboPorts
            // 
            this.cboPorts.FormattingEnabled = true;
            this.cboPorts.Location = new System.Drawing.Point(104, 2);
            this.cboPorts.Name = "cboPorts";
            this.cboPorts.Size = new System.Drawing.Size(121, 28);
            this.cboPorts.TabIndex = 25;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(86, 20);
            this.label1.TabIndex = 24;
            this.label1.Text = "Serial Port:";
            // 
            // lblElapsed
            // 
            this.lblElapsed.AutoSize = true;
            this.lblElapsed.Location = new System.Drawing.Point(12, 71);
            this.lblElapsed.Name = "lblElapsed";
            this.lblElapsed.Size = new System.Drawing.Size(122, 20);
            this.lblElapsed.TabIndex = 31;
            this.lblElapsed.Text = "Time Elapsed: 0";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblSpeedCmd);
            this.groupBox1.Controls.Add(this.chkOpenLoop);
            this.groupBox1.Controls.Add(this.trkSpeed);
            this.groupBox1.Location = new System.Drawing.Point(16, 213);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(519, 134);
            this.groupBox1.TabIndex = 32;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Open Loop";
            // 
            // lblSpeedCmd
            // 
            this.lblSpeedCmd.AutoSize = true;
            this.lblSpeedCmd.Location = new System.Drawing.Point(6, 102);
            this.lblSpeedCmd.Name = "lblSpeedCmd";
            this.lblSpeedCmd.Size = new System.Drawing.Size(166, 20);
            this.lblSpeedCmd.TabIndex = 2;
            this.lblSpeedCmd.Text = "Manual Speed Cmd: 0";
            // 
            // chkOpenLoop
            // 
            this.chkOpenLoop.AutoSize = true;
            this.chkOpenLoop.Location = new System.Drawing.Point(6, 25);
            this.chkOpenLoop.Name = "chkOpenLoop";
            this.chkOpenLoop.Size = new System.Drawing.Size(123, 24);
            this.chkOpenLoop.TabIndex = 0;
            this.chkOpenLoop.Text = "Open Loop?";
            this.chkOpenLoop.UseVisualStyleBackColor = true;
            this.chkOpenLoop.CheckedChanged += new System.EventHandler(this.chkOpenLoop_CheckedChanged);
            // 
            // trkSpeed
            // 
            this.trkSpeed.Location = new System.Drawing.Point(6, 53);
            this.trkSpeed.Name = "trkSpeed";
            this.trkSpeed.Size = new System.Drawing.Size(507, 69);
            this.trkSpeed.TabIndex = 36;
            this.trkSpeed.Scroll += new System.EventHandler(this.trkSpeed_Scroll);
            // 
            // lblTempBrine
            // 
            this.lblTempBrine.AutoSize = true;
            this.lblTempBrine.Location = new System.Drawing.Point(351, 71);
            this.lblTempBrine.Name = "lblTempBrine";
            this.lblTempBrine.Size = new System.Drawing.Size(40, 20);
            this.lblTempBrine.TabIndex = 34;
            this.lblTempBrine.Text = "36.5";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(170, 71);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(175, 20);
            this.label3.TabIndex = 33;
            this.label3.Text = "Brine Temperature (°C):";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.checkBoxMilkshake);
            this.groupBox2.Controls.Add(this.checkBoxIceCream);
            this.groupBox2.Controls.Add(this.btnStop);
            this.groupBox2.Controls.Add(this.btnStart);
            this.groupBox2.Location = new System.Drawing.Point(16, 94);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(519, 113);
            this.groupBox2.TabIndex = 35;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Closed Loop";
            // 
            // checkBoxMilkshake
            // 
            this.checkBoxMilkshake.AutoSize = true;
            this.checkBoxMilkshake.Location = new System.Drawing.Point(158, 75);
            this.checkBoxMilkshake.Name = "checkBoxMilkshake";
            this.checkBoxMilkshake.Size = new System.Drawing.Size(105, 24);
            this.checkBoxMilkshake.TabIndex = 37;
            this.checkBoxMilkshake.Text = "Milkshake";
            this.checkBoxMilkshake.UseVisualStyleBackColor = true;
            this.checkBoxMilkshake.CheckedChanged += new System.EventHandler(this.checkBoxMilkshake_CheckedChanged);
            // 
            // checkBoxIceCream
            // 
            this.checkBoxIceCream.AutoSize = true;
            this.checkBoxIceCream.Location = new System.Drawing.Point(10, 75);
            this.checkBoxIceCream.Name = "checkBoxIceCream";
            this.checkBoxIceCream.Size = new System.Drawing.Size(108, 24);
            this.checkBoxIceCream.TabIndex = 36;
            this.checkBoxIceCream.Text = "Ice Cream";
            this.checkBoxIceCream.UseVisualStyleBackColor = true;
            this.checkBoxIceCream.CheckedChanged += new System.EventHandler(this.checkBoxIceCream_CheckedChanged);
            // 
            // btnLogging
            // 
            this.btnLogging.Location = new System.Drawing.Point(12, 353);
            this.btnLogging.Name = "btnLogging";
            this.btnLogging.Size = new System.Drawing.Size(245, 44);
            this.btnLogging.TabIndex = 31;
            this.btnLogging.Text = "Start Logging";
            this.btnLogging.UseVisualStyleBackColor = true;
            this.btnLogging.Click += new System.EventHandler(this.btnLogging_Click);
            // 
            // lblChurnPhase
            // 
            this.lblChurnPhase.AutoSize = true;
            this.lblChurnPhase.Location = new System.Drawing.Point(420, 71);
            this.lblChurnPhase.Name = "lblChurnPhase";
            this.lblChurnPhase.Size = new System.Drawing.Size(0, 20);
            this.lblChurnPhase.TabIndex = 36;
            // 
            // buttonReduceSpeed
            // 
            this.buttonReduceSpeed.Location = new System.Drawing.Point(263, 353);
            this.buttonReduceSpeed.Name = "buttonReduceSpeed";
            this.buttonReduceSpeed.Size = new System.Drawing.Size(173, 44);
            this.buttonReduceSpeed.TabIndex = 38;
            this.buttonReduceSpeed.Text = "Reduce Speed (-5)";
            this.buttonReduceSpeed.UseVisualStyleBackColor = true;
            this.buttonReduceSpeed.Click += new System.EventHandler(this.buttonReduceSpeed_Click);
            // 
            // buttonClearReducedSpeed
            // 
            this.buttonClearReducedSpeed.Location = new System.Drawing.Point(442, 353);
            this.buttonClearReducedSpeed.Name = "buttonClearReducedSpeed";
            this.buttonClearReducedSpeed.Size = new System.Drawing.Size(92, 44);
            this.buttonClearReducedSpeed.TabIndex = 39;
            this.buttonClearReducedSpeed.Text = "Clear";
            this.buttonClearReducedSpeed.UseVisualStyleBackColor = true;
            this.buttonClearReducedSpeed.Click += new System.EventHandler(this.buttonClearReducedSpeed_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(546, 409);
            this.Controls.Add(this.buttonClearReducedSpeed);
            this.Controls.Add(this.buttonReduceSpeed);
            this.Controls.Add(this.lblChurnPhase);
            this.Controls.Add(this.btnLogging);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.lblTempBrine);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblElapsed);
            this.Controls.Add(this.lblState);
            this.Controls.Add(this.lblSpeed);
            this.Controls.Add(this.lblTempMix);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.cboPorts);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trkSpeed)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label lblState;
        private System.Windows.Forms.Label lblSpeed;
        private System.Windows.Forms.Label lblTempMix;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.ComboBox cboPorts;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblElapsed;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkOpenLoop;
        private System.Windows.Forms.Label lblSpeedCmd;
        private System.Windows.Forms.Label lblTempBrine;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnLogging;
        private System.Windows.Forms.TrackBar trkSpeed;
        private System.Windows.Forms.CheckBox checkBoxMilkshake;
        private System.Windows.Forms.CheckBox checkBoxIceCream;
        private System.Windows.Forms.Label lblChurnPhase;
        private System.Windows.Forms.Button buttonReduceSpeed;
        private System.Windows.Forms.Button buttonClearReducedSpeed;
    }
}

