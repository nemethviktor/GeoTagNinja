namespace GeoTagNinja
{
    partial class FrmPleaseWaitBox
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
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.lbl_ParsingFolders = new System.Windows.Forms.Label();
            this.lbl_CancelPressed = new System.Windows.Forms.Label();
            this.lbl_PleaseWaitBoxMessage = new System.Windows.Forms.Label();
            this.lbl_PleaseWaitBoxActionScanning = new System.Windows.Forms.Label();
            this.lbl_PleaseWaitBoxActionParsing = new System.Windows.Forms.Label();
            this.lbl_PressCancelToStop = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btn_Cancel
            // 
            this.btn_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Cancel.Location = new System.Drawing.Point(361, 205);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(75, 23);
            this.btn_Cancel.TabIndex = 0;
            this.btn_Cancel.Text = "Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = true;
            this.btn_Cancel.Click += new System.EventHandler(this.btn_Cancel_Click);
            // 
            // lbl_ParsingFolders
            // 
            this.lbl_ParsingFolders.Location = new System.Drawing.Point(13, 13);
            this.lbl_ParsingFolders.Name = "lbl_ParsingFolders";
            this.lbl_ParsingFolders.Size = new System.Drawing.Size(594, 13);
            this.lbl_ParsingFolders.TabIndex = 1;
            this.lbl_ParsingFolders.Text = "lbl_ParsingFolders";
            // 
            // lbl_CancelPressed
            // 
            this.lbl_CancelPressed.Location = new System.Drawing.Point(12, 82);
            this.lbl_CancelPressed.Name = "lbl_CancelPressed";
            this.lbl_CancelPressed.Size = new System.Drawing.Size(594, 13);
            this.lbl_CancelPressed.TabIndex = 2;
            this.lbl_CancelPressed.Text = "lbl_CancelPressed";
            this.lbl_CancelPressed.Visible = false;
            // 
            // lbl_PleaseWaitBoxMessage
            // 
            this.lbl_PleaseWaitBoxMessage.Location = new System.Drawing.Point(118, 158);
            this.lbl_PleaseWaitBoxMessage.Name = "lbl_PleaseWaitBoxMessage";
            this.lbl_PleaseWaitBoxMessage.Size = new System.Drawing.Size(561, 13);
            this.lbl_PleaseWaitBoxMessage.TabIndex = 3;
            this.lbl_PleaseWaitBoxMessage.Text = "lbl_PleaseWaitBoxMessage";
            // 
            // lbl_PleaseWaitBoxActionScanning
            // 
            this.lbl_PleaseWaitBoxActionScanning.Location = new System.Drawing.Point(13, 134);
            this.lbl_PleaseWaitBoxActionScanning.Name = "lbl_PleaseWaitBoxActionScanning";
            this.lbl_PleaseWaitBoxActionScanning.Size = new System.Drawing.Size(170, 13);
            this.lbl_PleaseWaitBoxActionScanning.TabIndex = 4;
            this.lbl_PleaseWaitBoxActionScanning.Text = "lbl_PleaseWaitBoxActionScanning";
            // 
            // lbl_PleaseWaitBoxActionParsing
            // 
            this.lbl_PleaseWaitBoxActionParsing.Location = new System.Drawing.Point(13, 134);
            this.lbl_PleaseWaitBoxActionParsing.Name = "lbl_PleaseWaitBoxActionParsing";
            this.lbl_PleaseWaitBoxActionParsing.Size = new System.Drawing.Size(170, 13);
            this.lbl_PleaseWaitBoxActionParsing.TabIndex = 5;
            this.lbl_PleaseWaitBoxActionParsing.Text = "lbl_PleaseWaitBoxActionParsing";
            // 
            // lbl_PressCancelToStop
            // 
            this.lbl_PressCancelToStop.Location = new System.Drawing.Point(13, 42);
            this.lbl_PressCancelToStop.Name = "lbl_PressCancelToStop";
            this.lbl_PressCancelToStop.Size = new System.Drawing.Size(594, 13);
            this.lbl_PressCancelToStop.TabIndex = 6;
            this.lbl_PressCancelToStop.Text = "lbl_PressCancelToStop";
            // 
            // FrmPleaseWaitBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btn_Cancel;
            this.ClientSize = new System.Drawing.Size(800, 240);
            this.ControlBox = false;
            this.Controls.Add(this.lbl_PressCancelToStop);
            this.Controls.Add(this.lbl_PleaseWaitBoxActionParsing);
            this.Controls.Add(this.lbl_PleaseWaitBoxActionScanning);
            this.Controls.Add(this.lbl_PleaseWaitBoxMessage);
            this.Controls.Add(this.lbl_CancelPressed);
            this.Controls.Add(this.lbl_ParsingFolders);
            this.Controls.Add(this.btn_Cancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FrmPleaseWaitBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FrmPleaseWaitBox";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmPleaseWaitBox_FormClosing);
            this.Load += new System.EventHandler(this.FrmPleaseWaitBox_Load);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label lbl_ParsingFolders;
        private System.Windows.Forms.Label lbl_CancelPressed;
        internal System.Windows.Forms.Label lbl_PleaseWaitBoxMessage;
        internal System.Windows.Forms.Button btn_Cancel;
        internal System.Windows.Forms.Label lbl_PleaseWaitBoxActionScanning;
        internal System.Windows.Forms.Label lbl_PleaseWaitBoxActionParsing;
        internal System.Windows.Forms.Label lbl_PressCancelToStop;
    }
}