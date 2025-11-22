namespace GeoTagNinja.View.DialogAndMessageBoxes
{
    partial class RevolutQRBox
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RevolutQRBox));
            this.pbx_QR = new System.Windows.Forms.PictureBox();
            this.btn_OK = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_QR)).BeginInit();
            this.SuspendLayout();
            // 
            // pbx_QR
            // 
            this.pbx_QR.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pbx_QR.BackgroundImage")));
            this.pbx_QR.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pbx_QR.Location = new System.Drawing.Point(12, 38);
            this.pbx_QR.Name = "pbx_QR";
            this.pbx_QR.Size = new System.Drawing.Size(414, 338);
            this.pbx_QR.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbx_QR.TabIndex = 0;
            this.pbx_QR.TabStop = false;
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(178, 415);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(75, 23);
            this.btn_OK.TabIndex = 1;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // RevolutQRBox
            // 
            this.AcceptButton = this.btn_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(440, 450);
            this.ControlBox = false;
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.pbx_QR);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RevolutQRBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "RevolutQR";
            this.Load += new System.EventHandler(this.RevolutQRBox_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbx_QR)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pbx_QR;
        private System.Windows.Forms.Button btn_OK;
    }
}