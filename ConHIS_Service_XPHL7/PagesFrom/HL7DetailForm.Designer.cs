using System;
using System.Windows.Forms;

namespace ConHIS_Service_XPHL7
{
    partial class HL7DetailForm
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
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
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
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1000, 700);
            this.tabControl.TabIndex = 0;
            // 
            // tabMSH
            // 
            this.tabMSH.Location = new System.Drawing.Point(4, 22);
            this.tabMSH.Name = "tabMSH";
            this.tabMSH.Padding = new System.Windows.Forms.Padding(3);
            this.tabMSH.Size = new System.Drawing.Size(992, 674);
            this.tabMSH.TabIndex = 0;
            this.tabMSH.Text = "MSH - Message Header";
            this.tabMSH.UseVisualStyleBackColor = true;
            // 
            // tabPID
            // 
            this.tabPID.Location = new System.Drawing.Point(4, 22);
            this.tabPID.Name = "tabPID";
            this.tabPID.Padding = new System.Windows.Forms.Padding(3);
            this.tabPID.Size = new System.Drawing.Size(992, 674);
            this.tabPID.TabIndex = 1;
            this.tabPID.Text = "PID - Patient ID";
            this.tabPID.UseVisualStyleBackColor = true;
            // 
            // tabPV1
            // 
            this.tabPV1.Location = new System.Drawing.Point(4, 22);
            this.tabPV1.Name = "tabPV1";
            this.tabPV1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPV1.Size = new System.Drawing.Size(992, 674);
            this.tabPV1.TabIndex = 2;
            this.tabPV1.Text = "PV1 - Patient Visit";
            this.tabPV1.UseVisualStyleBackColor = true;
            // 
            // tabORC
            // 
            this.tabORC.Location = new System.Drawing.Point(4, 22);
            this.tabORC.Name = "tabORC";
            this.tabORC.Padding = new System.Windows.Forms.Padding(3);
            this.tabORC.Size = new System.Drawing.Size(992, 674);
            this.tabORC.TabIndex = 3;
            this.tabORC.Text = "ORC - Common Order";
            this.tabORC.UseVisualStyleBackColor = true;
            // 
            // tabAL1
            // 
            this.tabAL1.Location = new System.Drawing.Point(4, 22);
            this.tabAL1.Name = "tabAL1";
            this.tabAL1.Padding = new System.Windows.Forms.Padding(3);
            this.tabAL1.Size = new System.Drawing.Size(992, 674);
            this.tabAL1.TabIndex = 4;
            this.tabAL1.Text = "AL1 - Allergies";
            this.tabAL1.UseVisualStyleBackColor = true;
            // 
            // tabRXD
            // 
            this.tabRXD.Location = new System.Drawing.Point(4, 22);
            this.tabRXD.Name = "tabRXD";
            this.tabRXD.Padding = new System.Windows.Forms.Padding(3);
            this.tabRXD.Size = new System.Drawing.Size(992, 674);
            this.tabRXD.TabIndex = 5;
            this.tabRXD.Text = "RXD - Pharmacy Dispense";
            this.tabRXD.UseVisualStyleBackColor = true;
            // 
            // tabRXR
            // 
            this.tabRXR.Location = new System.Drawing.Point(4, 22);
            this.tabRXR.Name = "tabRXR";
            this.tabRXR.Padding = new System.Windows.Forms.Padding(3);
            this.tabRXR.Size = new System.Drawing.Size(992, 674);
            this.tabRXR.TabIndex = 6;
            this.tabRXR.Text = "RXR - Route Info";
            this.tabRXR.UseVisualStyleBackColor = true;
            // 
            // tabNTE
            // 
            this.tabNTE.Location = new System.Drawing.Point(4, 22);
            this.tabNTE.Name = "tabNTE";
            this.tabNTE.Padding = new System.Windows.Forms.Padding(3);
            this.tabNTE.Size = new System.Drawing.Size(992, 674);
            this.tabNTE.TabIndex = 7;
            this.tabNTE.Text = "NTE - Notes";
            this.tabNTE.UseVisualStyleBackColor = true;
            // 
            // HL7DetailForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 700);
            this.Controls.Add(this.tabControl);
            this.Name = "HL7DetailForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HL7 Message Details";
            this.tabControl.ResumeLayout(false);
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
    }
}