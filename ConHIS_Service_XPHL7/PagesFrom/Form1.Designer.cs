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
            this.startStopButton = new System.Windows.Forms.Button();
            this.manualCheckButton = new System.Windows.Forms.Button();
            this.testHL7Button = new System.Windows.Forms.Button();
            this.exportButton = new System.Windows.Forms.Button();
            this.settingsButton = new System.Windows.Forms.Button();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.dateLabel = new System.Windows.Forms.Label();
            this.dateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.searchLabel = new System.Windows.Forms.Label();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.searchButton = new System.Windows.Forms.Button();
            this.refreshButton = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.totalPanel = new System.Windows.Forms.Panel();
            this.totalLabel = new System.Windows.Forms.Label();
            this.totalCountLabel = new System.Windows.Forms.Label();
            this.successPanel = new System.Windows.Forms.Panel();
            this.successLabel = new System.Windows.Forms.Label();
            this.successCountLabel = new System.Windows.Forms.Label();
            this.failedPanel = new System.Windows.Forms.Panel();
            this.failedLabel = new System.Windows.Forms.Label();
            this.failedCountLabel = new System.Windows.Forms.Label();
            this.pendingPanel = new System.Windows.Forms.Panel();
            this.pendingLabel = new System.Windows.Forms.Label();
            this.pendingCountLabel = new System.Windows.Forms.Label();
            this.rejectPanel = new System.Windows.Forms.Panel();
            this.rejectLabel = new System.Windows.Forms.Label();
            this.rejectCountLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.totalPanel.SuspendLayout();
            this.successPanel.SuspendLayout();
            this.failedPanel.SuspendLayout();
            this.pendingPanel.SuspendLayout();
            this.rejectPanel.SuspendLayout();
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
            // settingsButton
            this.settingsButton = new System.Windows.Forms.Button();
            this.settingsButton.Location = new System.Drawing.Point(393, 18);
            this.settingsButton.Margin = new System.Windows.Forms.Padding(2);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(90, 32);
            this.settingsButton.TabIndex = 8;
            this.settingsButton.Text = "⚙️ Settings";
            this.settingsButton.UseVisualStyleBackColor = true;
            this.settingsButton.Click += new System.EventHandler(this.SettingsButton_Click);
            //
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Location = new System.Drawing.Point(15, 340);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.ReadOnly = true;
            this.dataGridView.RowHeadersVisible = false;
            this.dataGridView.RowHeadersWidth = 51;
            this.dataGridView.RowTemplate.Height = 24;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView.Size = new System.Drawing.Size(1170, 395);
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
            this.groupBox1.Controls.Add(this.settingsButton);
            this.groupBox1.Location = new System.Drawing.Point(15, 75);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1170, 62);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Controls";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.dateLabel);
            this.groupBox2.Controls.Add(this.dateTimePicker);
            this.groupBox2.Controls.Add(this.searchLabel);
            this.groupBox2.Controls.Add(this.searchTextBox);
            this.groupBox2.Controls.Add(this.searchButton);
            this.groupBox2.Controls.Add(this.refreshButton);
            this.groupBox2.Location = new System.Drawing.Point(15, 145);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1170, 55);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Search Filter";
            // 
            // dateLabel
            // 
            this.dateLabel.AutoSize = true;
            this.dateLabel.Location = new System.Drawing.Point(285, 24);
            this.dateLabel.Name = "dateLabel";
            this.dateLabel.Size = new System.Drawing.Size(33, 13);
            this.dateLabel.TabIndex = 4;
            this.dateLabel.Text = "Date:";
            // 
            // dateTimePicker
            // 
            this.dateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker.Location = new System.Drawing.Point(325, 21);
            this.dateTimePicker.Name = "dateTimePicker";
            this.dateTimePicker.Size = new System.Drawing.Size(120, 20);
            this.dateTimePicker.TabIndex = 5;
            this.dateTimePicker.ValueChanged += new System.EventHandler(this.DateTimePicker_ValueChanged);
            // 
            // searchLabel
            // 
            this.searchLabel.AutoSize = true;
            this.searchLabel.Location = new System.Drawing.Point(12, 24);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(80, 13);
            this.searchLabel.TabIndex = 0;
            this.searchLabel.Text = "Order No / HN:";
            // 
            // searchTextBox
            // 
            this.searchTextBox.Location = new System.Drawing.Point(120, 21);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(150, 20);
            this.searchTextBox.TabIndex = 1;
            this.searchTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchTextBox_KeyDown);
            // 
            // searchButton
            // 
            this.searchButton.Location = new System.Drawing.Point(460, 18);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(75, 25);
            this.searchButton.TabIndex = 2;
            this.searchButton.Text = "Search";
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += new System.EventHandler(this.SearchButton_Click);
            // 
            // refreshButton
            // 
            this.refreshButton.Location = new System.Drawing.Point(540, 18);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(75, 25);
            this.refreshButton.TabIndex = 3;
            this.refreshButton.Text = "Refresh";
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.totalPanel);
            this.groupBox3.Controls.Add(this.successPanel);
            this.groupBox3.Controls.Add(this.failedPanel);
            this.groupBox3.Controls.Add(this.pendingPanel);
            this.groupBox3.Controls.Add(this.rejectPanel);
            this.groupBox3.Location = new System.Drawing.Point(15, 210);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(1170, 90);
            this.groupBox3.TabIndex = 11;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Status Summary";
            // 
            // totalPanel
            // 
            this.totalPanel.BackColor = System.Drawing.Color.White;
            this.totalPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.totalPanel.Controls.Add(this.totalLabel);
            this.totalPanel.Controls.Add(this.totalCountLabel);
            this.totalPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.totalPanel.Location = new System.Drawing.Point(15, 20);
            this.totalPanel.Name = "totalPanel";
            this.totalPanel.Size = new System.Drawing.Size(220, 55);
            this.totalPanel.TabIndex = 0;
            this.totalPanel.Click += new System.EventHandler(this.TotalPanel_Click);
            // 
            // totalLabel
            // 
            this.totalLabel.Font = new System.Drawing.Font("Tahoma", 7.5F);
            this.totalLabel.ForeColor = System.Drawing.Color.Gray;
            this.totalLabel.Location = new System.Drawing.Point(5, 8);
            this.totalLabel.Name = "totalLabel";
            this.totalLabel.Size = new System.Drawing.Size(145, 15);
            this.totalLabel.TabIndex = 0;
            this.totalLabel.Text = "จำนวนรายการทั้งหมด";
            // 
            // totalCountLabel
            // 
            this.totalCountLabel.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Bold);
            this.totalCountLabel.ForeColor = System.Drawing.Color.Black;
            this.totalCountLabel.Location = new System.Drawing.Point(5, 23);
            this.totalCountLabel.Name = "totalCountLabel";
            this.totalCountLabel.Size = new System.Drawing.Size(145, 28);
            this.totalCountLabel.TabIndex = 1;
            this.totalCountLabel.Text = "0";
            this.totalCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // successPanel
            // 
            this.successPanel.BackColor = System.Drawing.Color.White;
            this.successPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.successPanel.Controls.Add(this.successLabel);
            this.successPanel.Controls.Add(this.successCountLabel);
            this.successPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.successPanel.Location = new System.Drawing.Point(245, 20);
            this.successPanel.Name = "successPanel";
            this.successPanel.Size = new System.Drawing.Size(220, 55);
            this.successPanel.TabIndex = 1;
            this.successPanel.Click += new System.EventHandler(this.SuccessPanel_Click);
            // 
            // successLabel
            // 
            this.successLabel.Font = new System.Drawing.Font("Tahoma", 7.5F);
            this.successLabel.ForeColor = System.Drawing.Color.Gray;
            this.successLabel.Location = new System.Drawing.Point(5, 8);
            this.successLabel.Name = "successLabel";
            this.successLabel.Size = new System.Drawing.Size(145, 15);
            this.successLabel.TabIndex = 0;
            this.successLabel.Text = "รายการส่งสมบูรณ์";
            // 
            // successCountLabel
            // 
            this.successCountLabel.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Bold);
            this.successCountLabel.ForeColor = System.Drawing.Color.Green;
            this.successCountLabel.Location = new System.Drawing.Point(5, 23);
            this.successCountLabel.Name = "successCountLabel";
            this.successCountLabel.Size = new System.Drawing.Size(145, 28);
            this.successCountLabel.TabIndex = 1;
            this.successCountLabel.Text = "0";
            this.successCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // failedPanel
            // 
            this.failedPanel.BackColor = System.Drawing.Color.White;
            this.failedPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.failedPanel.Controls.Add(this.failedLabel);
            this.failedPanel.Controls.Add(this.failedCountLabel);
            this.failedPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.failedPanel.Location = new System.Drawing.Point(470, 20);
            this.failedPanel.Name = "failedPanel";
            this.failedPanel.Size = new System.Drawing.Size(220, 55);
            this.failedPanel.TabIndex = 2;
            this.failedPanel.Click += new System.EventHandler(this.FailedPanel_Click);
            // 
            // failedLabel
            // 
            this.failedLabel.Font = new System.Drawing.Font("Tahoma", 7.5F);
            this.failedLabel.ForeColor = System.Drawing.Color.Gray;
            this.failedLabel.Location = new System.Drawing.Point(5, 8);
            this.failedLabel.Name = "failedLabel";
            this.failedLabel.Size = new System.Drawing.Size(145, 15);
            this.failedLabel.TabIndex = 0;
            this.failedLabel.Text = "รายการถูกปฏิเสธ";
            // 
            // failedCountLabel
            // 
            this.failedCountLabel.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Bold);
            this.failedCountLabel.ForeColor = System.Drawing.Color.Red;
            this.failedCountLabel.Location = new System.Drawing.Point(5, 23);
            this.failedCountLabel.Name = "failedCountLabel";
            this.failedCountLabel.Size = new System.Drawing.Size(145, 28);
            this.failedCountLabel.TabIndex = 1;
            this.failedCountLabel.Text = "0";
            this.failedCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pendingPanel
            // 
            this.pendingPanel.BackColor = System.Drawing.Color.White;
            this.pendingPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pendingPanel.Controls.Add(this.pendingLabel);
            this.pendingPanel.Controls.Add(this.pendingCountLabel);
            this.pendingPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pendingPanel.Location = new System.Drawing.Point(695, 20);
            this.pendingPanel.Name = "pendingPanel";
            this.pendingPanel.Size = new System.Drawing.Size(220, 55);
            this.pendingPanel.TabIndex = 3;
            this.pendingPanel.Click += new System.EventHandler(this.PendingPanel_Click);
            // 
            // pendingLabel
            // 
            this.pendingLabel.Font = new System.Drawing.Font("Tahoma", 7.5F);
            this.pendingLabel.ForeColor = System.Drawing.Color.Gray;
            this.pendingLabel.Location = new System.Drawing.Point(5, 8);
            this.pendingLabel.Name = "pendingLabel";
            this.pendingLabel.Size = new System.Drawing.Size(145, 15);
            this.pendingLabel.TabIndex = 0;
            this.pendingLabel.Text = "จำนวนรายการเตือนเน้นยื";
            // 
            // pendingCountLabel
            // 
            this.pendingCountLabel.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Bold);
            this.pendingCountLabel.ForeColor = System.Drawing.Color.Orange;
            this.pendingCountLabel.Location = new System.Drawing.Point(5, 23);
            this.pendingCountLabel.Name = "pendingCountLabel";
            this.pendingCountLabel.Size = new System.Drawing.Size(145, 28);
            this.pendingCountLabel.TabIndex = 1;
            this.pendingCountLabel.Text = "0";
            this.pendingCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // rejectPanel
            // 
            this.rejectPanel.BackColor = System.Drawing.Color.White;
            this.rejectPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rejectPanel.Controls.Add(this.rejectLabel);
            this.rejectPanel.Controls.Add(this.rejectCountLabel);
            this.rejectPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rejectPanel.Location = new System.Drawing.Point(920, 20);
            this.rejectPanel.Name = "rejectPanel";
            this.rejectPanel.Size = new System.Drawing.Size(220, 55);
            this.rejectPanel.TabIndex = 4;
            this.rejectPanel.Click += new System.EventHandler(this.RejectPanel_Click);
            // 
            // rejectLabel
            // 
            this.rejectLabel.Font = new System.Drawing.Font("Tahoma", 7.5F);
            this.rejectLabel.ForeColor = System.Drawing.Color.Gray;
            this.rejectLabel.Location = new System.Drawing.Point(5, 8);
            this.rejectLabel.Name = "rejectLabel";
            this.rejectLabel.Size = new System.Drawing.Size(145, 15);
            this.rejectLabel.TabIndex = 0;
            this.rejectLabel.Text = "จำนวนรายการส่งคืนระบบ";
            // 
            // rejectCountLabel
            // 
            this.rejectCountLabel.Font = new System.Drawing.Font("Tahoma", 18F, System.Drawing.FontStyle.Bold);
            this.rejectCountLabel.ForeColor = System.Drawing.Color.DarkGray;
            this.rejectCountLabel.Location = new System.Drawing.Point(5, 23);
            this.rejectCountLabel.Name = "rejectCountLabel";
            this.rejectCountLabel.Size = new System.Drawing.Size(145, 28);
            this.rejectCountLabel.TabIndex = 1;
            this.rejectCountLabel.Text = "0";
            this.rejectCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 750);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lastCheckLabel);
            this.Controls.Add(this.statusLabel);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ConHIS Service - Drug Dispense Monitor";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.totalPanel.ResumeLayout(false);
            this.successPanel.ResumeLayout(false);
            this.failedPanel.ResumeLayout(false);
            this.pendingPanel.ResumeLayout(false);
            this.rejectPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label lastCheckLabel;
        private System.Windows.Forms.Button startStopButton;
        private System.Windows.Forms.Button manualCheckButton;
        private System.Windows.Forms.Button testHL7Button;
        private System.Windows.Forms.Button exportButton;
        private System.Windows.Forms.Button settingsButton;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox searchTextBox;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.Button refreshButton;
        private System.Windows.Forms.Label searchLabel;
        private System.Windows.Forms.DateTimePicker dateTimePicker;
        private System.Windows.Forms.Label dateLabel;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Panel totalPanel;
        private System.Windows.Forms.Panel successPanel;
        private System.Windows.Forms.Panel failedPanel;
        private System.Windows.Forms.Panel pendingPanel;
        private System.Windows.Forms.Panel rejectPanel;
        // ประกาศ Label เหล่านี้เพียงครั้งเดียวที่นี่
        private System.Windows.Forms.Label totalCountLabel;
        private System.Windows.Forms.Label successCountLabel;
        private System.Windows.Forms.Label failedCountLabel;
        private System.Windows.Forms.Label pendingCountLabel;
        private System.Windows.Forms.Label rejectCountLabel;
        private System.Windows.Forms.Label totalLabel;
        private System.Windows.Forms.Label successLabel;
        private System.Windows.Forms.Label failedLabel;
        private System.Windows.Forms.Label pendingLabel;
        private System.Windows.Forms.Label rejectLabel;
    }
}