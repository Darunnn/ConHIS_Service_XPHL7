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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.statusLabel = new System.Windows.Forms.Label();
            this.lastCheckLabel = new System.Windows.Forms.Label();
            this.lastFoundLabel = new System.Windows.Forms.Label();
            this.lastSuccessLabel = new System.Windows.Forms.Label();
            this.connectionStatusLabel = new System.Windows.Forms.Label();
            this.startStopIPDButton = new System.Windows.Forms.Button();
            this.startStopOPDButton = new System.Windows.Forms.Button();
            this.manualCheckButton = new System.Windows.Forms.Button();
            this.exportButton = new System.Windows.Forms.Button();
            this.settingsButton = new System.Windows.Forms.Button();
            this.dateLabel = new System.Windows.Forms.Label();
            this.dateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.searchLabel = new System.Windows.Forms.Label();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.searchButton = new System.Windows.Forms.Button();
            this.refreshButton = new System.Windows.Forms.Button();
            this.totalPanel = new System.Windows.Forms.Panel();
            this.totalLabel = new System.Windows.Forms.Label();
            this.totalCountLabel = new System.Windows.Forms.Label();
            this.successPanel = new System.Windows.Forms.Panel();
            this.successLabel = new System.Windows.Forms.Label();
            this.successCountLabel = new System.Windows.Forms.Label();
            this.failedPanel = new System.Windows.Forms.Panel();
            this.failedLabel = new System.Windows.Forms.Label();
            this.failedCountLabel = new System.Windows.Forms.Label();
            this.ipdPanel = new System.Windows.Forms.Panel();
            this.ipdLabel = new System.Windows.Forms.Label();
            this.ipdCountLabel = new System.Windows.Forms.Label();
            this.opdPanel = new System.Windows.Forms.Panel();
            this.opdLabel = new System.Windows.Forms.Label();
            this.opdCountLabel = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.totalPanel.SuspendLayout();
            this.successPanel.SuspendLayout();
            this.failedPanel.SuspendLayout();
            this.ipdPanel.SuspendLayout();
            this.opdPanel.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.statusLabel.Location = new System.Drawing.Point(15, 20);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(68, 14);
            this.statusLabel.TabIndex = 0;
            this.statusLabel.Text = "Status: ...";
            // 
            // lastCheckLabel
            // 
            this.lastCheckLabel.AutoSize = true;
            this.lastCheckLabel.Location = new System.Drawing.Point(15, 40);
            this.lastCheckLabel.Name = "lastCheckLabel";
            this.lastCheckLabel.Size = new System.Drawing.Size(70, 13);
            this.lastCheckLabel.TabIndex = 1;
            this.lastCheckLabel.Text = "Last Check: -";
            // 
            // lastFoundLabel
            // 
            this.lastFoundLabel.AutoSize = true;
            this.lastFoundLabel.Location = new System.Drawing.Point(15, 58);
            this.lastFoundLabel.Name = "lastFoundLabel";
            this.lastFoundLabel.Size = new System.Drawing.Size(69, 13);
            this.lastFoundLabel.TabIndex = 2;
            this.lastFoundLabel.Text = "Last Found: -";
            // 
            // lastSuccessLabel
            // 
            this.lastSuccessLabel.AutoSize = true;
            this.lastSuccessLabel.Location = new System.Drawing.Point(400, 40);
            this.lastSuccessLabel.Name = "lastSuccessLabel";
            this.lastSuccessLabel.Size = new System.Drawing.Size(80, 13);
            this.lastSuccessLabel.TabIndex = 3;
            this.lastSuccessLabel.Text = "Last Success: -";
            // 
            // connectionStatusLabel
            // 
            this.connectionStatusLabel.AutoSize = true;
            this.connectionStatusLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.connectionStatusLabel.ForeColor = System.Drawing.Color.Gray;
            this.connectionStatusLabel.Location = new System.Drawing.Point(15, 76);
            this.connectionStatusLabel.Name = "connectionStatusLabel";
            this.connectionStatusLabel.Size = new System.Drawing.Size(139, 13);
            this.connectionStatusLabel.TabIndex = 4;
            this.connectionStatusLabel.Text = "Database: Connecting...";
            // 
            // startStopIPDButton
            // 
            this.startStopIPDButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.startStopIPDButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.startStopIPDButton.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.startStopIPDButton.ForeColor = System.Drawing.Color.White;
            this.startStopIPDButton.Location = new System.Drawing.Point(15, 20);
            this.startStopIPDButton.Name = "startStopIPDButton";
            this.startStopIPDButton.Size = new System.Drawing.Size(140, 32);
            this.startStopIPDButton.TabIndex = 0;
            this.startStopIPDButton.Text = "▶ Start IPD";
            this.startStopIPDButton.UseVisualStyleBackColor = false;
            this.startStopIPDButton.Click += new System.EventHandler(this.StartStopIPDButton_Click);
            // 
            // startStopOPDButton
            // 
            this.startStopOPDButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.startStopOPDButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.startStopOPDButton.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.startStopOPDButton.ForeColor = System.Drawing.Color.White;
            this.startStopOPDButton.Location = new System.Drawing.Point(165, 20);
            this.startStopOPDButton.Name = "startStopOPDButton";
            this.startStopOPDButton.Size = new System.Drawing.Size(140, 32);
            this.startStopOPDButton.TabIndex = 1;
            this.startStopOPDButton.Text = "▶ Start OPD";
            this.startStopOPDButton.UseVisualStyleBackColor = false;
           this.startStopOPDButton.Click += new System.EventHandler(this.StartStopOPDButton_Click);
            // 
            // manualCheckButton
            // 
            this.manualCheckButton.Location = new System.Drawing.Point(315, 20);
            this.manualCheckButton.Name = "manualCheckButton";
            this.manualCheckButton.Size = new System.Drawing.Size(120, 32);
            this.manualCheckButton.TabIndex = 2;
            this.manualCheckButton.Text = "🔄 Manual Check";
            this.manualCheckButton.UseVisualStyleBackColor = true;
            this.manualCheckButton.Click += new System.EventHandler(this.ManualCheckButton_Click);
            // 
            // exportButton
            // 
            this.exportButton.Location = new System.Drawing.Point(0, 0);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(0, 0);
            this.exportButton.TabIndex = 99;
            this.exportButton.Visible = false;
            // 
            // settingsButton
            // 
            this.settingsButton.Location = new System.Drawing.Point(445, 20);
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new System.Drawing.Size(120, 32);
            this.settingsButton.TabIndex = 3;
            this.settingsButton.Text = "⚙️ Settings";
            this.settingsButton.UseVisualStyleBackColor = true;
            this.settingsButton.Click += new System.EventHandler(this.SettingsButton_Click);
            // 
            // dateLabel
            // 
            this.dateLabel.AutoSize = true;
            this.dateLabel.Location = new System.Drawing.Point(270, 27);
            this.dateLabel.Name = "dateLabel";
            this.dateLabel.Size = new System.Drawing.Size(33, 13);
            this.dateLabel.TabIndex = 2;
            this.dateLabel.Text = "Date:";
            // 
            // dateTimePicker
            // 
            this.dateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dateTimePicker.Location = new System.Drawing.Point(310, 24);
            this.dateTimePicker.Name = "dateTimePicker";
            this.dateTimePicker.Size = new System.Drawing.Size(120, 20);
            this.dateTimePicker.TabIndex = 3;
            this.dateTimePicker.ValueChanged += new System.EventHandler(this.DateTimePicker_ValueChanged);
            // 
            // searchLabel
            // 
            this.searchLabel.AutoSize = true;
            this.searchLabel.Location = new System.Drawing.Point(15, 27);
            this.searchLabel.Name = "searchLabel";
            this.searchLabel.Size = new System.Drawing.Size(80, 13);
            this.searchLabel.TabIndex = 0;
            this.searchLabel.Text = "Order No / HN:";
            // 
            // searchTextBox
            // 
            this.searchTextBox.Location = new System.Drawing.Point(100, 24);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(150, 20);
            this.searchTextBox.TabIndex = 1;
            this.searchTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchTextBox_KeyDown);
            // 
            // searchButton
            // 
            this.searchButton.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.searchButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.searchButton.ForeColor = System.Drawing.Color.White;
            this.searchButton.Location = new System.Drawing.Point(450, 21);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(90, 26);
            this.searchButton.TabIndex = 4;
            this.searchButton.Text = "🔍 Search";
            this.searchButton.UseVisualStyleBackColor = false;
            this.searchButton.Click += new System.EventHandler(this.SearchButton_Click);
            // 
            // refreshButton
            // 
            this.refreshButton.Location = new System.Drawing.Point(550, 21);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(90, 26);
            this.refreshButton.TabIndex = 5;
            this.refreshButton.Text = "🔄 Refresh";
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // totalPanel
            // 
            this.totalPanel.BackColor = System.Drawing.Color.White;
            this.totalPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.totalPanel.Controls.Add(this.totalLabel);
            this.totalPanel.Controls.Add(this.totalCountLabel);
            this.totalPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.totalPanel.Location = new System.Drawing.Point(20, 22);
            this.totalPanel.Name = "totalPanel";
            this.totalPanel.Size = new System.Drawing.Size(180, 60);
            this.totalPanel.TabIndex = 0;
            this.totalPanel.Click += new System.EventHandler(this.TotalPanel_Click);
            // 
            // totalLabel
            // 
            this.totalLabel.Font = new System.Drawing.Font("Tahoma", 8F);
            this.totalLabel.ForeColor = System.Drawing.Color.Gray;
            this.totalLabel.Location = new System.Drawing.Point(10, 10);
            this.totalLabel.Name = "totalLabel";
            this.totalLabel.Size = new System.Drawing.Size(160, 16);
            this.totalLabel.TabIndex = 0;
            this.totalLabel.Text = "จำนวนรายการทั้งหมด";
            // 
            // totalCountLabel
            // 
            this.totalCountLabel.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.totalCountLabel.ForeColor = System.Drawing.Color.Black;
            this.totalCountLabel.Location = new System.Drawing.Point(10, 26);
            this.totalCountLabel.Name = "totalCountLabel";
            this.totalCountLabel.Size = new System.Drawing.Size(160, 30);
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
            this.successPanel.Location = new System.Drawing.Point(215, 22);
            this.successPanel.Name = "successPanel";
            this.successPanel.Size = new System.Drawing.Size(180, 60);
            this.successPanel.TabIndex = 1;
            this.successPanel.Click += new System.EventHandler(this.SuccessPanel_Click);
            // 
            // successLabel
            // 
            this.successLabel.Font = new System.Drawing.Font("Tahoma", 8F);
            this.successLabel.ForeColor = System.Drawing.Color.Gray;
            this.successLabel.Location = new System.Drawing.Point(10, 10);
            this.successLabel.Name = "successLabel";
            this.successLabel.Size = new System.Drawing.Size(160, 16);
            this.successLabel.TabIndex = 0;
            this.successLabel.Text = "รายการส่งสำเร็จ";
            // 
            // successCountLabel
            // 
            this.successCountLabel.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.successCountLabel.ForeColor = System.Drawing.Color.Green;
            this.successCountLabel.Location = new System.Drawing.Point(10, 26);
            this.successCountLabel.Name = "successCountLabel";
            this.successCountLabel.Size = new System.Drawing.Size(160, 30);
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
            this.failedPanel.Location = new System.Drawing.Point(410, 22);
            this.failedPanel.Name = "failedPanel";
            this.failedPanel.Size = new System.Drawing.Size(180, 60);
            this.failedPanel.TabIndex = 2;
            this.failedPanel.Click += new System.EventHandler(this.FailedPanel_Click);
            // 
            // failedLabel
            // 
            this.failedLabel.Font = new System.Drawing.Font("Tahoma", 8F);
            this.failedLabel.ForeColor = System.Drawing.Color.Gray;
            this.failedLabel.Location = new System.Drawing.Point(10, 10);
            this.failedLabel.Name = "failedLabel";
            this.failedLabel.Size = new System.Drawing.Size(160, 16);
            this.failedLabel.TabIndex = 0;
            this.failedLabel.Text = "รายการล้มเหลว";
            // 
            // failedCountLabel
            // 
            this.failedCountLabel.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.failedCountLabel.ForeColor = System.Drawing.Color.Red;
            this.failedCountLabel.Location = new System.Drawing.Point(10, 26);
            this.failedCountLabel.Name = "failedCountLabel";
            this.failedCountLabel.Size = new System.Drawing.Size(160, 30);
            this.failedCountLabel.TabIndex = 1;
            this.failedCountLabel.Text = "0";
            this.failedCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ipdPanel
            // 
            this.ipdPanel.BackColor = System.Drawing.Color.White;
            this.ipdPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ipdPanel.Controls.Add(this.ipdLabel);
            this.ipdPanel.Controls.Add(this.ipdCountLabel);
            this.ipdPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ipdPanel.Location = new System.Drawing.Point(605, 22);
            this.ipdPanel.Name = "ipdPanel";
            this.ipdPanel.Size = new System.Drawing.Size(160, 60);
            this.ipdPanel.TabIndex = 3;
            this.ipdPanel.Click += new System.EventHandler(this.IPDPanel_Click);
            // 
            // ipdLabel
            // 
            this.ipdLabel.Font = new System.Drawing.Font("Tahoma", 8F);
            this.ipdLabel.ForeColor = System.Drawing.Color.Gray;
            this.ipdLabel.Location = new System.Drawing.Point(10, 10);
            this.ipdLabel.Name = "ipdLabel";
            this.ipdLabel.Size = new System.Drawing.Size(140, 16);
            this.ipdLabel.TabIndex = 0;
            this.ipdLabel.Text = "รายการ IPD";
            // 
            // ipdCountLabel
            // 
            this.ipdCountLabel.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.ipdCountLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(152)))), ((int)(((byte)(219)))));
            this.ipdCountLabel.Location = new System.Drawing.Point(10, 26);
            this.ipdCountLabel.Name = "ipdCountLabel";
            this.ipdCountLabel.Size = new System.Drawing.Size(140, 30);
            this.ipdCountLabel.TabIndex = 1;
            this.ipdCountLabel.Text = "0";
            this.ipdCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // opdPanel
            // 
            this.opdPanel.BackColor = System.Drawing.Color.White;
            this.opdPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.opdPanel.Controls.Add(this.opdLabel);
            this.opdPanel.Controls.Add(this.opdCountLabel);
            this.opdPanel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.opdPanel.Location = new System.Drawing.Point(780, 22);
            this.opdPanel.Name = "opdPanel";
            this.opdPanel.Size = new System.Drawing.Size(160, 60);
            this.opdPanel.TabIndex = 4;
            this.opdPanel.Click += new System.EventHandler(this.OPDPanel_Click);
            // 
            // opdLabel
            // 
            this.opdLabel.Font = new System.Drawing.Font("Tahoma", 8F);
            this.opdLabel.ForeColor = System.Drawing.Color.Gray;
            this.opdLabel.Location = new System.Drawing.Point(10, 10);
            this.opdLabel.Name = "opdLabel";
            this.opdLabel.Size = new System.Drawing.Size(140, 16);
            this.opdLabel.TabIndex = 0;
            this.opdLabel.Text = "รายการ OPD";
            // 
            // opdCountLabel
            // 
            this.opdCountLabel.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.opdCountLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(46)))), ((int)(((byte)(204)))), ((int)(((byte)(113)))));
            this.opdCountLabel.Location = new System.Drawing.Point(10, 26);
            this.opdCountLabel.Name = "opdCountLabel";
            this.opdCountLabel.Size = new System.Drawing.Size(140, 30);
            this.opdCountLabel.TabIndex = 1;
            this.opdCountLabel.Text = "0";
            this.opdCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.statusLabel);
            this.groupBox1.Controls.Add(this.lastCheckLabel);
            this.groupBox1.Controls.Add(this.lastFoundLabel);
            this.groupBox1.Controls.Add(this.lastSuccessLabel);
            this.groupBox1.Controls.Add(this.connectionStatusLabel);
            this.groupBox1.Location = new System.Drawing.Point(15, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(957, 100);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "📊 Status Information";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.startStopIPDButton);
            this.groupBox2.Controls.Add(this.startStopOPDButton);
            this.groupBox2.Controls.Add(this.manualCheckButton);
            this.groupBox2.Controls.Add(this.settingsButton);
            this.groupBox2.Location = new System.Drawing.Point(15, 118);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(957, 62);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "🎮 Service Controls";
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.searchLabel);
            this.groupBox3.Controls.Add(this.searchTextBox);
            this.groupBox3.Controls.Add(this.dateLabel);
            this.groupBox3.Controls.Add(this.dateTimePicker);
            this.groupBox3.Controls.Add(this.searchButton);
            this.groupBox3.Controls.Add(this.refreshButton);
            this.groupBox3.Location = new System.Drawing.Point(15, 186);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(957, 60);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "🔍 Search & Filter";
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.totalPanel);
            this.groupBox4.Controls.Add(this.successPanel);
            this.groupBox4.Controls.Add(this.failedPanel);
            this.groupBox4.Controls.Add(this.ipdPanel);
            this.groupBox4.Controls.Add(this.opdPanel);
            this.groupBox4.Location = new System.Drawing.Point(15, 252);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(957, 95);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "📈 Status Summary";
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView.BackgroundColor = System.Drawing.Color.White;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Location = new System.Drawing.Point(12, 359);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.ReadOnly = true;
            this.dataGridView.RowHeadersVisible = false;
            this.dataGridView.RowHeadersWidth = 51;
            this.dataGridView.RowTemplate.Height = 24;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView.Size = new System.Drawing.Size(960, 240);
            this.dataGridView.TabIndex = 4;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.ClientSize = new System.Drawing.Size(984, 611);
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(1000, 650);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ConHIS Service - Drug Dispense Monitor";
            
            this.totalPanel.ResumeLayout(false);
            this.successPanel.ResumeLayout(false);
            this.failedPanel.ResumeLayout(false);
            this.ipdPanel.ResumeLayout(false);
            this.opdPanel.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        // Status Zone
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label lastCheckLabel;
        private System.Windows.Forms.Label lastFoundLabel;
        private System.Windows.Forms.Label lastSuccessLabel;
        private System.Windows.Forms.Label connectionStatusLabel;

        // Controls Zone
        private System.Windows.Forms.Button startStopIPDButton;
        private System.Windows.Forms.Button startStopOPDButton;
        private System.Windows.Forms.Button manualCheckButton;
        private System.Windows.Forms.Button exportButton;
        private System.Windows.Forms.Button settingsButton;

        // Search & Filter Zone
        private System.Windows.Forms.Label searchLabel;
        private System.Windows.Forms.TextBox searchTextBox;
        private System.Windows.Forms.Label dateLabel;
        private System.Windows.Forms.DateTimePicker dateTimePicker;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.Button refreshButton;

        // Summary Zone
        private System.Windows.Forms.Panel totalPanel;
        private System.Windows.Forms.Label totalLabel;
        private System.Windows.Forms.Label totalCountLabel;
        private System.Windows.Forms.Panel successPanel;
        private System.Windows.Forms.Label successLabel;
        private System.Windows.Forms.Label successCountLabel;
        private System.Windows.Forms.Panel failedPanel;
        private System.Windows.Forms.Label failedLabel;
        private System.Windows.Forms.Label failedCountLabel;
        private System.Windows.Forms.Panel ipdPanel;
        private System.Windows.Forms.Label ipdLabel;
        private System.Windows.Forms.Label ipdCountLabel;
        private System.Windows.Forms.Panel opdPanel;
        private System.Windows.Forms.Label opdLabel;
        private System.Windows.Forms.Label opdCountLabel;


        // GroupBoxes
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;

        // Data Zone
        private System.Windows.Forms.DataGridView dataGridView;

    }
}