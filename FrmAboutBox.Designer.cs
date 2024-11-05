namespace GeoTagNinja
{
    partial class FrmAboutBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmAboutBox));
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.rtb_AboutBox = new System.Windows.Forms.RichTextBox();
            this.pbx_Logo = new System.Windows.Forms.PictureBox();
            this.tbx_Description = new System.Windows.Forms.TextBox();
            this.btn_Generic_OK = new System.Windows.Forms.Button();
            this.tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Logo)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
            this.tableLayoutPanel.Controls.Add(this.rtb_AboutBox, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.pbx_Logo, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.tbx_Description, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.btn_Generic_OK, 1, 2);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            // 
            // rtb_AboutBox
            // 
            this.rtb_AboutBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.rtb_AboutBox, "rtb_AboutBox");
            this.rtb_AboutBox.Name = "rtb_AboutBox";
            this.rtb_AboutBox.ReadOnly = true;
            // 
            // pbx_Logo
            // 
            resources.ApplyResources(this.pbx_Logo, "pbx_Logo");
            this.pbx_Logo.Name = "pbx_Logo";
            this.tableLayoutPanel.SetRowSpan(this.pbx_Logo, 2);
            this.pbx_Logo.TabStop = false;
            // 
            // tbx_Description
            // 
            this.tbx_Description.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.tbx_Description, "tbx_Description");
            this.tbx_Description.Name = "tbx_Description";
            this.tbx_Description.ReadOnly = true;
            this.tbx_Description.TabStop = false;
            // 
            // btn_Generic_OK
            // 
            resources.ApplyResources(this.btn_Generic_OK, "btn_Generic_OK");
            this.btn_Generic_OK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Generic_OK.Name = "btn_Generic_OK";
            this.btn_Generic_OK.Click += new System.EventHandler(this.btn_Generic_OK_Click);
            // 
            // FrmAboutBox
            // 
            this.AcceptButton = this.btn_Generic_OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmAboutBox";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Logo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.PictureBox pbx_Logo;
        private System.Windows.Forms.TextBox tbx_Description;
        private System.Windows.Forms.Button btn_Generic_OK;
        private System.Windows.Forms.RichTextBox rtb_AboutBox;
    }
}
