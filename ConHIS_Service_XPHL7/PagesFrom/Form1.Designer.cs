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
            this.statusLabel = new System.Windows.Forms.Label();
            this.lastCheckLabel = new System.Windows.Forms.Label();
            this.recordCountLabel = new System.Windows.Forms.Label();
            this.startStopButton = new System.Windows.Forms.Button();
            this.manualCheckButton = new System.Windows.Forms.Button();
            this.testHL7Button = new System.Windows.Forms.Button();
            this.exportButton = new System.Windows.Forms.Button();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(15, 12);
            this.statusLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(52, 13);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Status: ...";
            // 
            // lastCheckLabel
            // 
            this.lastCheckLabel.AutoSize = true;
            this.lastCheckLabel.Location = new System.Drawing.Point(15, 32);
            this.lastCheckLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lastCheckLabel.Name = "lastCheckLabel";
            this.lastCheckLabel.Size = new System.Drawing.Size(70, 13);
            this.lastCheckLabel.TabIndex = 1;
            this.lastCheckLabel.Text = "Last Check: -";
            // 
            // recordCountLabel
            // 
            this.recordCountLabel.AutoSize = true;
            this.recordCountLabel.Location = new System.Drawing.Point(15, 52);
            this.recordCountLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.recordCountLabel.Name = "recordCountLabel";
            this.recordCountLabel.Size = new System.Drawing.Size(86, 13);
            this.recordCountLabel.TabIndex = 2;
            this.recordCountLabel.Text = "Total Records: 0";
            // 
            // startStopButton
            // 
            this.startStopButton.Location = new System.Drawing.Point(15, 18);
            this.startStopButton.Margin = new System.Windows.Forms.Padding(2);
            this.startStopButton.Name = "startStopButton";
            this.startStopButton.Size = new System.Drawing.Size(90, 32);
            this.startStopButton.TabIndex = 3;
            this.startStopButton.Text = "Start Service";
            this.startStopButton.UseVisualStyleBackColor = true;
            this.startStopButton.Click += new System.EventHandler(this.StartStopButton_Click);
            // 
            // manualCheckButton
            // 
            this.manualCheckButton.Location = new System.Drawing.Point(110, 18);
            this.manualCheckButton.Margin = new System.Windows.Forms.Padding(2);
            this.manualCheckButton.Name = "manualCheckButton";
            this.manualCheckButton.Size = new System.Drawing.Size(90, 32);
            this.manualCheckButton.TabIndex = 4;
            this.manualCheckButton.Text = "Manual Check";
            this.manualCheckButton.UseVisualStyleBackColor = true;
            this.manualCheckButton.Click += new System.EventHandler(this.ManualCheckButton_Click);
            // 
            // testHL7Button
            // 
            this.testHL7Button.Location = new System.Drawing.Point(205, 18);
            this.testHL7Button.Margin = new System.Windows.Forms.Padding(2);
            this.testHL7Button.Name = "testHL7Button";
            this.testHL7Button.Size = new System.Drawing.Size(90, 32);
            this.testHL7Button.TabIndex = 5;
            this.testHL7Button.Text = "Test HL7 File";
            this.testHL7Button.UseVisualStyleBackColor = true;
            this.testHL7Button.Click += new System.EventHandler(this.TestHL7Button_Click);
            // 
            // exportButton
            // 
            this.exportButton.Location = new System.Drawing.Point(299, 18);
            this.exportButton.Margin = new System.Windows.Forms.Padding(2);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(90, 32);
            this.exportButton.TabIndex = 7;
            this.exportButton.Text = "Export to CSV";
            this.exportButton.UseVisualStyleBackColor = true;
            this.exportButton.Click += new System.EventHandler(this.ExportButton_Click);
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Location = new System.Drawing.Point(15, 145);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.ReadOnly = true;
            this.dataGridView.RowHeadersWidth = 51;
            this.dataGridView.RowTemplate.Height = 24;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView.Size = new System.Drawing.Size(855, 365);
            this.dataGridView.TabIndex = 9;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.startStopButton);
            this.groupBox1.Controls.Add(this.exportButton);
            this.groupBox1.Controls.Add(this.manualCheckButton);
            this.groupBox1.Controls.Add(this.testHL7Button);
            this.groupBox1.Location = new System.Drawing.Point(15, 75);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(855, 62);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Controls";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(885, 525);
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.recordCountLabel);
            this.Controls.Add(this.lastCheckLabel);
            this.Controls.Add(this.statusLabel);
            this.MinimumSize = new System.Drawing.Size(700, 400);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ConHIS Service - Drug Dispense Monitor";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label lastCheckLabel;
        private System.Windows.Forms.Label recordCountLabel;
        private System.Windows.Forms.Button startStopButton;
        private System.Windows.Forms.Button manualCheckButton;
        private System.Windows.Forms.Button testHL7Button;
        private System.Windows.Forms.Button exportButton;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.GroupBox groupBox1;
    }
}