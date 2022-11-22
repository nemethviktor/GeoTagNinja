namespace GeoTagNinja
{
    partial class FrmSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSettings));
            this.tct_Settings = new System.Windows.Forms.TabControl();
            this.tpg_Application = new System.Windows.Forms.TabPage();
            this.ckb_ResetMapToZero = new System.Windows.Forms.CheckBox();
            this.cbx_Language = new System.Windows.Forms.ComboBox();
            this.rbx_Register_ArcGIS = new System.Windows.Forms.RichTextBox();
            this.rbx_Register_GeoNames = new System.Windows.Forms.RichTextBox();
            this.pbx_Browse_Startup_Folder = new System.Windows.Forms.PictureBox();
            this.lbl_Startup_Folder = new System.Windows.Forms.Label();
            this.lbl_Language = new System.Windows.Forms.Label();
            this.lbl_GeoNames_Pwd = new System.Windows.Forms.Label();
            this.lbl_GeoNames_UserName = new System.Windows.Forms.Label();
            this.lbl_ARCGIS_APIKey = new System.Windows.Forms.Label();
            this.tbx_Startup_Folder = new System.Windows.Forms.TextBox();
            this.tbx_GeoNames_Pwd = new System.Windows.Forms.TextBox();
            this.tbx_GeoNames_UserName = new System.Windows.Forms.TextBox();
            this.tbx_ARCGIS_APIKey = new System.Windows.Forms.TextBox();
            this.tpg_FileOptions = new System.Windows.Forms.TabPage();
            this.ckb_ProcessOriginalFile = new System.Windows.Forms.CheckBox();
            this.ckb_OverwriteOriginal = new System.Windows.Forms.CheckBox();
            this.ckb_AddXMPSideCar = new System.Windows.Forms.CheckBox();
            this.lbx_fileExtensions = new System.Windows.Forms.ListBox();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.fbd_StartupFolder = new System.Windows.Forms.FolderBrowserDialog();
            this.ckb_ResetFileDateToCreated = new System.Windows.Forms.CheckBox();
            this.tct_Settings.SuspendLayout();
            this.tpg_Application.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Browse_Startup_Folder)).BeginInit();
            this.tpg_FileOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // tct_Settings
            // 
            this.tct_Settings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tct_Settings.Controls.Add(this.tpg_Application);
            this.tct_Settings.Controls.Add(this.tpg_FileOptions);
            this.tct_Settings.Location = new System.Drawing.Point(16, 12);
            this.tct_Settings.Margin = new System.Windows.Forms.Padding(2);
            this.tct_Settings.Name = "tct_Settings";
            this.tct_Settings.SelectedIndex = 0;
            this.tct_Settings.Size = new System.Drawing.Size(621, 428);
            this.tct_Settings.TabIndex = 0;
            this.tct_Settings.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.Tab_Settings_Selecting);
            this.tct_Settings.Deselecting += new System.Windows.Forms.TabControlCancelEventHandler(this.Tab_Settings_Deselecting);
            // 
            // tpg_Application
            // 
            this.tpg_Application.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tpg_Application.Controls.Add(this.ckb_ResetMapToZero);
            this.tpg_Application.Controls.Add(this.cbx_Language);
            this.tpg_Application.Controls.Add(this.rbx_Register_ArcGIS);
            this.tpg_Application.Controls.Add(this.rbx_Register_GeoNames);
            this.tpg_Application.Controls.Add(this.pbx_Browse_Startup_Folder);
            this.tpg_Application.Controls.Add(this.lbl_Startup_Folder);
            this.tpg_Application.Controls.Add(this.lbl_Language);
            this.tpg_Application.Controls.Add(this.lbl_GeoNames_Pwd);
            this.tpg_Application.Controls.Add(this.lbl_GeoNames_UserName);
            this.tpg_Application.Controls.Add(this.lbl_ARCGIS_APIKey);
            this.tpg_Application.Controls.Add(this.tbx_Startup_Folder);
            this.tpg_Application.Controls.Add(this.tbx_GeoNames_Pwd);
            this.tpg_Application.Controls.Add(this.tbx_GeoNames_UserName);
            this.tpg_Application.Controls.Add(this.tbx_ARCGIS_APIKey);
            this.tpg_Application.Location = new System.Drawing.Point(4, 22);
            this.tpg_Application.Margin = new System.Windows.Forms.Padding(2);
            this.tpg_Application.Name = "tpg_Application";
            this.tpg_Application.Padding = new System.Windows.Forms.Padding(2);
            this.tpg_Application.Size = new System.Drawing.Size(613, 402);
            this.tpg_Application.TabIndex = 0;
            this.tpg_Application.Text = "tpg_Application";
            // 
            // ckb_ResetMapToZero
            // 
            this.ckb_ResetMapToZero.AutoSize = true;
            this.ckb_ResetMapToZero.Location = new System.Drawing.Point(21, 258);
            this.ckb_ResetMapToZero.Name = "ckb_ResetMapToZero";
            this.ckb_ResetMapToZero.Size = new System.Drawing.Size(134, 17);
            this.ckb_ResetMapToZero.TabIndex = 15;
            this.ckb_ResetMapToZero.Text = "ckb_ResetMapToZero";
            this.ckb_ResetMapToZero.UseVisualStyleBackColor = true;
            this.ckb_ResetMapToZero.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // cbx_Language
            // 
            this.cbx_Language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_Language.FormattingEnabled = true;
            this.cbx_Language.Location = new System.Drawing.Point(136, 228);
            this.cbx_Language.Name = "cbx_Language";
            this.cbx_Language.Size = new System.Drawing.Size(121, 21);
            this.cbx_Language.TabIndex = 13;
            this.cbx_Language.SelectedValueChanged += new System.EventHandler(this.Any_cbx_TextChanged);
            this.cbx_Language.TextChanged += new System.EventHandler(this.Any_cbx_TextChanged);
            // 
            // rbx_Register_ArcGIS
            // 
            this.rbx_Register_ArcGIS.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rbx_Register_ArcGIS.Location = new System.Drawing.Point(20, 45);
            this.rbx_Register_ArcGIS.Name = "rbx_Register_ArcGIS";
            this.rbx_Register_ArcGIS.ReadOnly = true;
            this.rbx_Register_ArcGIS.Size = new System.Drawing.Size(405, 42);
            this.rbx_Register_ArcGIS.TabIndex = 12;
            this.rbx_Register_ArcGIS.Text = "rbx_Register_ArcGIS";
            // 
            // rbx_Register_GeoNames
            // 
            this.rbx_Register_GeoNames.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rbx_Register_GeoNames.Location = new System.Drawing.Point(20, 126);
            this.rbx_Register_GeoNames.Name = "rbx_Register_GeoNames";
            this.rbx_Register_GeoNames.ReadOnly = true;
            this.rbx_Register_GeoNames.Size = new System.Drawing.Size(405, 42);
            this.rbx_Register_GeoNames.TabIndex = 12;
            this.rbx_Register_GeoNames.Text = "rbx_Register_GeoNames";
            // 
            // pbx_Browse_Startup_Folder
            // 
            this.pbx_Browse_Startup_Folder.Image = ((System.Drawing.Image)(resources.GetObject("pbx_Browse_Startup_Folder.Image")));
            this.pbx_Browse_Startup_Folder.Location = new System.Drawing.Point(527, 24);
            this.pbx_Browse_Startup_Folder.Margin = new System.Windows.Forms.Padding(2);
            this.pbx_Browse_Startup_Folder.Name = "pbx_Browse_Startup_Folder";
            this.pbx_Browse_Startup_Folder.Size = new System.Drawing.Size(16, 16);
            this.pbx_Browse_Startup_Folder.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbx_Browse_Startup_Folder.TabIndex = 11;
            this.pbx_Browse_Startup_Folder.TabStop = false;
            this.pbx_Browse_Startup_Folder.Click += new System.EventHandler(this.Pbx_Browse_Startup_Folder_Click);
            // 
            // lbl_Startup_Folder
            // 
            this.lbl_Startup_Folder.AutoSize = true;
            this.lbl_Startup_Folder.Location = new System.Drawing.Point(17, 24);
            this.lbl_Startup_Folder.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Startup_Folder.Name = "lbl_Startup_Folder";
            this.lbl_Startup_Folder.Size = new System.Drawing.Size(92, 13);
            this.lbl_Startup_Folder.TabIndex = 9;
            this.lbl_Startup_Folder.Text = "lbl_Startup_Folder";
            // 
            // lbl_Language
            // 
            this.lbl_Language.AutoSize = true;
            this.lbl_Language.Location = new System.Drawing.Point(18, 232);
            this.lbl_Language.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Language.Name = "lbl_Language";
            this.lbl_Language.Size = new System.Drawing.Size(71, 13);
            this.lbl_Language.TabIndex = 9;
            this.lbl_Language.Text = "lbl_Language";
            // 
            // lbl_GeoNames_Pwd
            // 
            this.lbl_GeoNames_Pwd.AutoSize = true;
            this.lbl_GeoNames_Pwd.Location = new System.Drawing.Point(17, 204);
            this.lbl_GeoNames_Pwd.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_GeoNames_Pwd.Name = "lbl_GeoNames_Pwd";
            this.lbl_GeoNames_Pwd.Size = new System.Drawing.Size(103, 13);
            this.lbl_GeoNames_Pwd.TabIndex = 9;
            this.lbl_GeoNames_Pwd.Text = "lbl_GeoNames_Pwd";
            // 
            // lbl_GeoNames_UserName
            // 
            this.lbl_GeoNames_UserName.AutoSize = true;
            this.lbl_GeoNames_UserName.Location = new System.Drawing.Point(17, 176);
            this.lbl_GeoNames_UserName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_GeoNames_UserName.Name = "lbl_GeoNames_UserName";
            this.lbl_GeoNames_UserName.Size = new System.Drawing.Size(132, 13);
            this.lbl_GeoNames_UserName.TabIndex = 9;
            this.lbl_GeoNames_UserName.Text = "lbl_GeoNames_UserName";
            // 
            // lbl_ARCGIS_APIKey
            // 
            this.lbl_ARCGIS_APIKey.AutoSize = true;
            this.lbl_ARCGIS_APIKey.Location = new System.Drawing.Point(17, 95);
            this.lbl_ARCGIS_APIKey.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_ARCGIS_APIKey.Name = "lbl_ARCGIS_APIKey";
            this.lbl_ARCGIS_APIKey.Size = new System.Drawing.Size(104, 13);
            this.lbl_ARCGIS_APIKey.TabIndex = 9;
            this.lbl_ARCGIS_APIKey.Text = "lbl_ARCGIS_APIKey";
            // 
            // tbx_Startup_Folder
            // 
            this.tbx_Startup_Folder.Location = new System.Drawing.Point(136, 20);
            this.tbx_Startup_Folder.Margin = new System.Windows.Forms.Padding(2);
            this.tbx_Startup_Folder.Name = "tbx_Startup_Folder";
            this.tbx_Startup_Folder.ReadOnly = true;
            this.tbx_Startup_Folder.Size = new System.Drawing.Size(372, 20);
            this.tbx_Startup_Folder.TabIndex = 1;
            this.tbx_Startup_Folder.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // tbx_GeoNames_Pwd
            // 
            this.tbx_GeoNames_Pwd.Location = new System.Drawing.Point(136, 200);
            this.tbx_GeoNames_Pwd.Margin = new System.Windows.Forms.Padding(2);
            this.tbx_GeoNames_Pwd.Name = "tbx_GeoNames_Pwd";
            this.tbx_GeoNames_Pwd.PasswordChar = '*';
            this.tbx_GeoNames_Pwd.Size = new System.Drawing.Size(372, 20);
            this.tbx_GeoNames_Pwd.TabIndex = 4;
            this.tbx_GeoNames_Pwd.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // tbx_GeoNames_UserName
            // 
            this.tbx_GeoNames_UserName.Location = new System.Drawing.Point(136, 172);
            this.tbx_GeoNames_UserName.Margin = new System.Windows.Forms.Padding(2);
            this.tbx_GeoNames_UserName.Name = "tbx_GeoNames_UserName";
            this.tbx_GeoNames_UserName.Size = new System.Drawing.Size(372, 20);
            this.tbx_GeoNames_UserName.TabIndex = 3;
            this.tbx_GeoNames_UserName.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // tbx_ARCGIS_APIKey
            // 
            this.tbx_ARCGIS_APIKey.Location = new System.Drawing.Point(136, 92);
            this.tbx_ARCGIS_APIKey.Margin = new System.Windows.Forms.Padding(2);
            this.tbx_ARCGIS_APIKey.Name = "tbx_ARCGIS_APIKey";
            this.tbx_ARCGIS_APIKey.PasswordChar = '*';
            this.tbx_ARCGIS_APIKey.Size = new System.Drawing.Size(372, 20);
            this.tbx_ARCGIS_APIKey.TabIndex = 2;
            this.tbx_ARCGIS_APIKey.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // tpg_FileOptions
            // 
            this.tpg_FileOptions.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tpg_FileOptions.Controls.Add(this.ckb_ResetFileDateToCreated);
            this.tpg_FileOptions.Controls.Add(this.ckb_ProcessOriginalFile);
            this.tpg_FileOptions.Controls.Add(this.ckb_OverwriteOriginal);
            this.tpg_FileOptions.Controls.Add(this.ckb_AddXMPSideCar);
            this.tpg_FileOptions.Controls.Add(this.lbx_fileExtensions);
            this.tpg_FileOptions.Location = new System.Drawing.Point(4, 22);
            this.tpg_FileOptions.Margin = new System.Windows.Forms.Padding(2);
            this.tpg_FileOptions.Name = "tpg_FileOptions";
            this.tpg_FileOptions.Padding = new System.Windows.Forms.Padding(2);
            this.tpg_FileOptions.Size = new System.Drawing.Size(613, 402);
            this.tpg_FileOptions.TabIndex = 1;
            this.tpg_FileOptions.Text = "tpg_FileOptions";
            this.tpg_FileOptions.Enter += new System.EventHandler(this.Pg_fileoptions_Enter);
            // 
            // ckb_ProcessOriginalFile
            // 
            this.ckb_ProcessOriginalFile.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_ProcessOriginalFile.Location = new System.Drawing.Point(397, 23);
            this.ckb_ProcessOriginalFile.MinimumSize = new System.Drawing.Size(100, 20);
            this.ckb_ProcessOriginalFile.Name = "ckb_ProcessOriginalFile";
            this.ckb_ProcessOriginalFile.Size = new System.Drawing.Size(190, 38);
            this.ckb_ProcessOriginalFile.TabIndex = 3;
            this.ckb_ProcessOriginalFile.Text = "ckb_ProcessOriginalFile";
            this.ckb_ProcessOriginalFile.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_ProcessOriginalFile.UseVisualStyleBackColor = true;
            this.ckb_ProcessOriginalFile.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_OverwriteOriginal
            // 
            this.ckb_OverwriteOriginal.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_OverwriteOriginal.Location = new System.Drawing.Point(397, 183);
            this.ckb_OverwriteOriginal.MinimumSize = new System.Drawing.Size(100, 20);
            this.ckb_OverwriteOriginal.Name = "ckb_OverwriteOriginal";
            this.ckb_OverwriteOriginal.Size = new System.Drawing.Size(190, 38);
            this.ckb_OverwriteOriginal.TabIndex = 1;
            this.ckb_OverwriteOriginal.Text = "ckb_OverwriteOriginal";
            this.ckb_OverwriteOriginal.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_OverwriteOriginal.UseVisualStyleBackColor = true;
            this.ckb_OverwriteOriginal.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_AddXMPSideCar
            // 
            this.ckb_AddXMPSideCar.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_AddXMPSideCar.Location = new System.Drawing.Point(397, 123);
            this.ckb_AddXMPSideCar.MinimumSize = new System.Drawing.Size(100, 20);
            this.ckb_AddXMPSideCar.Name = "ckb_AddXMPSideCar";
            this.ckb_AddXMPSideCar.Size = new System.Drawing.Size(190, 38);
            this.ckb_AddXMPSideCar.TabIndex = 1;
            this.ckb_AddXMPSideCar.Text = "ckb_AddXMPSideCar";
            this.ckb_AddXMPSideCar.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_AddXMPSideCar.UseVisualStyleBackColor = true;
            this.ckb_AddXMPSideCar.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // lbx_fileExtensions
            // 
            this.lbx_fileExtensions.FormattingEnabled = true;
            this.lbx_fileExtensions.Location = new System.Drawing.Point(6, 6);
            this.lbx_fileExtensions.Name = "lbx_fileExtensions";
            this.lbx_fileExtensions.Size = new System.Drawing.Size(385, 368);
            this.lbx_fileExtensions.TabIndex = 0;
            this.lbx_fileExtensions.SelectedIndexChanged += new System.EventHandler(this.Lbx_fileExtensions_SelectedIndexChanged);
            // 
            // btn_OK
            // 
            this.btn_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btn_OK.Location = new System.Drawing.Point(506, 459);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(2);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(60, 19);
            this.btn_OK.TabIndex = 5;
            this.btn_OK.Text = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.Btn_OK_Click);
            // 
            // btn_Cancel
            // 
            this.btn_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Cancel.Location = new System.Drawing.Point(576, 459);
            this.btn_Cancel.Margin = new System.Windows.Forms.Padding(2);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(60, 19);
            this.btn_Cancel.TabIndex = 6;
            this.btn_Cancel.Text = "btn_Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = true;
            this.btn_Cancel.Click += new System.EventHandler(this.Btn_Cancel_Click);
            // 
            // fbd_StartupFolder
            // 
            this.fbd_StartupFolder.Description = "fbd_StartupFolder";
            this.fbd_StartupFolder.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // ckb_ResetFileDateToCreated
            // 
            this.ckb_ResetFileDateToCreated.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_ResetFileDateToCreated.Location = new System.Drawing.Point(419, 67);
            this.ckb_ResetFileDateToCreated.MinimumSize = new System.Drawing.Size(100, 20);
            this.ckb_ResetFileDateToCreated.Name = "ckb_ResetFileDateToCreated";
            this.ckb_ResetFileDateToCreated.Size = new System.Drawing.Size(168, 38);
            this.ckb_ResetFileDateToCreated.TabIndex = 4;
            this.ckb_ResetFileDateToCreated.Text = "ckb_ResetFileDateToCreated";
            this.ckb_ResetFileDateToCreated.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_ResetFileDateToCreated.UseVisualStyleBackColor = true;
            // 
            // FrmSettings
            // 
            this.AcceptButton = this.btn_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.btn_Cancel;
            this.ClientSize = new System.Drawing.Size(652, 485);
            this.Controls.Add(this.btn_Cancel);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.tct_Settings);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Location = new System.Drawing.Point(350, 100);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(20, 490);
            this.Name = "FrmSettings";
            this.ShowInTaskbar = false;
            this.Text = "FrmSettings";
            this.Load += new System.EventHandler(this.FrmSettings_Load);
            this.tct_Settings.ResumeLayout(false);
            this.tpg_Application.ResumeLayout(false);
            this.tpg_Application.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Browse_Startup_Folder)).EndInit();
            this.tpg_FileOptions.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TabPage tpg_Application;
        private System.Windows.Forms.TabPage tpg_FileOptions;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Cancel;
        private System.Windows.Forms.Label lbl_ARCGIS_APIKey;
        private System.Windows.Forms.TabControl tct_Settings;
        private System.Windows.Forms.Label lbl_Startup_Folder;
        public System.Windows.Forms.TextBox tbx_Startup_Folder;
        private System.Windows.Forms.FolderBrowserDialog fbd_StartupFolder;
        private System.Windows.Forms.PictureBox pbx_Browse_Startup_Folder;
        private System.Windows.Forms.RichTextBox rbx_Register_ArcGIS;
        private System.Windows.Forms.RichTextBox rbx_Register_GeoNames;
        private System.Windows.Forms.Label lbl_GeoNames_UserName;
        public System.Windows.Forms.TextBox tbx_GeoNames_UserName;
        public System.Windows.Forms.TextBox tbx_ARCGIS_APIKey;
        private System.Windows.Forms.Label lbl_GeoNames_Pwd;
        public System.Windows.Forms.TextBox tbx_GeoNames_Pwd;
        private System.Windows.Forms.CheckBox ckb_OverwriteOriginal;
        private System.Windows.Forms.CheckBox ckb_AddXMPSideCar;
        private System.Windows.Forms.ListBox lbx_fileExtensions;
        private System.Windows.Forms.Label lbl_Language;
        private System.Windows.Forms.ComboBox cbx_Language;
        private System.Windows.Forms.CheckBox ckb_ResetMapToZero;
        private System.Windows.Forms.CheckBox ckb_ProcessOriginalFile;
        private System.Windows.Forms.CheckBox ckb_ResetFileDateToCreated;
    }
}