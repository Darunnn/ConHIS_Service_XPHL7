using System;
using System.Windows.Forms;

namespace ConHIS_Service_XPHL7
{
    partial class HL7DetailForm
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabMSH = new System.Windows.Forms.TabPage();
            this.tabPID = new System.Windows.Forms.TabPage();
            this.tabPV1 = new System.Windows.Forms.TabPage();
            this.tabORC = new System.Windows.Forms.TabPage();
            this.tabAL1 = new System.Windows.Forms.TabPage();
            this.tabRXD = new System.Windows.Forms.TabPage();
            this.tabRXR = new System.Windows.Forms.TabPage();
            this.tabNTE = new System.Windows.Forms.TabPage();
            this.tabLogs = new System.Windows.Forms.TabPage();
            this.btnViewLogs = new System.Windows.Forms.Button();
            this.panelTop = new System.Windows.Forms.Panel();
            this.lblOrderNo = new System.Windows.Forms.Label();
            this.logTextBox = new System.Windows.Forms.TextBox();
            this.btnRefreshLogs = new System.Windows.Forms.Button();
            this.btnExportLogs = new System.Windows.Forms.Button();
            
            this.tabControl.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.tabLogs.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.lblOrderNo);
            this.panelTop.Controls.Add(this.btnViewLogs);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1000, 50);
            this.panelTop.TabIndex = 1;
            // 
            // lblOrderNo
            // 
            this.lblOrderNo.AutoSize = true;
            this.lblOrderNo.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.lblOrderNo.Location = new System.Drawing.Point(12, 15);
            this.lblOrderNo.Name = "lblOrderNo";
            this.lblOrderNo.Size = new System.Drawing.Size(100, 17);
            this.lblOrderNo.TabIndex = 0;
            this.lblOrderNo.Text = "Order No: ";
            // 
            // btnViewLogs
            // 
            this.btnViewLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnViewLogs.BackColor = System.Drawing.Color.LightSkyBlue;
            this.btnViewLogs.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.btnViewLogs.Location = new System.Drawing.Point(850, 10);
            this.btnViewLogs.Name = "btnViewLogs";
            this.btnViewLogs.Size = new System.Drawing.Size(130, 30);
            this.btnViewLogs.TabIndex = 1;
            this.btnViewLogs.Text = "View Logs";
            this.btnViewLogs.UseVisualStyleBackColor = false;
            this.btnViewLogs.Click += new System.EventHandler(this.BtnViewLogs_Click);
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.tabMSH);
            this.tabControl.Controls.Add(this.tabPID);
            this.tabControl.Controls.Add(this.tabPV1);
            this.tabControl.Controls.Add(this.tabORC);
            this.tabControl.Controls.Add(this.tabAL1);
            this.tabControl.Controls.Add(this.tabRXD);
            this.tabControl.Controls.Add(this.tabRXR);
            this.tabControl.Controls.Add(this.tabNTE);
            this.tabControl.Controls.Add(this.tabLogs);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 50);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1000, 650);
            this.tabControl.TabIndex = 0;
            // 
            // tabMSH
            // 
            this.tabMSH.Location = new System.Drawing.Point(4, 22);
            this.tabMSH.Name = "tabMSH";
            this.tabMSH.Padding = new System.Windows.Forms.Padding(3);
            this.tabMSH.Size = new System.Drawing.Size(992, 624);
            this.tabMSH.TabIndex = 0;
            this.tabMSH.Text = "MSH - Message Header";
            this.tabMSH.UseVisualStyleBackColor = true;
            // 
            // tabPID
            // 
            this.tabPID.Location = new System.Drawing.Point(4, 22);
            this.tabPID.Name = "tabPID";
            this.tabPID.Padding = new System.Windows.Forms.Padding(3);
            this.tabPID.Size = new System.Drawing.Size(992, 624);
            this.tabPID.TabIndex = 1;
            this.tabPID.Text = "PID - Patient ID";
            this.tabPID.UseVisualStyleBackColor = true;
            // 
            // tabPV1
            // 
            this.tabPV1.Location = new System.Drawing.Point(4, 22);
            this.tabPV1.Name = "tabPV1";
            this.tabPV1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPV1.Size = new System.Drawing.Size(992, 624);
            this.tabPV1.TabIndex = 2;
            this.tabPV1.Text = "PV1 - Patient Visit";
            this.tabPV1.UseVisualStyleBackColor = true;
            // 
            // tabORC
            // 
            this.tabORC.Location = new System.Drawing.Point(4, 22);
            this.tabORC.Name = "tabORC";
            this.tabORC.Padding = new System.Windows.Forms.Padding(3);
            this.tabORC.Size = new System.Drawing.Size(992, 624);
            this.tabORC.TabIndex = 3;
            this.tabORC.Text = "ORC - Common Order";
            this.tabORC.UseVisualStyleBackColor = true;
            // 
            // tabAL1
            // 
            this.tabAL1.Location = new System.Drawing.Point(4, 22);
            this.tabAL1.Name = "tabAL1";
            this.tabAL1.Padding = new System.Windows.Forms.Padding(3);
            this.tabAL1.Size = new System.Drawing.Size(992, 624);
            this.tabAL1.TabIndex = 4;
            this.tabAL1.Text = "AL1 - Allergies";
            this.tabAL1.UseVisualStyleBackColor = true;
            // 
            // tabRXD
            // 
            this.tabRXD.Location = new System.Drawing.Point(4, 22);
            this.tabRXD.Name = "tabRXD";
            this.tabRXD.Padding = new System.Windows.Forms.Padding(3);
            this.tabRXD.Size = new System.Drawing.Size(992, 624);
            this.tabRXD.TabIndex = 5;
            this.tabRXD.Text = "RXD - Pharmacy Dispense";
            this.tabRXD.UseVisualStyleBackColor = true;
            // 
            // tabRXR
            // 
            this.tabRXR.Location = new System.Drawing.Point(4, 22);
            this.tabRXR.Name = "tabRXR";
            this.tabRXR.Padding = new System.Windows.Forms.Padding(3);
            this.tabRXR.Size = new System.Drawing.Size(992, 624);
            this.tabRXR.TabIndex = 6;
            this.tabRXR.Text = "RXR - Route Info";
            this.tabRXR.UseVisualStyleBackColor = true;
            // 
            // tabNTE
            // 
            this.tabNTE.Location = new System.Drawing.Point(4, 22);
            this.tabNTE.Name = "tabNTE";
            this.tabNTE.Padding = new System.Windows.Forms.Padding(3);
            this.tabNTE.Size = new System.Drawing.Size(992, 624);
            this.tabNTE.TabIndex = 7;
            this.tabNTE.Text = "NTE - Notes";
            this.tabNTE.UseVisualStyleBackColor = true;
            // 
            // tabLogs
            // 
            this.tabLogs.Controls.Add(this.logTextBox);
            this.tabLogs.Controls.Add(this.btnRefreshLogs);
            this.tabLogs.Controls.Add(this.btnExportLogs);
            
            this.tabLogs.Location = new System.Drawing.Point(4, 22);
            this.tabLogs.Name = "tabLogs";
            this.tabLogs.Padding = new System.Windows.Forms.Padding(3);
            this.tabLogs.Size = new System.Drawing.Size(992, 624);
            this.tabLogs.TabIndex = 8;
            this.tabLogs.Text = "Logs";
            this.tabLogs.UseVisualStyleBackColor = true;
            // 
            // logTextBox
            // 
            this.logTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.logTextBox.BackColor = System.Drawing.Color.Black;
            this.logTextBox.Font = new System.Drawing.Font("Consolas", 9F);
            this.logTextBox.ForeColor = System.Drawing.Color.Lime;
            this.logTextBox.Location = new System.Drawing.Point(6, 45);
            this.logTextBox.Multiline = true;
            this.logTextBox.Name = "logTextBox";
            this.logTextBox.ReadOnly = true;
            this.logTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.logTextBox.Size = new System.Drawing.Size(980, 573);
            this.logTextBox.TabIndex = 0;
            this.logTextBox.WordWrap = false;
            // 
            // btnRefreshLogs
            // 
            this.btnRefreshLogs.Location = new System.Drawing.Point(6, 10);
            this.btnRefreshLogs.Name = "btnRefreshLogs";
            this.btnRefreshLogs.Size = new System.Drawing.Size(100, 30);
            this.btnRefreshLogs.TabIndex = 1;
            this.btnRefreshLogs.Text = "Refresh";
            this.btnRefreshLogs.UseVisualStyleBackColor = true;
            this.btnRefreshLogs.Click += new System.EventHandler(this.BtnRefreshLogs_Click);
            // 
            // btnExportLogs
            // 
            this.btnExportLogs.Location = new System.Drawing.Point(112, 10);
            this.btnExportLogs.Name = "btnExportLogs";
            this.btnExportLogs.Size = new System.Drawing.Size(100, 30);
            this.btnExportLogs.TabIndex = 2;
            this.btnExportLogs.Text = "Export";
            this.btnExportLogs.UseVisualStyleBackColor = true;
            this.btnExportLogs.Click += new System.EventHandler(this.BtnExportLogs_Click);
            //
            // HL7DetailForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 700);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.panelTop);
            this.Name = "HL7DetailForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HL7 Message Details";
            this.tabControl.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.tabLogs.ResumeLayout(false);
            this.tabLogs.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabMSH;
        private System.Windows.Forms.TabPage tabPID;
        private System.Windows.Forms.TabPage tabPV1;
        private System.Windows.Forms.TabPage tabORC;
        private System.Windows.Forms.TabPage tabAL1;
        private System.Windows.Forms.TabPage tabRXD;
        private System.Windows.Forms.TabPage tabRXR;
        private System.Windows.Forms.TabPage tabNTE;
        private System.Windows.Forms.TabPage tabLogs;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblOrderNo;
        private System.Windows.Forms.Button btnViewLogs;
        private System.Windows.Forms.TextBox logTextBox;
        private System.Windows.Forms.Button btnRefreshLogs;
        private System.Windows.Forms.Button btnExportLogs;
        
    }
}