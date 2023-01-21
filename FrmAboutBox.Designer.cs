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
            this.pbx_Logo = new System.Windows.Forms.PictureBox();
            this.tbx_Description = new System.Windows.Forms.TextBox();
            this.tbx_CompanyName = new System.Windows.Forms.TextBox();
            this.tbx_ProductName = new System.Windows.Forms.TextBox();
            this.tbx_Copyright = new System.Windows.Forms.TextBox();
            this.btn_OK = new System.Windows.Forms.Button();
            this.tbx_Website = new System.Windows.Forms.LinkLabel();
            this.tbx_Version = new System.Windows.Forms.TextBox();
            this.tbx_Paypal = new System.Windows.Forms.LinkLabel();
            this.tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Logo)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67F));
            this.tableLayoutPanel.Controls.Add(this.pbx_Logo, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.tbx_Description, 1, 6);
            this.tableLayoutPanel.Controls.Add(this.tbx_CompanyName, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.tbx_ProductName, 1, 3);
            this.tableLayoutPanel.Controls.Add(this.tbx_Copyright, 1, 2);
            this.tableLayoutPanel.Controls.Add(this.btn_OK, 1, 7);
            this.tableLayoutPanel.Controls.Add(this.tbx_Website, 1, 4);
            this.tableLayoutPanel.Controls.Add(this.tbx_Version, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.tbx_Paypal, 1, 5);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(9, 9);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 4;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.Size = new System.Drawing.Size(556, 265);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // pbx_Logo
            // 
            this.pbx_Logo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbx_Logo.Image = ((System.Drawing.Image)(resources.GetObject("pbx_Logo.Image")));
            this.pbx_Logo.Location = new System.Drawing.Point(3, 3);
            this.pbx_Logo.Name = "pbx_Logo";
            this.tableLayoutPanel.SetRowSpan(this.pbx_Logo, 7);
            this.pbx_Logo.Size = new System.Drawing.Size(177, 234);
            this.pbx_Logo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbx_Logo.TabIndex = 12;
            this.pbx_Logo.TabStop = false;
            // 
            // tbx_Description
            // 
            this.tbx_Description.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbx_Description.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbx_Description.Location = new System.Drawing.Point(188, 165);
            this.tbx_Description.Margin = new System.Windows.Forms.Padding(5);
            this.tbx_Description.Multiline = true;
            this.tbx_Description.Name = "tbx_Description";
            this.tbx_Description.ReadOnly = true;
            this.tbx_Description.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbx_Description.Size = new System.Drawing.Size(363, 70);
            this.tbx_Description.TabIndex = 23;
            this.tbx_Description.TabStop = false;
            // 
            // tbx_CompanyName
            // 
            this.tbx_CompanyName.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbx_CompanyName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbx_CompanyName.Location = new System.Drawing.Point(188, 20);
            this.tbx_CompanyName.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.tbx_CompanyName.Name = "tbx_CompanyName";
            this.tbx_CompanyName.ReadOnly = true;
            this.tbx_CompanyName.Size = new System.Drawing.Size(368, 13);
            this.tbx_CompanyName.TabIndex = 22;
            // 
            // tbx_ProductName
            // 
            this.tbx_ProductName.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbx_ProductName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbx_ProductName.Location = new System.Drawing.Point(188, 60);
            this.tbx_ProductName.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.tbx_ProductName.Name = "tbx_ProductName";
            this.tbx_ProductName.ReadOnly = true;
            this.tbx_ProductName.Size = new System.Drawing.Size(368, 13);
            this.tbx_ProductName.TabIndex = 19;
            // 
            // tbx_Copyright
            // 
            this.tbx_Copyright.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbx_Copyright.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbx_Copyright.Location = new System.Drawing.Point(188, 40);
            this.tbx_Copyright.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.tbx_Copyright.Name = "tbx_Copyright";
            this.tbx_Copyright.ReadOnly = true;
            this.tbx_Copyright.Size = new System.Drawing.Size(368, 13);
            this.tbx_Copyright.TabIndex = 21;
            // 
            // btn_OK
            // 
            this.btn_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_OK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_OK.Location = new System.Drawing.Point(478, 244);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(75, 18);
            this.btn_OK.TabIndex = 24;
            this.btn_OK.Text = "&OK";
            this.btn_OK.Click += new System.EventHandler(this.Btn_OK_Click);
            // 
            // tbx_Website
            // 
            this.tbx_Website.Location = new System.Drawing.Point(188, 80);
            this.tbx_Website.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.tbx_Website.Name = "tbx_Website";
            this.tbx_Website.Size = new System.Drawing.Size(365, 13);
            this.tbx_Website.TabIndex = 26;
            this.tbx_Website.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.tbx_Website_LinkClicked);
            // 
            // tbx_Version
            // 
            this.tbx_Version.BackColor = System.Drawing.SystemColors.Control;
            this.tbx_Version.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbx_Version.Location = new System.Drawing.Point(188, 0);
            this.tbx_Version.Margin = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.tbx_Version.Name = "tbx_Version";
            this.tbx_Version.ReadOnly = true;
            this.tbx_Version.Size = new System.Drawing.Size(365, 13);
            this.tbx_Version.TabIndex = 27;
            // 
            // tbx_Paypal
            // 
            this.tbx_Paypal.Image = global::GeoTagNinja.Properties.Resources.Paypal;
            this.tbx_Paypal.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.tbx_Paypal.Location = new System.Drawing.Point(203, 100);
            this.tbx_Paypal.Margin = new System.Windows.Forms.Padding(20, 0, 2, 0);
            this.tbx_Paypal.Name = "tbx_Paypal";
            this.tbx_Paypal.Size = new System.Drawing.Size(188, 60);
            this.tbx_Paypal.TabIndex = 25;
            this.tbx_Paypal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.tbx_Paypal.Click += new System.EventHandler(this.tbx_Paypal_Click);
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
            this.Load += new System.EventHandler(this.FrmAboutBox_Load);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Logo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.PictureBox pbx_Logo;
        private System.Windows.Forms.TextBox tbx_ProductName;
        private System.Windows.Forms.TextBox tbx_Copyright;
        private System.Windows.Forms.TextBox tbx_CompanyName;
        private System.Windows.Forms.TextBox tbx_Description;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.LinkLabel tbx_Paypal;
        private System.Windows.Forms.LinkLabel tbx_Website;
        private System.Windows.Forms.TextBox tbx_Version;
    }
}
