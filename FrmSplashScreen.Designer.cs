namespace GeoTagNinja
{
    partial class FrmSplashScreen
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSplashScreen));
            this.pbr_Splash = new System.Windows.Forms.ProgressBar();
            this.SuspendLayout();
            // 
            // pbr_Splash
            // 
            this.pbr_Splash.Location = new System.Drawing.Point(12, 401);
            this.pbr_Splash.Name = "pbr_Splash";
            this.pbr_Splash.Size = new System.Drawing.Size(284, 23);
            this.pbr_Splash.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.pbr_Splash.TabIndex = 0;
            // 
            // FrmSplashScreen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ClientSize = new System.Drawing.Size(308, 450);
            this.ControlBox = false;
            this.Controls.Add(this.pbr_Splash);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "FrmSplashScreen";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "GeoTagNinja";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.FrmSplashScreen_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar pbr_Splash;
    }
}