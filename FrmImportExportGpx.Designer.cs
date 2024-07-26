namespace GeoTagNinja
{
    partial class FrmImportExportGpx
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmImportExportGpx));
            this.tcr_ImportExport = new System.Windows.Forms.TabControl();
            this.tpg_ImportExport_Import = new System.Windows.Forms.TabPage();
            this.gbx_OtherSettings = new System.Windows.Forms.GroupBox();
            this.ckb_OverlayGPXForSelectedDatesOnly = new System.Windows.Forms.CheckBox();
            this.ckb_LoadTrackOntoMap = new System.Windows.Forms.CheckBox();
            this.ckb_DoNotQueryAPI = new System.Windows.Forms.CheckBox();
            this.gbx_ImportGPXSource = new System.Windows.Forms.GroupBox();
            this.pbx_importOneFile = new System.Windows.Forms.PictureBox();
            this.pbx_importFromAnotherFolder = new System.Windows.Forms.PictureBox();
            this.lbl_importFromAnotherFolder = new System.Windows.Forms.Label();
            this.lbl_importOneFile = new System.Windows.Forms.Label();
            this.rbt_importFromAnotherFolder = new System.Windows.Forms.RadioButton();
            this.rbt_importFromCurrentFolder = new System.Windows.Forms.RadioButton();
            this.rbt_importOneFile = new System.Windows.Forms.RadioButton();
            this.gbx_ImportGPXTimeSetting = new System.Windows.Forms.GroupBox();
            this.btn_PullMostRecentTrackSyncShift = new System.Windows.Forms.Button();
            this.nud_GeoMaxExtSecs = new System.Windows.Forms.NumericUpDown();
            this.nud_GeoMaxIntSecs = new System.Windows.Forms.NumericUpDown();
            this.lbl_GeoMaxExtSecs = new System.Windows.Forms.Label();
            this.lbl_GeoMaxIntSecs = new System.Windows.Forms.Label();
            this.lbl_CameraTimeNote = new System.Windows.Forms.Label();
            this.lbl_TZNote = new System.Windows.Forms.Label();
            this.lbl_TZValue = new System.Windows.Forms.Label();
            this.ckb_UseDST = new System.Windows.Forms.CheckBox();
            this.lbl_TZTime = new System.Windows.Forms.Label();
            this.ckb_UseTimeZone = new System.Windows.Forms.CheckBox();
            this.cbx_ImportUseTimeZone = new System.Windows.Forms.ComboBox();
            this.lbl_CameraTimeData = new System.Windows.Forms.Label();
            this.lbl_CameraTime = new System.Windows.Forms.Label();
            this.lbl_ImportGPXSeconds = new System.Windows.Forms.Label();
            this.lbl_ImportGPXMinutes = new System.Windows.Forms.Label();
            this.lbl_ImportGPXHours = new System.Windows.Forms.Label();
            this.lbl_ImportGPXDays = new System.Windows.Forms.Label();
            this.nud_Seconds = new System.Windows.Forms.NumericUpDown();
            this.nud_Minutes = new System.Windows.Forms.NumericUpDown();
            this.nud_Hours = new System.Windows.Forms.NumericUpDown();
            this.nud_Days = new System.Windows.Forms.NumericUpDown();
            this.lbl_ShiftTrackTimeBy = new System.Windows.Forms.Label();
            this.cbx_ImportTimeAgainst = new System.Windows.Forms.ComboBox();
            this.lbl_ImportTimeAgainst = new System.Windows.Forms.Label();
            this.tpg_ImportExport_Export = new System.Windows.Forms.TabPage();
            this.gbx_SaveTrackOptions = new System.Windows.Forms.GroupBox();
            this.pbx_GPSDataPaste = new System.Windows.Forms.PictureBox();
            this.lbl_ExportTrackTimeStampTypeNote = new System.Windows.Forms.Label();
            this.cbx_ExportTrackTimeStampType = new System.Windows.Forms.ComboBox();
            this.lbl_ExportTrackTimeStampType = new System.Windows.Forms.Label();
            this.cbx_ExportTrackOrderBy = new System.Windows.Forms.ComboBox();
            this.lbl_ExportTrackOrderBy = new System.Windows.Forms.Label();
            this.ckb_ExportTrackIncludeAltitude = new System.Windows.Forms.CheckBox();
            this.pbx_Browse_SaveTo_Folder = new System.Windows.Forms.PictureBox();
            this.lbl_SaveTrackTo = new System.Windows.Forms.Label();
            this.tbx_SaveTrackTo = new System.Windows.Forms.TextBox();
            this.ofd_importOneFile = new System.Windows.Forms.OpenFileDialog();
            this.fbd_importFromAnotherFolder = new System.Windows.Forms.FolderBrowserDialog();
            this.ofd_SaveTrackTo = new System.Windows.Forms.OpenFileDialog();
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.tcr_ImportExport.SuspendLayout();
            this.tpg_ImportExport_Import.SuspendLayout();
            this.gbx_OtherSettings.SuspendLayout();
            this.gbx_ImportGPXSource.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_importOneFile)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_importFromAnotherFolder)).BeginInit();
            this.gbx_ImportGPXTimeSetting.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_GeoMaxExtSecs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_GeoMaxIntSecs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Seconds)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Minutes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Hours)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Days)).BeginInit();
            this.tpg_ImportExport_Export.SuspendLayout();
            this.gbx_SaveTrackOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_GPSDataPaste)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Browse_SaveTo_Folder)).BeginInit();
            this.SuspendLayout();
            // 
            // tcr_ImportExport
            // 
            this.tcr_ImportExport.Controls.Add(this.tpg_ImportExport_Import);
            this.tcr_ImportExport.Controls.Add(this.tpg_ImportExport_Export);
            this.tcr_ImportExport.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcr_ImportExport.Location = new System.Drawing.Point(13, 13);
            this.tcr_ImportExport.Name = "tcr_ImportExport";
            this.tcr_ImportExport.SelectedIndex = 0;
            this.tcr_ImportExport.Size = new System.Drawing.Size(777, 416);
            this.tcr_ImportExport.TabIndex = 0;
            // 
            // tpg_ImportExport_Import
            // 
            this.tpg_ImportExport_Import.AutoScroll = true;
            this.tpg_ImportExport_Import.AutoScrollMargin = new System.Drawing.Size(0, 10);
            this.tpg_ImportExport_Import.Controls.Add(this.gbx_OtherSettings);
            this.tpg_ImportExport_Import.Controls.Add(this.gbx_ImportGPXSource);
            this.tpg_ImportExport_Import.Controls.Add(this.gbx_ImportGPXTimeSetting);
            this.tpg_ImportExport_Import.Location = new System.Drawing.Point(4, 22);
            this.tpg_ImportExport_Import.Name = "tpg_ImportExport_Import";
            this.tpg_ImportExport_Import.Padding = new System.Windows.Forms.Padding(3);
            this.tpg_ImportExport_Import.Size = new System.Drawing.Size(769, 390);
            this.tpg_ImportExport_Import.TabIndex = 0;
            this.tpg_ImportExport_Import.Text = "tpg_ImportExport_Import";
            this.tpg_ImportExport_Import.UseVisualStyleBackColor = true;
            // 
            // gbx_OtherSettings
            // 
            this.gbx_OtherSettings.Controls.Add(this.ckb_OverlayGPXForSelectedDatesOnly);
            this.gbx_OtherSettings.Controls.Add(this.ckb_LoadTrackOntoMap);
            this.gbx_OtherSettings.Controls.Add(this.ckb_DoNotQueryAPI);
            this.gbx_OtherSettings.Location = new System.Drawing.Point(3, 554);
            this.gbx_OtherSettings.Name = "gbx_OtherSettings";
            this.gbx_OtherSettings.Size = new System.Drawing.Size(725, 117);
            this.gbx_OtherSettings.TabIndex = 32;
            this.gbx_OtherSettings.TabStop = false;
            this.gbx_OtherSettings.Text = "gbx_OtherSettings";
            // 
            // ckb_OverlayGPXForSelectedDatesOnly
            // 
            this.ckb_OverlayGPXForSelectedDatesOnly.AutoSize = true;
            this.ckb_OverlayGPXForSelectedDatesOnly.Location = new System.Drawing.Point(46, 83);
            this.ckb_OverlayGPXForSelectedDatesOnly.Name = "ckb_OverlayGPXForSelectedDatesOnly";
            this.ckb_OverlayGPXForSelectedDatesOnly.Size = new System.Drawing.Size(214, 17);
            this.ckb_OverlayGPXForSelectedDatesOnly.TabIndex = 30;
            this.ckb_OverlayGPXForSelectedDatesOnly.Text = "ckb_OverlayGPXForSelectedDatesOnly";
            this.ckb_OverlayGPXForSelectedDatesOnly.UseVisualStyleBackColor = true;
            // 
            // ckb_LoadTrackOntoMap
            // 
            this.ckb_LoadTrackOntoMap.AutoSize = true;
            this.ckb_LoadTrackOntoMap.Location = new System.Drawing.Point(27, 57);
            this.ckb_LoadTrackOntoMap.Name = "ckb_LoadTrackOntoMap";
            this.ckb_LoadTrackOntoMap.Size = new System.Drawing.Size(146, 17);
            this.ckb_LoadTrackOntoMap.TabIndex = 29;
            this.ckb_LoadTrackOntoMap.Text = "ckb_LoadTrackOntoMap";
            this.ckb_LoadTrackOntoMap.UseVisualStyleBackColor = true;
            this.ckb_LoadTrackOntoMap.CheckedChanged += new System.EventHandler(this.ckb_LoadTrackOntoMap_CheckedChanged);
            // 
            // ckb_DoNotQueryAPI
            // 
            this.ckb_DoNotQueryAPI.AutoSize = true;
            this.ckb_DoNotQueryAPI.Location = new System.Drawing.Point(27, 31);
            this.ckb_DoNotQueryAPI.Name = "ckb_DoNotQueryAPI";
            this.ckb_DoNotQueryAPI.Size = new System.Drawing.Size(126, 17);
            this.ckb_DoNotQueryAPI.TabIndex = 28;
            this.ckb_DoNotQueryAPI.Text = "ckb_DoNotQueryAPI";
            this.ckb_DoNotQueryAPI.UseVisualStyleBackColor = true;
            // 
            // gbx_ImportGPXSource
            // 
            this.gbx_ImportGPXSource.Controls.Add(this.pbx_importOneFile);
            this.gbx_ImportGPXSource.Controls.Add(this.pbx_importFromAnotherFolder);
            this.gbx_ImportGPXSource.Controls.Add(this.lbl_importFromAnotherFolder);
            this.gbx_ImportGPXSource.Controls.Add(this.lbl_importOneFile);
            this.gbx_ImportGPXSource.Controls.Add(this.rbt_importFromAnotherFolder);
            this.gbx_ImportGPXSource.Controls.Add(this.rbt_importFromCurrentFolder);
            this.gbx_ImportGPXSource.Controls.Add(this.rbt_importOneFile);
            this.gbx_ImportGPXSource.Location = new System.Drawing.Point(2, 14);
            this.gbx_ImportGPXSource.Name = "gbx_ImportGPXSource";
            this.gbx_ImportGPXSource.Size = new System.Drawing.Size(726, 125);
            this.gbx_ImportGPXSource.TabIndex = 28;
            this.gbx_ImportGPXSource.TabStop = false;
            this.gbx_ImportGPXSource.Text = "gbx_ImportGPXSource";
            // 
            // pbx_importOneFile
            // 
            this.pbx_importOneFile.Image = ((System.Drawing.Image)(resources.GetObject("pbx_importOneFile.Image")));
            this.pbx_importOneFile.Location = new System.Drawing.Point(292, 27);
            this.pbx_importOneFile.Margin = new System.Windows.Forms.Padding(2);
            this.pbx_importOneFile.Name = "pbx_importOneFile";
            this.pbx_importOneFile.Size = new System.Drawing.Size(16, 16);
            this.pbx_importOneFile.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbx_importOneFile.TabIndex = 20;
            this.pbx_importOneFile.TabStop = false;
            this.pbx_importOneFile.Click += new System.EventHandler(this.pbx_importOneFile_Click);
            // 
            // pbx_importFromAnotherFolder
            // 
            this.pbx_importFromAnotherFolder.Image = ((System.Drawing.Image)(resources.GetObject("pbx_importFromAnotherFolder.Image")));
            this.pbx_importFromAnotherFolder.Location = new System.Drawing.Point(292, 95);
            this.pbx_importFromAnotherFolder.Margin = new System.Windows.Forms.Padding(2);
            this.pbx_importFromAnotherFolder.Name = "pbx_importFromAnotherFolder";
            this.pbx_importFromAnotherFolder.Size = new System.Drawing.Size(16, 16);
            this.pbx_importFromAnotherFolder.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbx_importFromAnotherFolder.TabIndex = 19;
            this.pbx_importFromAnotherFolder.TabStop = false;
            this.pbx_importFromAnotherFolder.Click += new System.EventHandler(this.pbx_importFromAnotherFolder_Click);
            // 
            // lbl_importFromAnotherFolder
            // 
            this.lbl_importFromAnotherFolder.AutoSize = true;
            this.lbl_importFromAnotherFolder.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbl_importFromAnotherFolder.Location = new System.Drawing.Point(313, 94);
            this.lbl_importFromAnotherFolder.Name = "lbl_importFromAnotherFolder";
            this.lbl_importFromAnotherFolder.Size = new System.Drawing.Size(142, 15);
            this.lbl_importFromAnotherFolder.TabIndex = 18;
            this.lbl_importFromAnotherFolder.Text = "lbl_importFromAnotherFolder";
            // 
            // lbl_importOneFile
            // 
            this.lbl_importOneFile.AutoSize = true;
            this.lbl_importOneFile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbl_importOneFile.Location = new System.Drawing.Point(313, 30);
            this.lbl_importOneFile.Name = "lbl_importOneFile";
            this.lbl_importOneFile.Size = new System.Drawing.Size(89, 15);
            this.lbl_importOneFile.TabIndex = 17;
            this.lbl_importOneFile.Text = "lbl_importOneFile";
            // 
            // rbt_importFromAnotherFolder
            // 
            this.rbt_importFromAnotherFolder.AutoSize = true;
            this.rbt_importFromAnotherFolder.Location = new System.Drawing.Point(31, 92);
            this.rbt_importFromAnotherFolder.Name = "rbt_importFromAnotherFolder";
            this.rbt_importFromAnotherFolder.Size = new System.Drawing.Size(160, 17);
            this.rbt_importFromAnotherFolder.TabIndex = 16;
            this.rbt_importFromAnotherFolder.TabStop = true;
            this.rbt_importFromAnotherFolder.Text = "rbt_importFromAnotherFolder";
            this.rbt_importFromAnotherFolder.UseVisualStyleBackColor = true;
            this.rbt_importFromAnotherFolder.CheckedChanged += new System.EventHandler(this.rbt_importFromAnotherFolder_CheckedChanged);
            // 
            // rbt_importFromCurrentFolder
            // 
            this.rbt_importFromCurrentFolder.AutoSize = true;
            this.rbt_importFromCurrentFolder.Location = new System.Drawing.Point(31, 60);
            this.rbt_importFromCurrentFolder.Name = "rbt_importFromCurrentFolder";
            this.rbt_importFromCurrentFolder.Size = new System.Drawing.Size(157, 17);
            this.rbt_importFromCurrentFolder.TabIndex = 15;
            this.rbt_importFromCurrentFolder.TabStop = true;
            this.rbt_importFromCurrentFolder.Text = "rbt_importFromCurrentFolder";
            this.rbt_importFromCurrentFolder.UseVisualStyleBackColor = true;
            this.rbt_importFromCurrentFolder.CheckedChanged += new System.EventHandler(this.rbt_importFromCurrentFolder_CheckedChanged);
            // 
            // rbt_importOneFile
            // 
            this.rbt_importOneFile.AutoSize = true;
            this.rbt_importOneFile.Location = new System.Drawing.Point(31, 28);
            this.rbt_importOneFile.Name = "rbt_importOneFile";
            this.rbt_importOneFile.Size = new System.Drawing.Size(107, 17);
            this.rbt_importOneFile.TabIndex = 14;
            this.rbt_importOneFile.TabStop = true;
            this.rbt_importOneFile.Text = "rbt_importOneFile";
            this.rbt_importOneFile.UseVisualStyleBackColor = true;
            this.rbt_importOneFile.CheckedChanged += new System.EventHandler(this.rbt_importOneFile_CheckedChanged);
            // 
            // gbx_ImportGPXTimeSetting
            // 
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.btn_PullMostRecentTrackSyncShift);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.nud_GeoMaxExtSecs);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.nud_GeoMaxIntSecs);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_GeoMaxExtSecs);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_GeoMaxIntSecs);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_CameraTimeNote);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_TZNote);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_TZValue);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.ckb_UseDST);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_TZTime);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.ckb_UseTimeZone);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.cbx_ImportUseTimeZone);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_CameraTimeData);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_CameraTime);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_ImportGPXSeconds);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_ImportGPXMinutes);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_ImportGPXHours);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_ImportGPXDays);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.nud_Seconds);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.nud_Minutes);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.nud_Hours);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.nud_Days);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_ShiftTrackTimeBy);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.cbx_ImportTimeAgainst);
            this.gbx_ImportGPXTimeSetting.Controls.Add(this.lbl_ImportTimeAgainst);
            this.gbx_ImportGPXTimeSetting.Location = new System.Drawing.Point(3, 160);
            this.gbx_ImportGPXTimeSetting.Name = "gbx_ImportGPXTimeSetting";
            this.gbx_ImportGPXTimeSetting.Size = new System.Drawing.Size(725, 381);
            this.gbx_ImportGPXTimeSetting.TabIndex = 29;
            this.gbx_ImportGPXTimeSetting.TabStop = false;
            this.gbx_ImportGPXTimeSetting.Text = "gbx_ImportGPXTimeSetting";
            // 
            // btn_PullMostRecentTrackSyncShift
            // 
            this.btn_PullMostRecentTrackSyncShift.Location = new System.Drawing.Point(47, 332);
            this.btn_PullMostRecentTrackSyncShift.Name = "btn_PullMostRecentTrackSyncShift";
            this.btn_PullMostRecentTrackSyncShift.Size = new System.Drawing.Size(177, 23);
            this.btn_PullMostRecentTrackSyncShift.TabIndex = 25;
            this.btn_PullMostRecentTrackSyncShift.Text = "btn_PullMostRecentTrackSyncShift";
            this.btn_PullMostRecentTrackSyncShift.UseVisualStyleBackColor = true;
            this.btn_PullMostRecentTrackSyncShift.Click += new System.EventHandler(this.btn_PullMostRecentTrackSyncShift_Click);
            // 
            // nud_GeoMaxExtSecs
            // 
            this.nud_GeoMaxExtSecs.Location = new System.Drawing.Point(540, 196);
            this.nud_GeoMaxExtSecs.Maximum = new decimal(new int[] {
            86400,
            0,
            0,
            0});
            this.nud_GeoMaxExtSecs.Name = "nud_GeoMaxExtSecs";
            this.nud_GeoMaxExtSecs.Size = new System.Drawing.Size(61, 20);
            this.nud_GeoMaxExtSecs.TabIndex = 24;
            this.nud_GeoMaxExtSecs.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nud_GeoMaxExtSecs.Value = new decimal(new int[] {
            1800,
            0,
            0,
            0});
            // 
            // nud_GeoMaxIntSecs
            // 
            this.nud_GeoMaxIntSecs.Location = new System.Drawing.Point(540, 162);
            this.nud_GeoMaxIntSecs.Maximum = new decimal(new int[] {
            86400,
            0,
            0,
            0});
            this.nud_GeoMaxIntSecs.Name = "nud_GeoMaxIntSecs";
            this.nud_GeoMaxIntSecs.Size = new System.Drawing.Size(61, 20);
            this.nud_GeoMaxIntSecs.TabIndex = 23;
            this.nud_GeoMaxIntSecs.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nud_GeoMaxIntSecs.Value = new decimal(new int[] {
            1800,
            0,
            0,
            0});
            // 
            // lbl_GeoMaxExtSecs
            // 
            this.lbl_GeoMaxExtSecs.AutoSize = true;
            this.lbl_GeoMaxExtSecs.Location = new System.Drawing.Point(27, 198);
            this.lbl_GeoMaxExtSecs.MaximumSize = new System.Drawing.Size(600, 0);
            this.lbl_GeoMaxExtSecs.Name = "lbl_GeoMaxExtSecs";
            this.lbl_GeoMaxExtSecs.Size = new System.Drawing.Size(102, 13);
            this.lbl_GeoMaxExtSecs.TabIndex = 22;
            this.lbl_GeoMaxExtSecs.Text = "lbl_GeoMaxExtSecs";
            // 
            // lbl_GeoMaxIntSecs
            // 
            this.lbl_GeoMaxIntSecs.AutoSize = true;
            this.lbl_GeoMaxIntSecs.Location = new System.Drawing.Point(27, 162);
            this.lbl_GeoMaxIntSecs.MaximumSize = new System.Drawing.Size(600, 0);
            this.lbl_GeoMaxIntSecs.Name = "lbl_GeoMaxIntSecs";
            this.lbl_GeoMaxIntSecs.Size = new System.Drawing.Size(99, 13);
            this.lbl_GeoMaxIntSecs.TabIndex = 21;
            this.lbl_GeoMaxIntSecs.Text = "lbl_GeoMaxIntSecs";
            // 
            // lbl_CameraTimeNote
            // 
            this.lbl_CameraTimeNote.AutoSize = true;
            this.lbl_CameraTimeNote.Location = new System.Drawing.Point(27, 234);
            this.lbl_CameraTimeNote.MaximumSize = new System.Drawing.Size(600, 0);
            this.lbl_CameraTimeNote.Name = "lbl_CameraTimeNote";
            this.lbl_CameraTimeNote.Size = new System.Drawing.Size(105, 13);
            this.lbl_CameraTimeNote.TabIndex = 20;
            this.lbl_CameraTimeNote.Text = "lbl_CameraTimeNote";
            // 
            // lbl_TZNote
            // 
            this.lbl_TZNote.AutoSize = true;
            this.lbl_TZNote.Location = new System.Drawing.Point(27, 61);
            this.lbl_TZNote.MaximumSize = new System.Drawing.Size(600, 0);
            this.lbl_TZNote.Name = "lbl_TZNote";
            this.lbl_TZNote.Size = new System.Drawing.Size(60, 13);
            this.lbl_TZNote.TabIndex = 19;
            this.lbl_TZNote.Text = "lbl_TZNote";
            // 
            // lbl_TZValue
            // 
            this.lbl_TZValue.AutoSize = true;
            this.lbl_TZValue.Location = new System.Drawing.Point(448, 121);
            this.lbl_TZValue.Name = "lbl_TZValue";
            this.lbl_TZValue.Size = new System.Drawing.Size(64, 13);
            this.lbl_TZValue.TabIndex = 18;
            this.lbl_TZValue.Text = "lbl_TZValue";
            // 
            // ckb_UseDST
            // 
            this.ckb_UseDST.AutoSize = true;
            this.ckb_UseDST.Location = new System.Drawing.Point(47, 119);
            this.ckb_UseDST.Name = "ckb_UseDST";
            this.ckb_UseDST.Size = new System.Drawing.Size(91, 17);
            this.ckb_UseDST.TabIndex = 17;
            this.ckb_UseDST.Text = "ckb_UseDST";
            this.ckb_UseDST.UseVisualStyleBackColor = true;
            this.ckb_UseDST.CheckedChanged += new System.EventHandler(this.ckb_UseDST_CheckedChanged);
            // 
            // lbl_TZTime
            // 
            this.lbl_TZTime.AutoSize = true;
            this.lbl_TZTime.Location = new System.Drawing.Point(272, 121);
            this.lbl_TZTime.Name = "lbl_TZTime";
            this.lbl_TZTime.Size = new System.Drawing.Size(60, 13);
            this.lbl_TZTime.TabIndex = 16;
            this.lbl_TZTime.Text = "lbl_TZTime";
            // 
            // ckb_UseTimeZone
            // 
            this.ckb_UseTimeZone.AutoSize = true;
            this.ckb_UseTimeZone.Location = new System.Drawing.Point(30, 93);
            this.ckb_UseTimeZone.Name = "ckb_UseTimeZone";
            this.ckb_UseTimeZone.Size = new System.Drawing.Size(117, 17);
            this.ckb_UseTimeZone.TabIndex = 15;
            this.ckb_UseTimeZone.Text = "ckb_UseTimeZone";
            this.ckb_UseTimeZone.UseVisualStyleBackColor = true;
            this.ckb_UseTimeZone.CheckedChanged += new System.EventHandler(this.ckb_UseTimeZone_CheckedChanged);
            // 
            // cbx_ImportUseTimeZone
            // 
            this.cbx_ImportUseTimeZone.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_ImportUseTimeZone.FormattingEnabled = true;
            this.cbx_ImportUseTimeZone.Location = new System.Drawing.Point(272, 93);
            this.cbx_ImportUseTimeZone.Name = "cbx_ImportUseTimeZone";
            this.cbx_ImportUseTimeZone.Size = new System.Drawing.Size(437, 21);
            this.cbx_ImportUseTimeZone.TabIndex = 14;
            this.cbx_ImportUseTimeZone.SelectedIndexChanged += new System.EventHandler(this.cbx_UseTimeZone_SelectedIndexChanged);
            // 
            // lbl_CameraTimeData
            // 
            this.lbl_CameraTimeData.AutoSize = true;
            this.lbl_CameraTimeData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbl_CameraTimeData.Location = new System.Drawing.Point(494, 294);
            this.lbl_CameraTimeData.Name = "lbl_CameraTimeData";
            this.lbl_CameraTimeData.Size = new System.Drawing.Size(107, 15);
            this.lbl_CameraTimeData.TabIndex = 12;
            this.lbl_CameraTimeData.Text = "lbl_CameraTimeData";
            // 
            // lbl_CameraTime
            // 
            this.lbl_CameraTime.AutoSize = true;
            this.lbl_CameraTime.Location = new System.Drawing.Point(491, 267);
            this.lbl_CameraTime.Name = "lbl_CameraTime";
            this.lbl_CameraTime.Size = new System.Drawing.Size(82, 13);
            this.lbl_CameraTime.TabIndex = 11;
            this.lbl_CameraTime.Text = "lbl_CameraTime";
            // 
            // lbl_ImportGPXSeconds
            // 
            this.lbl_ImportGPXSeconds.AutoSize = true;
            this.lbl_ImportGPXSeconds.Location = new System.Drawing.Point(407, 267);
            this.lbl_ImportGPXSeconds.Name = "lbl_ImportGPXSeconds";
            this.lbl_ImportGPXSeconds.Size = new System.Drawing.Size(116, 13);
            this.lbl_ImportGPXSeconds.TabIndex = 10;
            this.lbl_ImportGPXSeconds.Text = "lbl_ImportGPXSeconds";
            // 
            // lbl_ImportGPXMinutes
            // 
            this.lbl_ImportGPXMinutes.AutoSize = true;
            this.lbl_ImportGPXMinutes.Location = new System.Drawing.Point(332, 267);
            this.lbl_ImportGPXMinutes.Name = "lbl_ImportGPXMinutes";
            this.lbl_ImportGPXMinutes.Size = new System.Drawing.Size(111, 13);
            this.lbl_ImportGPXMinutes.TabIndex = 9;
            this.lbl_ImportGPXMinutes.Text = "lbl_ImportGPXMinutes";
            // 
            // lbl_ImportGPXHours
            // 
            this.lbl_ImportGPXHours.AutoSize = true;
            this.lbl_ImportGPXHours.Location = new System.Drawing.Point(266, 267);
            this.lbl_ImportGPXHours.Name = "lbl_ImportGPXHours";
            this.lbl_ImportGPXHours.Size = new System.Drawing.Size(102, 13);
            this.lbl_ImportGPXHours.TabIndex = 8;
            this.lbl_ImportGPXHours.Text = "lbl_ImportGPXHours";
            // 
            // lbl_ImportGPXDays
            // 
            this.lbl_ImportGPXDays.AutoSize = true;
            this.lbl_ImportGPXDays.Location = new System.Drawing.Point(204, 267);
            this.lbl_ImportGPXDays.Name = "lbl_ImportGPXDays";
            this.lbl_ImportGPXDays.Size = new System.Drawing.Size(98, 13);
            this.lbl_ImportGPXDays.TabIndex = 7;
            this.lbl_ImportGPXDays.Text = "lbl_ImportGPXDays";
            // 
            // nud_Seconds
            // 
            this.nud_Seconds.Location = new System.Drawing.Point(420, 293);
            this.nud_Seconds.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
            this.nud_Seconds.Minimum = new decimal(new int[] {
            59,
            0,
            0,
            -2147483648});
            this.nud_Seconds.Name = "nud_Seconds";
            this.nud_Seconds.Size = new System.Drawing.Size(36, 20);
            this.nud_Seconds.TabIndex = 6;
            // 
            // nud_Minutes
            // 
            this.nud_Minutes.Location = new System.Drawing.Point(345, 293);
            this.nud_Minutes.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
            this.nud_Minutes.Minimum = new decimal(new int[] {
            59,
            0,
            0,
            -2147483648});
            this.nud_Minutes.Name = "nud_Minutes";
            this.nud_Minutes.Size = new System.Drawing.Size(36, 20);
            this.nud_Minutes.TabIndex = 5;
            // 
            // nud_Hours
            // 
            this.nud_Hours.Location = new System.Drawing.Point(279, 293);
            this.nud_Hours.Maximum = new decimal(new int[] {
            23,
            0,
            0,
            0});
            this.nud_Hours.Minimum = new decimal(new int[] {
            23,
            0,
            0,
            -2147483648});
            this.nud_Hours.Name = "nud_Hours";
            this.nud_Hours.Size = new System.Drawing.Size(36, 20);
            this.nud_Hours.TabIndex = 4;
            // 
            // nud_Days
            // 
            this.nud_Days.Location = new System.Drawing.Point(214, 293);
            this.nud_Days.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.nud_Days.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            -2147483648});
            this.nud_Days.Name = "nud_Days";
            this.nud_Days.Size = new System.Drawing.Size(36, 20);
            this.nud_Days.TabIndex = 3;
            // 
            // lbl_ShiftTrackTimeBy
            // 
            this.lbl_ShiftTrackTimeBy.AutoSize = true;
            this.lbl_ShiftTrackTimeBy.Location = new System.Drawing.Point(27, 294);
            this.lbl_ShiftTrackTimeBy.Name = "lbl_ShiftTrackTimeBy";
            this.lbl_ShiftTrackTimeBy.Size = new System.Drawing.Size(107, 13);
            this.lbl_ShiftTrackTimeBy.TabIndex = 2;
            this.lbl_ShiftTrackTimeBy.Text = "lbl_ShiftTrackTimeBy";
            // 
            // cbx_ImportTimeAgainst
            // 
            this.cbx_ImportTimeAgainst.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_ImportTimeAgainst.FormattingEnabled = true;
            this.cbx_ImportTimeAgainst.Location = new System.Drawing.Point(358, 26);
            this.cbx_ImportTimeAgainst.Name = "cbx_ImportTimeAgainst";
            this.cbx_ImportTimeAgainst.Size = new System.Drawing.Size(125, 21);
            this.cbx_ImportTimeAgainst.TabIndex = 1;
            // 
            // lbl_ImportTimeAgainst
            // 
            this.lbl_ImportTimeAgainst.AutoSize = true;
            this.lbl_ImportTimeAgainst.Location = new System.Drawing.Point(27, 29);
            this.lbl_ImportTimeAgainst.Name = "lbl_ImportTimeAgainst";
            this.lbl_ImportTimeAgainst.Size = new System.Drawing.Size(110, 13);
            this.lbl_ImportTimeAgainst.TabIndex = 0;
            this.lbl_ImportTimeAgainst.Text = "lbl_ImportTimeAgainst";
            // 
            // tpg_ImportExport_Export
            // 
            this.tpg_ImportExport_Export.AutoScroll = true;
            this.tpg_ImportExport_Export.AutoScrollMargin = new System.Drawing.Size(0, 10);
            this.tpg_ImportExport_Export.Controls.Add(this.gbx_SaveTrackOptions);
            this.tpg_ImportExport_Export.Location = new System.Drawing.Point(4, 22);
            this.tpg_ImportExport_Export.Name = "tpg_ImportExport_Export";
            this.tpg_ImportExport_Export.Padding = new System.Windows.Forms.Padding(3);
            this.tpg_ImportExport_Export.Size = new System.Drawing.Size(769, 390);
            this.tpg_ImportExport_Export.TabIndex = 1;
            this.tpg_ImportExport_Export.Text = "tpg_ImportExport_Export";
            this.tpg_ImportExport_Export.UseVisualStyleBackColor = true;
            // 
            // gbx_SaveTrackOptions
            // 
            this.gbx_SaveTrackOptions.Controls.Add(this.pbx_GPSDataPaste);
            this.gbx_SaveTrackOptions.Controls.Add(this.lbl_ExportTrackTimeStampTypeNote);
            this.gbx_SaveTrackOptions.Controls.Add(this.cbx_ExportTrackTimeStampType);
            this.gbx_SaveTrackOptions.Controls.Add(this.lbl_ExportTrackTimeStampType);
            this.gbx_SaveTrackOptions.Controls.Add(this.cbx_ExportTrackOrderBy);
            this.gbx_SaveTrackOptions.Controls.Add(this.lbl_ExportTrackOrderBy);
            this.gbx_SaveTrackOptions.Controls.Add(this.ckb_ExportTrackIncludeAltitude);
            this.gbx_SaveTrackOptions.Controls.Add(this.pbx_Browse_SaveTo_Folder);
            this.gbx_SaveTrackOptions.Controls.Add(this.lbl_SaveTrackTo);
            this.gbx_SaveTrackOptions.Controls.Add(this.tbx_SaveTrackTo);
            this.gbx_SaveTrackOptions.Location = new System.Drawing.Point(23, 16);
            this.gbx_SaveTrackOptions.Name = "gbx_SaveTrackOptions";
            this.gbx_SaveTrackOptions.Size = new System.Drawing.Size(706, 194);
            this.gbx_SaveTrackOptions.TabIndex = 0;
            this.gbx_SaveTrackOptions.TabStop = false;
            this.gbx_SaveTrackOptions.Text = "gbx_SaveTrackOptions";
            // 
            // pbx_GPSDataPaste
            // 
            this.pbx_GPSDataPaste.Image = ((System.Drawing.Image)(resources.GetObject("pbx_GPSDataPaste.Image")));
            this.pbx_GPSDataPaste.Location = new System.Drawing.Point(35, 151);
            this.pbx_GPSDataPaste.Name = "pbx_GPSDataPaste";
            this.pbx_GPSDataPaste.Size = new System.Drawing.Size(16, 16);
            this.pbx_GPSDataPaste.TabIndex = 25;
            this.pbx_GPSDataPaste.TabStop = false;
            // 
            // lbl_ExportTrackTimeStampTypeNote
            // 
            this.lbl_ExportTrackTimeStampTypeNote.AutoSize = true;
            this.lbl_ExportTrackTimeStampTypeNote.Location = new System.Drawing.Point(56, 151);
            this.lbl_ExportTrackTimeStampTypeNote.Name = "lbl_ExportTrackTimeStampTypeNote";
            this.lbl_ExportTrackTimeStampTypeNote.Size = new System.Drawing.Size(181, 13);
            this.lbl_ExportTrackTimeStampTypeNote.TabIndex = 24;
            this.lbl_ExportTrackTimeStampTypeNote.Text = "lbl_ExportTrackTimeStampTypeNote";
            // 
            // cbx_ExportTrackTimeStampType
            // 
            this.cbx_ExportTrackTimeStampType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_ExportTrackTimeStampType.FormattingEnabled = true;
            this.cbx_ExportTrackTimeStampType.Location = new System.Drawing.Point(358, 120);
            this.cbx_ExportTrackTimeStampType.Name = "cbx_ExportTrackTimeStampType";
            this.cbx_ExportTrackTimeStampType.Size = new System.Drawing.Size(125, 21);
            this.cbx_ExportTrackTimeStampType.TabIndex = 23;
            // 
            // lbl_ExportTrackTimeStampType
            // 
            this.lbl_ExportTrackTimeStampType.AutoSize = true;
            this.lbl_ExportTrackTimeStampType.Location = new System.Drawing.Point(32, 123);
            this.lbl_ExportTrackTimeStampType.Name = "lbl_ExportTrackTimeStampType";
            this.lbl_ExportTrackTimeStampType.Size = new System.Drawing.Size(158, 13);
            this.lbl_ExportTrackTimeStampType.TabIndex = 22;
            this.lbl_ExportTrackTimeStampType.Text = "lbl_ExportTrackTimeStampType";
            // 
            // cbx_ExportTrackOrderBy
            // 
            this.cbx_ExportTrackOrderBy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_ExportTrackOrderBy.FormattingEnabled = true;
            this.cbx_ExportTrackOrderBy.Location = new System.Drawing.Point(358, 93);
            this.cbx_ExportTrackOrderBy.Name = "cbx_ExportTrackOrderBy";
            this.cbx_ExportTrackOrderBy.Size = new System.Drawing.Size(125, 21);
            this.cbx_ExportTrackOrderBy.TabIndex = 21;
            // 
            // lbl_ExportTrackOrderBy
            // 
            this.lbl_ExportTrackOrderBy.AutoSize = true;
            this.lbl_ExportTrackOrderBy.Location = new System.Drawing.Point(32, 96);
            this.lbl_ExportTrackOrderBy.Name = "lbl_ExportTrackOrderBy";
            this.lbl_ExportTrackOrderBy.Size = new System.Drawing.Size(119, 13);
            this.lbl_ExportTrackOrderBy.TabIndex = 20;
            this.lbl_ExportTrackOrderBy.Text = "lbl_ExportTrackOrderBy";
            // 
            // ckb_ExportTrackIncludeAltitude
            // 
            this.ckb_ExportTrackIncludeAltitude.AutoSize = true;
            this.ckb_ExportTrackIncludeAltitude.Location = new System.Drawing.Point(32, 63);
            this.ckb_ExportTrackIncludeAltitude.Name = "ckb_ExportTrackIncludeAltitude";
            this.ckb_ExportTrackIncludeAltitude.Size = new System.Drawing.Size(178, 17);
            this.ckb_ExportTrackIncludeAltitude.TabIndex = 20;
            this.ckb_ExportTrackIncludeAltitude.Text = "ckb_ExportTrackIncludeAltitude";
            this.ckb_ExportTrackIncludeAltitude.UseVisualStyleBackColor = true;
            // 
            // pbx_Browse_SaveTo_Folder
            // 
            this.pbx_Browse_SaveTo_Folder.Image = ((System.Drawing.Image)(resources.GetObject("pbx_Browse_SaveTo_Folder.Image")));
            this.pbx_Browse_SaveTo_Folder.Location = new System.Drawing.Point(530, 34);
            this.pbx_Browse_SaveTo_Folder.Margin = new System.Windows.Forms.Padding(2);
            this.pbx_Browse_SaveTo_Folder.Name = "pbx_Browse_SaveTo_Folder";
            this.pbx_Browse_SaveTo_Folder.Size = new System.Drawing.Size(16, 16);
            this.pbx_Browse_SaveTo_Folder.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pbx_Browse_SaveTo_Folder.TabIndex = 17;
            this.pbx_Browse_SaveTo_Folder.TabStop = false;
            this.pbx_Browse_SaveTo_Folder.Click += new System.EventHandler(this.pbx_Browse_SaveTo_Folder_Click);
            // 
            // lbl_SaveTrackTo
            // 
            this.lbl_SaveTrackTo.AutoSize = true;
            this.lbl_SaveTrackTo.Location = new System.Drawing.Point(23, 34);
            this.lbl_SaveTrackTo.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_SaveTrackTo.Name = "lbl_SaveTrackTo";
            this.lbl_SaveTrackTo.Size = new System.Drawing.Size(89, 13);
            this.lbl_SaveTrackTo.TabIndex = 16;
            this.lbl_SaveTrackTo.Text = "lbl_SaveTrackTo";
            // 
            // tbx_SaveTrackTo
            // 
            this.tbx_SaveTrackTo.Location = new System.Drawing.Point(139, 30);
            this.tbx_SaveTrackTo.Margin = new System.Windows.Forms.Padding(2);
            this.tbx_SaveTrackTo.Name = "tbx_SaveTrackTo";
            this.tbx_SaveTrackTo.ReadOnly = true;
            this.tbx_SaveTrackTo.Size = new System.Drawing.Size(372, 20);
            this.tbx_SaveTrackTo.TabIndex = 15;
            // 
            // ofd_SaveTrackTo
            // 
            this.ofd_SaveTrackTo.CheckFileExists = false;
            this.ofd_SaveTrackTo.DefaultExt = "gpx";
            this.ofd_SaveTrackTo.FileName = "trackfile.gpx";
            // 
            // btn_Cancel
            // 
            this.btn_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Cancel.Location = new System.Drawing.Point(702, 439);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(75, 23);
            this.btn_Cancel.TabIndex = 33;
            this.btn_Cancel.Text = "btn_Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = true;
            this.btn_Cancel.Click += new System.EventHandler(this.btn_Cancel_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.Location = new System.Drawing.Point(608, 439);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(75, 23);
            this.btn_OK.TabIndex = 32;
            this.btn_OK.Text = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // FrmImportExportGpx
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(815, 470);
            this.ControlBox = false;
            this.Controls.Add(this.btn_Cancel);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.tcr_ImportExport);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmImportExportGpx";
            this.ShowInTaskbar = false;
            this.Text = "FrmImportExportGpx";
            this.tcr_ImportExport.ResumeLayout(false);
            this.tpg_ImportExport_Import.ResumeLayout(false);
            this.gbx_OtherSettings.ResumeLayout(false);
            this.gbx_OtherSettings.PerformLayout();
            this.gbx_ImportGPXSource.ResumeLayout(false);
            this.gbx_ImportGPXSource.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_importOneFile)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_importFromAnotherFolder)).EndInit();
            this.gbx_ImportGPXTimeSetting.ResumeLayout(false);
            this.gbx_ImportGPXTimeSetting.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_GeoMaxExtSecs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_GeoMaxIntSecs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Seconds)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Minutes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Hours)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_Days)).EndInit();
            this.tpg_ImportExport_Export.ResumeLayout(false);
            this.gbx_SaveTrackOptions.ResumeLayout(false);
            this.gbx_SaveTrackOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_GPSDataPaste)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_Browse_SaveTo_Folder)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tcr_ImportExport;
        private System.Windows.Forms.TabPage tpg_ImportExport_Import;
        private System.Windows.Forms.GroupBox gbx_OtherSettings;
        private System.Windows.Forms.CheckBox ckb_OverlayGPXForSelectedDatesOnly;
        private System.Windows.Forms.CheckBox ckb_LoadTrackOntoMap;
        private System.Windows.Forms.CheckBox ckb_DoNotQueryAPI;
        private System.Windows.Forms.GroupBox gbx_ImportGPXSource;
        private System.Windows.Forms.PictureBox pbx_importOneFile;
        private System.Windows.Forms.PictureBox pbx_importFromAnotherFolder;
        private System.Windows.Forms.Label lbl_importFromAnotherFolder;
        private System.Windows.Forms.Label lbl_importOneFile;
        private System.Windows.Forms.RadioButton rbt_importFromAnotherFolder;
        private System.Windows.Forms.RadioButton rbt_importFromCurrentFolder;
        private System.Windows.Forms.RadioButton rbt_importOneFile;
        private System.Windows.Forms.GroupBox gbx_ImportGPXTimeSetting;
        private System.Windows.Forms.Button btn_PullMostRecentTrackSyncShift;
        private System.Windows.Forms.NumericUpDown nud_GeoMaxExtSecs;
        private System.Windows.Forms.NumericUpDown nud_GeoMaxIntSecs;
        private System.Windows.Forms.Label lbl_GeoMaxExtSecs;
        private System.Windows.Forms.Label lbl_GeoMaxIntSecs;
        private System.Windows.Forms.Label lbl_CameraTimeNote;
        private System.Windows.Forms.Label lbl_TZNote;
        private System.Windows.Forms.Label lbl_TZValue;
        private System.Windows.Forms.CheckBox ckb_UseDST;
        private System.Windows.Forms.Label lbl_TZTime;
        private System.Windows.Forms.CheckBox ckb_UseTimeZone;
        private System.Windows.Forms.ComboBox cbx_ImportUseTimeZone;
        private System.Windows.Forms.Label lbl_CameraTimeData;
        private System.Windows.Forms.Label lbl_CameraTime;
        private System.Windows.Forms.Label lbl_ImportGPXSeconds;
        private System.Windows.Forms.Label lbl_ImportGPXMinutes;
        private System.Windows.Forms.Label lbl_ImportGPXHours;
        private System.Windows.Forms.Label lbl_ImportGPXDays;
        private System.Windows.Forms.NumericUpDown nud_Seconds;
        private System.Windows.Forms.NumericUpDown nud_Minutes;
        private System.Windows.Forms.NumericUpDown nud_Hours;
        private System.Windows.Forms.NumericUpDown nud_Days;
        private System.Windows.Forms.Label lbl_ShiftTrackTimeBy;
        private System.Windows.Forms.ComboBox cbx_ImportTimeAgainst;
        private System.Windows.Forms.Label lbl_ImportTimeAgainst;
        private System.Windows.Forms.TabPage tpg_ImportExport_Export;
        private System.Windows.Forms.OpenFileDialog ofd_importOneFile;
        private System.Windows.Forms.FolderBrowserDialog fbd_importFromAnotherFolder;
        private System.Windows.Forms.GroupBox gbx_SaveTrackOptions;
        private System.Windows.Forms.PictureBox pbx_Browse_SaveTo_Folder;
        private System.Windows.Forms.Label lbl_SaveTrackTo;
        public System.Windows.Forms.TextBox tbx_SaveTrackTo;
        private System.Windows.Forms.OpenFileDialog ofd_SaveTrackTo;
        private System.Windows.Forms.Button btn_Cancel;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.CheckBox ckb_ExportTrackIncludeAltitude;
        private System.Windows.Forms.ComboBox cbx_ExportTrackOrderBy;
        private System.Windows.Forms.Label lbl_ExportTrackOrderBy;
        private System.Windows.Forms.ComboBox cbx_ExportTrackTimeStampType;
        private System.Windows.Forms.Label lbl_ExportTrackTimeStampType;
        private System.Windows.Forms.Label lbl_ExportTrackTimeStampTypeNote;
        private System.Windows.Forms.PictureBox pbx_GPSDataPaste;
    }
}