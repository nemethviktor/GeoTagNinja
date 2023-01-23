using GeoTagNinja.View.ListView;
using System.Reflection;
using System.Windows.Forms;

namespace GeoTagNinja
{
    partial class FrmMainApp
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMainApp));
            this.mns_MenuStrip = new System.Windows.Forms.MenuStrip();
            this.tmi_File = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_SaveAll = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_EditFiles = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_ImportGPX = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_CopyGeoData = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_PasteGeoData = new System.Windows.Forms.ToolStripMenuItem();
            this.tss_ToolStripSeparator_Main = new System.Windows.Forms.ToolStripSeparator();
            this.tmi_File_Quit = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Settings = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Settings_Settings = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Settings_Favourites = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Help = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Help_About = new System.Windows.Forms.ToolStripMenuItem();
            this.tsr_MainAppToolStrip = new System.Windows.Forms.ToolStrip();
            this.tsb_SaveFiles = new System.Windows.Forms.ToolStripButton();
            this.tsb_Refresh_lvwFileList = new System.Windows.Forms.ToolStripButton();
            this.tsb_EditFile = new System.Windows.Forms.ToolStripButton();
            this.tsb_GetAllFromWeb = new System.Windows.Forms.ToolStripButton();
            this.tsb_RemoveGeoData = new System.Windows.Forms.ToolStripButton();
            this.tsb_ImportGPX = new System.Windows.Forms.ToolStripButton();
            this.tsr_FolderControl = new System.Windows.Forms.ToolStrip();
            this.tbx_FolderName = new System.Windows.Forms.ToolStripTextBox();
            this.tsb_OneFolderUp = new System.Windows.Forms.ToolStripButton();
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.splitContainerLeftTop = new System.Windows.Forms.SplitContainer();
            this.lvw_FileList = new GeoTagNinja.View.ListView.FileListView();
            this.clh_FileName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.cms_FileListView = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tmi_ShowHideCols = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_removeCachedData = new System.Windows.Forms.ToolStripMenuItem();
            this.pbx_imagePreview = new GeoTagNinja.ImagePreview();
            this.flp_ProcessingInfo = new System.Windows.Forms.FlowLayoutPanel();
            this.lbl_ParseProgress = new System.Windows.Forms.Label();
            this.tct_Main = new System.Windows.Forms.TabControl();
            this.tpg_Map = new System.Windows.Forms.TabPage();
            this.wbv_MapArea = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.igl_RightHandSide = new System.Windows.Forms.ImageList(this.components);
            this.flp_GeoCoords = new System.Windows.Forms.FlowLayoutPanel();
            this.lbl_lat = new System.Windows.Forms.Label();
            this.lbl_lng = new System.Windows.Forms.Label();
            this.btn_NavigateMapGo = new System.Windows.Forms.Button();
            this.btn_loctToFile = new System.Windows.Forms.Button();
            this.lbl_Favourites = new System.Windows.Forms.Label();
            this.cbx_Favourites = new System.Windows.Forms.ComboBox();
            this.btn_SaveLocation = new System.Windows.Forms.Button();
            this.btn_LoadFavourite = new System.Windows.Forms.Button();
            this.btn_ManageFavourites = new System.Windows.Forms.Button();
            this.ttp_NavigateMapGo = new System.Windows.Forms.ToolTip(this.components);
            this.ttp_loctToFile = new System.Windows.Forms.ToolTip(this.components);
            this.ttp_SaveFavourite = new System.Windows.Forms.ToolTip(this.components);
            this.ttp_LoadFavourite = new System.Windows.Forms.ToolTip(this.components);
            this.ttp_ManageFavourites = new System.Windows.Forms.ToolTip(this.components);
            this.nud_lat = new System.Windows.Forms.NumericUpDown();
            this.nud_lng = new System.Windows.Forms.NumericUpDown();
            this.mns_MenuStrip.SuspendLayout();
            this.tsr_MainAppToolStrip.SuspendLayout();
            this.tsr_FolderControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).BeginInit();
            this.splitContainerMain.Panel1.SuspendLayout();
            this.splitContainerMain.Panel2.SuspendLayout();
            this.splitContainerMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerLeftTop)).BeginInit();
            this.splitContainerLeftTop.Panel1.SuspendLayout();
            this.splitContainerLeftTop.Panel2.SuspendLayout();
            this.splitContainerLeftTop.SuspendLayout();
            this.cms_FileListView.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_imagePreview)).BeginInit();
            this.flp_ProcessingInfo.SuspendLayout();
            this.tct_Main.SuspendLayout();
            this.tpg_Map.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.wbv_MapArea)).BeginInit();
            this.flp_GeoCoords.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_lat)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_lng)).BeginInit();
            this.SuspendLayout();
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
            this.mns_MenuStrip.Padding = new System.Windows.Forms.Padding(4, 1, 0, 1);
            this.mns_MenuStrip.Size = new System.Drawing.Size(730, 24);
            this.mns_MenuStrip.TabIndex = 9;
            this.mns_MenuStrip.Text = "mns_MenuStrip";
            // 
            // tmi_File
            // 
            this.tmi_File.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_File_SaveAll,
            this.tmi_File_EditFiles,
            this.tmi_File_ImportGPX,
            this.tmi_File_CopyGeoData,
            this.tmi_File_PasteGeoData,
            this.tss_ToolStripSeparator_Main,
            this.tmi_File_Quit});
            this.tmi_File.Name = "tmi_File";
            this.tmi_File.Size = new System.Drawing.Size(60, 22);
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
            // tmi_File_ImportGPX
            // 
            this.tmi_File_ImportGPX.Name = "tmi_File_ImportGPX";
            this.tmi_File_ImportGPX.Size = new System.Drawing.Size(275, 22);
            this.tmi_File_ImportGPX.Text = "tmi_File_ImportGPX";
            this.tmi_File_ImportGPX.Click += new System.EventHandler(this.tmi_File_ImportGPX_Click);
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
            this.tmi_Settings_Settings,
            this.tmi_Settings_Favourites});
            this.tmi_Settings.Name = "tmi_Settings";
            this.tmi_Settings.Size = new System.Drawing.Size(84, 22);
            this.tmi_Settings.Text = "tmi_Settings";
            // 
            // tmi_Settings_Settings
            // 
            this.tmi_Settings_Settings.Name = "tmi_Settings_Settings";
            this.tmi_Settings_Settings.Size = new System.Drawing.Size(198, 22);
            this.tmi_Settings_Settings.Text = "tmi_Settings_Settings";
            this.tmi_Settings_Settings.Click += new System.EventHandler(this.tmi_Settings_Settings_Click);
            // 
            // tmi_Settings_Favourites
            // 
            this.tmi_Settings_Favourites.Name = "tmi_Settings_Favourites";
            this.tmi_Settings_Favourites.Size = new System.Drawing.Size(198, 22);
            this.tmi_Settings_Favourites.Text = "tmi_Settings_Favourites";
            this.tmi_Settings_Favourites.Click += new System.EventHandler(this.tmi_Settings_Favourites_Click);
            // 
            // tmi_Help
            // 
            this.tmi_Help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_Help_About});
            this.tmi_Help.Name = "tmi_Help";
            this.tmi_Help.Size = new System.Drawing.Size(67, 22);
            this.tmi_Help.Text = "tmi_Help";
            // 
            // tmi_Help_About
            // 
            this.tmi_Help_About.Name = "tmi_Help_About";
            this.tmi_Help_About.Size = new System.Drawing.Size(160, 22);
            this.tmi_Help_About.Text = "tmi_Help_About";
            this.tmi_Help_About.Click += new System.EventHandler(this.tmi_Help_About_Click);
            // 
            // tsr_MainAppToolStrip
            // 
            this.tsr_MainAppToolStrip.ImageScalingSize = new System.Drawing.Size(0, 0);
            this.tsr_MainAppToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsb_SaveFiles,
            this.tsb_Refresh_lvwFileList,
            this.tsb_EditFile,
            this.tsb_GetAllFromWeb,
            this.tsb_RemoveGeoData,
            this.tsb_ImportGPX});
            this.tsr_MainAppToolStrip.Location = new System.Drawing.Point(0, 24);
            this.tsr_MainAppToolStrip.Name = "tsr_MainAppToolStrip";
            this.tsr_MainAppToolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 2, 0);
            this.tsr_MainAppToolStrip.Size = new System.Drawing.Size(730, 25);
            this.tsr_MainAppToolStrip.TabIndex = 12;
            this.tsr_MainAppToolStrip.Text = "tsr_MainAppToolStrip";
            // 
            // tsb_SaveFiles
            // 
            this.tsb_SaveFiles.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsb_SaveFiles.Image = ((System.Drawing.Image)(resources.GetObject("tsb_SaveFiles.Image")));
            this.tsb_SaveFiles.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.tsb_SaveFiles.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsb_SaveFiles.Name = "tsb_SaveFiles";
            this.tsb_SaveFiles.Size = new System.Drawing.Size(23, 22);
            this.tsb_SaveFiles.Text = "tsb_SaveFiles";
            this.tsb_SaveFiles.Click += new System.EventHandler(this.tsb_SaveFiles_Click);
            // 
            // tsb_Refresh_lvwFileList
            // 
            this.tsb_Refresh_lvwFileList.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsb_Refresh_lvwFileList.Image = ((System.Drawing.Image)(resources.GetObject("tsb_Refresh_lvwFileList.Image")));
            this.tsb_Refresh_lvwFileList.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.tsb_Refresh_lvwFileList.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsb_Refresh_lvwFileList.Name = "tsb_Refresh_lvwFileList";
            this.tsb_Refresh_lvwFileList.Size = new System.Drawing.Size(23, 22);
            this.tsb_Refresh_lvwFileList.Text = "tsb_Refresh_lvwFileList";
            this.tsb_Refresh_lvwFileList.ToolTipText = "btn_Refresh_lvwFileList";
            this.tsb_Refresh_lvwFileList.Click += new System.EventHandler(this.tsb_Refresh_lvwFileList_Click);
            // 
            // tsb_EditFile
            // 
            this.tsb_EditFile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsb_EditFile.Image = ((System.Drawing.Image)(resources.GetObject("tsb_EditFile.Image")));
            this.tsb_EditFile.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.tsb_EditFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsb_EditFile.Name = "tsb_EditFile";
            this.tsb_EditFile.Size = new System.Drawing.Size(23, 22);
            this.tsb_EditFile.Text = "tsb_EditFile";
            this.tsb_EditFile.Click += new System.EventHandler(this.tsb_EditFile_Click);
            // 
            // tsb_GetAllFromWeb
            // 
            this.tsb_GetAllFromWeb.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsb_GetAllFromWeb.Image = ((System.Drawing.Image)(resources.GetObject("tsb_GetAllFromWeb.Image")));
            this.tsb_GetAllFromWeb.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.tsb_GetAllFromWeb.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsb_GetAllFromWeb.Name = "tsb_GetAllFromWeb";
            this.tsb_GetAllFromWeb.Size = new System.Drawing.Size(23, 22);
            this.tsb_GetAllFromWeb.Text = "tsb_GetAllFromWeb";
            this.tsb_GetAllFromWeb.Click += new System.EventHandler(this.tsb_GetAllFromWeb_Click);
            // 
            // tsb_RemoveGeoData
            // 
            this.tsb_RemoveGeoData.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsb_RemoveGeoData.Image = ((System.Drawing.Image)(resources.GetObject("tsb_RemoveGeoData.Image")));
            this.tsb_RemoveGeoData.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.tsb_RemoveGeoData.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsb_RemoveGeoData.Name = "tsb_RemoveGeoData";
            this.tsb_RemoveGeoData.Size = new System.Drawing.Size(23, 22);
            this.tsb_RemoveGeoData.Text = "tsb_RemoveGeoData";
            this.tsb_RemoveGeoData.Click += new System.EventHandler(this.tsb_RemoveGeoData_Click);
            // 
            // tsb_ImportGPX
            // 
            this.tsb_ImportGPX.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsb_ImportGPX.Image = ((System.Drawing.Image)(resources.GetObject("tsb_ImportGPX.Image")));
            this.tsb_ImportGPX.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.tsb_ImportGPX.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsb_ImportGPX.Name = "tsb_ImportGPX";
            this.tsb_ImportGPX.Size = new System.Drawing.Size(23, 22);
            this.tsb_ImportGPX.Text = "tsb_ImportGPX";
            this.tsb_ImportGPX.Click += new System.EventHandler(this.tsb_ImportGPX_Click);
            // 
            // tsr_FolderControl
            // 
            this.tsr_FolderControl.ImageScalingSize = new System.Drawing.Size(0, 0);
            this.tsr_FolderControl.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbx_FolderName,
            this.tsb_OneFolderUp});
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
            this.tbx_FolderName.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.tbx_FolderName.Name = "tbx_FolderName";
            this.tbx_FolderName.Size = new System.Drawing.Size(400, 25);
            this.tbx_FolderName.Enter += new System.EventHandler(this.tbx_FolderName_Enter);
            this.tbx_FolderName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbx_FolderName_KeyDown);
            this.tbx_FolderName.Click += new System.EventHandler(this.tbx_FolderName_Enter);
            // 
            // tsb_OneFolderUp
            // 
            this.tsb_OneFolderUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsb_OneFolderUp.Image = ((System.Drawing.Image)(resources.GetObject("tsb_OneFolderUp.Image")));
            this.tsb_OneFolderUp.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.tsb_OneFolderUp.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsb_OneFolderUp.Name = "tsb_OneFolderUp";
            this.tsb_OneFolderUp.Size = new System.Drawing.Size(23, 22);
            this.tsb_OneFolderUp.Text = "tsb_OneFolderUp";
            this.tsb_OneFolderUp.ToolTipText = "tsb_OneFolderUp";
            this.tsb_OneFolderUp.Click += new System.EventHandler(this.btn_OneFolderUp_Click);
            // 
            // splitContainerMain
            // 
            this.splitContainerMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerMain.Location = new System.Drawing.Point(0, 74);
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.splitContainerLeftTop);
            this.splitContainerMain.Panel1.Controls.Add(this.flp_ProcessingInfo);
            this.splitContainerMain.Panel1.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.tct_Main);
            this.splitContainerMain.Panel2.Controls.Add(this.flp_GeoCoords);
            this.splitContainerMain.Panel2.Padding = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this.splitContainerMain.Size = new System.Drawing.Size(730, 411);
            this.splitContainerMain.SplitterDistance = 333;
            this.splitContainerMain.SplitterWidth = 6;
            this.splitContainerMain.TabIndex = 19;
            // 
            // splitContainerLeftTop
            // 
            this.splitContainerLeftTop.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainerLeftTop.Location = new System.Drawing.Point(5, 0);
            this.splitContainerLeftTop.Margin = new System.Windows.Forms.Padding(0);
            this.splitContainerLeftTop.Name = "splitContainerLeftTop";
            this.splitContainerLeftTop.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainerLeftTop.Panel1
            // 
            this.splitContainerLeftTop.Panel1.Controls.Add(this.lvw_FileList);
            this.splitContainerLeftTop.Panel1MinSize = 150;
            // 
            // splitContainerLeftTop.Panel2
            // 
            this.splitContainerLeftTop.Panel2.Controls.Add(this.pbx_imagePreview);
            this.splitContainerLeftTop.Panel2MinSize = 100;
            this.splitContainerLeftTop.Size = new System.Drawing.Size(328, 381);
            this.splitContainerLeftTop.SplitterDistance = 181;
            this.splitContainerLeftTop.SplitterWidth = 6;
            this.splitContainerLeftTop.TabIndex = 0;
            // 
            // lvw_FileList
            // 
            this.lvw_FileList.AllowColumnReorder = true;
            this.lvw_FileList.BackColor = System.Drawing.Color.SeaShell;
            this.lvw_FileList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.clh_FileName});
            this.lvw_FileList.ContextMenuStrip = this.cms_FileListView;
            this.lvw_FileList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvw_FileList.FullRowSelect = true;
            this.lvw_FileList.GridLines = true;
            this.lvw_FileList.HideSelection = false;
            this.lvw_FileList.Location = new System.Drawing.Point(0, 0);
            this.lvw_FileList.Margin = new System.Windows.Forms.Padding(2);
            this.lvw_FileList.Name = "lvw_FileList";
            this.lvw_FileList.Size = new System.Drawing.Size(328, 181);
            this.lvw_FileList.TabIndex = 12;
            this.lvw_FileList.UseCompatibleStateImageBehavior = false;
            this.lvw_FileList.View = System.Windows.Forms.View.Details;
            this.lvw_FileList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvw_FileList_KeyDown);
            this.lvw_FileList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lvw_FileList_KeyUp);
            this.lvw_FileList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lvw_FileList_MouseDoubleClick);
            this.lvw_FileList.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lvw_FileList_MouseUp);
            // 
            // clh_FileName
            // 
            this.clh_FileName.Name = "clh_FileName";
            this.clh_FileName.Text = "clh_FileName";
            // 
            // cms_FileListView
            // 
            this.cms_FileListView.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.cms_FileListView.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_ShowHideCols,
            this.tmi_removeCachedData});
            this.cms_FileListView.Name = "cms_FileListView";
            this.cms_FileListView.Size = new System.Drawing.Size(161, 48);
            // 
            // tmi_ShowHideCols
            // 
            this.tmi_ShowHideCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tmi_ShowHideCols.Name = "tmi_ShowHideCols";
            this.tmi_ShowHideCols.Size = new System.Drawing.Size(160, 22);
            this.tmi_ShowHideCols.Text = "Select Columns";
            this.tmi_ShowHideCols.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.tmi_ShowHideCols.Click += new System.EventHandler(this.selectColumnsToolStripMenuItem_Click);
            // 
            // tmi_removeCachedData
            // 
            this.tmi_removeCachedData.Name = "tmi_removeCachedData";
            this.tmi_removeCachedData.Size = new System.Drawing.Size(160, 22);
            this.tmi_removeCachedData.Text = "Remove Cached";
            this.tmi_removeCachedData.Click += new System.EventHandler(this.tmi_removeCachedData_Click);
            // 
            // pbx_imagePreview
            // 
            this.pbx_imagePreview.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pbx_imagePreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbx_imagePreview.EmptyText = "No image to show";
            this.pbx_imagePreview.Image = null;
            this.pbx_imagePreview.Location = new System.Drawing.Point(0, 0);
            this.pbx_imagePreview.Margin = new System.Windows.Forms.Padding(2);
            this.pbx_imagePreview.Name = "pbx_imagePreview";
            this.pbx_imagePreview.Size = new System.Drawing.Size(328, 194);
            this.pbx_imagePreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbx_imagePreview.TabIndex = 19;
            this.pbx_imagePreview.TabStop = false;
            // 
            // flp_ProcessingInfo
            // 
            this.flp_ProcessingInfo.Controls.Add(this.lbl_ParseProgress);
            this.flp_ProcessingInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flp_ProcessingInfo.Location = new System.Drawing.Point(5, 381);
            this.flp_ProcessingInfo.MaximumSize = new System.Drawing.Size(0, 30);
            this.flp_ProcessingInfo.MinimumSize = new System.Drawing.Size(0, 30);
            this.flp_ProcessingInfo.Name = "flp_ProcessingInfo";
            this.flp_ProcessingInfo.Size = new System.Drawing.Size(328, 30);
            this.flp_ProcessingInfo.TabIndex = 16;
            this.flp_ProcessingInfo.WrapContents = false;
            // 
            // lbl_ParseProgress
            // 
            this.lbl_ParseProgress.AutoSize = true;
            this.lbl_ParseProgress.Location = new System.Drawing.Point(0, 7);
            this.lbl_ParseProgress.Margin = new System.Windows.Forms.Padding(0, 7, 3, 0);
            this.lbl_ParseProgress.Name = "lbl_ParseProgress";
            this.lbl_ParseProgress.Size = new System.Drawing.Size(91, 13);
            this.lbl_ParseProgress.TabIndex = 15;
            this.lbl_ParseProgress.Text = "lbl_ParseProgress";
            // 
            // tct_Main
            // 
            this.tct_Main.Controls.Add(this.tpg_Map);
            this.tct_Main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tct_Main.ImageList = this.igl_RightHandSide;
            this.tct_Main.Location = new System.Drawing.Point(0, 0);
            this.tct_Main.Margin = new System.Windows.Forms.Padding(2);
            this.tct_Main.Name = "tct_Main";
            this.tct_Main.Padding = new System.Drawing.Point(0, 0);
            this.tct_Main.SelectedIndex = 0;
            this.tct_Main.Size = new System.Drawing.Size(386, 365);
            this.tct_Main.TabIndex = 2;
            // 
            // tpg_Map
            // 
            this.tpg_Map.Controls.Add(this.wbv_MapArea);
            this.tpg_Map.ImageKey = "PublishOnDemand.png";
            this.tpg_Map.Location = new System.Drawing.Point(4, 23);
            this.tpg_Map.Margin = new System.Windows.Forms.Padding(2);
            this.tpg_Map.Name = "tpg_Map";
            this.tpg_Map.Padding = new System.Windows.Forms.Padding(2);
            this.tpg_Map.Size = new System.Drawing.Size(378, 338);
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
            this.wbv_MapArea.Size = new System.Drawing.Size(374, 334);
            this.wbv_MapArea.TabIndex = 1;
            this.wbv_MapArea.ZoomFactor = 1D;
            // 
            // igl_RightHandSide
            // 
            this.igl_RightHandSide.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("igl_RightHandSide.ImageStream")));
            this.igl_RightHandSide.TransparentColor = System.Drawing.Color.Transparent;
            this.igl_RightHandSide.Images.SetKeyName(0, "PublishOnDemand.png");
            // 
            // flp_GeoCoords
            // 
            this.flp_GeoCoords.Controls.Add(this.lbl_lat);
            this.flp_GeoCoords.Controls.Add(this.nud_lat);
            this.flp_GeoCoords.Controls.Add(this.lbl_lng);
            this.flp_GeoCoords.Controls.Add(this.nud_lng);
            this.flp_GeoCoords.Controls.Add(this.btn_NavigateMapGo);
            this.flp_GeoCoords.Controls.Add(this.btn_loctToFile);
            this.flp_GeoCoords.Controls.Add(this.lbl_Favourites);
            this.flp_GeoCoords.Controls.Add(this.cbx_Favourites);
            this.flp_GeoCoords.Controls.Add(this.btn_SaveLocation);
            this.flp_GeoCoords.Controls.Add(this.btn_LoadFavourite);
            this.flp_GeoCoords.Controls.Add(this.btn_ManageFavourites);
            this.flp_GeoCoords.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flp_GeoCoords.Location = new System.Drawing.Point(0, 365);
            this.flp_GeoCoords.MinimumSize = new System.Drawing.Size(0, 30);
            this.flp_GeoCoords.Name = "flp_GeoCoords";
            this.flp_GeoCoords.Size = new System.Drawing.Size(386, 46);
            this.flp_GeoCoords.TabIndex = 12;
            // 
            // lbl_lat
            // 
            this.lbl_lat.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lbl_lat.AutoSize = true;
            this.lbl_lat.Location = new System.Drawing.Point(10, 6);
            this.lbl_lat.Margin = new System.Windows.Forms.Padding(10, 0, 2, 0);
            this.lbl_lat.Name = "lbl_lat";
            this.lbl_lat.Size = new System.Drawing.Size(34, 13);
            this.lbl_lat.TabIndex = 6;
            this.lbl_lat.Text = "lbl_lat";
            // 
            // lbl_lng
            // 
            this.lbl_lng.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lbl_lng.AutoSize = true;
            this.lbl_lng.Location = new System.Drawing.Point(192, 6);
            this.lbl_lng.Margin = new System.Windows.Forms.Padding(20, 0, 2, 0);
            this.lbl_lng.Name = "lbl_lng";
            this.lbl_lng.Size = new System.Drawing.Size(37, 13);
            this.lbl_lng.TabIndex = 8;
            this.lbl_lng.Text = "lbl_lng";
            // 
            // btn_NavigateMapGo
            // 
            this.btn_NavigateMapGo.BackColor = System.Drawing.SystemColors.Menu;
            this.btn_NavigateMapGo.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_NavigateMapGo.BackgroundImage")));
            this.btn_NavigateMapGo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn_NavigateMapGo.Location = new System.Drawing.Point(20, 28);
            this.btn_NavigateMapGo.Margin = new System.Windows.Forms.Padding(20, 2, 2, 2);
            this.btn_NavigateMapGo.Name = "btn_NavigateMapGo";
            this.btn_NavigateMapGo.Size = new System.Drawing.Size(24, 24);
            this.btn_NavigateMapGo.TabIndex = 11;
            this.btn_NavigateMapGo.Text = "btn_NavigateMapGo";
            this.btn_NavigateMapGo.UseVisualStyleBackColor = false;
            this.btn_NavigateMapGo.Click += new System.EventHandler(this.btn_NavigateMapGo_Click);
            // 
            // btn_loctToFile
            // 
            this.btn_loctToFile.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_loctToFile.BackgroundImage")));
            this.btn_loctToFile.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn_loctToFile.Location = new System.Drawing.Point(48, 28);
            this.btn_loctToFile.Margin = new System.Windows.Forms.Padding(2);
            this.btn_loctToFile.Name = "btn_loctToFile";
            this.btn_loctToFile.Size = new System.Drawing.Size(24, 24);
            this.btn_loctToFile.TabIndex = 10;
            this.btn_loctToFile.Text = "btn_loctToFile";
            this.btn_loctToFile.UseVisualStyleBackColor = true;
            this.btn_loctToFile.Click += new System.EventHandler(this.btn_loctToFile_Click);
            // 
            // lbl_Favourites
            // 
            this.lbl_Favourites.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lbl_Favourites.AutoSize = true;
            this.lbl_Favourites.Location = new System.Drawing.Point(84, 33);
            this.lbl_Favourites.Margin = new System.Windows.Forms.Padding(10, 0, 2, 0);
            this.lbl_Favourites.Name = "lbl_Favourites";
            this.lbl_Favourites.Size = new System.Drawing.Size(72, 13);
            this.lbl_Favourites.TabIndex = 12;
            this.lbl_Favourites.Text = "lbl_Favourites";
            // 
            // cbx_Favourites
            // 
            this.cbx_Favourites.FormattingEnabled = true;
            this.cbx_Favourites.Location = new System.Drawing.Point(161, 29);
            this.cbx_Favourites.Name = "cbx_Favourites";
            this.cbx_Favourites.Size = new System.Drawing.Size(148, 21);
            this.cbx_Favourites.TabIndex = 13;
            this.cbx_Favourites.SelectedValueChanged += new System.EventHandler(this.cbx_Favourites_SelectedValueChanged);
            // 
            // btn_SaveLocation
            // 
            this.btn_SaveLocation.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_SaveLocation.BackgroundImage")));
            this.btn_SaveLocation.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn_SaveLocation.Location = new System.Drawing.Point(314, 28);
            this.btn_SaveLocation.Margin = new System.Windows.Forms.Padding(2);
            this.btn_SaveLocation.Name = "btn_SaveLocation";
            this.btn_SaveLocation.Size = new System.Drawing.Size(24, 24);
            this.btn_SaveLocation.TabIndex = 14;
            this.btn_SaveLocation.Text = "btn_SaveLocation";
            this.btn_SaveLocation.UseVisualStyleBackColor = true;
            this.btn_SaveLocation.Click += new System.EventHandler(this.btn_SaveLocation_Click);
            // 
            // btn_LoadFavourite
            // 
            this.btn_LoadFavourite.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_LoadFavourite.BackgroundImage")));
            this.btn_LoadFavourite.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn_LoadFavourite.Location = new System.Drawing.Point(342, 28);
            this.btn_LoadFavourite.Margin = new System.Windows.Forms.Padding(2);
            this.btn_LoadFavourite.Name = "btn_LoadFavourite";
            this.btn_LoadFavourite.Size = new System.Drawing.Size(24, 24);
            this.btn_LoadFavourite.TabIndex = 15;
            this.btn_LoadFavourite.Text = "btn_LoadFavourite";
            this.btn_LoadFavourite.UseVisualStyleBackColor = true;
            this.btn_LoadFavourite.Click += new System.EventHandler(this.btn_LoadFavourite_Click);
            // 
            // btn_ManageFavourites
            // 
            this.btn_ManageFavourites.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("btn_ManageFavourites.BackgroundImage")));
            this.btn_ManageFavourites.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.btn_ManageFavourites.Location = new System.Drawing.Point(2, 56);
            this.btn_ManageFavourites.Margin = new System.Windows.Forms.Padding(2);
            this.btn_ManageFavourites.Name = "btn_ManageFavourites";
            this.btn_ManageFavourites.Size = new System.Drawing.Size(24, 24);
            this.btn_ManageFavourites.TabIndex = 16;
            this.btn_ManageFavourites.Text = "btn_ManageFavourites";
            this.btn_ManageFavourites.UseVisualStyleBackColor = true;
            this.btn_ManageFavourites.Click += new System.EventHandler(this.btn_ManageFavourites_Click);
            // 
            // nud_lat
            // 
            this.nud_lat.DecimalPlaces = 6;
            this.nud_lat.Location = new System.Drawing.Point(49, 3);
            this.nud_lat.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
            this.nud_lat.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
            this.nud_lat.Name = "nud_lat";
            this.nud_lat.Size = new System.Drawing.Size(120, 20);
            this.nud_lat.TabIndex = 17;
            // 
            // nud_lng
            // 
            this.nud_lng.DecimalPlaces = 6;
            this.nud_lng.Location = new System.Drawing.Point(234, 3);
            this.nud_lng.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
            this.nud_lng.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
            this.nud_lng.Name = "nud_lng";
            this.nud_lng.Size = new System.Drawing.Size(120, 20);
            this.nud_lng.TabIndex = 18;
            // 
            // FrmMainApp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(730, 485);
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.tsr_FolderControl);
            this.Controls.Add(this.tsr_MainAppToolStrip);
            this.Controls.Add(this.mns_MenuStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.mns_MenuStrip;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimumSize = new System.Drawing.Size(669, 403);
            this.Name = "FrmMainApp";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "GeoTagNinja";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmMainApp_FormClosing);
            this.Load += new System.EventHandler(this.FrmMainApp_Load);
            this.ResizeBegin += new System.EventHandler(this.FrmMainApp_ResizeBegin);
            this.ResizeEnd += new System.EventHandler(this.FrmMainApp_ResizeEnd);
            this.mns_MenuStrip.ResumeLayout(false);
            this.mns_MenuStrip.PerformLayout();
            this.tsr_MainAppToolStrip.ResumeLayout(false);
            this.tsr_MainAppToolStrip.PerformLayout();
            this.tsr_FolderControl.ResumeLayout(false);
            this.tsr_FolderControl.PerformLayout();
            this.splitContainerMain.Panel1.ResumeLayout(false);
            this.splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerMain)).EndInit();
            this.splitContainerMain.ResumeLayout(false);
            this.splitContainerLeftTop.Panel1.ResumeLayout(false);
            this.splitContainerLeftTop.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerLeftTop)).EndInit();
            this.splitContainerLeftTop.ResumeLayout(false);
            this.cms_FileListView.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbx_imagePreview)).EndInit();
            this.flp_ProcessingInfo.ResumeLayout(false);
            this.flp_ProcessingInfo.PerformLayout();
            this.tct_Main.ResumeLayout(false);
            this.tpg_Map.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.wbv_MapArea)).EndInit();
            this.flp_GeoCoords.ResumeLayout(false);
            this.flp_GeoCoords.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_lat)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_lng)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip mns_MenuStrip;
        private System.Windows.Forms.ToolStripMenuItem tmi_File;
        private System.Windows.Forms.ToolStripMenuItem tmi_File_SaveAll;
        private System.Windows.Forms.ToolStripSeparator tss_ToolStripSeparator_Main;
        private System.Windows.Forms.ToolStripMenuItem tmi_File_Quit;
        private ToolStripMenuItem tmi_Settings;
        private ToolStripMenuItem tmi_Settings_Settings;
        private ToolStrip tsr_MainAppToolStrip;
        private ToolStripButton tsb_Refresh_lvwFileList;
        private ToolStrip tsr_FolderControl;
        private ToolStripButton tsb_OneFolderUp;
        internal ToolStripTextBox tbx_FolderName;
        private ToolStripMenuItem tmi_Help;
        private ToolStripMenuItem tmi_Help_About;
        private ToolStripMenuItem tmi_File_EditFiles;
        private ToolStripButton tsb_SaveFiles;
        private ToolStripMenuItem tmi_File_CopyGeoData;
        private ToolStripMenuItem tmi_File_PasteGeoData;
        private ToolStripButton tsb_EditFile;
        private ToolStripButton tsb_RemoveGeoData;
        private ToolStripMenuItem tmi_File_ImportGPX;
        private ToolStripButton tsb_ImportGPX;
        private ToolStripButton tsb_GetAllFromWeb;
        private SplitContainer splitContainerMain;
        private SplitContainer splitContainerLeftTop;
        internal FileListView lvw_FileList;
        private ColumnHeader clh_FileName;
        internal ImagePreview pbx_imagePreview;
        internal Label lbl_ParseProgress;
        private TabControl tct_Main;
        private TabPage tpg_Map;
        private Microsoft.Web.WebView2.WinForms.WebView2 wbv_MapArea;
        private Button btn_loctToFile;
        private Button btn_NavigateMapGo;
        private Label lbl_lng;
        private Label lbl_lat;
        private ToolTip ttp_NavigateMapGo;
        private ToolTip ttp_loctToFile;
        private ImageList igl_RightHandSide;
        private FlowLayoutPanel flp_GeoCoords;
        private FlowLayoutPanel flp_ProcessingInfo;
        private ContextMenuStrip cms_FileListView;
        private ToolStripMenuItem tmi_ShowHideCols;
        private Label lbl_Favourites;
        internal ComboBox cbx_Favourites;
        private Button btn_SaveLocation;
        private Button btn_LoadFavourite;
        private ToolTip ttp_SaveFavourite;
        private ToolTip ttp_LoadFavourite;
        private ToolStripMenuItem tmi_removeCachedData;
        private ToolStripMenuItem tmi_Settings_Favourites;
        private Button btn_ManageFavourites;
        private ToolTip ttp_ManageFavourites;
        internal NumericUpDown nud_lat;
        internal NumericUpDown nud_lng;
    }
}

