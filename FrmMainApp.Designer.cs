using GeoTagNinja.Helpers;
using GeoTagNinja.View.ListView;
using System.Reflection;
using System.Windows.Forms;
using GeoTagNinja.View;
using static GeoTagNinja.View.ListView.FileListView;

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
            this.splitContainerMain = new System.Windows.Forms.SplitContainer();
            this.splitContainerLeftTop = new System.Windows.Forms.SplitContainer();
            this.lvw_FileList = new GeoTagNinja.View.ListView.FileListView();
            this.clh_FileName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.cms_FileListView = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cmi_ShowHideCols = new System.Windows.Forms.ToolStripMenuItem();
            this.cmi_removeCachedData = new System.Windows.Forms.ToolStripMenuItem();
            this.cmi_OpenCoordsInAPI = new System.Windows.Forms.ToolStripMenuItem();
            this.pbx_imagePreview = new GeoTagNinja.View.ImagePreview();
            this.flp_ProcessingInfo = new System.Windows.Forms.FlowLayoutPanel();
            this.lbl_ParseProgress = new System.Windows.Forms.Label();
            this.tcr_Main = new System.Windows.Forms.TabControl();
            this.tpg_Map = new System.Windows.Forms.TabPage();
            this.flp_GeoCoords = new System.Windows.Forms.FlowLayoutPanel();
            this.lbl_lat = new System.Windows.Forms.Label();
            this.nud_lat = new System.Windows.Forms.NumericUpDown();
            this.lbl_lng = new System.Windows.Forms.Label();
            this.nud_lng = new System.Windows.Forms.NumericUpDown();
            this.btn_NavigateMapGo = new System.Windows.Forms.Button();
            this.btn_loctToFile = new System.Windows.Forms.Button();
            this.btn_loctToFileDestination = new System.Windows.Forms.Button();
            this.lbl_Favourites = new System.Windows.Forms.Label();
            this.cbx_Favourites = new System.Windows.Forms.ComboBox();
            this.btn_SaveFavourite = new System.Windows.Forms.Button();
            this.btn_LoadFavourite = new System.Windows.Forms.Button();
            this.btn_ManageFavourites = new System.Windows.Forms.Button();
            this.wbv_MapArea = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.tpg_Exif = new System.Windows.Forms.TabPage();
            this.lvw_ExifData = new System.Windows.Forms.ListView();
            this.clh_ExifTag = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.clh_OriginalValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.clh_ModifiedValue = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.igl_RightHandSide = new System.Windows.Forms.ImageList(this.components);
            this.mns_MenuStrip = new System.Windows.Forms.MenuStrip();
            this.tmi_File = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_SaveAll = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_EditFiles = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_ImportExportGPX = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_CopyGeoData = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_File_PasteGeoData = new System.Windows.Forms.ToolStripMenuItem();
            this.tss_ToolStripSeparator_Main = new System.Windows.Forms.ToolStripSeparator();
            this.tmi_File_FlatMode = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tmi_File_Quit = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Settings = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Settings_Settings = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Settings_Favourites = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Help = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Help_About = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Help_FeedbackFeatureRequest = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_Help_BugReport = new System.Windows.Forms.ToolStripMenuItem();
            this.tsr_MainAppToolStrip = new System.Windows.Forms.ToolStrip();
            this.tsb_SaveFiles = new System.Windows.Forms.ToolStripButton();
            this.tsb_Refresh_lvwFileList = new System.Windows.Forms.ToolStripButton();
            this.tsb_EditFile = new System.Windows.Forms.ToolStripButton();
            this.tsb_GetAllFromWeb = new System.Windows.Forms.ToolStripButton();
            this.tsb_RemoveGeoData = new System.Windows.Forms.ToolStripButton();
            this.tsb_ImportExportGPX = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsb_FeedbackFeatureRequest = new System.Windows.Forms.ToolStripButton();
            this.tsb_BugReport = new System.Windows.Forms.ToolStripButton();
            this.tsr_FolderControl = new System.Windows.Forms.ToolStrip();
            this.tbx_FolderName = new System.Windows.Forms.ToolStripTextBox();
            this.tsb_OneFolderUp = new System.Windows.Forms.ToolStripButton();
            this.ttp_NavigateMapGo = new System.Windows.Forms.ToolTip(this.components);
            this.ttp_loctToFile = new System.Windows.Forms.ToolTip(this.components);
            this.ttp_SaveFavourite = new System.Windows.Forms.ToolTip(this.components);
            this.ttp_LoadFavourite = new System.Windows.Forms.ToolTip(this.components);
            this.ttp_ManageFavourites = new System.Windows.Forms.ToolTip(this.components);
            this.ttp_loctToFileDestination = new System.Windows.Forms.ToolTip(this.components);
            this.ttp_FeedbackFeatureRequest = new System.Windows.Forms.ToolTip(this.components);
            this.ttp_BugReport = new System.Windows.Forms.ToolTip(this.components);
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
            this.tcr_Main.SuspendLayout();
            this.tpg_Map.SuspendLayout();
            this.flp_GeoCoords.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_lat)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_lng)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.wbv_MapArea)).BeginInit();
            this.tpg_Exif.SuspendLayout();
            this.mns_MenuStrip.SuspendLayout();
            this.tsr_MainAppToolStrip.SuspendLayout();
            this.tsr_FolderControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainerMain
            // 
            resources.ApplyResources(this.splitContainerMain, "splitContainerMain");
            this.splitContainerMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            this.splitContainerMain.Panel1.Controls.Add(this.splitContainerLeftTop);
            this.splitContainerMain.Panel1.Controls.Add(this.flp_ProcessingInfo);
            resources.ApplyResources(this.splitContainerMain.Panel1, "splitContainerMain.Panel1");
            // 
            // splitContainerMain.Panel2
            // 
            this.splitContainerMain.Panel2.Controls.Add(this.tcr_Main);
            resources.ApplyResources(this.splitContainerMain.Panel2, "splitContainerMain.Panel2");
            // 
            // splitContainerLeftTop
            // 
            resources.ApplyResources(this.splitContainerLeftTop, "splitContainerLeftTop");
            this.splitContainerLeftTop.Name = "splitContainerLeftTop";
            // 
            // splitContainerLeftTop.Panel1
            // 
            this.splitContainerLeftTop.Panel1.Controls.Add(this.lvw_FileList);
            // 
            // splitContainerLeftTop.Panel2
            // 
            this.splitContainerLeftTop.Panel2.Controls.Add(this.pbx_imagePreview);
            // 
            // lvw_FileList
            // 
            this.lvw_FileList.AllowColumnReorder = true;
            this.lvw_FileList.BackColor = System.Drawing.Color.SeaShell;
            this.lvw_FileList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.clh_FileName});
            this.lvw_FileList.ContextMenuStrip = this.cms_FileListView;
            resources.ApplyResources(this.lvw_FileList, "lvw_FileList");
            this.lvw_FileList.FullRowSelect = true;
            this.lvw_FileList.GridLines = true;
            this.lvw_FileList.HideSelection = false;
            this.lvw_FileList.Name = "lvw_FileList";
            this.lvw_FileList.OwnerDraw = true;
            this.lvw_FileList.UseCompatibleStateImageBehavior = false;
            this.lvw_FileList.View = System.Windows.Forms.View.Details;
            this.lvw_FileList.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.ListView_DrawColumnHeader);
            this.lvw_FileList.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.ListView_DrawItem);
            this.lvw_FileList.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.ListView_DrawSubItem);
            this.lvw_FileList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvw_FileList_KeyDown);
            this.lvw_FileList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lvw_FileList_KeyUp);
            this.lvw_FileList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lvw_FileList_MouseDoubleClick);
            this.lvw_FileList.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lvw_FileList_MouseUp);
            // 
            // clh_FileName
            // 
            this.clh_FileName.Name = "clh_FileName";
            resources.ApplyResources(this.clh_FileName, "clh_FileName");
            // 
            // cms_FileListView
            // 
            this.cms_FileListView.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.cms_FileListView.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cmi_ShowHideCols,
            this.cmi_removeCachedData,
            this.cmi_OpenCoordsInAPI});
            this.cms_FileListView.Name = "cms_FileListView";
            resources.ApplyResources(this.cms_FileListView, "cms_FileListView");
            // 
            // cmi_ShowHideCols
            // 
            this.cmi_ShowHideCols.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.cmi_ShowHideCols.Name = "cmi_ShowHideCols";
            resources.ApplyResources(this.cmi_ShowHideCols, "cmi_ShowHideCols");
            this.cmi_ShowHideCols.Click += new System.EventHandler(this.selectColumnsToolStripMenuItem_Click);
            // 
            // cmi_removeCachedData
            // 
            this.cmi_removeCachedData.Name = "cmi_removeCachedData";
            resources.ApplyResources(this.cmi_removeCachedData, "cmi_removeCachedData");
            this.cmi_removeCachedData.Click += new System.EventHandler((sender,
                                                                        e) => this.cmi_removeCachedData_Click(sender, e));
            // 
            // cmi_OpenCoordsInAPI
            // 
            this.cmi_OpenCoordsInAPI.Name = "cmi_OpenCoordsInAPI";
            resources.ApplyResources(this.cmi_OpenCoordsInAPI, "cmi_OpenCoordsInAPI");
            this.cmi_OpenCoordsInAPI.Click += new System.EventHandler(this.cmi_OpenCoordsInAPI_Click);
            // 
            // pbx_imagePreview
            // 
            this.pbx_imagePreview.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            resources.ApplyResources(this.pbx_imagePreview, "pbx_imagePreview");
            this.pbx_imagePreview.EmptyText = "No image to show";
            this.pbx_imagePreview.Name = "pbx_imagePreview";
            this.pbx_imagePreview.TabStop = false;
            // 
            // flp_ProcessingInfo
            // 
            this.flp_ProcessingInfo.Controls.Add(this.lbl_ParseProgress);
            resources.ApplyResources(this.flp_ProcessingInfo, "flp_ProcessingInfo");
            this.flp_ProcessingInfo.Name = "flp_ProcessingInfo";
            // 
            // lbl_ParseProgress
            // 
            resources.ApplyResources(this.lbl_ParseProgress, "lbl_ParseProgress");
            this.lbl_ParseProgress.Name = "lbl_ParseProgress";
            // 
            // tcr_Main
            // 
            this.tcr_Main.Controls.Add(this.tpg_Map);
            this.tcr_Main.Controls.Add(this.tpg_Exif);
            resources.ApplyResources(this.tcr_Main, "tcr_Main");
            this.tcr_Main.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcr_Main.ImageList = this.igl_RightHandSide;
            this.tcr_Main.Name = "tcr_Main";
            this.tcr_Main.SelectedIndex = 0;
            // 
            // tpg_Map
            // 
            this.tpg_Map.Controls.Add(this.flp_GeoCoords);
            this.tpg_Map.Controls.Add(this.wbv_MapArea);
            resources.ApplyResources(this.tpg_Map, "tpg_Map");
            this.tpg_Map.Name = "tpg_Map";
            this.tpg_Map.UseVisualStyleBackColor = true;
            // 
            // flp_GeoCoords
            // 
            this.flp_GeoCoords.Controls.Add(this.lbl_lat);
            this.flp_GeoCoords.Controls.Add(this.nud_lat);
            this.flp_GeoCoords.Controls.Add(this.lbl_lng);
            this.flp_GeoCoords.Controls.Add(this.nud_lng);
            this.flp_GeoCoords.Controls.Add(this.btn_NavigateMapGo);
            this.flp_GeoCoords.Controls.Add(this.btn_loctToFile);
            this.flp_GeoCoords.Controls.Add(this.btn_loctToFileDestination);
            this.flp_GeoCoords.Controls.Add(this.lbl_Favourites);
            this.flp_GeoCoords.Controls.Add(this.cbx_Favourites);
            this.flp_GeoCoords.Controls.Add(this.btn_SaveFavourite);
            this.flp_GeoCoords.Controls.Add(this.btn_LoadFavourite);
            this.flp_GeoCoords.Controls.Add(this.btn_ManageFavourites);
            resources.ApplyResources(this.flp_GeoCoords, "flp_GeoCoords");
            this.flp_GeoCoords.Name = "flp_GeoCoords";
            // 
            // lbl_lat
            // 
            resources.ApplyResources(this.lbl_lat, "lbl_lat");
            this.lbl_lat.Name = "lbl_lat";
            // 
            // nud_lat
            // 
            this.nud_lat.DecimalPlaces = 6;
            resources.ApplyResources(this.nud_lat, "nud_lat");
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
            // 
            // lbl_lng
            // 
            resources.ApplyResources(this.lbl_lng, "lbl_lng");
            this.lbl_lng.Name = "lbl_lng";
            // 
            // nud_lng
            // 
            this.nud_lng.DecimalPlaces = 6;
            resources.ApplyResources(this.nud_lng, "nud_lng");
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
            // 
            // btn_NavigateMapGo
            // 
            this.btn_NavigateMapGo.BackColor = System.Drawing.SystemColors.Menu;
            resources.ApplyResources(this.btn_NavigateMapGo, "btn_NavigateMapGo");
            this.btn_NavigateMapGo.Name = "btn_NavigateMapGo";
            this.btn_NavigateMapGo.UseVisualStyleBackColor = false;
            this.btn_NavigateMapGo.Click += new System.EventHandler(this.btn_NavigateMapGo_Click);
            // 
            // btn_loctToFile
            // 
            resources.ApplyResources(this.btn_loctToFile, "btn_loctToFile");
            this.btn_loctToFile.Name = "btn_loctToFile";
            this.btn_loctToFile.UseVisualStyleBackColor = true;
            this.btn_loctToFile.Click += new System.EventHandler(this.btn_loctToFile_Click);
            // 
            // btn_loctToFileDestination
            // 
            resources.ApplyResources(this.btn_loctToFileDestination, "btn_loctToFileDestination");
            this.btn_loctToFileDestination.Name = "btn_loctToFileDestination";
            this.btn_loctToFileDestination.UseVisualStyleBackColor = true;
            this.btn_loctToFileDestination.Click += new System.EventHandler(this.btn_loctToFile_Click);
            // 
            // lbl_Favourites
            // 
            resources.ApplyResources(this.lbl_Favourites, "lbl_Favourites");
            this.lbl_Favourites.Name = "lbl_Favourites";
            // 
            // cbx_Favourites
            // 
            this.cbx_Favourites.FormattingEnabled = true;
            resources.ApplyResources(this.cbx_Favourites, "cbx_Favourites");
            this.cbx_Favourites.Name = "cbx_Favourites";
            this.cbx_Favourites.SelectedValueChanged += new System.EventHandler(this.cbx_Favourites_SelectedValueChanged);
            // 
            // btn_SaveFavourite
            // 
            resources.ApplyResources(this.btn_SaveFavourite, "btn_SaveFavourite");
            this.btn_SaveFavourite.Name = "btn_SaveFavourite";
            this.btn_SaveFavourite.UseVisualStyleBackColor = true;
            this.btn_SaveFavourite.Click += new System.EventHandler(this.btn_SaveLocation_Click);
            // 
            // btn_LoadFavourite
            // 
            resources.ApplyResources(this.btn_LoadFavourite, "btn_LoadFavourite");
            this.btn_LoadFavourite.Name = "btn_LoadFavourite";
            this.btn_LoadFavourite.UseVisualStyleBackColor = true;
            this.btn_LoadFavourite.Click += new System.EventHandler(this.btn_LoadFavourite_Click);
            // 
            // btn_ManageFavourites
            // 
            resources.ApplyResources(this.btn_ManageFavourites, "btn_ManageFavourites");
            this.btn_ManageFavourites.Name = "btn_ManageFavourites";
            this.btn_ManageFavourites.UseVisualStyleBackColor = true;
            this.btn_ManageFavourites.Click += new System.EventHandler(this.btn_ManageFavourites_Click);
            // 
            // wbv_MapArea
            // 
            this.wbv_MapArea.AllowExternalDrop = true;
            this.wbv_MapArea.CreationProperties = null;
            this.wbv_MapArea.DefaultBackgroundColor = System.Drawing.Color.White;
            resources.ApplyResources(this.wbv_MapArea, "wbv_MapArea");
            this.wbv_MapArea.Name = "wbv_MapArea";
            this.wbv_MapArea.ZoomFactor = 1D;
            this.wbv_MapArea.Click += new System.EventHandler(this.wbv_MapArea_Click);
            // 
            // tpg_Exif
            // 
            this.tpg_Exif.Controls.Add(this.lvw_ExifData);
            resources.ApplyResources(this.tpg_Exif, "tpg_Exif");
            this.tpg_Exif.Name = "tpg_Exif";
            this.tpg_Exif.UseVisualStyleBackColor = true;
            // 
            // lvw_ExifData
            // 
            this.lvw_ExifData.BackColor = System.Drawing.SystemColors.ControlLight;
            this.lvw_ExifData.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.clh_ExifTag,
            this.clh_OriginalValue,
            this.clh_ModifiedValue});
            resources.ApplyResources(this.lvw_ExifData, "lvw_ExifData");
            this.lvw_ExifData.FullRowSelect = true;
            this.lvw_ExifData.GridLines = true;
            this.lvw_ExifData.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvw_ExifData.HideSelection = false;
            this.lvw_ExifData.Name = "lvw_ExifData";
            this.lvw_ExifData.OwnerDraw = true;
            this.lvw_ExifData.UseCompatibleStateImageBehavior = false;
            this.lvw_ExifData.View = System.Windows.Forms.View.Details;
            this.lvw_ExifData.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.ListView_DrawColumnHeader);
            this.lvw_ExifData.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.ListView_DrawItem);
            this.lvw_ExifData.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.ListView_DrawSubItem);
            this.lvw_ExifData.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvw_ExifData_KeyDown);
            this.lvw_ExifData.KeyUp += new System.Windows.Forms.KeyEventHandler(this.lvw_ExifData_KeyUp);
            // 
            // clh_ExifTag
            // 
            resources.ApplyResources(this.clh_ExifTag, "clh_ExifTag");
            // 
            // clh_OriginalValue
            // 
            resources.ApplyResources(this.clh_OriginalValue, "clh_OriginalValue");
            // 
            // clh_ModifiedValue
            // 
            resources.ApplyResources(this.clh_ModifiedValue, "clh_ModifiedValue");
            // 
            // igl_RightHandSide
            // 
            this.igl_RightHandSide.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("igl_RightHandSide.ImageStream")));
            this.igl_RightHandSide.TransparentColor = System.Drawing.Color.Transparent;
            this.igl_RightHandSide.Images.SetKeyName(0, "PublishOnDemand.png");
            this.igl_RightHandSide.Images.SetKeyName(1, "StringData.png");
            // 
            // mns_MenuStrip
            // 
            this.mns_MenuStrip.ImageScalingSize = new System.Drawing.Size(28, 28);
            this.mns_MenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_File,
            this.tmi_Settings,
            this.tmi_Help});
            resources.ApplyResources(this.mns_MenuStrip, "mns_MenuStrip");
            this.mns_MenuStrip.Name = "mns_MenuStrip";
            // 
            // tmi_File
            // 
            this.tmi_File.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_File_SaveAll,
            this.tmi_File_EditFiles,
            this.tmi_File_ImportExportGPX,
            this.tmi_File_CopyGeoData,
            this.tmi_File_PasteGeoData,
            this.tss_ToolStripSeparator_Main,
            this.tmi_File_FlatMode,
            this.toolStripSeparator2,
            this.tmi_File_Quit});
            this.tmi_File.Name = "tmi_File";
            resources.ApplyResources(this.tmi_File, "tmi_File");
            this.tmi_File.DropDownOpening += new System.EventHandler(this.tmi_File_DropDownOpening);
            // 
            // tmi_File_SaveAll
            // 
            this.tmi_File_SaveAll.Name = "tmi_File_SaveAll";
            resources.ApplyResources(this.tmi_File_SaveAll, "tmi_File_SaveAll");
            this.tmi_File_SaveAll.Click += new System.EventHandler(this.tmi_File_SaveAll_Click);
            // 
            // tmi_File_EditFiles
            // 
            this.tmi_File_EditFiles.Name = "tmi_File_EditFiles";
            resources.ApplyResources(this.tmi_File_EditFiles, "tmi_File_EditFiles");
            this.tmi_File_EditFiles.Click += new System.EventHandler(this.tmi_File_EditFiles_Click);
            // 
            // tmi_File_ImportExportGPX
            // 
            this.tmi_File_ImportExportGPX.Name = "tmi_File_ImportExportGPX";
            resources.ApplyResources(this.tmi_File_ImportExportGPX, "tmi_File_ImportExportGPX");
            this.tmi_File_ImportExportGPX.Click += new System.EventHandler(this.tmi_File_ImportExportGPX_Click);
            // 
            // tmi_File_CopyGeoData
            // 
            this.tmi_File_CopyGeoData.Name = "tmi_File_CopyGeoData";
            resources.ApplyResources(this.tmi_File_CopyGeoData, "tmi_File_CopyGeoData");
            this.tmi_File_CopyGeoData.Click += new System.EventHandler(this.tmi_File_CopyGeoData_Click);
            // 
            // tmi_File_PasteGeoData
            // 
            this.tmi_File_PasteGeoData.Name = "tmi_File_PasteGeoData";
            resources.ApplyResources(this.tmi_File_PasteGeoData, "tmi_File_PasteGeoData");
            this.tmi_File_PasteGeoData.Click += new System.EventHandler(this.tmi_File_PasteGeoData_Click);
            // 
            // tss_ToolStripSeparator_Main
            // 
            this.tss_ToolStripSeparator_Main.Name = "tss_ToolStripSeparator_Main";
            resources.ApplyResources(this.tss_ToolStripSeparator_Main, "tss_ToolStripSeparator_Main");
            // 
            // tmi_File_FlatMode
            // 
            this.tmi_File_FlatMode.CheckOnClick = true;
            this.tmi_File_FlatMode.Name = "tmi_File_FlatMode";
            resources.ApplyResources(this.tmi_File_FlatMode, "tmi_File_FlatMode");
            this.tmi_File_FlatMode.Click += new System.EventHandler(this.tmiFileFlatModeToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // tmi_File_Quit
            // 
            this.tmi_File_Quit.Name = "tmi_File_Quit";
            resources.ApplyResources(this.tmi_File_Quit, "tmi_File_Quit");
            this.tmi_File_Quit.Click += new System.EventHandler(this.tmi_File_Quit_Click);
            // 
            // tmi_Settings
            // 
            this.tmi_Settings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_Settings_Settings,
            this.tmi_Settings_Favourites});
            this.tmi_Settings.Name = "tmi_Settings";
            resources.ApplyResources(this.tmi_Settings, "tmi_Settings");
            // 
            // tmi_Settings_Settings
            // 
            this.tmi_Settings_Settings.Name = "tmi_Settings_Settings";
            resources.ApplyResources(this.tmi_Settings_Settings, "tmi_Settings_Settings");
            this.tmi_Settings_Settings.Click += new System.EventHandler(this.tmi_Settings_Settings_Click);
            // 
            // tmi_Settings_Favourites
            // 
            this.tmi_Settings_Favourites.Name = "tmi_Settings_Favourites";
            resources.ApplyResources(this.tmi_Settings_Favourites, "tmi_Settings_Favourites");
            this.tmi_Settings_Favourites.Click += new System.EventHandler(this.tmi_Settings_Favourites_Click);
            // 
            // tmi_Help
            // 
            this.tmi_Help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_Help_About,
            this.tmi_Help_FeedbackFeatureRequest,
            this.tmi_Help_BugReport});
            this.tmi_Help.Name = "tmi_Help";
            resources.ApplyResources(this.tmi_Help, "tmi_Help");
            // 
            // tmi_Help_About
            // 
            this.tmi_Help_About.Name = "tmi_Help_About";
            resources.ApplyResources(this.tmi_Help_About, "tmi_Help_About");
            this.tmi_Help_About.Click += new System.EventHandler(this.tmi_Help_About_Click);
            // 
            // tmi_Help_FeedbackFeatureRequest
            // 
            this.tmi_Help_FeedbackFeatureRequest.Name = "tmi_Help_FeedbackFeatureRequest";
            resources.ApplyResources(this.tmi_Help_FeedbackFeatureRequest, "tmi_Help_FeedbackFeatureRequest");
            this.tmi_Help_FeedbackFeatureRequest.Click += new System.EventHandler(this.tmi_Help_FeedbackFeatureRequest_Click);
            // 
            // tmi_Help_BugReport
            // 
            this.tmi_Help_BugReport.Name = "tmi_Help_BugReport";
            resources.ApplyResources(this.tmi_Help_BugReport, "tmi_Help_BugReport");
            this.tmi_Help_BugReport.Click += new System.EventHandler(this.tmi_Help_BugReport_Click);
            // 
            // tsr_MainAppToolStrip
            // 
            this.tsr_MainAppToolStrip.GripMargin = new System.Windows.Forms.Padding(0);
            this.tsr_MainAppToolStrip.ImageScalingSize = new System.Drawing.Size(0, 0);
            this.tsr_MainAppToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsb_SaveFiles,
            this.tsb_Refresh_lvwFileList,
            this.tsb_EditFile,
            this.tsb_GetAllFromWeb,
            this.tsb_RemoveGeoData,
            this.tsb_ImportExportGPX,
            this.toolStripSeparator1,
            this.tsb_FeedbackFeatureRequest,
            this.tsb_BugReport});
            resources.ApplyResources(this.tsr_MainAppToolStrip, "tsr_MainAppToolStrip");
            this.tsr_MainAppToolStrip.Name = "tsr_MainAppToolStrip";
            // 
            // tsb_SaveFiles
            // 
            this.tsb_SaveFiles.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.tsb_SaveFiles, "tsb_SaveFiles");
            this.tsb_SaveFiles.Name = "tsb_SaveFiles";
            this.tsb_SaveFiles.Click += new System.EventHandler(this.tsb_SaveFiles_Click);
            // 
            // tsb_Refresh_lvwFileList
            // 
            this.tsb_Refresh_lvwFileList.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.tsb_Refresh_lvwFileList, "tsb_Refresh_lvwFileList");
            this.tsb_Refresh_lvwFileList.Name = "tsb_Refresh_lvwFileList";
            this.tsb_Refresh_lvwFileList.Click += new System.EventHandler(this.tsb_Refresh_lvwFileList_Click);
            // 
            // tsb_EditFile
            // 
            this.tsb_EditFile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.tsb_EditFile, "tsb_EditFile");
            this.tsb_EditFile.Name = "tsb_EditFile";
            this.tsb_EditFile.Click += new System.EventHandler(this.tsb_EditFile_Click);
            // 
            // tsb_GetAllFromWeb
            // 
            this.tsb_GetAllFromWeb.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.tsb_GetAllFromWeb, "tsb_GetAllFromWeb");
            this.tsb_GetAllFromWeb.Name = "tsb_GetAllFromWeb";
            this.tsb_GetAllFromWeb.Click += new System.EventHandler(this.tsb_GetAllFromWeb_Click);
            // 
            // tsb_RemoveGeoData
            // 
            this.tsb_RemoveGeoData.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.tsb_RemoveGeoData, "tsb_RemoveGeoData");
            this.tsb_RemoveGeoData.Name = "tsb_RemoveGeoData";
            this.tsb_RemoveGeoData.Click += new System.EventHandler(this.tsb_RemoveGeoData_Click);
            // 
            // tsb_ImportExportGPX
            // 
            this.tsb_ImportExportGPX.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.tsb_ImportExportGPX, "tsb_ImportExportGPX");
            this.tsb_ImportExportGPX.Name = "tsb_ImportExportGPX";
            this.tsb_ImportExportGPX.Click += new System.EventHandler(this.tsb_ImportExportGPX_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // tsb_FeedbackFeatureRequest
            // 
            this.tsb_FeedbackFeatureRequest.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.tsb_FeedbackFeatureRequest, "tsb_FeedbackFeatureRequest");
            this.tsb_FeedbackFeatureRequest.Name = "tsb_FeedbackFeatureRequest";
            this.tsb_FeedbackFeatureRequest.Click += new System.EventHandler(this.tsb_FeedbackFeatureRequest_Click);
            // 
            // tsb_BugReport
            // 
            this.tsb_BugReport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.tsb_BugReport, "tsb_BugReport");
            this.tsb_BugReport.Name = "tsb_BugReport";
            this.tsb_BugReport.Click += new System.EventHandler(this.tsb_BugReport_Click);
            // 
            // tsr_FolderControl
            // 
            this.tsr_FolderControl.GripMargin = new System.Windows.Forms.Padding(0);
            this.tsr_FolderControl.ImageScalingSize = new System.Drawing.Size(0, 0);
            this.tsr_FolderControl.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbx_FolderName,
            this.tsb_OneFolderUp});
            resources.ApplyResources(this.tsr_FolderControl, "tsr_FolderControl");
            this.tsr_FolderControl.Name = "tsr_FolderControl";
            // 
            // tbx_FolderName
            // 
            this.tbx_FolderName.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.tbx_FolderName.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.tbx_FolderName.Name = "tbx_FolderName";
            resources.ApplyResources(this.tbx_FolderName, "tbx_FolderName");
            this.tbx_FolderName.Enter += new System.EventHandler(this.tbx_FolderName_Enter);
            this.tbx_FolderName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbx_FolderName_KeyDown);
            this.tbx_FolderName.Click += new System.EventHandler(this.tbx_FolderName_Enter);
            // 
            // tsb_OneFolderUp
            // 
            this.tsb_OneFolderUp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            resources.ApplyResources(this.tsb_OneFolderUp, "tsb_OneFolderUp");
            this.tsb_OneFolderUp.Name = "tsb_OneFolderUp";
            this.tsb_OneFolderUp.Click += new System.EventHandler(this.btn_OneFolderUp_Click);
            // 
            // FrmMainApp
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainerMain);
            this.Controls.Add(this.tsr_FolderControl);
            this.Controls.Add(this.tsr_MainAppToolStrip);
            this.Controls.Add(this.mns_MenuStrip);
            this.MainMenuStrip = this.mns_MenuStrip;
            this.Name = "FrmMainApp";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmMainApp_FormClosing);
            this.Load += new System.EventHandler(this.FrmMainApp_Load);
            this.ResizeBegin += new System.EventHandler(this.FrmMainApp_ResizeBegin);
            this.ResizeEnd += new System.EventHandler(this.FrmMainApp_ResizeEnd);
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
            this.tcr_Main.ResumeLayout(false);
            this.tpg_Map.ResumeLayout(false);
            this.flp_GeoCoords.ResumeLayout(false);
            this.flp_GeoCoords.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_lat)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_lng)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.wbv_MapArea)).EndInit();
            this.tpg_Exif.ResumeLayout(false);
            this.mns_MenuStrip.ResumeLayout(false);
            this.mns_MenuStrip.PerformLayout();
            this.tsr_MainAppToolStrip.ResumeLayout(false);
            this.tsr_MainAppToolStrip.PerformLayout();
            this.tsr_FolderControl.ResumeLayout(false);
            this.tsr_FolderControl.PerformLayout();
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
        private ToolStripMenuItem tmi_File_ImportExportGPX;
        private ToolStripButton tsb_ImportExportGPX;
        private ToolStripButton tsb_GetAllFromWeb;
        internal SplitContainer splitContainerMain;
        internal SplitContainer splitContainerLeftTop;
        internal FileListView lvw_FileList;
        private ColumnHeader clh_FileName;
        internal ImagePreview pbx_imagePreview;
        internal Label lbl_ParseProgress;
        internal TabControl tcr_Main;
        internal TabPage tpg_Map;
        internal Microsoft.Web.WebView2.WinForms.WebView2 wbv_MapArea;
        private ToolTip ttp_NavigateMapGo;
        private ToolTip ttp_loctToFile;
        private ImageList igl_RightHandSide;
        private FlowLayoutPanel flp_ProcessingInfo;
        private ContextMenuStrip cms_FileListView;
        private ToolStripMenuItem cmi_ShowHideCols;
        private ToolTip ttp_SaveFavourite;
        private ToolTip ttp_LoadFavourite;
        private ToolStripMenuItem cmi_removeCachedData;
        private ToolStripMenuItem tmi_Settings_Favourites;
        private ToolTip ttp_ManageFavourites;
        private ToolStripMenuItem cmi_OpenCoordsInAPI;
        private ToolTip ttp_loctToFileDestination;
        private FlowLayoutPanel flp_GeoCoords;
        private Label lbl_lat;
        internal NumericUpDown nud_lat;
        private Label lbl_lng;
        internal NumericUpDown nud_lng;
        private Button btn_NavigateMapGo;
        private Button btn_loctToFile;
        private Button btn_loctToFileDestination;
        private Label lbl_Favourites;
        internal ComboBox cbx_Favourites;
        private Button btn_SaveFavourite;
        private Button btn_LoadFavourite;
        private Button btn_ManageFavourites;
        private TabPage tpg_Exif;
        internal ListView lvw_ExifData;
        internal ColumnHeader clh_ExifTag;
        internal ColumnHeader clh_OriginalValue;
        internal ColumnHeader clh_ModifiedValue;
        private ToolStripMenuItem tmi_Help_FeedbackFeatureRequest;
        private ToolStripMenuItem tmi_Help_BugReport;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton tsb_FeedbackFeatureRequest;
        private ToolStripButton tsb_BugReport;
        private ToolTip ttp_FeedbackFeatureRequest;
        private ToolTip ttp_BugReport;
        private ToolStripMenuItem tmi_File_FlatMode;
        private ToolStripSeparator toolStripSeparator2;
    }
}

