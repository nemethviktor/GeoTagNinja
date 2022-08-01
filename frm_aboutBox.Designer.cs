namespace GeoTagNinja
{
    partial class frm_aboutBox
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frm_aboutBox));
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.pbx_Logo = new System.Windows.Forms.PictureBox();
            this.tbx_Description = new System.Windows.Forms.TextBox();
            this.lbl_Version = new System.Windows.Forms.Label();
            this.lbl_CompanyName = new System.Windows.Forms.Label();
            this.lbl_ProductName = new System.Windows.Forms.Label();
            this.lbl_Copyright = new System.Windows.Forms.Label();
            this.btn_OK = new System.Windows.Forms.Button();
            this.lbl_Paypal = new System.Windows.Forms.LinkLabel();
            this.lbl_website = new System.Windows.Forms.LinkLabel();
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
            this.tableLayoutPanel.Controls.Add(this.lbl_Version, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.lbl_CompanyName, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.lbl_ProductName, 1, 3);
            this.tableLayoutPanel.Controls.Add(this.lbl_Copyright, 1, 2);
            this.tableLayoutPanel.Controls.Add(this.btn_OK, 1, 7);
            this.tableLayoutPanel.Controls.Add(this.lbl_Paypal, 1, 5);
            this.tableLayoutPanel.Controls.Add(this.lbl_website, 1, 4);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(9, 9);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 4;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.5F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
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
            this.pbx_Logo.Size = new System.Drawing.Size(177, 231);
            this.pbx_Logo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbx_Logo.TabIndex = 12;
            this.pbx_Logo.TabStop = false;
            // 
            // tbx_Description
            // 
            this.tbx_Description.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbx_Description.Location = new System.Drawing.Point(189, 149);
            this.tbx_Description.Margin = new System.Windows.Forms.Padding(6, 3, 3, 3);
            this.tbx_Description.Multiline = true;
            this.tbx_Description.Name = "tbx_Description";
            this.tbx_Description.ReadOnly = true;
            this.tbx_Description.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbx_Description.Size = new System.Drawing.Size(364, 85);
            this.tbx_Description.TabIndex = 23;
            this.tbx_Description.TabStop = false;
            this.tbx_Description.Text = "Description";
            // 
            // lbl_Version
            // 
            this.lbl_Version.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbl_Version.Location = new System.Drawing.Point(189, 0);
            this.lbl_Version.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this.lbl_Version.MaximumSize = new System.Drawing.Size(0, 17);
            this.lbl_Version.Name = "lbl_Version";
            this.lbl_Version.Size = new System.Drawing.Size(364, 17);
            this.lbl_Version.TabIndex = 0;
            this.lbl_Version.Text = "Version";
            this.lbl_Version.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbl_CompanyName
            // 
            this.lbl_CompanyName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbl_CompanyName.Location = new System.Drawing.Point(189, 22);
            this.lbl_CompanyName.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this.lbl_CompanyName.MaximumSize = new System.Drawing.Size(0, 17);
            this.lbl_CompanyName.Name = "lbl_CompanyName";
            this.lbl_CompanyName.Size = new System.Drawing.Size(364, 17);
            this.lbl_CompanyName.TabIndex = 22;
            this.lbl_CompanyName.Text = "Company Name";
            this.lbl_CompanyName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbl_ProductName
            // 
            this.lbl_ProductName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbl_ProductName.Location = new System.Drawing.Point(189, 66);
            this.lbl_ProductName.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this.lbl_ProductName.MaximumSize = new System.Drawing.Size(0, 17);
            this.lbl_ProductName.Name = "lbl_ProductName";
            this.lbl_ProductName.Size = new System.Drawing.Size(364, 17);
            this.lbl_ProductName.TabIndex = 19;
            this.lbl_ProductName.Text = "Product Name";
            this.lbl_ProductName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lbl_Copyright
            // 
            this.lbl_Copyright.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbl_Copyright.Location = new System.Drawing.Point(189, 44);
            this.lbl_Copyright.Margin = new System.Windows.Forms.Padding(6, 0, 3, 0);
            this.lbl_Copyright.MaximumSize = new System.Drawing.Size(0, 17);
            this.lbl_Copyright.Name = "lbl_Copyright";
            this.lbl_Copyright.Size = new System.Drawing.Size(364, 17);
            this.lbl_Copyright.TabIndex = 21;
            this.lbl_Copyright.Text = "Copyright";
            this.lbl_Copyright.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            // lbl_Paypal
            // 
            this.lbl_Paypal.Image = global::GeoTagNinja.Properties.Resources.Paypal;
            this.lbl_Paypal.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
            this.lbl_Paypal.Location = new System.Drawing.Point(185, 108);
            this.lbl_Paypal.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Paypal.Name = "lbl_Paypal";
            this.lbl_Paypal.Size = new System.Drawing.Size(134, 38);
            this.lbl_Paypal.TabIndex = 25;
            this.lbl_Paypal.Click += new System.EventHandler(this.lbl_Paypal_Click);
            // 
            // lbl_website
            // 
            this.lbl_website.AutoSize = true;
            this.lbl_website.Location = new System.Drawing.Point(186, 86);
            this.lbl_website.Name = "lbl_website";
            this.lbl_website.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.lbl_website.Size = new System.Drawing.Size(51, 13);
            this.lbl_website.TabIndex = 26;
            this.lbl_website.TabStop = true;
            this.lbl_website.Text = "Website";
            this.lbl_website.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lbl_website_LinkClicked);
            // 
            // frm_aboutBox
            // 
            this.AcceptButton = this.btn_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(574, 283);
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frm_aboutBox";
            this.Padding = new System.Windows.Forms.Padding(9);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.Load += new System.EventHandler(this.frm_aboutBox_Load);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Logo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.PictureBox pbx_Logo;
        private System.Windows.Forms.Label lbl_ProductName;
        private System.Windows.Forms.Label lbl_Version;
        private System.Windows.Forms.Label lbl_Copyright;
        private System.Windows.Forms.Label lbl_CompanyName;
        private System.Windows.Forms.TextBox tbx_Description;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.LinkLabel lbl_Paypal;
        private System.Windows.Forms.LinkLabel lbl_website;
    }
}
