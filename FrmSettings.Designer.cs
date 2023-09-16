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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSettings));
            this.tct_Settings = new System.Windows.Forms.TabControl();
            this.tpg_Application = new System.Windows.Forms.TabPage();
            this.gbx_AppSettings = new System.Windows.Forms.GroupBox();
            this.ckb_UseDarkMode = new System.Windows.Forms.CheckBox();
            this.ckb_UpdateCheckPreRelease = new System.Windows.Forms.CheckBox();
            this.lbl_Metres_Abbr = new System.Windows.Forms.Label();
            this.lbl_Feet_Abbr = new System.Windows.Forms.Label();
            this.ckb_UseImperialNotMetric = new System.Windows.Forms.CheckBox();
            this.ckb_RemoveGeoDataRemovesTimeOffset = new System.Windows.Forms.CheckBox();
            this.ckb_ResetMapToZero = new System.Windows.Forms.CheckBox();
            this.cbx_Language = new System.Windows.Forms.ComboBox();
            this.lbl_Language = new System.Windows.Forms.Label();
            this.pbx_Browse_Startup_Folder = new System.Windows.Forms.PictureBox();
            this.lbl_Startup_Folder = new System.Windows.Forms.Label();
            this.tbx_Startup_Folder = new System.Windows.Forms.TextBox();
            this.tpg_FileOptions = new System.Windows.Forms.TabPage();
            this.ckb_ResetFileDateToCreated = new System.Windows.Forms.CheckBox();
            this.ckb_ProcessOriginalFile = new System.Windows.Forms.CheckBox();
            this.ckb_OverwriteOriginal = new System.Windows.Forms.CheckBox();
            this.ckb_AddXMPSideCar = new System.Windows.Forms.CheckBox();
            this.lbx_fileExtensions = new System.Windows.Forms.ListBox();
            this.tpg_CustomCityLogic = new System.Windows.Forms.TabPage();
            this.rbx_CustomCityLogicExplanationBold = new System.Windows.Forms.RichTextBox();
            this.btn_ResetToDefaults = new System.Windows.Forms.Button();
            this.rbx_CustomCityLogicExplanation = new System.Windows.Forms.RichTextBox();
            this.dgv_CustomCityLogic = new System.Windows.Forms.DataGridView();
            this.tpg_CustomRules = new System.Windows.Forms.TabPage();
            this.rbx_CustomRulesExplanationBold = new System.Windows.Forms.RichTextBox();
            this.ckb_StopProcessingRules = new System.Windows.Forms.CheckBox();
            this.ckb_IncludePredeterminedCountries = new System.Windows.Forms.CheckBox();
            this.rbx_CustomRulesExplanation = new System.Windows.Forms.RichTextBox();
            this.dgv_CustomRules = new System.Windows.Forms.DataGridView();
            this.igl_Settings = new System.Windows.Forms.ImageList(this.components);
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.fbd_StartupFolder = new System.Windows.Forms.FolderBrowserDialog();
            this.tpg_GeoNames = new System.Windows.Forms.TabPage();
            this.gbx_GeoNamesSettings = new System.Windows.Forms.GroupBox();
            this.lbl_Miles = new System.Windows.Forms.Label();
            this.ckb_PopulatedPlacesOnly = new System.Windows.Forms.CheckBox();
            this.gbx_GeoNamesLanguageSettings = new System.Windows.Forms.GroupBox();
            this.cbx_TryUseGeoNamesLanguage = new System.Windows.Forms.ComboBox();
            this.rbt_TryUseGeoNamesLanguage = new System.Windows.Forms.RadioButton();
            this.rbt_UseGeoNamesLocalLanguage = new System.Windows.Forms.RadioButton();
            this.ckb_ReplaceBlankToponyms = new System.Windows.Forms.CheckBox();
            this.tbx_ReplaceBlankToponyms = new System.Windows.Forms.TextBox();
            this.lbl_ChoiceRadius = new System.Windows.Forms.Label();
            this.lbl_ChoiceOfferCount = new System.Windows.Forms.Label();
            this.nud_ChoiceRadius = new System.Windows.Forms.NumericUpDown();
            this.nud_ChoiceOfferCount = new System.Windows.Forms.NumericUpDown();
            this.rbx_Register_ArcGIS = new System.Windows.Forms.RichTextBox();
            this.rbx_Register_GeoNames = new System.Windows.Forms.RichTextBox();
            this.lbl_GeoNames_Pwd = new System.Windows.Forms.Label();
            this.lbl_GeoNames_UserName = new System.Windows.Forms.Label();
            this.lbl_ARCGIS_APIKey = new System.Windows.Forms.Label();
            this.tbx_GeoNames_Pwd = new System.Windows.Forms.TextBox();
            this.tbx_GeoNames_UserName = new System.Windows.Forms.TextBox();
            this.tbx_ARCGIS_APIKey = new System.Windows.Forms.TextBox();
            this.tct_Settings.SuspendLayout();
            this.tpg_Application.SuspendLayout();
            this.gbx_AppSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Browse_Startup_Folder)).BeginInit();
            this.tpg_FileOptions.SuspendLayout();
            this.tpg_CustomCityLogic.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_CustomCityLogic)).BeginInit();
            this.tpg_CustomRules.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_CustomRules)).BeginInit();
            this.tpg_GeoNames.SuspendLayout();
            this.gbx_GeoNamesSettings.SuspendLayout();
            this.gbx_GeoNamesLanguageSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_ChoiceRadius)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_ChoiceOfferCount)).BeginInit();
            this.SuspendLayout();
            // 
            // tct_Settings
            // 
            this.tct_Settings.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tct_Settings.Controls.Add(this.tpg_Application);
            this.tct_Settings.Controls.Add(this.tpg_GeoNames);
            this.tct_Settings.Controls.Add(this.tpg_FileOptions);
            this.tct_Settings.Controls.Add(this.tpg_CustomCityLogic);
            this.tct_Settings.Controls.Add(this.tpg_CustomRules);
            this.tct_Settings.ImageList = this.igl_Settings;
            this.tct_Settings.Location = new System.Drawing.Point(16, 12);
            this.tct_Settings.Margin = new System.Windows.Forms.Padding(2);
            this.tct_Settings.Name = "tct_Settings";
            this.tct_Settings.SelectedIndex = 0;
            this.tct_Settings.Size = new System.Drawing.Size(906, 511);
            this.tct_Settings.TabIndex = 0;
            // 
            // tpg_Application
            // 
            this.tpg_Application.AutoScroll = true;
            this.tpg_Application.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tpg_Application.Controls.Add(this.gbx_AppSettings);
            this.tpg_Application.ImageKey = "Settings.png";
            this.tpg_Application.Location = new System.Drawing.Point(4, 23);
            this.tpg_Application.Margin = new System.Windows.Forms.Padding(2);
            this.tpg_Application.Name = "tpg_Application";
            this.tpg_Application.Padding = new System.Windows.Forms.Padding(2);
            this.tpg_Application.Size = new System.Drawing.Size(898, 484);
            this.tpg_Application.TabIndex = 0;
            this.tpg_Application.Text = "tpg_Application";
            // 
            // gbx_AppSettings
            // 
            this.gbx_AppSettings.Controls.Add(this.ckb_UseDarkMode);
            this.gbx_AppSettings.Controls.Add(this.ckb_UpdateCheckPreRelease);
            this.gbx_AppSettings.Controls.Add(this.lbl_Metres_Abbr);
            this.gbx_AppSettings.Controls.Add(this.lbl_Feet_Abbr);
            this.gbx_AppSettings.Controls.Add(this.ckb_UseImperialNotMetric);
            this.gbx_AppSettings.Controls.Add(this.ckb_RemoveGeoDataRemovesTimeOffset);
            this.gbx_AppSettings.Controls.Add(this.ckb_ResetMapToZero);
            this.gbx_AppSettings.Controls.Add(this.cbx_Language);
            this.gbx_AppSettings.Controls.Add(this.lbl_Language);
            this.gbx_AppSettings.Controls.Add(this.pbx_Browse_Startup_Folder);
            this.gbx_AppSettings.Controls.Add(this.lbl_Startup_Folder);
            this.gbx_AppSettings.Controls.Add(this.tbx_Startup_Folder);
            this.gbx_AppSettings.Location = new System.Drawing.Point(16, 20);
            this.gbx_AppSettings.Name = "gbx_AppSettings";
            this.gbx_AppSettings.Size = new System.Drawing.Size(852, 219);
            this.gbx_AppSettings.TabIndex = 32;
            this.gbx_AppSettings.TabStop = false;
            this.gbx_AppSettings.Text = "gbx_AppSettings";
            // 
            // ckb_UseDarkMode
            // 
            this.ckb_UseDarkMode.AutoSize = true;
            this.ckb_UseDarkMode.Location = new System.Drawing.Point(15, 169);
            this.ckb_UseDarkMode.Name = "ckb_UseDarkMode";
            this.ckb_UseDarkMode.Size = new System.Drawing.Size(119, 17);
            this.ckb_UseDarkMode.TabIndex = 54;
            this.ckb_UseDarkMode.Text = "ckb_UseDarkMode";
            this.ckb_UseDarkMode.UseVisualStyleBackColor = true;
            this.ckb_UseDarkMode.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_UpdateCheckPreRelease
            // 
            this.ckb_UpdateCheckPreRelease.AutoSize = true;
            this.ckb_UpdateCheckPreRelease.Location = new System.Drawing.Point(15, 146);
            this.ckb_UpdateCheckPreRelease.Name = "ckb_UpdateCheckPreRelease";
            this.ckb_UpdateCheckPreRelease.Size = new System.Drawing.Size(171, 17);
            this.ckb_UpdateCheckPreRelease.TabIndex = 53;
            this.ckb_UpdateCheckPreRelease.Text = "ckb_UpdateCheckPreRelease";
            this.ckb_UpdateCheckPreRelease.UseVisualStyleBackColor = true;
            this.ckb_UpdateCheckPreRelease.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // lbl_Metres_Abbr
            // 
            this.lbl_Metres_Abbr.AutoSize = true;
            this.lbl_Metres_Abbr.Location = new System.Drawing.Point(331, 126);
            this.lbl_Metres_Abbr.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Metres_Abbr.Name = "lbl_Metres_Abbr";
            this.lbl_Metres_Abbr.Size = new System.Drawing.Size(83, 13);
            this.lbl_Metres_Abbr.TabIndex = 52;
            this.lbl_Metres_Abbr.Text = "lbl_Metres_Abbr";
            this.lbl_Metres_Abbr.Visible = false;
            // 
            // lbl_Feet_Abbr
            // 
            this.lbl_Feet_Abbr.AutoSize = true;
            this.lbl_Feet_Abbr.Location = new System.Drawing.Point(204, 126);
            this.lbl_Feet_Abbr.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Feet_Abbr.Name = "lbl_Feet_Abbr";
            this.lbl_Feet_Abbr.Size = new System.Drawing.Size(72, 13);
            this.lbl_Feet_Abbr.TabIndex = 51;
            this.lbl_Feet_Abbr.Text = "lbl_Feet_Abbr";
            this.lbl_Feet_Abbr.Visible = false;
            // 
            // ckb_UseImperialNotMetric
            // 
            this.ckb_UseImperialNotMetric.AutoSize = true;
            this.ckb_UseImperialNotMetric.Location = new System.Drawing.Point(15, 124);
            this.ckb_UseImperialNotMetric.Name = "ckb_UseImperialNotMetric";
            this.ckb_UseImperialNotMetric.Size = new System.Drawing.Size(151, 17);
            this.ckb_UseImperialNotMetric.TabIndex = 19;
            this.ckb_UseImperialNotMetric.Text = "ckb_UseImperialNotMetric";
            this.ckb_UseImperialNotMetric.UseVisualStyleBackColor = true;
            this.ckb_UseImperialNotMetric.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_RemoveGeoDataRemovesTimeOffset
            // 
            this.ckb_RemoveGeoDataRemovesTimeOffset.AutoSize = true;
            this.ckb_RemoveGeoDataRemovesTimeOffset.Location = new System.Drawing.Point(15, 102);
            this.ckb_RemoveGeoDataRemovesTimeOffset.Name = "ckb_RemoveGeoDataRemovesTimeOffset";
            this.ckb_RemoveGeoDataRemovesTimeOffset.Size = new System.Drawing.Size(229, 17);
            this.ckb_RemoveGeoDataRemovesTimeOffset.TabIndex = 18;
            this.ckb_RemoveGeoDataRemovesTimeOffset.Text = "ckb_RemoveGeoDataRemovesTimeOffset";
            this.ckb_RemoveGeoDataRemovesTimeOffset.UseVisualStyleBackColor = true;
            this.ckb_RemoveGeoDataRemovesTimeOffset.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_ResetMapToZero
            // 
            this.ckb_ResetMapToZero.AutoSize = true;
            this.ckb_ResetMapToZero.Location = new System.Drawing.Point(15, 80);
            this.ckb_ResetMapToZero.Name = "ckb_ResetMapToZero";
            this.ckb_ResetMapToZero.Size = new System.Drawing.Size(134, 17);
            this.ckb_ResetMapToZero.TabIndex = 17;
            this.ckb_ResetMapToZero.Text = "ckb_ResetMapToZero";
            this.ckb_ResetMapToZero.UseVisualStyleBackColor = true;
            this.ckb_ResetMapToZero.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // cbx_Language
            // 
            this.cbx_Language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_Language.FormattingEnabled = true;
            this.cbx_Language.Location = new System.Drawing.Point(185, 50);
            this.cbx_Language.Name = "cbx_Language";
            this.cbx_Language.Size = new System.Drawing.Size(155, 21);
            this.cbx_Language.TabIndex = 16;
            this.cbx_Language.SelectedValueChanged += new System.EventHandler(this.Any_cbx_TextChanged);
            // 
            // lbl_Language
            // 
            this.lbl_Language.AutoSize = true;
            this.lbl_Language.Location = new System.Drawing.Point(12, 53);
            this.lbl_Language.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Language.Name = "lbl_Language";
            this.lbl_Language.Size = new System.Drawing.Size(71, 13);
            this.lbl_Language.TabIndex = 15;
            this.lbl_Language.Text = "lbl_Language";
            // 
            // pbx_Browse_Startup_Folder
            // 
            this.pbx_Browse_Startup_Folder.Image = ((System.Drawing.Image)(resources.GetObject("pbx_Browse_Startup_Folder.Image")));
            this.pbx_Browse_Startup_Folder.Location = new System.Drawing.Point(519, 28);
            this.pbx_Browse_Startup_Folder.Margin = new System.Windows.Forms.Padding(2);
            this.pbx_Browse_Startup_Folder.Name = "pbx_Browse_Startup_Folder";
            this.pbx_Browse_Startup_Folder.Size = new System.Drawing.Size(16, 16);
            this.pbx_Browse_Startup_Folder.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbx_Browse_Startup_Folder.TabIndex = 14;
            this.pbx_Browse_Startup_Folder.TabStop = false;
            this.pbx_Browse_Startup_Folder.Click += new System.EventHandler(this.Pbx_Browse_Startup_Folder_Click);
            // 
            // lbl_Startup_Folder
            // 
            this.lbl_Startup_Folder.AutoSize = true;
            this.lbl_Startup_Folder.Location = new System.Drawing.Point(12, 28);
            this.lbl_Startup_Folder.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Startup_Folder.Name = "lbl_Startup_Folder";
            this.lbl_Startup_Folder.Size = new System.Drawing.Size(92, 13);
            this.lbl_Startup_Folder.TabIndex = 13;
            this.lbl_Startup_Folder.Text = "lbl_Startup_Folder";
            // 
            // tbx_Startup_Folder
            // 
            this.tbx_Startup_Folder.Location = new System.Drawing.Point(128, 24);
            this.tbx_Startup_Folder.Margin = new System.Windows.Forms.Padding(2);
            this.tbx_Startup_Folder.Name = "tbx_Startup_Folder";
            this.tbx_Startup_Folder.ReadOnly = true;
            this.tbx_Startup_Folder.Size = new System.Drawing.Size(372, 20);
            this.tbx_Startup_Folder.TabIndex = 12;
            this.tbx_Startup_Folder.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // tpg_FileOptions
            // 
            this.tpg_FileOptions.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tpg_FileOptions.Controls.Add(this.ckb_ResetFileDateToCreated);
            this.tpg_FileOptions.Controls.Add(this.ckb_ProcessOriginalFile);
            this.tpg_FileOptions.Controls.Add(this.ckb_OverwriteOriginal);
            this.tpg_FileOptions.Controls.Add(this.ckb_AddXMPSideCar);
            this.tpg_FileOptions.Controls.Add(this.lbx_fileExtensions);
            this.tpg_FileOptions.ImageKey = "SettingsFile.png";
            this.tpg_FileOptions.Location = new System.Drawing.Point(4, 23);
            this.tpg_FileOptions.Margin = new System.Windows.Forms.Padding(2);
            this.tpg_FileOptions.Name = "tpg_FileOptions";
            this.tpg_FileOptions.Padding = new System.Windows.Forms.Padding(2);
            this.tpg_FileOptions.Size = new System.Drawing.Size(898, 484);
            this.tpg_FileOptions.TabIndex = 1;
            this.tpg_FileOptions.Text = "tpg_FileOptions";
            this.tpg_FileOptions.Enter += new System.EventHandler(this.tpg_FileOptions_Enter);
            // 
            // ckb_ResetFileDateToCreated
            // 
            this.ckb_ResetFileDateToCreated.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_ResetFileDateToCreated.Location = new System.Drawing.Point(567, 62);
            this.ckb_ResetFileDateToCreated.MinimumSize = new System.Drawing.Size(100, 20);
            this.ckb_ResetFileDateToCreated.Name = "ckb_ResetFileDateToCreated";
            this.ckb_ResetFileDateToCreated.Size = new System.Drawing.Size(264, 62);
            this.ckb_ResetFileDateToCreated.TabIndex = 4;
            this.ckb_ResetFileDateToCreated.Text = "ckb_ResetFileDateToCreated";
            this.ckb_ResetFileDateToCreated.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_ResetFileDateToCreated.UseVisualStyleBackColor = true;
            this.ckb_ResetFileDateToCreated.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_ProcessOriginalFile
            // 
            this.ckb_ProcessOriginalFile.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_ProcessOriginalFile.Location = new System.Drawing.Point(545, 18);
            this.ckb_ProcessOriginalFile.MinimumSize = new System.Drawing.Size(100, 20);
            this.ckb_ProcessOriginalFile.Name = "ckb_ProcessOriginalFile";
            this.ckb_ProcessOriginalFile.Size = new System.Drawing.Size(286, 38);
            this.ckb_ProcessOriginalFile.TabIndex = 3;
            this.ckb_ProcessOriginalFile.Text = "ckb_ProcessOriginalFile";
            this.ckb_ProcessOriginalFile.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_ProcessOriginalFile.UseVisualStyleBackColor = true;
            this.ckb_ProcessOriginalFile.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_OverwriteOriginal
            // 
            this.ckb_OverwriteOriginal.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_OverwriteOriginal.Location = new System.Drawing.Point(545, 190);
            this.ckb_OverwriteOriginal.MinimumSize = new System.Drawing.Size(100, 20);
            this.ckb_OverwriteOriginal.Name = "ckb_OverwriteOriginal";
            this.ckb_OverwriteOriginal.Size = new System.Drawing.Size(286, 38);
            this.ckb_OverwriteOriginal.TabIndex = 1;
            this.ckb_OverwriteOriginal.Text = "ckb_OverwriteOriginal";
            this.ckb_OverwriteOriginal.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_OverwriteOriginal.UseVisualStyleBackColor = true;
            this.ckb_OverwriteOriginal.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_AddXMPSideCar
            // 
            this.ckb_AddXMPSideCar.CheckAlign = System.Drawing.ContentAlignment.TopLeft;
            this.ckb_AddXMPSideCar.Location = new System.Drawing.Point(545, 130);
            this.ckb_AddXMPSideCar.MinimumSize = new System.Drawing.Size(100, 20);
            this.ckb_AddXMPSideCar.Name = "ckb_AddXMPSideCar";
            this.ckb_AddXMPSideCar.Size = new System.Drawing.Size(286, 38);
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
            this.lbx_fileExtensions.Size = new System.Drawing.Size(512, 368);
            this.lbx_fileExtensions.TabIndex = 0;
            this.lbx_fileExtensions.SelectedIndexChanged += new System.EventHandler(this.Lbx_fileExtensions_SelectedIndexChanged);
            // 
            // tpg_CustomCityLogic
            // 
            this.tpg_CustomCityLogic.Controls.Add(this.rbx_CustomCityLogicExplanationBold);
            this.tpg_CustomCityLogic.Controls.Add(this.btn_ResetToDefaults);
            this.tpg_CustomCityLogic.Controls.Add(this.rbx_CustomCityLogicExplanation);
            this.tpg_CustomCityLogic.Controls.Add(this.dgv_CustomCityLogic);
            this.tpg_CustomCityLogic.ImageKey = "CustomAction.png";
            this.tpg_CustomCityLogic.Location = new System.Drawing.Point(4, 23);
            this.tpg_CustomCityLogic.Name = "tpg_CustomCityLogic";
            this.tpg_CustomCityLogic.Padding = new System.Windows.Forms.Padding(3);
            this.tpg_CustomCityLogic.Size = new System.Drawing.Size(898, 484);
            this.tpg_CustomCityLogic.TabIndex = 3;
            this.tpg_CustomCityLogic.Text = "tpg_CustomCityLogic";
            this.tpg_CustomCityLogic.UseVisualStyleBackColor = true;
            // 
            // rbx_CustomCityLogicExplanationBold
            // 
            this.rbx_CustomCityLogicExplanationBold.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rbx_CustomCityLogicExplanationBold.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbx_CustomCityLogicExplanationBold.Location = new System.Drawing.Point(6, 144);
            this.rbx_CustomCityLogicExplanationBold.Name = "rbx_CustomCityLogicExplanationBold";
            this.rbx_CustomCityLogicExplanationBold.ReadOnly = true;
            this.rbx_CustomCityLogicExplanationBold.Size = new System.Drawing.Size(885, 40);
            this.rbx_CustomCityLogicExplanationBold.TabIndex = 4;
            this.rbx_CustomCityLogicExplanationBold.Text = "";
            // 
            // btn_ResetToDefaults
            // 
            this.btn_ResetToDefaults.Location = new System.Drawing.Point(771, 613);
            this.btn_ResetToDefaults.Name = "btn_ResetToDefaults";
            this.btn_ResetToDefaults.Size = new System.Drawing.Size(106, 23);
            this.btn_ResetToDefaults.TabIndex = 3;
            this.btn_ResetToDefaults.Text = "btn_ResetToDefaults";
            this.btn_ResetToDefaults.UseVisualStyleBackColor = true;
            this.btn_ResetToDefaults.Click += new System.EventHandler(this.btn_ResetToDefaults_Click);
            // 
            // rbx_CustomCityLogicExplanation
            // 
            this.rbx_CustomCityLogicExplanation.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rbx_CustomCityLogicExplanation.Location = new System.Drawing.Point(7, 6);
            this.rbx_CustomCityLogicExplanation.Name = "rbx_CustomCityLogicExplanation";
            this.rbx_CustomCityLogicExplanation.ReadOnly = true;
            this.rbx_CustomCityLogicExplanation.Size = new System.Drawing.Size(885, 132);
            this.rbx_CustomCityLogicExplanation.TabIndex = 2;
            this.rbx_CustomCityLogicExplanation.Text = "";
            // 
            // dgv_CustomCityLogic
            // 
            this.dgv_CustomCityLogic.AllowUserToAddRows = false;
            this.dgv_CustomCityLogic.AllowUserToDeleteRows = false;
            this.dgv_CustomCityLogic.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_CustomCityLogic.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dgv_CustomCityLogic.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_CustomCityLogic.Location = new System.Drawing.Point(7, 190);
            this.dgv_CustomCityLogic.Name = "dgv_CustomCityLogic";
            this.dgv_CustomCityLogic.Size = new System.Drawing.Size(885, 405);
            this.dgv_CustomCityLogic.TabIndex = 1;
            // 
            // tpg_CustomRules
            // 
            this.tpg_CustomRules.Controls.Add(this.rbx_CustomRulesExplanationBold);
            this.tpg_CustomRules.Controls.Add(this.ckb_StopProcessingRules);
            this.tpg_CustomRules.Controls.Add(this.ckb_IncludePredeterminedCountries);
            this.tpg_CustomRules.Controls.Add(this.rbx_CustomRulesExplanation);
            this.tpg_CustomRules.Controls.Add(this.dgv_CustomRules);
            this.tpg_CustomRules.ImageKey = "CustomAction.png";
            this.tpg_CustomRules.Location = new System.Drawing.Point(4, 23);
            this.tpg_CustomRules.Name = "tpg_CustomRules";
            this.tpg_CustomRules.Padding = new System.Windows.Forms.Padding(3);
            this.tpg_CustomRules.Size = new System.Drawing.Size(898, 484);
            this.tpg_CustomRules.TabIndex = 2;
            this.tpg_CustomRules.Text = "tpg_CustomRules";
            this.tpg_CustomRules.UseVisualStyleBackColor = true;
            // 
            // rbx_CustomRulesExplanationBold
            // 
            this.rbx_CustomRulesExplanationBold.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rbx_CustomRulesExplanationBold.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbx_CustomRulesExplanationBold.Location = new System.Drawing.Point(7, 144);
            this.rbx_CustomRulesExplanationBold.Name = "rbx_CustomRulesExplanationBold";
            this.rbx_CustomRulesExplanationBold.ReadOnly = true;
            this.rbx_CustomRulesExplanationBold.Size = new System.Drawing.Size(885, 40);
            this.rbx_CustomRulesExplanationBold.TabIndex = 18;
            this.rbx_CustomRulesExplanationBold.Text = "";
            // 
            // ckb_StopProcessingRules
            // 
            this.ckb_StopProcessingRules.AutoSize = true;
            this.ckb_StopProcessingRules.Location = new System.Drawing.Point(535, 190);
            this.ckb_StopProcessingRules.Name = "ckb_StopProcessingRules";
            this.ckb_StopProcessingRules.Size = new System.Drawing.Size(151, 17);
            this.ckb_StopProcessingRules.TabIndex = 17;
            this.ckb_StopProcessingRules.Text = "ckb_StopProcessingRules";
            this.ckb_StopProcessingRules.UseVisualStyleBackColor = true;
            this.ckb_StopProcessingRules.CheckedChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_IncludePredeterminedCountries
            // 
            this.ckb_IncludePredeterminedCountries.AutoSize = true;
            this.ckb_IncludePredeterminedCountries.Location = new System.Drawing.Point(7, 190);
            this.ckb_IncludePredeterminedCountries.Name = "ckb_IncludePredeterminedCountries";
            this.ckb_IncludePredeterminedCountries.Size = new System.Drawing.Size(197, 17);
            this.ckb_IncludePredeterminedCountries.TabIndex = 16;
            this.ckb_IncludePredeterminedCountries.Text = "ckb_IncludePredeterminedCountries";
            this.ckb_IncludePredeterminedCountries.UseVisualStyleBackColor = true;
            this.ckb_IncludePredeterminedCountries.CheckedChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // rbx_CustomRulesExplanation
            // 
            this.rbx_CustomRulesExplanation.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rbx_CustomRulesExplanation.Location = new System.Drawing.Point(7, 6);
            this.rbx_CustomRulesExplanation.Name = "rbx_CustomRulesExplanation";
            this.rbx_CustomRulesExplanation.ReadOnly = true;
            this.rbx_CustomRulesExplanation.Size = new System.Drawing.Size(885, 132);
            this.rbx_CustomRulesExplanation.TabIndex = 1;
            this.rbx_CustomRulesExplanation.Text = "";
            this.rbx_CustomRulesExplanation.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.rbx_CustomRulesExplanation_LinkClicked);
            // 
            // dgv_CustomRules
            // 
            this.dgv_CustomRules.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_CustomRules.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dgv_CustomRules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_CustomRules.Location = new System.Drawing.Point(7, 220);
            this.dgv_CustomRules.Name = "dgv_CustomRules";
            this.dgv_CustomRules.Size = new System.Drawing.Size(885, 418);
            this.dgv_CustomRules.TabIndex = 0;
            this.dgv_CustomRules.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dgv_CustomRules_DataError);
            this.dgv_CustomRules.RowValidating += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.dgv_CustomRules_RowValidating);
            // 
            // igl_Settings
            // 
            this.igl_Settings.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("igl_Settings.ImageStream")));
            this.igl_Settings.TransparentColor = System.Drawing.Color.Transparent;
            this.igl_Settings.Images.SetKeyName(0, "LibrarySettings.png");
            this.igl_Settings.Images.SetKeyName(1, "Settings.png");
            this.igl_Settings.Images.SetKeyName(2, "SettingsFile.png");
            this.igl_Settings.Images.SetKeyName(3, "CustomAction.png");
            this.igl_Settings.Images.SetKeyName(4, "PublishOnDemand.png");
            // 
            // btn_OK
            // 
            this.btn_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btn_OK.Location = new System.Drawing.Point(791, 537);
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
            this.btn_Cancel.Location = new System.Drawing.Point(861, 537);
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
            // tpg_GeoNames
            // 
            this.tpg_GeoNames.Controls.Add(this.gbx_GeoNamesSettings);
            this.tpg_GeoNames.ImageKey = "PublishOnDemand.png";
            this.tpg_GeoNames.Location = new System.Drawing.Point(4, 23);
            this.tpg_GeoNames.Name = "tpg_GeoNames";
            this.tpg_GeoNames.Padding = new System.Windows.Forms.Padding(3);
            this.tpg_GeoNames.Size = new System.Drawing.Size(898, 484);
            this.tpg_GeoNames.TabIndex = 4;
            this.tpg_GeoNames.Text = "tpg_GeoNames";
            this.tpg_GeoNames.UseVisualStyleBackColor = true;
            // 
            // gbx_GeoNamesSettings
            // 
            this.gbx_GeoNamesSettings.Controls.Add(this.lbl_Miles);
            this.gbx_GeoNamesSettings.Controls.Add(this.ckb_PopulatedPlacesOnly);
            this.gbx_GeoNamesSettings.Controls.Add(this.gbx_GeoNamesLanguageSettings);
            this.gbx_GeoNamesSettings.Controls.Add(this.ckb_ReplaceBlankToponyms);
            this.gbx_GeoNamesSettings.Controls.Add(this.tbx_ReplaceBlankToponyms);
            this.gbx_GeoNamesSettings.Controls.Add(this.lbl_ChoiceRadius);
            this.gbx_GeoNamesSettings.Controls.Add(this.lbl_ChoiceOfferCount);
            this.gbx_GeoNamesSettings.Controls.Add(this.nud_ChoiceRadius);
            this.gbx_GeoNamesSettings.Controls.Add(this.nud_ChoiceOfferCount);
            this.gbx_GeoNamesSettings.Controls.Add(this.rbx_Register_ArcGIS);
            this.gbx_GeoNamesSettings.Controls.Add(this.rbx_Register_GeoNames);
            this.gbx_GeoNamesSettings.Controls.Add(this.lbl_GeoNames_Pwd);
            this.gbx_GeoNamesSettings.Controls.Add(this.lbl_GeoNames_UserName);
            this.gbx_GeoNamesSettings.Controls.Add(this.lbl_ARCGIS_APIKey);
            this.gbx_GeoNamesSettings.Controls.Add(this.tbx_GeoNames_Pwd);
            this.gbx_GeoNamesSettings.Controls.Add(this.tbx_GeoNames_UserName);
            this.gbx_GeoNamesSettings.Controls.Add(this.tbx_ARCGIS_APIKey);
            this.gbx_GeoNamesSettings.Location = new System.Drawing.Point(16, 20);
            this.gbx_GeoNamesSettings.Name = "gbx_GeoNamesSettings";
            this.gbx_GeoNamesSettings.Size = new System.Drawing.Size(852, 428);
            this.gbx_GeoNamesSettings.TabIndex = 34;
            this.gbx_GeoNamesSettings.TabStop = false;
            this.gbx_GeoNamesSettings.Text = "gbx_GeoNamesSettings";
            // 
            // lbl_Miles
            // 
            this.lbl_Miles.AutoSize = true;
            this.lbl_Miles.Location = new System.Drawing.Point(467, 342);
            this.lbl_Miles.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Miles.Name = "lbl_Miles";
            this.lbl_Miles.Size = new System.Drawing.Size(47, 13);
            this.lbl_Miles.TabIndex = 50;
            this.lbl_Miles.Text = "lbl_Miles";
            // 
            // ckb_PopulatedPlacesOnly
            // 
            this.ckb_PopulatedPlacesOnly.AutoSize = true;
            this.ckb_PopulatedPlacesOnly.Location = new System.Drawing.Point(12, 397);
            this.ckb_PopulatedPlacesOnly.Name = "ckb_PopulatedPlacesOnly";
            this.ckb_PopulatedPlacesOnly.Size = new System.Drawing.Size(151, 17);
            this.ckb_PopulatedPlacesOnly.TabIndex = 49;
            this.ckb_PopulatedPlacesOnly.Text = "ckb_PopulatedPlacesOnly";
            this.ckb_PopulatedPlacesOnly.UseVisualStyleBackColor = true;
            this.ckb_PopulatedPlacesOnly.CheckedChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // gbx_GeoNamesLanguageSettings
            // 
            this.gbx_GeoNamesLanguageSettings.Controls.Add(this.cbx_TryUseGeoNamesLanguage);
            this.gbx_GeoNamesLanguageSettings.Controls.Add(this.rbt_TryUseGeoNamesLanguage);
            this.gbx_GeoNamesLanguageSettings.Controls.Add(this.rbt_UseGeoNamesLocalLanguage);
            this.gbx_GeoNamesLanguageSettings.Location = new System.Drawing.Point(15, 215);
            this.gbx_GeoNamesLanguageSettings.Name = "gbx_GeoNamesLanguageSettings";
            this.gbx_GeoNamesLanguageSettings.Size = new System.Drawing.Size(548, 117);
            this.gbx_GeoNamesLanguageSettings.TabIndex = 48;
            this.gbx_GeoNamesLanguageSettings.TabStop = false;
            this.gbx_GeoNamesLanguageSettings.Text = "gbx_GeoNamesLanguageSettings";
            // 
            // cbx_TryUseGeoNamesLanguage
            // 
            this.cbx_TryUseGeoNamesLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_TryUseGeoNamesLanguage.FormattingEnabled = true;
            this.cbx_TryUseGeoNamesLanguage.Location = new System.Drawing.Point(34, 67);
            this.cbx_TryUseGeoNamesLanguage.Name = "cbx_TryUseGeoNamesLanguage";
            this.cbx_TryUseGeoNamesLanguage.Size = new System.Drawing.Size(277, 21);
            this.cbx_TryUseGeoNamesLanguage.TabIndex = 47;
            this.cbx_TryUseGeoNamesLanguage.SelectedIndexChanged += new System.EventHandler(this.Any_cbx_TextChanged);
            // 
            // rbt_TryUseGeoNamesLanguage
            // 
            this.rbt_TryUseGeoNamesLanguage.AutoSize = true;
            this.rbt_TryUseGeoNamesLanguage.Location = new System.Drawing.Point(7, 44);
            this.rbt_TryUseGeoNamesLanguage.Name = "rbt_TryUseGeoNamesLanguage";
            this.rbt_TryUseGeoNamesLanguage.Size = new System.Drawing.Size(178, 17);
            this.rbt_TryUseGeoNamesLanguage.TabIndex = 1;
            this.rbt_TryUseGeoNamesLanguage.TabStop = true;
            this.rbt_TryUseGeoNamesLanguage.Text = "rbt_TryUseGeoNamesLanguage";
            this.rbt_TryUseGeoNamesLanguage.UseVisualStyleBackColor = true;
            this.rbt_TryUseGeoNamesLanguage.CheckedChanged += new System.EventHandler(this.Any_rbt_CheckedChanged);
            // 
            // rbt_UseGeoNamesLocalLanguage
            // 
            this.rbt_UseGeoNamesLocalLanguage.AutoSize = true;
            this.rbt_UseGeoNamesLocalLanguage.Location = new System.Drawing.Point(7, 20);
            this.rbt_UseGeoNamesLocalLanguage.Name = "rbt_UseGeoNamesLocalLanguage";
            this.rbt_UseGeoNamesLocalLanguage.Size = new System.Drawing.Size(189, 17);
            this.rbt_UseGeoNamesLocalLanguage.TabIndex = 0;
            this.rbt_UseGeoNamesLocalLanguage.TabStop = true;
            this.rbt_UseGeoNamesLocalLanguage.Text = "rbt_UseGeoNamesLocalLanguage";
            this.rbt_UseGeoNamesLocalLanguage.UseVisualStyleBackColor = true;
            this.rbt_UseGeoNamesLocalLanguage.CheckedChanged += new System.EventHandler(this.Any_rbt_CheckedChanged);
            // 
            // ckb_ReplaceBlankToponyms
            // 
            this.ckb_ReplaceBlankToponyms.AutoSize = true;
            this.ckb_ReplaceBlankToponyms.Location = new System.Drawing.Point(12, 374);
            this.ckb_ReplaceBlankToponyms.Name = "ckb_ReplaceBlankToponyms";
            this.ckb_ReplaceBlankToponyms.Size = new System.Drawing.Size(166, 17);
            this.ckb_ReplaceBlankToponyms.TabIndex = 45;
            this.ckb_ReplaceBlankToponyms.Text = "ckb_ReplaceBlankToponyms";
            this.ckb_ReplaceBlankToponyms.UseVisualStyleBackColor = true;
            this.ckb_ReplaceBlankToponyms.CheckedChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // tbx_ReplaceBlankToponyms
            // 
            this.tbx_ReplaceBlankToponyms.Location = new System.Drawing.Point(408, 371);
            this.tbx_ReplaceBlankToponyms.Margin = new System.Windows.Forms.Padding(2);
            this.tbx_ReplaceBlankToponyms.Name = "tbx_ReplaceBlankToponyms";
            this.tbx_ReplaceBlankToponyms.Size = new System.Drawing.Size(155, 20);
            this.tbx_ReplaceBlankToponyms.TabIndex = 44;
            this.tbx_ReplaceBlankToponyms.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // lbl_ChoiceRadius
            // 
            this.lbl_ChoiceRadius.AutoSize = true;
            this.lbl_ChoiceRadius.Location = new System.Drawing.Point(277, 340);
            this.lbl_ChoiceRadius.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_ChoiceRadius.Name = "lbl_ChoiceRadius";
            this.lbl_ChoiceRadius.Size = new System.Drawing.Size(89, 13);
            this.lbl_ChoiceRadius.TabIndex = 43;
            this.lbl_ChoiceRadius.Text = "lbl_ChoiceRadius";
            // 
            // lbl_ChoiceOfferCount
            // 
            this.lbl_ChoiceOfferCount.AutoSize = true;
            this.lbl_ChoiceOfferCount.Location = new System.Drawing.Point(12, 340);
            this.lbl_ChoiceOfferCount.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_ChoiceOfferCount.Name = "lbl_ChoiceOfferCount";
            this.lbl_ChoiceOfferCount.Size = new System.Drawing.Size(107, 13);
            this.lbl_ChoiceOfferCount.TabIndex = 42;
            this.lbl_ChoiceOfferCount.Text = "lbl_ChoiceOfferCount";
            // 
            // nud_ChoiceRadius
            // 
            this.nud_ChoiceRadius.Location = new System.Drawing.Point(410, 340);
            this.nud_ChoiceRadius.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nud_ChoiceRadius.Name = "nud_ChoiceRadius";
            this.nud_ChoiceRadius.Size = new System.Drawing.Size(43, 20);
            this.nud_ChoiceRadius.TabIndex = 41;
            this.nud_ChoiceRadius.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nud_ChoiceRadius.ValueChanged += new System.EventHandler(this.Any_nud_ValueChanged);
            this.nud_ChoiceRadius.Enter += new System.EventHandler(this.Any_nud_Enter);
            this.nud_ChoiceRadius.Leave += new System.EventHandler(this.Any_nud_Leave);
            // 
            // nud_ChoiceOfferCount
            // 
            this.nud_ChoiceOfferCount.Location = new System.Drawing.Point(207, 338);
            this.nud_ChoiceOfferCount.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nud_ChoiceOfferCount.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nud_ChoiceOfferCount.Name = "nud_ChoiceOfferCount";
            this.nud_ChoiceOfferCount.Size = new System.Drawing.Size(43, 20);
            this.nud_ChoiceOfferCount.TabIndex = 40;
            this.nud_ChoiceOfferCount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nud_ChoiceOfferCount.ValueChanged += new System.EventHandler(this.Any_nud_ValueChanged);
            this.nud_ChoiceOfferCount.Enter += new System.EventHandler(this.Any_nud_Enter);
            this.nud_ChoiceOfferCount.Leave += new System.EventHandler(this.Any_nud_Leave);
            // 
            // rbx_Register_ArcGIS
            // 
            this.rbx_Register_ArcGIS.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rbx_Register_ArcGIS.Location = new System.Drawing.Point(12, 19);
            this.rbx_Register_ArcGIS.Name = "rbx_Register_ArcGIS";
            this.rbx_Register_ArcGIS.ReadOnly = true;
            this.rbx_Register_ArcGIS.Size = new System.Drawing.Size(551, 42);
            this.rbx_Register_ArcGIS.TabIndex = 38;
            this.rbx_Register_ArcGIS.Text = "rbx_Register_ArcGIS";
            // 
            // rbx_Register_GeoNames
            // 
            this.rbx_Register_GeoNames.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rbx_Register_GeoNames.Location = new System.Drawing.Point(12, 100);
            this.rbx_Register_GeoNames.Name = "rbx_Register_GeoNames";
            this.rbx_Register_GeoNames.ReadOnly = true;
            this.rbx_Register_GeoNames.Size = new System.Drawing.Size(551, 42);
            this.rbx_Register_GeoNames.TabIndex = 39;
            this.rbx_Register_GeoNames.Text = "rbx_Register_GeoNames";
            // 
            // lbl_GeoNames_Pwd
            // 
            this.lbl_GeoNames_Pwd.AutoSize = true;
            this.lbl_GeoNames_Pwd.Location = new System.Drawing.Point(12, 181);
            this.lbl_GeoNames_Pwd.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_GeoNames_Pwd.Name = "lbl_GeoNames_Pwd";
            this.lbl_GeoNames_Pwd.Size = new System.Drawing.Size(103, 13);
            this.lbl_GeoNames_Pwd.TabIndex = 35;
            this.lbl_GeoNames_Pwd.Text = "lbl_GeoNames_Pwd";
            // 
            // lbl_GeoNames_UserName
            // 
            this.lbl_GeoNames_UserName.AutoSize = true;
            this.lbl_GeoNames_UserName.Location = new System.Drawing.Point(12, 150);
            this.lbl_GeoNames_UserName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_GeoNames_UserName.Name = "lbl_GeoNames_UserName";
            this.lbl_GeoNames_UserName.Size = new System.Drawing.Size(132, 13);
            this.lbl_GeoNames_UserName.TabIndex = 36;
            this.lbl_GeoNames_UserName.Text = "lbl_GeoNames_UserName";
            // 
            // lbl_ARCGIS_APIKey
            // 
            this.lbl_ARCGIS_APIKey.AutoSize = true;
            this.lbl_ARCGIS_APIKey.Location = new System.Drawing.Point(12, 69);
            this.lbl_ARCGIS_APIKey.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_ARCGIS_APIKey.Name = "lbl_ARCGIS_APIKey";
            this.lbl_ARCGIS_APIKey.Size = new System.Drawing.Size(104, 13);
            this.lbl_ARCGIS_APIKey.TabIndex = 37;
            this.lbl_ARCGIS_APIKey.Text = "lbl_ARCGIS_APIKey";
            // 
            // tbx_GeoNames_Pwd
            // 
            this.tbx_GeoNames_Pwd.Location = new System.Drawing.Point(186, 178);
            this.tbx_GeoNames_Pwd.Margin = new System.Windows.Forms.Padding(2);
            this.tbx_GeoNames_Pwd.Name = "tbx_GeoNames_Pwd";
            this.tbx_GeoNames_Pwd.PasswordChar = '*';
            this.tbx_GeoNames_Pwd.Size = new System.Drawing.Size(155, 20);
            this.tbx_GeoNames_Pwd.TabIndex = 34;
            this.tbx_GeoNames_Pwd.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // tbx_GeoNames_UserName
            // 
            this.tbx_GeoNames_UserName.Location = new System.Drawing.Point(186, 147);
            this.tbx_GeoNames_UserName.Margin = new System.Windows.Forms.Padding(2);
            this.tbx_GeoNames_UserName.Name = "tbx_GeoNames_UserName";
            this.tbx_GeoNames_UserName.Size = new System.Drawing.Size(155, 20);
            this.tbx_GeoNames_UserName.TabIndex = 33;
            this.tbx_GeoNames_UserName.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // tbx_ARCGIS_APIKey
            // 
            this.tbx_ARCGIS_APIKey.Location = new System.Drawing.Point(128, 66);
            this.tbx_ARCGIS_APIKey.Margin = new System.Windows.Forms.Padding(2);
            this.tbx_ARCGIS_APIKey.Name = "tbx_ARCGIS_APIKey";
            this.tbx_ARCGIS_APIKey.PasswordChar = '*';
            this.tbx_ARCGIS_APIKey.Size = new System.Drawing.Size(372, 20);
            this.tbx_ARCGIS_APIKey.TabIndex = 32;
            this.tbx_ARCGIS_APIKey.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // FrmSettings
            // 
            this.AcceptButton = this.btn_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.btn_Cancel;
            this.ClientSize = new System.Drawing.Size(937, 568);
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
            this.gbx_AppSettings.ResumeLayout(false);
            this.gbx_AppSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Browse_Startup_Folder)).EndInit();
            this.tpg_FileOptions.ResumeLayout(false);
            this.tpg_CustomCityLogic.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_CustomCityLogic)).EndInit();
            this.tpg_CustomRules.ResumeLayout(false);
            this.tpg_CustomRules.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_CustomRules)).EndInit();
            this.tpg_GeoNames.ResumeLayout(false);
            this.gbx_GeoNamesSettings.ResumeLayout(false);
            this.gbx_GeoNamesSettings.PerformLayout();
            this.gbx_GeoNamesLanguageSettings.ResumeLayout(false);
            this.gbx_GeoNamesLanguageSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_ChoiceRadius)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_ChoiceOfferCount)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.TabPage tpg_Application;
        private System.Windows.Forms.TabPage tpg_FileOptions;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Cancel;
        private System.Windows.Forms.TabControl tct_Settings;
        private System.Windows.Forms.FolderBrowserDialog fbd_StartupFolder;
        private System.Windows.Forms.CheckBox ckb_OverwriteOriginal;
        private System.Windows.Forms.CheckBox ckb_AddXMPSideCar;
        private System.Windows.Forms.ListBox lbx_fileExtensions;
        private System.Windows.Forms.CheckBox ckb_ProcessOriginalFile;
        private System.Windows.Forms.CheckBox ckb_ResetFileDateToCreated;
        private System.Windows.Forms.ImageList igl_Settings;
        private System.Windows.Forms.TabPage tpg_CustomRules;
        private System.Windows.Forms.DataGridView dgv_CustomRules;
        private System.Windows.Forms.RichTextBox rbx_CustomRulesExplanation;
        private System.Windows.Forms.CheckBox ckb_StopProcessingRules;
        private System.Windows.Forms.CheckBox ckb_IncludePredeterminedCountries;
        private System.Windows.Forms.GroupBox gbx_AppSettings;
        private System.Windows.Forms.CheckBox ckb_RemoveGeoDataRemovesTimeOffset;
        private System.Windows.Forms.CheckBox ckb_ResetMapToZero;
        private System.Windows.Forms.ComboBox cbx_Language;
        private System.Windows.Forms.Label lbl_Language;
        private System.Windows.Forms.PictureBox pbx_Browse_Startup_Folder;
        private System.Windows.Forms.Label lbl_Startup_Folder;
        public System.Windows.Forms.TextBox tbx_Startup_Folder;
        private System.Windows.Forms.TabPage tpg_CustomCityLogic;
        private System.Windows.Forms.DataGridView dgv_CustomCityLogic;
        private System.Windows.Forms.RichTextBox rbx_CustomCityLogicExplanation;
        private System.Windows.Forms.Button btn_ResetToDefaults;
        private System.Windows.Forms.RichTextBox rbx_CustomCityLogicExplanationBold;
        private System.Windows.Forms.RichTextBox rbx_CustomRulesExplanationBold;
        private System.Windows.Forms.CheckBox ckb_UseImperialNotMetric;
        private System.Windows.Forms.Label lbl_Metres_Abbr;
        private System.Windows.Forms.Label lbl_Feet_Abbr;
        private System.Windows.Forms.CheckBox ckb_UpdateCheckPreRelease;
        private System.Windows.Forms.CheckBox ckb_UseDarkMode;
        private System.Windows.Forms.TabPage tpg_GeoNames;
        private System.Windows.Forms.GroupBox gbx_GeoNamesSettings;
        private System.Windows.Forms.Label lbl_Miles;
        private System.Windows.Forms.CheckBox ckb_PopulatedPlacesOnly;
        private System.Windows.Forms.GroupBox gbx_GeoNamesLanguageSettings;
        private System.Windows.Forms.ComboBox cbx_TryUseGeoNamesLanguage;
        private System.Windows.Forms.RadioButton rbt_TryUseGeoNamesLanguage;
        private System.Windows.Forms.RadioButton rbt_UseGeoNamesLocalLanguage;
        private System.Windows.Forms.CheckBox ckb_ReplaceBlankToponyms;
        public System.Windows.Forms.TextBox tbx_ReplaceBlankToponyms;
        private System.Windows.Forms.Label lbl_ChoiceRadius;
        private System.Windows.Forms.Label lbl_ChoiceOfferCount;
        internal System.Windows.Forms.NumericUpDown nud_ChoiceRadius;
        private System.Windows.Forms.NumericUpDown nud_ChoiceOfferCount;
        private System.Windows.Forms.RichTextBox rbx_Register_ArcGIS;
        private System.Windows.Forms.RichTextBox rbx_Register_GeoNames;
        private System.Windows.Forms.Label lbl_GeoNames_Pwd;
        private System.Windows.Forms.Label lbl_GeoNames_UserName;
        private System.Windows.Forms.Label lbl_ARCGIS_APIKey;
        public System.Windows.Forms.TextBox tbx_GeoNames_Pwd;
        public System.Windows.Forms.TextBox tbx_GeoNames_UserName;
        public System.Windows.Forms.TextBox tbx_ARCGIS_APIKey;
    }
}