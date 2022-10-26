using System.Reflection;
using System.Windows.Forms;

namespace GeoTagNinja
{
    partial class frm_MainApp
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frm_MainApp));
            this.tbx_lat = new System.Windows.Forms.TextBox();
            this.lbl_lat = new System.Windows.Forms.Label();
            this.lbl_lng = new System.Windows.Forms.Label();
            this.tbx_lng = new System.Windows.Forms.TextBox();
            this.btn_NavigateMapGo = new System.Windows.Forms.Button();
            this.mns_MenuStrip = new System.Windows.Forms.MenuStrip();
            this.tmi_File = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_SaveAll = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_EditFiles = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_CopyGeoData = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_PasteGeoData = new System.Windows.Forms.ToolStripMenuItem();
            this.tss_ToolStripSeparator_Main = new System.Windows.Forms.ToolStripSeparator();
            this.tmi_File_Quit = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Settings = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Settings_Settings = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Help = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Help_About = new System.Windows.Forms.ToolStripMenuItem();
            this.tct_Main = new System.Windows.Forms.TabControl();
            this.tpg_Map = new System.Windows.Forms.TabPage();
            this.wbv_MapArea = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.lvw_FileList = new System.Windows.Forms.ListView();
            this.clh_FileName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tsr_MainAppToolStrip = new System.Windows.Forms.ToolStrip();
            this.btn_SaveFiles = new System.Windows.Forms.ToolStripButton();
            this.btn_Refresh_lvwFileList = new System.Windows.Forms.ToolStripButton();
            this.btn_EditFile = new System.Windows.Forms.ToolStripButton();
            this.btn_RemoveGeoData = new System.Windows.Forms.ToolStripButton();
            this.tsr_FolderControl = new System.Windows.Forms.ToolStrip();
            this.tbx_FolderName = new System.Windows.Forms.ToolStripTextBox();
            this.btn_OneFolderUp = new System.Windows.Forms.ToolStripButton();
            this.btn_ts_Refresh_lvwFileList = new System.Windows.Forms.ToolStripButton();
            this.btn_loctToFile = new System.Windows.Forms.Button();
            this.lbl_ParseProgress = new System.Windows.Forms.Label();
            this.pbx_imagePreview = new System.Windows.Forms.PictureBox();
            this.mns_MenuStrip.SuspendLayout();
            this.tct_Main.SuspendLayout();
            this.tpg_Map.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.wbv_MapArea)).BeginInit();
            this.tsr_MainAppToolStrip.SuspendLayout();
            this.tsr_FolderControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_imagePreview)).BeginInit();
            this.SuspendLayout();
            // 
            // tbx_lat
            // 
            this.tbx_lat.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.tbx_lat.Location = new System.Drawing.Point(403, 411);
            this.tbx_lat.Margin = new System.Windows.Forms.Padding(0);
            this.tbx_lat.MaxLength = 20;
            this.tbx_lat.Name = "tbx_lat";
            this.tbx_lat.Size = new System.Drawing.Size(79, 20);
            this.tbx_lat.TabIndex = 2;
            this.tbx_lat.Text = "0";
            // 
            // lbl_lat
            // 
            this.lbl_lat.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lbl_lat.AutoSize = true;
            this.lbl_lat.Location = new System.Drawing.Point(373, 411);
            this.lbl_lat.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_lat.Name = "lbl_lat";
            this.lbl_lat.Size = new System.Drawing.Size(34, 13);
            this.lbl_lat.TabIndex = 1;
            this.lbl_lat.Text = "lbl_lat";
            // 
            // lbl_lng
            // 
            this.lbl_lng.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lbl_lng.AutoSize = true;
            this.lbl_lng.Location = new System.Drawing.Point(491, 411);
            this.lbl_lng.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_lng.Name = "lbl_lng";
            this.lbl_lng.Size = new System.Drawing.Size(37, 13);
            this.lbl_lng.TabIndex = 3;
            this.lbl_lng.Text = "lbl_lng";
            // 
            // tbx_lng
            // 
            this.tbx_lng.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.tbx_lng.Location = new System.Drawing.Point(526, 411);
            this.tbx_lng.Margin = new System.Windows.Forms.Padding(0);
            this.tbx_lng.MaxLength = 20;
            this.tbx_lng.Name = "tbx_lng";
            this.tbx_lng.Size = new System.Drawing.Size(79, 20);
            this.tbx_lng.TabIndex = 4;
            this.tbx_lng.Text = "0";
            // 
            // btn_NavigateMapGo
            // 
            this.btn_NavigateMapGo.Location = new System.Drawing.Point(607, 409);
            this.btn_NavigateMapGo.Margin = new System.Windows.Forms.Padding(2);
            this.btn_NavigateMapGo.Name = "btn_NavigateMapGo";
            this.btn_NavigateMapGo.Size = new System.Drawing.Size(41, 23);
            this.btn_NavigateMapGo.TabIndex = 5;
            this.btn_NavigateMapGo.Text = "btn_NavigateMapGo";
            this.btn_NavigateMapGo.UseVisualStyleBackColor = true;
            this.btn_NavigateMapGo.Click += new System.EventHandler(this.btn_NavigateMapGo_Click);
            // 
            // mns_MenuStrip
            // 
            this.mns_MenuStrip.ImageScalingSize = new System.Drawing.Size(28, 28);
            this.mns_MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_File,
            this.tmi_Settings,
            this.tmi_Help});
            this.mns_MenuStrip.Location = new System.Drawing.Point(0, 0);
            this.mns_MenuStrip.Name = "mns_MenuStrip";
            this.mns_MenuStrip.Size = new System.Drawing.Size(730, 24);
            this.mns_MenuStrip.TabIndex = 9;
            this.mns_MenuStrip.Text = "mns_MenuStrip";
            // 
            // tmi_File
            // 
            this.tmi_File.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_File_SaveAll,
            this.tmi_File_EditFiles,
            this.tmi_File_CopyGeoData,
            this.tmi_File_PasteGeoData,
            this.tss_ToolStripSeparator_Main,
            this.tmi_File_Quit});
            this.tmi_File.Name = "tmi_File";
            this.tmi_File.Size = new System.Drawing.Size(60, 20);
            this.tmi_File.Text = "tmi_File";
            // 
            // tmi_File_SaveAll
            // 
            this.tmi_File_SaveAll.Name = "tmi_File_SaveAll";
            this.tmi_File_SaveAll.ShortcutKeyDisplayString = "CTRL+S";
            this.tmi_File_SaveAll.Size = new System.Drawing.Size(275, 22);
            this.tmi_File_SaveAll.Text = "tmi_File_SaveAll";
            this.tmi_File_SaveAll.Click += new System.EventHandler(this.tmi_File_SaveAll_Click);
            // 
            // tmi_File_EditFiles
            // 
            this.tmi_File_EditFiles.Name = "tmi_File_EditFiles";
            this.tmi_File_EditFiles.ShortcutKeyDisplayString = "CTLR+Enter";
            this.tmi_File_EditFiles.Size = new System.Drawing.Size(275, 22);
            this.tmi_File_EditFiles.Text = "tmi_File_EditFiles";
            this.tmi_File_EditFiles.Click += new System.EventHandler(this.tmi_File_EditFiles_Click);
            // 
            // tmi_File_CopyGeoData
            // 
            this.tmi_File_CopyGeoData.Name = "tmi_File_CopyGeoData";
            this.tmi_File_CopyGeoData.ShortcutKeyDisplayString = "Sh + CTRL+C";
            this.tmi_File_CopyGeoData.Size = new System.Drawing.Size(275, 22);
            this.tmi_File_CopyGeoData.Text = "tmi_File_CopyGeoData";
            this.tmi_File_CopyGeoData.Click += new System.EventHandler(this.tmi_File_CopyGeoData_Click);
            // 
            // tmi_File_PasteGeoData
            // 
            this.tmi_File_PasteGeoData.Name = "tmi_File_PasteGeoData";
            this.tmi_File_PasteGeoData.ShortcutKeyDisplayString = "Sh + CTRL + V";
            this.tmi_File_PasteGeoData.Size = new System.Drawing.Size(275, 22);
            this.tmi_File_PasteGeoData.Text = "tmi_File_PasteGeoData";
            this.tmi_File_PasteGeoData.Click += new System.EventHandler(this.tmi_File_PasteGeoData_Click);
            // 
            // tss_ToolStripSeparator_Main
            // 
            this.tss_ToolStripSeparator_Main.Name = "tss_ToolStripSeparator_Main";
            this.tss_ToolStripSeparator_Main.Size = new System.Drawing.Size(272, 6);
            // 
            // tmi_File_Quit
            // 
            this.tmi_File_Quit.Name = "tmi_File_Quit";
            this.tmi_File_Quit.ShortcutKeyDisplayString = "ALT+F4";
            this.tmi_File_Quit.Size = new System.Drawing.Size(275, 22);
            this.tmi_File_Quit.Text = "tmi_File_Quit";
            this.tmi_File_Quit.Click += new System.EventHandler(this.tmi_File_Quit_Click);
            // 
            // tmi_Settings
            // 
            this.tmi_Settings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_Settings_Settings});
            this.tmi_Settings.Name = "tmi_Settings";
            this.tmi_Settings.Size = new System.Drawing.Size(84, 20);
            this.tmi_Settings.Text = "tmi_Settings";
            // 
            // tmi_Settings_Settings
            // 
            this.tmi_Settings_Settings.Name = "tmi_Settings_Settings";
            this.tmi_Settings_Settings.Size = new System.Drawing.Size(186, 22);
            this.tmi_Settings_Settings.Text = "tmi_Settings_Settings";
            this.tmi_Settings_Settings.Click += new System.EventHandler(this.tmi_Settings_Settings_Click);
            // 
            // tmi_Help
            // 
            this.tmi_Help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_Help_About});
            this.tmi_Help.Name = "tmi_Help";
            this.tmi_Help.Size = new System.Drawing.Size(67, 20);
            this.tmi_Help.Text = "tmi_Help";
            // 
            // tmi_Help_About
            // 
            this.tmi_Help_About.Name = "tmi_Help_About";
            this.tmi_Help_About.Size = new System.Drawing.Size(160, 22);
            this.tmi_Help_About.Text = "tmi_Help_About";
            this.tmi_Help_About.Click += new System.EventHandler(this.tmi_Help_About_Click);
            // 
            // tct_Main
            // 
            this.tct_Main.Controls.Add(this.tpg_Map);
            this.tct_Main.Location = new System.Drawing.Point(369, 76);
            this.tct_Main.Margin = new System.Windows.Forms.Padding(2);
            this.tct_Main.Name = "tct_Main";
            this.tct_Main.SelectedIndex = 0;
            this.tct_Main.Size = new System.Drawing.Size(350, 329);
            this.tct_Main.TabIndex = 1;
            // 
            // tpg_Map
            // 
            this.tpg_Map.Controls.Add(this.wbv_MapArea);
            this.tpg_Map.Location = new System.Drawing.Point(4, 22);
            this.tpg_Map.Margin = new System.Windows.Forms.Padding(2);
            this.tpg_Map.Name = "tpg_Map";
            this.tpg_Map.Padding = new System.Windows.Forms.Padding(2);
            this.tpg_Map.Size = new System.Drawing.Size(342, 303);
            this.tpg_Map.TabIndex = 0;
            this.tpg_Map.Text = "tpg_Map";
            this.tpg_Map.UseVisualStyleBackColor = true;
            // 
            // wbv_MapArea
            // 
            this.wbv_MapArea.AllowExternalDrop = true;
            this.wbv_MapArea.CreationProperties = null;
            this.wbv_MapArea.DefaultBackgroundColor = System.Drawing.Color.White;
            this.wbv_MapArea.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wbv_MapArea.Location = new System.Drawing.Point(2, 2);
            this.wbv_MapArea.Name = "wbv_MapArea";
            this.wbv_MapArea.Padding = new System.Windows.Forms.Padding(1);
            this.wbv_MapArea.Size = new System.Drawing.Size(338, 299);
            this.wbv_MapArea.TabIndex = 1;
            this.wbv_MapArea.ZoomFactor = 1D;
            // 
            // lvw_FileList
            // 
            this.lvw_FileList.BackColor = System.Drawing.Color.SeaShell;
            this.lvw_FileList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.clh_FileName});
            this.lvw_FileList.FullRowSelect = true;
            this.lvw_FileList.GridLines = true;
            this.lvw_FileList.HideSelection = false;
            this.lvw_FileList.Location = new System.Drawing.Point(12, 76);
            this.lvw_FileList.Margin = new System.Windows.Forms.Padding(2);
            this.lvw_FileList.Name = "lvw_FileList";
            this.lvw_FileList.Size = new System.Drawing.Size(338, 261);
            this.lvw_FileList.TabIndex = 11;
            this.lvw_FileList.UseCompatibleStateImageBehavior = false;
            this.lvw_FileList.View = System.Windows.Forms.View.Details;
            this.lvw_FileList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvw_FileList_KeyDown);
            this.lvw_FileList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lvw_FileList_MouseDoubleClick);
            this.lvw_FileList.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lvw_FileList_MouseUp);
            // 
            // clh_FileName
            // 
            this.clh_FileName.Name = "clh_FileName";
            this.clh_FileName.Text = "clh_FileName";
            // 
            // tsr_MainAppToolStrip
            // 
            this.tsr_MainAppToolStrip.ImageScalingSize = new System.Drawing.Size(0, 0);
            this.tsr_MainAppToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btn_SaveFiles,
            this.btn_Refresh_lvwFileList,
            this.btn_EditFile,
            this.btn_RemoveGeoData});
            this.tsr_MainAppToolStrip.Location = new System.Drawing.Point(0, 24);
            this.tsr_MainAppToolStrip.Name = "tsr_MainAppToolStrip";
            this.tsr_MainAppToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.tsr_MainAppToolStrip.Size = new System.Drawing.Size(730, 25);
            this.tsr_MainAppToolStrip.TabIndex = 12;
            this.tsr_MainAppToolStrip.Text = "tsr_MainAppToolStrip";
            // 
            // btn_SaveFiles
            // 
            this.btn_SaveFiles.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btn_SaveFiles.Image = ((System.Drawing.Image)(resources.GetObject("btn_SaveFiles.Image")));
            this.btn_SaveFiles.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btn_SaveFiles.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_SaveFiles.Name = "btn_SaveFiles";
            this.btn_SaveFiles.Size = new System.Drawing.Size(23, 22);
            this.btn_SaveFiles.Text = "btn_SaveFiles";
            this.btn_SaveFiles.Click += new System.EventHandler(this.btn_SaveFiles_Click);
            // 
            // btn_Refresh_lvwFileList
            // 
            this.btn_Refresh_lvwFileList.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btn_Refresh_lvwFileList.Image = ((System.Drawing.Image)(resources.GetObject("btn_Refresh_lvwFileList.Image")));
            this.btn_Refresh_lvwFileList.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btn_Refresh_lvwFileList.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_Refresh_lvwFileList.Name = "btn_Refresh_lvwFileList";
            this.btn_Refresh_lvwFileList.Size = new System.Drawing.Size(23, 22);
            this.btn_Refresh_lvwFileList.Text = "btn_Refresh_lvwFileList";
            this.btn_Refresh_lvwFileList.ToolTipText = "btn_Refresh_lvwFileList";
            this.btn_Refresh_lvwFileList.Click += new System.EventHandler(this.btn_Refresh_lvwFileList_Click);
            // 
            // btn_EditFile
            // 
            this.btn_EditFile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btn_EditFile.Image = ((System.Drawing.Image)(resources.GetObject("btn_EditFile.Image")));
            this.btn_EditFile.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btn_EditFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_EditFile.Name = "btn_EditFile";
            this.btn_EditFile.Size = new System.Drawing.Size(23, 22);
            this.btn_EditFile.Text = "btn_EditFile";
            this.btn_EditFile.Click += new System.EventHandler(this.btn_EditFile_Click);
            // 
            // btn_RemoveGeoData
            // 
            this.btn_RemoveGeoData.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btn_RemoveGeoData.Image = ((System.Drawing.Image)(resources.GetObject("btn_RemoveGeoData.Image")));
            this.btn_RemoveGeoData.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btn_RemoveGeoData.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_RemoveGeoData.Name = "btn_RemoveGeoData";
            this.btn_RemoveGeoData.Size = new System.Drawing.Size(23, 22);
            this.btn_RemoveGeoData.Text = "btn_RemoveGeoData";
            this.btn_RemoveGeoData.Click += new System.EventHandler(this.btn_RemoveGeoData_Click);
            // 
            // tsr_FolderControl
            // 
            this.tsr_FolderControl.ImageScalingSize = new System.Drawing.Size(0, 0);
            this.tsr_FolderControl.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbx_FolderName,
            this.btn_OneFolderUp,
            this.btn_ts_Refresh_lvwFileList});
            this.tsr_FolderControl.Location = new System.Drawing.Point(0, 49);
            this.tsr_FolderControl.Name = "tsr_FolderControl";
            this.tsr_FolderControl.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.tsr_FolderControl.Size = new System.Drawing.Size(730, 25);
            this.tsr_FolderControl.TabIndex = 13;
            this.tsr_FolderControl.Text = "tsr_FolderControl";
            // 
            // tbx_FolderName
            // 
            this.tbx_FolderName.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.tbx_FolderName.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.tbx_FolderName.Name = "tbx_FolderName";
            this.tbx_FolderName.Size = new System.Drawing.Size(400, 25);
            this.tbx_FolderName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbx_FolderName_KeyDown);
            // 
            // btn_OneFolderUp
            // 
            this.btn_OneFolderUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btn_OneFolderUp.Image = ((System.Drawing.Image)(resources.GetObject("btn_OneFolderUp.Image")));
            this.btn_OneFolderUp.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btn_OneFolderUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_OneFolderUp.Name = "btn_OneFolderUp";
            this.btn_OneFolderUp.Size = new System.Drawing.Size(23, 22);
            this.btn_OneFolderUp.Text = "btn_OneFolderUp";
            this.btn_OneFolderUp.ToolTipText = "Up One Level";
            this.btn_OneFolderUp.Click += new System.EventHandler(this.btn_OneFolderUp_Click);
            // 
            // btn_ts_Refresh_lvwFileList
            // 
            this.btn_ts_Refresh_lvwFileList.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btn_ts_Refresh_lvwFileList.Image = ((System.Drawing.Image)(resources.GetObject("btn_ts_Refresh_lvwFileList.Image")));
            this.btn_ts_Refresh_lvwFileList.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.btn_ts_Refresh_lvwFileList.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btn_ts_Refresh_lvwFileList.Name = "btn_ts_Refresh_lvwFileList";
            this.btn_ts_Refresh_lvwFileList.Size = new System.Drawing.Size(23, 22);
            this.btn_ts_Refresh_lvwFileList.Text = "btn_ts_Refresh_lvwFileList";
            this.btn_ts_Refresh_lvwFileList.ToolTipText = "Switch Folder";
            this.btn_ts_Refresh_lvwFileList.Click += new System.EventHandler(this.btn_ts_Refresh_lvwFileList_Click);
            // 
            // btn_loctToFile
            // 
            this.btn_loctToFile.Location = new System.Drawing.Point(652, 409);
            this.btn_loctToFile.Margin = new System.Windows.Forms.Padding(2);
            this.btn_loctToFile.Name = "btn_loctToFile";
            this.btn_loctToFile.Size = new System.Drawing.Size(67, 23);
            this.btn_loctToFile.TabIndex = 5;
            this.btn_loctToFile.Text = "btn_loctToFile";
            this.btn_loctToFile.UseVisualStyleBackColor = true;
            this.btn_loctToFile.Click += new System.EventHandler(this.btn_loctToFile_Click);
            // 
            // lbl_ParseProgress
            // 
            this.lbl_ParseProgress.Location = new System.Drawing.Point(12, 418);
            this.lbl_ParseProgress.Name = "lbl_ParseProgress";
            this.lbl_ParseProgress.Size = new System.Drawing.Size(250, 13);
            this.lbl_ParseProgress.TabIndex = 14;
            this.lbl_ParseProgress.Text = "lbl_ParseProgress";
            // 
            // pbx_imagePreview
            // 
            this.pbx_imagePreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbx_imagePreview.Location = new System.Drawing.Point(12, 345);
            this.pbx_imagePreview.Margin = new System.Windows.Forms.Padding(2);
            this.pbx_imagePreview.Name = "pbx_imagePreview";
            this.pbx_imagePreview.Size = new System.Drawing.Size(337, 72);
            this.pbx_imagePreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbx_imagePreview.TabIndex = 18;
            this.pbx_imagePreview.TabStop = false;
            // 
            // frm_MainApp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(730, 485);
            this.Controls.Add(this.pbx_imagePreview);
            this.Controls.Add(this.lbl_ParseProgress);
            this.Controls.Add(this.tsr_FolderControl);
            this.Controls.Add(this.tsr_MainAppToolStrip);
            this.Controls.Add(this.lvw_FileList);
            this.Controls.Add(this.tct_Main);
            this.Controls.Add(this.btn_loctToFile);
            this.Controls.Add(this.btn_NavigateMapGo);
            this.Controls.Add(this.lbl_lng);
            this.Controls.Add(this.tbx_lng);
            this.Controls.Add(this.lbl_lat);
            this.Controls.Add(this.tbx_lat);
            this.Controls.Add(this.mns_MenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.mns_MenuStrip;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimumSize = new System.Drawing.Size(673, 415);
            this.Name = "frm_MainApp";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "GeoTagNinja";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frm_MainApp_FormClosing);
            this.Load += new System.EventHandler(this.frm_MainApp_Load);
            this.mns_MenuStrip.ResumeLayout(false);
            this.mns_MenuStrip.PerformLayout();
            this.tct_Main.ResumeLayout(false);
            this.tpg_Map.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.wbv_MapArea)).EndInit();
            this.tsr_MainAppToolStrip.ResumeLayout(false);
            this.tsr_MainAppToolStrip.PerformLayout();
            this.tsr_FolderControl.ResumeLayout(false);
            this.tsr_FolderControl.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_imagePreview)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label lbl_lat;
        private System.Windows.Forms.Label lbl_lng;
        private System.Windows.Forms.Button btn_NavigateMapGo;
        public System.Windows.Forms.TextBox tbx_lat;
        public System.Windows.Forms.TextBox tbx_lng;
        private System.Windows.Forms.MenuStrip mns_MenuStrip;
        private System.Windows.Forms.ToolStripMenuItem tmi_File;
        private System.Windows.Forms.ToolStripMenuItem tmi_File_SaveAll;
        private System.Windows.Forms.ToolStripSeparator tss_ToolStripSeparator_Main;
        private System.Windows.Forms.ToolStripMenuItem tmi_File_Quit;
        private ToolStripMenuItem tmi_Settings;
        private ToolStripMenuItem tmi_Settings_Settings;
        private TabControl tct_Main;
        private TabPage tpg_Map;
        private Microsoft.Web.WebView2.WinForms.WebView2 wbv_MapArea;
        private ColumnHeader clh_FileName;
        private ToolStrip tsr_MainAppToolStrip;
        private ToolStripButton btn_Refresh_lvwFileList;
        private ToolStrip tsr_FolderControl;
        private ToolStripButton btn_ts_Refresh_lvwFileList;
        private ToolStripButton btn_OneFolderUp;
        internal ToolStripTextBox tbx_FolderName;
        internal ListView lvw_FileList;
        private Button btn_loctToFile;
        internal Label lbl_ParseProgress;
        private ToolStripMenuItem tmi_Help;
        private ToolStripMenuItem tmi_Help_About;
        internal PictureBox pbx_imagePreview;
        private ToolStripMenuItem tmi_File_EditFiles;
        private ToolStripButton btn_SaveFiles;
        private ToolStripMenuItem tmi_File_CopyGeoData;
        private ToolStripMenuItem tmi_File_PasteGeoData;
        private ToolStripButton btn_EditFile;
        private ToolStripButton btn_RemoveGeoData;
    }
}

