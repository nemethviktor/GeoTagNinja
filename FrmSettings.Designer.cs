using GeoTagNinja.Helpers;

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
            this.tcr_Settings = new System.Windows.Forms.TabControl();
            this.tpg_Application = new System.Windows.Forms.TabPage();
            this.gbx_AppSettings = new System.Windows.Forms.GroupBox();
            this.ckb_UseDarkMode = new System.Windows.Forms.CheckBox();
            this.gbx_MapColourMode = new System.Windows.Forms.GroupBox();
            this.rbt_MapColourModeDarkPale = new System.Windows.Forms.RadioButton();
            this.rbt_MapColourModeNormal = new System.Windows.Forms.RadioButton();
            this.rbt_MapColourModeDarkInverse = new System.Windows.Forms.RadioButton();
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
            this.tpg_GeoNames = new System.Windows.Forms.TabPage();
            this.gbx_GeoNamesSettings = new System.Windows.Forms.GroupBox();
            this.ckb_ShowPassword = new System.Windows.Forms.CheckBox();
            this.lbl_Generic_Miles = new System.Windows.Forms.Label();
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
            this.btn_Generic_OK = new System.Windows.Forms.Button();
            this.btn_Generic_Cancel = new System.Windows.Forms.Button();
            this.fbd_StartupFolder = new System.Windows.Forms.FolderBrowserDialog();
            this.btn_ExportSettings = new System.Windows.Forms.Button();
            this.btn_ImportSettings = new System.Windows.Forms.Button();
            this.tcr_Settings.SuspendLayout();
            this.tpg_Application.SuspendLayout();
            this.gbx_AppSettings.SuspendLayout();
            this.gbx_MapColourMode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Browse_Startup_Folder)).BeginInit();
            this.tpg_GeoNames.SuspendLayout();
            this.gbx_GeoNamesSettings.SuspendLayout();
            this.gbx_GeoNamesLanguageSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_ChoiceRadius)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_ChoiceOfferCount)).BeginInit();
            this.tpg_FileOptions.SuspendLayout();
            this.tpg_CustomCityLogic.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_CustomCityLogic)).BeginInit();
            this.tpg_CustomRules.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_CustomRules)).BeginInit();
            this.SuspendLayout();
            // 
            // tcr_Settings
            // 
            resources.ApplyResources(this.tcr_Settings, "tcr_Settings");
            this.tcr_Settings.Controls.Add(this.tpg_Application);
            this.tcr_Settings.Controls.Add(this.tpg_GeoNames);
            this.tcr_Settings.Controls.Add(this.tpg_FileOptions);
            this.tcr_Settings.Controls.Add(this.tpg_CustomCityLogic);
            this.tcr_Settings.Controls.Add(this.tpg_CustomRules);
            this.tcr_Settings.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcr_Settings.ImageList = this.igl_Settings;
            this.tcr_Settings.Name = "tcr_Settings";
            this.tcr_Settings.SelectedIndex = 0;
            // 
            // tpg_Application
            // 
            resources.ApplyResources(this.tpg_Application, "tpg_Application");
            this.tpg_Application.BackColor = System.Drawing.Color.WhiteSmoke;
            this.tpg_Application.Controls.Add(this.gbx_AppSettings);
            this.tpg_Application.Name = "tpg_Application";
            // 
            // gbx_AppSettings
            // 
            this.gbx_AppSettings.Controls.Add(this.ckb_UseDarkMode);
            this.gbx_AppSettings.Controls.Add(this.gbx_MapColourMode);
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
            resources.ApplyResources(this.gbx_AppSettings, "gbx_AppSettings");
            this.gbx_AppSettings.Name = "gbx_AppSettings";
            this.gbx_AppSettings.TabStop = false;
            // 
            // ckb_UseDarkMode
            // 
            resources.ApplyResources(this.ckb_UseDarkMode, "ckb_UseDarkMode");
            this.ckb_UseDarkMode.Name = "ckb_UseDarkMode";
            this.ckb_UseDarkMode.UseVisualStyleBackColor = true;
            this.ckb_UseDarkMode.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // gbx_MapColourMode
            // 
            this.gbx_MapColourMode.Controls.Add(this.rbt_MapColourModeDarkPale);
            this.gbx_MapColourMode.Controls.Add(this.rbt_MapColourModeNormal);
            this.gbx_MapColourMode.Controls.Add(this.rbt_MapColourModeDarkInverse);
            resources.ApplyResources(this.gbx_MapColourMode, "gbx_MapColourMode");
            this.gbx_MapColourMode.Name = "gbx_MapColourMode";
            this.gbx_MapColourMode.TabStop = false;
            // 
            // rbt_MapColourModeDarkPale
            // 
            resources.ApplyResources(this.rbt_MapColourModeDarkPale, "rbt_MapColourModeDarkPale");
            this.rbt_MapColourModeDarkPale.Name = "rbt_MapColourModeDarkPale";
            this.rbt_MapColourModeDarkPale.TabStop = true;
            this.rbt_MapColourModeDarkPale.UseVisualStyleBackColor = true;
            this.rbt_MapColourModeDarkPale.CheckedChanged += new System.EventHandler(this.Any_rbt_CheckedChanged);
            // 
            // rbt_MapColourModeNormal
            // 
            resources.ApplyResources(this.rbt_MapColourModeNormal, "rbt_MapColourModeNormal");
            this.rbt_MapColourModeNormal.Name = "rbt_MapColourModeNormal";
            this.rbt_MapColourModeNormal.TabStop = true;
            this.rbt_MapColourModeNormal.UseVisualStyleBackColor = true;
            this.rbt_MapColourModeNormal.CheckedChanged += new System.EventHandler(this.Any_rbt_CheckedChanged);
            // 
            // rbt_MapColourModeDarkInverse
            // 
            resources.ApplyResources(this.rbt_MapColourModeDarkInverse, "rbt_MapColourModeDarkInverse");
            this.rbt_MapColourModeDarkInverse.Name = "rbt_MapColourModeDarkInverse";
            this.rbt_MapColourModeDarkInverse.TabStop = true;
            this.rbt_MapColourModeDarkInverse.UseVisualStyleBackColor = true;
            this.rbt_MapColourModeDarkInverse.CheckedChanged += new System.EventHandler(this.Any_rbt_CheckedChanged);
            // 
            // ckb_UpdateCheckPreRelease
            // 
            resources.ApplyResources(this.ckb_UpdateCheckPreRelease, "ckb_UpdateCheckPreRelease");
            this.ckb_UpdateCheckPreRelease.Name = "ckb_UpdateCheckPreRelease";
            this.ckb_UpdateCheckPreRelease.UseVisualStyleBackColor = true;
            this.ckb_UpdateCheckPreRelease.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // lbl_Metres_Abbr
            // 
            resources.ApplyResources(this.lbl_Metres_Abbr, "lbl_Metres_Abbr");
            this.lbl_Metres_Abbr.Name = "lbl_Metres_Abbr";
            // 
            // lbl_Feet_Abbr
            // 
            resources.ApplyResources(this.lbl_Feet_Abbr, "lbl_Feet_Abbr");
            this.lbl_Feet_Abbr.Name = "lbl_Feet_Abbr";
            // 
            // ckb_UseImperialNotMetric
            // 
            resources.ApplyResources(this.ckb_UseImperialNotMetric, "ckb_UseImperialNotMetric");
            this.ckb_UseImperialNotMetric.Name = "ckb_UseImperialNotMetric";
            this.ckb_UseImperialNotMetric.UseVisualStyleBackColor = true;
            this.ckb_UseImperialNotMetric.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_RemoveGeoDataRemovesTimeOffset
            // 
            resources.ApplyResources(this.ckb_RemoveGeoDataRemovesTimeOffset, "ckb_RemoveGeoDataRemovesTimeOffset");
            this.ckb_RemoveGeoDataRemovesTimeOffset.Name = "ckb_RemoveGeoDataRemovesTimeOffset";
            this.ckb_RemoveGeoDataRemovesTimeOffset.UseVisualStyleBackColor = true;
            this.ckb_RemoveGeoDataRemovesTimeOffset.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_ResetMapToZero
            // 
            resources.ApplyResources(this.ckb_ResetMapToZero, "ckb_ResetMapToZero");
            this.ckb_ResetMapToZero.Name = "ckb_ResetMapToZero";
            this.ckb_ResetMapToZero.UseVisualStyleBackColor = true;
            this.ckb_ResetMapToZero.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // cbx_Language
            // 
            this.cbx_Language.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_Language.FormattingEnabled = true;
            resources.ApplyResources(this.cbx_Language, "cbx_Language");
            this.cbx_Language.Name = "cbx_Language";
            this.cbx_Language.SelectedValueChanged += new System.EventHandler(this.Any_cbx_TextChanged);
            // 
            // lbl_Language
            // 
            resources.ApplyResources(this.lbl_Language, "lbl_Language");
            this.lbl_Language.Name = "lbl_Language";
            // 
            // pbx_Browse_Startup_Folder
            // 
            resources.ApplyResources(this.pbx_Browse_Startup_Folder, "pbx_Browse_Startup_Folder");
            this.pbx_Browse_Startup_Folder.Name = "pbx_Browse_Startup_Folder";
            this.pbx_Browse_Startup_Folder.TabStop = false;
            this.pbx_Browse_Startup_Folder.Click += new System.EventHandler(this.Pbx_Browse_Startup_Folder_Click);
            // 
            // lbl_Startup_Folder
            // 
            resources.ApplyResources(this.lbl_Startup_Folder, "lbl_Startup_Folder");
            this.lbl_Startup_Folder.Name = "lbl_Startup_Folder";
            // 
            // tbx_Startup_Folder
            // 
            resources.ApplyResources(this.tbx_Startup_Folder, "tbx_Startup_Folder");
            this.tbx_Startup_Folder.Name = "tbx_Startup_Folder";
            this.tbx_Startup_Folder.ReadOnly = true;
            this.tbx_Startup_Folder.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // tpg_GeoNames
            // 
            this.tpg_GeoNames.Controls.Add(this.gbx_GeoNamesSettings);
            resources.ApplyResources(this.tpg_GeoNames, "tpg_GeoNames");
            this.tpg_GeoNames.Name = "tpg_GeoNames";
            this.tpg_GeoNames.UseVisualStyleBackColor = true;
            // 
            // gbx_GeoNamesSettings
            // 
            this.gbx_GeoNamesSettings.Controls.Add(this.ckb_ShowPassword);
            this.gbx_GeoNamesSettings.Controls.Add(this.lbl_Generic_Miles);
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
            resources.ApplyResources(this.gbx_GeoNamesSettings, "gbx_GeoNamesSettings");
            this.gbx_GeoNamesSettings.Name = "gbx_GeoNamesSettings";
            this.gbx_GeoNamesSettings.TabStop = false;
            // 
            // ckb_ShowPassword
            // 
            resources.ApplyResources(this.ckb_ShowPassword, "ckb_ShowPassword");
            this.ckb_ShowPassword.Name = "ckb_ShowPassword";
            this.ckb_ShowPassword.UseVisualStyleBackColor = true;
            this.ckb_ShowPassword.CheckedChanged += new System.EventHandler(this.ckb_ShowPassword_CheckedChanged);
            // 
            // lbl_Generic_Miles
            // 
            resources.ApplyResources(this.lbl_Generic_Miles, "lbl_Generic_Miles");
            this.lbl_Generic_Miles.Name = "lbl_Generic_Miles";
            // 
            // ckb_PopulatedPlacesOnly
            // 
            resources.ApplyResources(this.ckb_PopulatedPlacesOnly, "ckb_PopulatedPlacesOnly");
            this.ckb_PopulatedPlacesOnly.Name = "ckb_PopulatedPlacesOnly";
            this.ckb_PopulatedPlacesOnly.UseVisualStyleBackColor = true;
            this.ckb_PopulatedPlacesOnly.CheckedChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // gbx_GeoNamesLanguageSettings
            // 
            this.gbx_GeoNamesLanguageSettings.Controls.Add(this.cbx_TryUseGeoNamesLanguage);
            this.gbx_GeoNamesLanguageSettings.Controls.Add(this.rbt_TryUseGeoNamesLanguage);
            this.gbx_GeoNamesLanguageSettings.Controls.Add(this.rbt_UseGeoNamesLocalLanguage);
            resources.ApplyResources(this.gbx_GeoNamesLanguageSettings, "gbx_GeoNamesLanguageSettings");
            this.gbx_GeoNamesLanguageSettings.Name = "gbx_GeoNamesLanguageSettings";
            this.gbx_GeoNamesLanguageSettings.TabStop = false;
            // 
            // cbx_TryUseGeoNamesLanguage
            // 
            this.cbx_TryUseGeoNamesLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_TryUseGeoNamesLanguage.FormattingEnabled = true;
            resources.ApplyResources(this.cbx_TryUseGeoNamesLanguage, "cbx_TryUseGeoNamesLanguage");
            this.cbx_TryUseGeoNamesLanguage.Name = "cbx_TryUseGeoNamesLanguage";
            this.cbx_TryUseGeoNamesLanguage.SelectedIndexChanged += new System.EventHandler(this.Any_cbx_TextChanged);
            // 
            // rbt_TryUseGeoNamesLanguage
            // 
            resources.ApplyResources(this.rbt_TryUseGeoNamesLanguage, "rbt_TryUseGeoNamesLanguage");
            this.rbt_TryUseGeoNamesLanguage.Name = "rbt_TryUseGeoNamesLanguage";
            this.rbt_TryUseGeoNamesLanguage.TabStop = true;
            this.rbt_TryUseGeoNamesLanguage.UseVisualStyleBackColor = true;
            this.rbt_TryUseGeoNamesLanguage.CheckedChanged += new System.EventHandler(this.Any_rbt_CheckedChanged);
            // 
            // rbt_UseGeoNamesLocalLanguage
            // 
            resources.ApplyResources(this.rbt_UseGeoNamesLocalLanguage, "rbt_UseGeoNamesLocalLanguage");
            this.rbt_UseGeoNamesLocalLanguage.Name = "rbt_UseGeoNamesLocalLanguage";
            this.rbt_UseGeoNamesLocalLanguage.TabStop = true;
            this.rbt_UseGeoNamesLocalLanguage.UseVisualStyleBackColor = true;
            this.rbt_UseGeoNamesLocalLanguage.CheckedChanged += new System.EventHandler(this.Any_rbt_CheckedChanged);
            // 
            // ckb_ReplaceBlankToponyms
            // 
            resources.ApplyResources(this.ckb_ReplaceBlankToponyms, "ckb_ReplaceBlankToponyms");
            this.ckb_ReplaceBlankToponyms.Name = "ckb_ReplaceBlankToponyms";
            this.ckb_ReplaceBlankToponyms.UseVisualStyleBackColor = true;
            this.ckb_ReplaceBlankToponyms.CheckedChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // tbx_ReplaceBlankToponyms
            // 
            resources.ApplyResources(this.tbx_ReplaceBlankToponyms, "tbx_ReplaceBlankToponyms");
            this.tbx_ReplaceBlankToponyms.Name = "tbx_ReplaceBlankToponyms";
            this.tbx_ReplaceBlankToponyms.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // lbl_ChoiceRadius
            // 
            resources.ApplyResources(this.lbl_ChoiceRadius, "lbl_ChoiceRadius");
            this.lbl_ChoiceRadius.Name = "lbl_ChoiceRadius";
            // 
            // lbl_ChoiceOfferCount
            // 
            resources.ApplyResources(this.lbl_ChoiceOfferCount, "lbl_ChoiceOfferCount");
            this.lbl_ChoiceOfferCount.Name = "lbl_ChoiceOfferCount";
            // 
            // nud_ChoiceRadius
            // 
            resources.ApplyResources(this.nud_ChoiceRadius, "nud_ChoiceRadius");
            this.nud_ChoiceRadius.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nud_ChoiceRadius.Name = "nud_ChoiceRadius";
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
            resources.ApplyResources(this.nud_ChoiceOfferCount, "nud_ChoiceOfferCount");
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
            resources.ApplyResources(this.rbx_Register_ArcGIS, "rbx_Register_ArcGIS");
            this.rbx_Register_ArcGIS.Name = "rbx_Register_ArcGIS";
            this.rbx_Register_ArcGIS.ReadOnly = true;
            // 
            // rbx_Register_GeoNames
            // 
            this.rbx_Register_GeoNames.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.rbx_Register_GeoNames, "rbx_Register_GeoNames");
            this.rbx_Register_GeoNames.Name = "rbx_Register_GeoNames";
            this.rbx_Register_GeoNames.ReadOnly = true;
            // 
            // lbl_GeoNames_Pwd
            // 
            resources.ApplyResources(this.lbl_GeoNames_Pwd, "lbl_GeoNames_Pwd");
            this.lbl_GeoNames_Pwd.Name = "lbl_GeoNames_Pwd";
            // 
            // lbl_GeoNames_UserName
            // 
            resources.ApplyResources(this.lbl_GeoNames_UserName, "lbl_GeoNames_UserName");
            this.lbl_GeoNames_UserName.Name = "lbl_GeoNames_UserName";
            // 
            // lbl_ARCGIS_APIKey
            // 
            resources.ApplyResources(this.lbl_ARCGIS_APIKey, "lbl_ARCGIS_APIKey");
            this.lbl_ARCGIS_APIKey.Name = "lbl_ARCGIS_APIKey";
            // 
            // tbx_GeoNames_Pwd
            // 
            resources.ApplyResources(this.tbx_GeoNames_Pwd, "tbx_GeoNames_Pwd");
            this.tbx_GeoNames_Pwd.Name = "tbx_GeoNames_Pwd";
            this.tbx_GeoNames_Pwd.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // tbx_GeoNames_UserName
            // 
            resources.ApplyResources(this.tbx_GeoNames_UserName, "tbx_GeoNames_UserName");
            this.tbx_GeoNames_UserName.Name = "tbx_GeoNames_UserName";
            this.tbx_GeoNames_UserName.TextChanged += new System.EventHandler(this.Any_tbx_TextChanged);
            // 
            // tbx_ARCGIS_APIKey
            // 
            resources.ApplyResources(this.tbx_ARCGIS_APIKey, "tbx_ARCGIS_APIKey");
            this.tbx_ARCGIS_APIKey.Name = "tbx_ARCGIS_APIKey";
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
            resources.ApplyResources(this.tpg_FileOptions, "tpg_FileOptions");
            this.tpg_FileOptions.Name = "tpg_FileOptions";
            this.tpg_FileOptions.Enter += new System.EventHandler(this.tpg_FileOptions_Enter);
            // 
            // ckb_ResetFileDateToCreated
            // 
            resources.ApplyResources(this.ckb_ResetFileDateToCreated, "ckb_ResetFileDateToCreated");
            this.ckb_ResetFileDateToCreated.Name = "ckb_ResetFileDateToCreated";
            this.ckb_ResetFileDateToCreated.UseVisualStyleBackColor = true;
            this.ckb_ResetFileDateToCreated.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_ProcessOriginalFile
            // 
            resources.ApplyResources(this.ckb_ProcessOriginalFile, "ckb_ProcessOriginalFile");
            this.ckb_ProcessOriginalFile.Name = "ckb_ProcessOriginalFile";
            this.ckb_ProcessOriginalFile.UseVisualStyleBackColor = true;
            this.ckb_ProcessOriginalFile.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_OverwriteOriginal
            // 
            resources.ApplyResources(this.ckb_OverwriteOriginal, "ckb_OverwriteOriginal");
            this.ckb_OverwriteOriginal.Name = "ckb_OverwriteOriginal";
            this.ckb_OverwriteOriginal.UseVisualStyleBackColor = true;
            this.ckb_OverwriteOriginal.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_AddXMPSideCar
            // 
            resources.ApplyResources(this.ckb_AddXMPSideCar, "ckb_AddXMPSideCar");
            this.ckb_AddXMPSideCar.Name = "ckb_AddXMPSideCar";
            this.ckb_AddXMPSideCar.UseVisualStyleBackColor = true;
            this.ckb_AddXMPSideCar.CheckStateChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // lbx_fileExtensions
            // 
            this.lbx_fileExtensions.FormattingEnabled = true;
            resources.ApplyResources(this.lbx_fileExtensions, "lbx_fileExtensions");
            this.lbx_fileExtensions.Name = "lbx_fileExtensions";
            this.lbx_fileExtensions.SelectedIndexChanged += new System.EventHandler(this.Lbx_fileExtensions_SelectedIndexChanged);
            // 
            // tpg_CustomCityLogic
            // 
            this.tpg_CustomCityLogic.Controls.Add(this.rbx_CustomCityLogicExplanationBold);
            this.tpg_CustomCityLogic.Controls.Add(this.btn_ResetToDefaults);
            this.tpg_CustomCityLogic.Controls.Add(this.rbx_CustomCityLogicExplanation);
            this.tpg_CustomCityLogic.Controls.Add(this.dgv_CustomCityLogic);
            resources.ApplyResources(this.tpg_CustomCityLogic, "tpg_CustomCityLogic");
            this.tpg_CustomCityLogic.Name = "tpg_CustomCityLogic";
            this.tpg_CustomCityLogic.UseVisualStyleBackColor = true;
            // 
            // rbx_CustomCityLogicExplanationBold
            // 
            this.rbx_CustomCityLogicExplanationBold.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.rbx_CustomCityLogicExplanationBold, "rbx_CustomCityLogicExplanationBold");
            this.rbx_CustomCityLogicExplanationBold.Name = "rbx_CustomCityLogicExplanationBold";
            this.rbx_CustomCityLogicExplanationBold.ReadOnly = true;
            // 
            // btn_ResetToDefaults
            // 
            resources.ApplyResources(this.btn_ResetToDefaults, "btn_ResetToDefaults");
            this.btn_ResetToDefaults.Name = "btn_ResetToDefaults";
            this.btn_ResetToDefaults.UseVisualStyleBackColor = true;
            this.btn_ResetToDefaults.Click += new System.EventHandler(this.btn_ResetToDefaults_Click);
            // 
            // rbx_CustomCityLogicExplanation
            // 
            this.rbx_CustomCityLogicExplanation.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.rbx_CustomCityLogicExplanation, "rbx_CustomCityLogicExplanation");
            this.rbx_CustomCityLogicExplanation.Name = "rbx_CustomCityLogicExplanation";
            this.rbx_CustomCityLogicExplanation.ReadOnly = true;
            // 
            // dgv_CustomCityLogic
            // 
            this.dgv_CustomCityLogic.AllowUserToAddRows = false;
            this.dgv_CustomCityLogic.AllowUserToDeleteRows = false;
            this.dgv_CustomCityLogic.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_CustomCityLogic.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dgv_CustomCityLogic.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(this.dgv_CustomCityLogic, "dgv_CustomCityLogic");
            this.dgv_CustomCityLogic.Name = "dgv_CustomCityLogic";
            // 
            // tpg_CustomRules
            // 
            this.tpg_CustomRules.Controls.Add(this.rbx_CustomRulesExplanationBold);
            this.tpg_CustomRules.Controls.Add(this.ckb_StopProcessingRules);
            this.tpg_CustomRules.Controls.Add(this.ckb_IncludePredeterminedCountries);
            this.tpg_CustomRules.Controls.Add(this.rbx_CustomRulesExplanation);
            this.tpg_CustomRules.Controls.Add(this.dgv_CustomRules);
            resources.ApplyResources(this.tpg_CustomRules, "tpg_CustomRules");
            this.tpg_CustomRules.Name = "tpg_CustomRules";
            this.tpg_CustomRules.UseVisualStyleBackColor = true;
            // 
            // rbx_CustomRulesExplanationBold
            // 
            this.rbx_CustomRulesExplanationBold.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.rbx_CustomRulesExplanationBold, "rbx_CustomRulesExplanationBold");
            this.rbx_CustomRulesExplanationBold.Name = "rbx_CustomRulesExplanationBold";
            this.rbx_CustomRulesExplanationBold.ReadOnly = true;
            // 
            // ckb_StopProcessingRules
            // 
            resources.ApplyResources(this.ckb_StopProcessingRules, "ckb_StopProcessingRules");
            this.ckb_StopProcessingRules.Name = "ckb_StopProcessingRules";
            this.ckb_StopProcessingRules.UseVisualStyleBackColor = true;
            this.ckb_StopProcessingRules.CheckedChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // ckb_IncludePredeterminedCountries
            // 
            resources.ApplyResources(this.ckb_IncludePredeterminedCountries, "ckb_IncludePredeterminedCountries");
            this.ckb_IncludePredeterminedCountries.Name = "ckb_IncludePredeterminedCountries";
            this.ckb_IncludePredeterminedCountries.UseVisualStyleBackColor = true;
            this.ckb_IncludePredeterminedCountries.CheckedChanged += new System.EventHandler(this.Any_ckb_CheckStateChanged);
            // 
            // rbx_CustomRulesExplanation
            // 
            this.rbx_CustomRulesExplanation.BorderStyle = System.Windows.Forms.BorderStyle.None;
            resources.ApplyResources(this.rbx_CustomRulesExplanation, "rbx_CustomRulesExplanation");
            this.rbx_CustomRulesExplanation.Name = "rbx_CustomRulesExplanation";
            this.rbx_CustomRulesExplanation.ReadOnly = true;
            this.rbx_CustomRulesExplanation.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.rbx_CustomRulesExplanation_LinkClicked);
            // 
            // dgv_CustomRules
            // 
            this.dgv_CustomRules.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgv_CustomRules.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dgv_CustomRules.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            resources.ApplyResources(this.dgv_CustomRules, "dgv_CustomRules");
            this.dgv_CustomRules.Name = "dgv_CustomRules";
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
            // btn_Generic_OK
            // 
            resources.ApplyResources(this.btn_Generic_OK, "btn_Generic_OK");
            this.btn_Generic_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btn_Generic_OK.Name = "btn_Generic_OK";
            this.btn_Generic_OK.UseVisualStyleBackColor = true;
            this.btn_Generic_OK.Click += new System.EventHandler(this.btn_Generic_OK_Click);
            // 
            // btn_Generic_Cancel
            // 
            resources.ApplyResources(this.btn_Generic_Cancel, "btn_Generic_Cancel");
            this.btn_Generic_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Generic_Cancel.Name = "btn_Generic_Cancel";
            this.btn_Generic_Cancel.UseVisualStyleBackColor = true;
            this.btn_Generic_Cancel.Click += new System.EventHandler(this.btn_Generic_Cancel_Click);
            // 
            // fbd_StartupFolder
            // 
            resources.ApplyResources(this.fbd_StartupFolder, "fbd_StartupFolder");
            this.fbd_StartupFolder.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // btn_ExportSettings
            // 
            resources.ApplyResources(this.btn_ExportSettings, "btn_ExportSettings");
            this.btn_ExportSettings.Name = "btn_ExportSettings";
            this.btn_ExportSettings.UseVisualStyleBackColor = true;
            this.btn_ExportSettings.Click += new System.EventHandler(this.btn_ExportSettings_Click);
            // 
            // btn_ImportSettings
            // 
            resources.ApplyResources(this.btn_ImportSettings, "btn_ImportSettings");
            this.btn_ImportSettings.Name = "btn_ImportSettings";
            this.btn_ImportSettings.UseVisualStyleBackColor = true;
            this.btn_ImportSettings.Click += new System.EventHandler(this.btn_ImportSettings_Click);
            // 
            // FrmSettings
            // 
            this.AcceptButton = this.btn_Generic_OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btn_Generic_Cancel;
            this.Controls.Add(this.btn_ExportSettings);
            this.Controls.Add(this.btn_ImportSettings);
            this.Controls.Add(this.btn_Generic_Cancel);
            this.Controls.Add(this.btn_Generic_OK);
            this.Controls.Add(this.tcr_Settings);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmSettings";
            this.ShowInTaskbar = false;
            this.Load += new System.EventHandler(this.FrmSettings_Load);
            this.tcr_Settings.ResumeLayout(false);
            this.tpg_Application.ResumeLayout(false);
            this.gbx_AppSettings.ResumeLayout(false);
            this.gbx_AppSettings.PerformLayout();
            this.gbx_MapColourMode.ResumeLayout(false);
            this.gbx_MapColourMode.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Browse_Startup_Folder)).EndInit();
            this.tpg_GeoNames.ResumeLayout(false);
            this.gbx_GeoNamesSettings.ResumeLayout(false);
            this.gbx_GeoNamesSettings.PerformLayout();
            this.gbx_GeoNamesLanguageSettings.ResumeLayout(false);
            this.gbx_GeoNamesLanguageSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_ChoiceRadius)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_ChoiceOfferCount)).EndInit();
            this.tpg_FileOptions.ResumeLayout(false);
            this.tpg_CustomCityLogic.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_CustomCityLogic)).EndInit();
            this.tpg_CustomRules.ResumeLayout(false);
            this.tpg_CustomRules.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_CustomRules)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.TabPage tpg_Application;
        private System.Windows.Forms.TabPage tpg_FileOptions;
        private System.Windows.Forms.Button btn_Generic_OK;
        private System.Windows.Forms.Button btn_Generic_Cancel;
        private System.Windows.Forms.TabControl tcr_Settings;
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
        private System.Windows.Forms.TabPage tpg_GeoNames;
        private System.Windows.Forms.GroupBox gbx_GeoNamesSettings;
        private System.Windows.Forms.Label lbl_Generic_Miles;
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
        private System.Windows.Forms.GroupBox gbx_MapColourMode;
        private System.Windows.Forms.RadioButton rbt_MapColourModeDarkPale;
        private System.Windows.Forms.RadioButton rbt_MapColourModeNormal;
        private System.Windows.Forms.RadioButton rbt_MapColourModeDarkInverse;
        private System.Windows.Forms.CheckBox ckb_UseDarkMode;
        private System.Windows.Forms.Button btn_ExportSettings;
        private System.Windows.Forms.Button btn_ImportSettings;
        private System.Windows.Forms.CheckBox ckb_ShowPassword;
    }
}