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
            this.btn_OK = new System.Windows.Forms.Button();
            this.tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Logo)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67F));
            this.tableLayoutPanel.Controls.Add(this.rtb_AboutBox, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.pbx_Logo, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.tbx_Description, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.btn_OK, 1, 2);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(9, 9);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 1;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.Size = new System.Drawing.Size(556, 265);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // rtb_AboutBox
            // 
            this.rtb_AboutBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtb_AboutBox.Location = new System.Drawing.Point(186, 3);
            this.rtb_AboutBox.Name = "rtb_AboutBox";
            this.rtb_AboutBox.ReadOnly = true;
            this.rtb_AboutBox.Size = new System.Drawing.Size(365, 152);
            this.rtb_AboutBox.TabIndex = 29;
            this.rtb_AboutBox.Text = "";
            // 
            // pbx_Logo
            // 
            this.pbx_Logo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbx_Logo.Image = ((System.Drawing.Image)(resources.GetObject("pbx_Logo.Image")));
            this.pbx_Logo.Location = new System.Drawing.Point(3, 3);
            this.pbx_Logo.Name = "pbx_Logo";
            this.tableLayoutPanel.SetRowSpan(this.pbx_Logo, 2);
            this.pbx_Logo.Size = new System.Drawing.Size(177, 230);
            this.pbx_Logo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbx_Logo.TabIndex = 12;
            this.pbx_Logo.TabStop = false;
            // 
            // tbx_Description
            // 
            this.tbx_Description.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbx_Description.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbx_Description.Location = new System.Drawing.Point(188, 163);
            this.tbx_Description.Margin = new System.Windows.Forms.Padding(5);
            this.tbx_Description.Multiline = true;
            this.tbx_Description.Name = "tbx_Description";
            this.tbx_Description.ReadOnly = true;
            this.tbx_Description.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbx_Description.Size = new System.Drawing.Size(363, 68);
            this.tbx_Description.TabIndex = 23;
            this.tbx_Description.TabStop = false;
            // 
            // btn_OK
            // 
            this.btn_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_OK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_OK.Location = new System.Drawing.Point(478, 239);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(75, 23);
            this.btn_OK.TabIndex = 24;
            this.btn_OK.Text = "&OK";
            this.btn_OK.Click += new System.EventHandler(this.Btn_OK_Click);
            // 
            // FrmAboutBox
            // 
            this.AcceptButton = this.btn_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(574, 283);
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmAboutBox";
            this.Padding = new System.Windows.Forms.Padding(9);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FrmAboutBox";
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Logo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.PictureBox pbx_Logo;
        private System.Windows.Forms.TextBox tbx_Description;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.RichTextBox rtb_AboutBox;
    }
}
