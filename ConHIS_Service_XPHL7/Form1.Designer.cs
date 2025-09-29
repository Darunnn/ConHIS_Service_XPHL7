namespace ConHIS_Service_XPHL7
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.statusLabel = new System.Windows.Forms.Label();
            this.lastCheckLabel = new System.Windows.Forms.Label();
            this.startStopButton = new System.Windows.Forms.Button();
            this.manualCheckButton = new System.Windows.Forms.Button();
            this.testHL7Button = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(65, 22);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(90, 20);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Status: ...";
            // 
            // lastCheckLabel
            // 
            this.lastCheckLabel.AutoSize = true;
            this.lastCheckLabel.Location = new System.Drawing.Point(68, 57);
            this.lastCheckLabel.Name = "lastCheckLabel";
            this.lastCheckLabel.Size = new System.Drawing.Size(120, 20);
            this.lastCheckLabel.TabIndex = 1;
            this.lastCheckLabel.Text = "Last Check: -";
            // 
            // startStopButton
            // 
            this.startStopButton.Location = new System.Drawing.Point(42, 132);
            this.startStopButton.Name = "startStopButton";
            this.startStopButton.Size = new System.Drawing.Size(100, 40);
            this.startStopButton.TabIndex = 2;
            this.startStopButton.Text = "Start Service";
            this.startStopButton.UseVisualStyleBackColor = true;
            this.startStopButton.Click += new System.EventHandler(this.StartStopButton_Click);
            // 
            // manualCheckButton
            // 
            this.manualCheckButton.Location = new System.Drawing.Point(159, 132);
            this.manualCheckButton.Name = "manualCheckButton";
            this.manualCheckButton.Size = new System.Drawing.Size(120, 40);
            this.manualCheckButton.TabIndex = 3;
            this.manualCheckButton.Text = "Manual Check";
            this.manualCheckButton.UseVisualStyleBackColor = true;
            this.manualCheckButton.Click += new System.EventHandler(this.ManualCheckButton_Click);
            // 
            // testHL7Button
            // 
            this.testHL7Button.Location = new System.Drawing.Point(295, 132);
            this.testHL7Button.Name = "testHL7Button";
            this.testHL7Button.Size = new System.Drawing.Size(120, 40);
            this.testHL7Button.TabIndex = 4;
            this.testHL7Button.Text = "Test HL7 File";
            this.testHL7Button.UseVisualStyleBackColor = true;
            this.testHL7Button.Click += new System.EventHandler(this.TestHL7Button_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(650, 300);
            this.Controls.Add(this.testHL7Button);
            this.Controls.Add(this.manualCheckButton);
            this.Controls.Add(this.startStopButton);
            this.Controls.Add(this.lastCheckLabel);
            this.Controls.Add(this.statusLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ConHIS Service - Drug Dispense Monitor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion


        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label lastCheckLabel;
        private System.Windows.Forms.Button startStopButton;
        private System.Windows.Forms.Button manualCheckButton;
        private System.Windows.Forms.Button testHL7Button;
    }
}