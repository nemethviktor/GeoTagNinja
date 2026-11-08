namespace GeoTagNinja.View.Forms
{
    partial class FrmManageFavourites
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
            this.gbx_AllFavourites = new System.Windows.Forms.GroupBox();
            this.tsp_Favourites = new System.Windows.Forms.ToolStrip();
            this.tsd_GroupBy = new System.Windows.Forms.ToolStripDropDownButton();
            this.tmi_GroupByCountryCode = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_GroupByCountry = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_GroupByState = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_GroupByCity = new System.Windows.Forms.ToolStripMenuItem();
            this.tmi_GroupByCoordinates = new System.Windows.Forms.ToolStripMenuItem();
            this.tss_GroupBy = new System.Windows.Forms.ToolStripSeparator();
            this.tmi_GroupByNone = new System.Windows.Forms.ToolStripMenuItem();
            this.tsb_Duplicate = new System.Windows.Forms.ToolStripButton();
            this.tsb_Delete = new System.Windows.Forms.ToolStripButton();
            this.tsb_AddFromSelection = new System.Windows.Forms.ToolStripButton();
            this.trv_Favourites = new System.Windows.Forms.TreeView();
            this.lbl_FavouritesCount = new System.Windows.Forms.Label();
            this.gbx_FavouriteDetails = new System.Windows.Forms.GroupBox();
            this.lbl_FavouriteName = new System.Windows.Forms.Label();
            this.tbx_FavouriteName = new System.Windows.Forms.TextBox();
            this.lbl_FavouriteCoordinates = new System.Windows.Forms.Label();
            this.tbx_FavouriteCoordinates = new System.Windows.Forms.TextBox();
            this.gbx_FavouriteAdditionalInfo = new System.Windows.Forms.GroupBox();
            this.lbl_FavouriteRadius = new System.Windows.Forms.Label();
            this.nud_FavouriteRadius = new System.Windows.Forms.NumericUpDown();
            this.ckb_FavouriteAutoAssign = new System.Windows.Forms.CheckBox();
            this.ckb_FavouriteSnap = new System.Windows.Forms.CheckBox();
            this.lbl_GPSAltitude = new System.Windows.Forms.Label();
            this.tbx_FavouriteAltitude = new System.Windows.Forms.TextBox();
            this.lbl_FavouriteTimeZone = new System.Windows.Forms.Label();
            this.cbx_FavouriteTimeZone = new System.Windows.Forms.ComboBox();
            this.lbl_CountryCode = new System.Windows.Forms.Label();
            this.cbx_CountryCode = new System.Windows.Forms.ComboBox();
            this.lbl_Country = new System.Windows.Forms.Label();
            this.cbx_Country = new System.Windows.Forms.ComboBox();
            this.lbl_State = new System.Windows.Forms.Label();
            this.tbx_State = new System.Windows.Forms.TextBox();
            this.lbl_City = new System.Windows.Forms.Label();
            this.tbx_City = new System.Windows.Forms.TextBox();
            this.lbl_Sublocation = new System.Windows.Forms.Label();
            this.tbx_Sublocation = new System.Windows.Forms.TextBox();
            this.btn_FavouriteGetFromWeb = new System.Windows.Forms.Button();
            this.lbl_FavouriteKeywords = new System.Windows.Forms.Label();
            this.tbx_FavouriteKeyword = new System.Windows.Forms.TextBox();
            this.btn_FavouriteAddKeyword = new System.Windows.Forms.Button();
            this.btn_FavouriteRemoveKeyword = new System.Windows.Forms.Button();
            this.lbx_FavouriteKeywords = new System.Windows.Forms.ListBox();
            this.btn_FavouriteClearAll = new System.Windows.Forms.Button();
            this.btn_FavouriteAccept = new System.Windows.Forms.Button();
            this.btn_FavouriteDiscard = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.gbx_AllFavourites.SuspendLayout();
            this.tsp_Favourites.SuspendLayout();
            this.gbx_FavouriteDetails.SuspendLayout();
            this.gbx_FavouriteAdditionalInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_FavouriteRadius)).BeginInit();
            this.SuspendLayout();
            //
            // gbx_AllFavourites
            //
            this.gbx_AllFavourites.Controls.Add(this.lbl_FavouritesCount);
            this.gbx_AllFavourites.Controls.Add(this.trv_Favourites);
            this.gbx_AllFavourites.Controls.Add(this.tsp_Favourites);
            this.gbx_AllFavourites.Location = new System.Drawing.Point(12, 8);
            this.gbx_AllFavourites.Name = "gbx_AllFavourites";
            this.gbx_AllFavourites.Size = new System.Drawing.Size(440, 596);
            this.gbx_AllFavourites.TabIndex = 0;
            this.gbx_AllFavourites.TabStop = false;
            this.gbx_AllFavourites.Text = "All Favourites";
            this.gbx_AllFavourites.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left))));
            //
            // tsp_Favourites
            //
            this.tsp_Favourites.AutoSize = false;
            this.tsp_Favourites.Dock = System.Windows.Forms.DockStyle.None;
            this.tsp_Favourites.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.tsp_Favourites.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsd_GroupBy,
            this.tsb_Duplicate,
            this.tsb_Delete,
            this.tsb_AddFromSelection});
            this.tsp_Favourites.Location = new System.Drawing.Point(8, 20);
            this.tsp_Favourites.Name = "tsp_Favourites";
            this.tsp_Favourites.Size = new System.Drawing.Size(424, 26);
            this.tsp_Favourites.TabIndex = 0;
            //
            // tsd_GroupBy
            //
            this.tsd_GroupBy.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tmi_GroupByCountryCode,
            this.tmi_GroupByCountry,
            this.tmi_GroupByState,
            this.tmi_GroupByCity,
            this.tmi_GroupByCoordinates,
            this.tss_GroupBy,
            this.tmi_GroupByNone});
            this.tsd_GroupBy.Name = "tsd_GroupBy";
            this.tsd_GroupBy.Text = "Group By";
            //
            // tmi_GroupByCountryCode
            //
            this.tmi_GroupByCountryCode.CheckOnClick = true;
            this.tmi_GroupByCountryCode.Name = "tmi_GroupByCountryCode";
            this.tmi_GroupByCountryCode.Text = "Country Code";
            this.tmi_GroupByCountryCode.Click += new System.EventHandler(this.any_GroupByItem_Click);
            //
            // tmi_GroupByCountry
            //
            this.tmi_GroupByCountry.CheckOnClick = true;
            this.tmi_GroupByCountry.Name = "tmi_GroupByCountry";
            this.tmi_GroupByCountry.Text = "Country";
            this.tmi_GroupByCountry.Click += new System.EventHandler(this.any_GroupByItem_Click);
            //
            // tmi_GroupByState
            //
            this.tmi_GroupByState.CheckOnClick = true;
            this.tmi_GroupByState.Name = "tmi_GroupByState";
            this.tmi_GroupByState.Text = "State/Province";
            this.tmi_GroupByState.Click += new System.EventHandler(this.any_GroupByItem_Click);
            //
            // tmi_GroupByCity
            //
            this.tmi_GroupByCity.CheckOnClick = true;
            this.tmi_GroupByCity.Name = "tmi_GroupByCity";
            this.tmi_GroupByCity.Text = "City";
            this.tmi_GroupByCity.Click += new System.EventHandler(this.any_GroupByItem_Click);
            //
            // tmi_GroupByCoordinates
            //
            this.tmi_GroupByCoordinates.CheckOnClick = true;
            this.tmi_GroupByCoordinates.Name = "tmi_GroupByCoordinates";
            this.tmi_GroupByCoordinates.Text = "Coordinates";
            this.tmi_GroupByCoordinates.Click += new System.EventHandler(this.any_GroupByItem_Click);
            //
            // tss_GroupBy
            //
            this.tss_GroupBy.Name = "tss_GroupBy";
            //
            // tmi_GroupByNone
            //
            this.tmi_GroupByNone.Name = "tmi_GroupByNone";
            this.tmi_GroupByNone.Text = "No Groupings";
            this.tmi_GroupByNone.Click += new System.EventHandler(this.tmi_GroupByNone_Click);
            //
            // tsb_Duplicate
            //
            this.tsb_Duplicate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsb_Duplicate.Name = "tsb_Duplicate";
            this.tsb_Duplicate.Text = "Duplicate";
            this.tsb_Duplicate.Click += new System.EventHandler(this.tsb_Duplicate_Click);
            //
            // tsb_Delete
            //
            this.tsb_Delete.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsb_Delete.Name = "tsb_Delete";
            this.tsb_Delete.Text = "Delete";
            this.tsb_Delete.Click += new System.EventHandler(this.tsb_Delete_Click);
            //
            // tsb_AddFromSelection
            //
            this.tsb_AddFromSelection.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsb_AddFromSelection.Name = "tsb_AddFromSelection";
            this.tsb_AddFromSelection.Text = "Add from selected photo";
            this.tsb_AddFromSelection.Click += new System.EventHandler(this.tsb_AddFromSelection_Click);
            //
            // trv_Favourites
            //
            this.trv_Favourites.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trv_Favourites.HideSelection = false;
            this.trv_Favourites.Location = new System.Drawing.Point(8, 50);
            this.trv_Favourites.Name = "trv_Favourites";
            this.trv_Favourites.Size = new System.Drawing.Size(424, 516);
            this.trv_Favourites.TabIndex = 1;
            this.trv_Favourites.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.trv_Favourites_BeforeSelect);
            this.trv_Favourites.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.trv_Favourites_AfterSelect);
            //
            // lbl_FavouritesCount
            //
            this.lbl_FavouritesCount.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_FavouritesCount.Location = new System.Drawing.Point(8, 570);
            this.lbl_FavouritesCount.Name = "lbl_FavouritesCount";
            this.lbl_FavouritesCount.Size = new System.Drawing.Size(424, 20);
            this.lbl_FavouritesCount.TabIndex = 2;
            this.lbl_FavouritesCount.Text = "0 Items (0 Selected)";
            //
            // gbx_FavouriteDetails
            //
            this.gbx_FavouriteDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbx_FavouriteDetails.Controls.Add(this.btn_FavouriteDiscard);
            this.gbx_FavouriteDetails.Controls.Add(this.btn_FavouriteAccept);
            this.gbx_FavouriteDetails.Controls.Add(this.gbx_FavouriteAdditionalInfo);
            this.gbx_FavouriteDetails.Controls.Add(this.tbx_FavouriteCoordinates);
            this.gbx_FavouriteDetails.Controls.Add(this.lbl_FavouriteCoordinates);
            this.gbx_FavouriteDetails.Controls.Add(this.tbx_FavouriteName);
            this.gbx_FavouriteDetails.Controls.Add(this.lbl_FavouriteName);
            this.gbx_FavouriteDetails.Location = new System.Drawing.Point(464, 8);
            this.gbx_FavouriteDetails.Name = "gbx_FavouriteDetails";
            this.gbx_FavouriteDetails.Size = new System.Drawing.Size(492, 596);
            this.gbx_FavouriteDetails.TabIndex = 1;
            this.gbx_FavouriteDetails.TabStop = false;
            this.gbx_FavouriteDetails.Text = "Details";
            //
            // lbl_FavouriteName
            //
            this.lbl_FavouriteName.AutoSize = true;
            this.lbl_FavouriteName.Location = new System.Drawing.Point(14, 28);
            this.lbl_FavouriteName.Name = "lbl_FavouriteName";
            this.lbl_FavouriteName.Size = new System.Drawing.Size(38, 13);
            this.lbl_FavouriteName.TabIndex = 0;
            this.lbl_FavouriteName.Text = "Name:";
            //
            // tbx_FavouriteName
            //
            this.tbx_FavouriteName.Location = new System.Drawing.Point(150, 24);
            this.tbx_FavouriteName.Name = "tbx_FavouriteName";
            this.tbx_FavouriteName.Size = new System.Drawing.Size(326, 20);
            this.tbx_FavouriteName.TabIndex = 1;
            this.tbx_FavouriteName.TextChanged += new System.EventHandler(this.any_Edit_Changed);
            //
            // lbl_FavouriteCoordinates
            //
            this.lbl_FavouriteCoordinates.AutoSize = true;
            this.lbl_FavouriteCoordinates.Location = new System.Drawing.Point(14, 58);
            this.lbl_FavouriteCoordinates.Name = "lbl_FavouriteCoordinates";
            this.lbl_FavouriteCoordinates.Size = new System.Drawing.Size(69, 13);
            this.lbl_FavouriteCoordinates.TabIndex = 2;
            this.lbl_FavouriteCoordinates.Text = "Coordinates:";
            //
            // tbx_FavouriteCoordinates
            //
            this.tbx_FavouriteCoordinates.Location = new System.Drawing.Point(150, 54);
            this.tbx_FavouriteCoordinates.Name = "tbx_FavouriteCoordinates";
            this.tbx_FavouriteCoordinates.ReadOnly = true;
            this.tbx_FavouriteCoordinates.Size = new System.Drawing.Size(326, 20);
            this.tbx_FavouriteCoordinates.TabIndex = 3;
            //
            // gbx_FavouriteAdditionalInfo
            //
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.btn_FavouriteClearAll);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.lbx_FavouriteKeywords);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.btn_FavouriteRemoveKeyword);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.btn_FavouriteAddKeyword);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.tbx_FavouriteKeyword);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.lbl_FavouriteKeywords);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.btn_FavouriteGetFromWeb);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.tbx_Sublocation);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.lbl_Sublocation);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.tbx_City);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.lbl_City);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.tbx_State);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.lbl_State);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.cbx_Country);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.lbl_Country);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.cbx_CountryCode);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.lbl_CountryCode);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.cbx_FavouriteTimeZone);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.lbl_FavouriteTimeZone);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.tbx_FavouriteAltitude);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.lbl_GPSAltitude);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.ckb_FavouriteSnap);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.ckb_FavouriteAutoAssign);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.nud_FavouriteRadius);
            this.gbx_FavouriteAdditionalInfo.Controls.Add(this.lbl_FavouriteRadius);
            this.gbx_FavouriteAdditionalInfo.Location = new System.Drawing.Point(8, 86);
            this.gbx_FavouriteAdditionalInfo.Name = "gbx_FavouriteAdditionalInfo";
            this.gbx_FavouriteAdditionalInfo.Size = new System.Drawing.Size(476, 466);
            this.gbx_FavouriteAdditionalInfo.TabIndex = 4;
            this.gbx_FavouriteAdditionalInfo.TabStop = false;
            this.gbx_FavouriteAdditionalInfo.Text = "Additional Info";
            //
            // lbl_FavouriteRadius
            //
            this.lbl_FavouriteRadius.AutoSize = true;
            this.lbl_FavouriteRadius.Location = new System.Drawing.Point(10, 28);
            this.lbl_FavouriteRadius.Name = "lbl_FavouriteRadius";
            this.lbl_FavouriteRadius.Size = new System.Drawing.Size(66, 13);
            this.lbl_FavouriteRadius.TabIndex = 0;
            this.lbl_FavouriteRadius.Text = "Radius [m]:";
            //
            // nud_FavouriteRadius
            //
            this.nud_FavouriteRadius.Location = new System.Drawing.Point(140, 24);
            this.nud_FavouriteRadius.Maximum = new decimal(new int[] {
            20000000,
            0,
            0,
            0});
            this.nud_FavouriteRadius.Name = "nud_FavouriteRadius";
            this.nud_FavouriteRadius.Size = new System.Drawing.Size(100, 20);
            this.nud_FavouriteRadius.TabIndex = 1;
            this.nud_FavouriteRadius.ValueChanged += new System.EventHandler(this.any_Edit_Changed);
            //
            // ckb_FavouriteAutoAssign
            //
            this.ckb_FavouriteAutoAssign.AutoSize = true;
            this.ckb_FavouriteAutoAssign.Location = new System.Drawing.Point(256, 26);
            this.ckb_FavouriteAutoAssign.Name = "ckb_FavouriteAutoAssign";
            this.ckb_FavouriteAutoAssign.Size = new System.Drawing.Size(86, 17);
            this.ckb_FavouriteAutoAssign.TabIndex = 2;
            this.ckb_FavouriteAutoAssign.Text = "Auto Assign";
            this.ckb_FavouriteAutoAssign.UseVisualStyleBackColor = true;
            this.ckb_FavouriteAutoAssign.CheckedChanged += new System.EventHandler(this.ckb_FavouriteAutoAssign_CheckedChanged);
            //
            // ckb_FavouriteSnap
            //
            this.ckb_FavouriteSnap.AutoSize = true;
            this.ckb_FavouriteSnap.Location = new System.Drawing.Point(274, 50);
            this.ckb_FavouriteSnap.Name = "ckb_FavouriteSnap";
            this.ckb_FavouriteSnap.Size = new System.Drawing.Size(180, 17);
            this.ckb_FavouriteSnap.TabIndex = 3;
            this.ckb_FavouriteSnap.Text = "Snap Images to Favourite Position";
            this.ckb_FavouriteSnap.UseVisualStyleBackColor = true;
            this.ckb_FavouriteSnap.CheckedChanged += new System.EventHandler(this.any_Edit_Changed);
            //
            // lbl_GPSAltitude
            //
            this.lbl_GPSAltitude.AutoSize = true;
            this.lbl_GPSAltitude.Location = new System.Drawing.Point(10, 82);
            this.lbl_GPSAltitude.Name = "lbl_GPSAltitude";
            this.lbl_GPSAltitude.Size = new System.Drawing.Size(45, 13);
            this.lbl_GPSAltitude.TabIndex = 4;
            this.lbl_GPSAltitude.Text = "Altitude";
            //
            // tbx_FavouriteAltitude
            //
            this.tbx_FavouriteAltitude.Location = new System.Drawing.Point(140, 78);
            this.tbx_FavouriteAltitude.Name = "tbx_FavouriteAltitude";
            this.tbx_FavouriteAltitude.Size = new System.Drawing.Size(110, 20);
            this.tbx_FavouriteAltitude.TabIndex = 5;
            this.tbx_FavouriteAltitude.TextChanged += new System.EventHandler(this.any_Edit_Changed);
            //
            // lbl_FavouriteTimeZone
            //
            this.lbl_FavouriteTimeZone.AutoSize = true;
            this.lbl_FavouriteTimeZone.Location = new System.Drawing.Point(10, 110);
            this.lbl_FavouriteTimeZone.Name = "lbl_FavouriteTimeZone";
            this.lbl_FavouriteTimeZone.Size = new System.Drawing.Size(61, 13);
            this.lbl_FavouriteTimeZone.TabIndex = 6;
            this.lbl_FavouriteTimeZone.Text = "Time Zone:";
            //
            // cbx_FavouriteTimeZone
            //
            this.cbx_FavouriteTimeZone.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cbx_FavouriteTimeZone.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbx_FavouriteTimeZone.FormattingEnabled = true;
            this.cbx_FavouriteTimeZone.Location = new System.Drawing.Point(140, 106);
            this.cbx_FavouriteTimeZone.Name = "cbx_FavouriteTimeZone";
            this.cbx_FavouriteTimeZone.Size = new System.Drawing.Size(326, 21);
            this.cbx_FavouriteTimeZone.TabIndex = 7;
            this.cbx_FavouriteTimeZone.TextChanged += new System.EventHandler(this.any_Edit_Changed);
            //
            // lbl_CountryCode
            //
            this.lbl_CountryCode.AutoSize = true;
            this.lbl_CountryCode.Location = new System.Drawing.Point(10, 138);
            this.lbl_CountryCode.Name = "lbl_CountryCode";
            this.lbl_CountryCode.Size = new System.Drawing.Size(70, 13);
            this.lbl_CountryCode.TabIndex = 8;
            this.lbl_CountryCode.Text = "Country Code:";
            //
            // cbx_CountryCode
            //
            this.cbx_CountryCode.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cbx_CountryCode.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbx_CountryCode.FormattingEnabled = true;
            this.cbx_CountryCode.Location = new System.Drawing.Point(140, 134);
            this.cbx_CountryCode.Name = "cbx_CountryCode";
            this.cbx_CountryCode.Size = new System.Drawing.Size(110, 21);
            this.cbx_CountryCode.TabIndex = 9;
            this.cbx_CountryCode.SelectedIndexChanged += new System.EventHandler(this.cbx_CountryCode_SelectedIndexChanged);
            //
            // lbl_Country
            //
            this.lbl_Country.AutoSize = true;
            this.lbl_Country.Location = new System.Drawing.Point(10, 166);
            this.lbl_Country.Name = "lbl_Country";
            this.lbl_Country.Size = new System.Drawing.Size(46, 13);
            this.lbl_Country.TabIndex = 10;
            this.lbl_Country.Text = "Country:";
            //
            // cbx_Country
            //
            this.cbx_Country.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.cbx_Country.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbx_Country.FormattingEnabled = true;
            this.cbx_Country.Location = new System.Drawing.Point(140, 162);
            this.cbx_Country.Name = "cbx_Country";
            this.cbx_Country.Size = new System.Drawing.Size(326, 21);
            this.cbx_Country.TabIndex = 11;
            this.cbx_Country.SelectedIndexChanged += new System.EventHandler(this.cbx_Country_SelectedIndexChanged);
            //
            // lbl_State
            //
            this.lbl_State.AutoSize = true;
            this.lbl_State.Location = new System.Drawing.Point(10, 194);
            this.lbl_State.Name = "lbl_State";
            this.lbl_State.Size = new System.Drawing.Size(86, 13);
            this.lbl_State.TabIndex = 12;
            this.lbl_State.Text = "State/Province:";
            //
            // tbx_State
            //
            this.tbx_State.Location = new System.Drawing.Point(140, 190);
            this.tbx_State.Name = "tbx_State";
            this.tbx_State.Size = new System.Drawing.Size(326, 20);
            this.tbx_State.TabIndex = 13;
            this.tbx_State.TextChanged += new System.EventHandler(this.any_Edit_Changed);
            //
            // lbl_City
            //
            this.lbl_City.AutoSize = true;
            this.lbl_City.Location = new System.Drawing.Point(10, 222);
            this.lbl_City.Name = "lbl_City";
            this.lbl_City.Size = new System.Drawing.Size(27, 13);
            this.lbl_City.TabIndex = 14;
            this.lbl_City.Text = "City:";
            //
            // tbx_City
            //
            this.tbx_City.Location = new System.Drawing.Point(140, 218);
            this.tbx_City.Name = "tbx_City";
            this.tbx_City.Size = new System.Drawing.Size(326, 20);
            this.tbx_City.TabIndex = 15;
            this.tbx_City.TextChanged += new System.EventHandler(this.any_Edit_Changed);
            //
            // lbl_Sublocation
            //
            this.lbl_Sublocation.AutoSize = true;
            this.lbl_Sublocation.Location = new System.Drawing.Point(10, 250);
            this.lbl_Sublocation.Name = "lbl_Sublocation";
            this.lbl_Sublocation.Size = new System.Drawing.Size(65, 13);
            this.lbl_Sublocation.TabIndex = 16;
            this.lbl_Sublocation.Text = "Sublocation:";
            //
            // tbx_Sublocation
            //
            this.tbx_Sublocation.Location = new System.Drawing.Point(140, 246);
            this.tbx_Sublocation.Name = "tbx_Sublocation";
            this.tbx_Sublocation.Size = new System.Drawing.Size(326, 20);
            this.tbx_Sublocation.TabIndex = 17;
            this.tbx_Sublocation.TextChanged += new System.EventHandler(this.any_Edit_Changed);
            //
            // btn_FavouriteGetFromWeb
            //
            this.btn_FavouriteGetFromWeb.Location = new System.Drawing.Point(10, 278);
            this.btn_FavouriteGetFromWeb.Name = "btn_FavouriteGetFromWeb";
            this.btn_FavouriteGetFromWeb.Size = new System.Drawing.Size(456, 26);
            this.btn_FavouriteGetFromWeb.TabIndex = 18;
            this.btn_FavouriteGetFromWeb.Text = "Get from Web";
            this.btn_FavouriteGetFromWeb.UseVisualStyleBackColor = true;
            this.btn_FavouriteGetFromWeb.Click += new System.EventHandler(this.btn_FavouriteGetFromWeb_Click);
            //
            // lbl_FavouriteKeywords
            //
            this.lbl_FavouriteKeywords.AutoSize = true;
            this.lbl_FavouriteKeywords.Location = new System.Drawing.Point(10, 316);
            this.lbl_FavouriteKeywords.Name = "lbl_FavouriteKeywords";
            this.lbl_FavouriteKeywords.Size = new System.Drawing.Size(56, 13);
            this.lbl_FavouriteKeywords.TabIndex = 19;
            this.lbl_FavouriteKeywords.Text = "Keywords:";
            //
            // tbx_FavouriteKeyword
            //
            this.tbx_FavouriteKeyword.Location = new System.Drawing.Point(10, 334);
            this.tbx_FavouriteKeyword.Name = "tbx_FavouriteKeyword";
            this.tbx_FavouriteKeyword.Size = new System.Drawing.Size(290, 20);
            this.tbx_FavouriteKeyword.TabIndex = 20;
            this.tbx_FavouriteKeyword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbx_FavouriteKeyword_KeyDown);
            //
            // btn_FavouriteAddKeyword
            //
            this.btn_FavouriteAddKeyword.Location = new System.Drawing.Point(306, 332);
            this.btn_FavouriteAddKeyword.Name = "btn_FavouriteAddKeyword";
            this.btn_FavouriteAddKeyword.Size = new System.Drawing.Size(76, 24);
            this.btn_FavouriteAddKeyword.TabIndex = 21;
            this.btn_FavouriteAddKeyword.Text = "Add";
            this.btn_FavouriteAddKeyword.UseVisualStyleBackColor = true;
            this.btn_FavouriteAddKeyword.Click += new System.EventHandler(this.btn_FavouriteAddKeyword_Click);
            //
            // btn_FavouriteRemoveKeyword
            //
            this.btn_FavouriteRemoveKeyword.Location = new System.Drawing.Point(388, 332);
            this.btn_FavouriteRemoveKeyword.Name = "btn_FavouriteRemoveKeyword";
            this.btn_FavouriteRemoveKeyword.Size = new System.Drawing.Size(78, 24);
            this.btn_FavouriteRemoveKeyword.TabIndex = 22;
            this.btn_FavouriteRemoveKeyword.Text = "Remove";
            this.btn_FavouriteRemoveKeyword.UseVisualStyleBackColor = true;
            this.btn_FavouriteRemoveKeyword.Click += new System.EventHandler(this.btn_FavouriteRemoveKeyword_Click);
            //
            // lbx_FavouriteKeywords
            //
            this.lbx_FavouriteKeywords.FormattingEnabled = true;
            this.lbx_FavouriteKeywords.Location = new System.Drawing.Point(10, 362);
            this.lbx_FavouriteKeywords.Name = "lbx_FavouriteKeywords";
            this.lbx_FavouriteKeywords.Size = new System.Drawing.Size(456, 56);
            this.lbx_FavouriteKeywords.TabIndex = 23;
            this.lbx_FavouriteKeywords.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lbx_FavouriteKeywords_KeyDown);
            //
            // btn_FavouriteClearAll
            //
            this.btn_FavouriteClearAll.Location = new System.Drawing.Point(10, 430);
            this.btn_FavouriteClearAll.Name = "btn_FavouriteClearAll";
            this.btn_FavouriteClearAll.Size = new System.Drawing.Size(456, 26);
            this.btn_FavouriteClearAll.TabIndex = 24;
            this.btn_FavouriteClearAll.Text = "Clear All Values";
            this.btn_FavouriteClearAll.UseVisualStyleBackColor = true;
            this.btn_FavouriteClearAll.Click += new System.EventHandler(this.btn_FavouriteClearAll_Click);
            //
            // btn_FavouriteAccept
            //
            this.btn_FavouriteAccept.Location = new System.Drawing.Point(8, 560);
            this.btn_FavouriteAccept.Name = "btn_FavouriteAccept";
            this.btn_FavouriteAccept.Size = new System.Drawing.Size(236, 26);
            this.btn_FavouriteAccept.TabIndex = 5;
            this.btn_FavouriteAccept.Text = "Accept";
            this.btn_FavouriteAccept.UseVisualStyleBackColor = true;
            this.btn_FavouriteAccept.Click += new System.EventHandler(this.btn_FavouriteAccept_Click);
            //
            // btn_FavouriteDiscard
            //
            this.btn_FavouriteDiscard.Location = new System.Drawing.Point(248, 560);
            this.btn_FavouriteDiscard.Name = "btn_FavouriteDiscard";
            this.btn_FavouriteDiscard.Size = new System.Drawing.Size(236, 26);
            this.btn_FavouriteDiscard.TabIndex = 6;
            this.btn_FavouriteDiscard.Text = "Discard";
            this.btn_FavouriteDiscard.UseVisualStyleBackColor = true;
            this.btn_FavouriteDiscard.Click += new System.EventHandler(this.btn_FavouriteDiscard_Click);
            //
            // btn_OK
            //
            this.btn_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_OK.Location = new System.Drawing.Point(776, 614);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(86, 28);
            this.btn_OK.TabIndex = 2;
            this.btn_OK.Text = "OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            //
            // btn_Cancel
            //
            this.btn_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Cancel.Location = new System.Drawing.Point(870, 614);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(86, 28);
            this.btn_Cancel.TabIndex = 3;
            this.btn_Cancel.Text = "Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = true;
            //
            // FrmManageFavourites
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btn_Cancel;
            this.ClientSize = new System.Drawing.Size(968, 654);
            this.Controls.Add(this.btn_Cancel);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.gbx_FavouriteDetails);
            this.Controls.Add(this.gbx_AllFavourites);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(984, 693);
            this.Name = "FrmManageFavourites";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manage Favourites";
            this.Load += new System.EventHandler(this.FrmManageFavourites_Load);
            this.gbx_AllFavourites.ResumeLayout(false);
            this.tsp_Favourites.ResumeLayout(false);
            this.tsp_Favourites.PerformLayout();
            this.gbx_FavouriteDetails.ResumeLayout(false);
            this.gbx_FavouriteDetails.PerformLayout();
            this.gbx_FavouriteAdditionalInfo.ResumeLayout(false);
            this.gbx_FavouriteAdditionalInfo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_FavouriteRadius)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbx_AllFavourites;
        private System.Windows.Forms.ToolStrip tsp_Favourites;
        private System.Windows.Forms.ToolStripDropDownButton tsd_GroupBy;
        private System.Windows.Forms.ToolStripMenuItem tmi_GroupByCountryCode;
        private System.Windows.Forms.ToolStripMenuItem tmi_GroupByCountry;
        private System.Windows.Forms.ToolStripMenuItem tmi_GroupByState;
        private System.Windows.Forms.ToolStripMenuItem tmi_GroupByCity;
        private System.Windows.Forms.ToolStripMenuItem tmi_GroupByCoordinates;
        private System.Windows.Forms.ToolStripSeparator tss_GroupBy;
        private System.Windows.Forms.ToolStripMenuItem tmi_GroupByNone;
        private System.Windows.Forms.ToolStripButton tsb_Duplicate;
        private System.Windows.Forms.ToolStripButton tsb_Delete;
        private System.Windows.Forms.ToolStripButton tsb_AddFromSelection;
        private System.Windows.Forms.TreeView trv_Favourites;
        private System.Windows.Forms.Label lbl_FavouritesCount;
        private System.Windows.Forms.GroupBox gbx_FavouriteDetails;
        private System.Windows.Forms.Label lbl_FavouriteName;
        private System.Windows.Forms.TextBox tbx_FavouriteName;
        private System.Windows.Forms.Label lbl_FavouriteCoordinates;
        private System.Windows.Forms.TextBox tbx_FavouriteCoordinates;
        private System.Windows.Forms.GroupBox gbx_FavouriteAdditionalInfo;
        private System.Windows.Forms.Label lbl_FavouriteRadius;
        private System.Windows.Forms.NumericUpDown nud_FavouriteRadius;
        private System.Windows.Forms.CheckBox ckb_FavouriteAutoAssign;
        private System.Windows.Forms.CheckBox ckb_FavouriteSnap;
        private System.Windows.Forms.Label lbl_GPSAltitude;
        private System.Windows.Forms.TextBox tbx_FavouriteAltitude;
        private System.Windows.Forms.Label lbl_FavouriteTimeZone;
        private System.Windows.Forms.ComboBox cbx_FavouriteTimeZone;
        private System.Windows.Forms.Label lbl_CountryCode;
        private System.Windows.Forms.ComboBox cbx_CountryCode;
        private System.Windows.Forms.Label lbl_Country;
        private System.Windows.Forms.ComboBox cbx_Country;
        private System.Windows.Forms.Label lbl_State;
        private System.Windows.Forms.TextBox tbx_State;
        private System.Windows.Forms.Label lbl_City;
        private System.Windows.Forms.TextBox tbx_City;
        private System.Windows.Forms.Label lbl_Sublocation;
        private System.Windows.Forms.TextBox tbx_Sublocation;
        private System.Windows.Forms.Button btn_FavouriteGetFromWeb;
        private System.Windows.Forms.Label lbl_FavouriteKeywords;
        private System.Windows.Forms.TextBox tbx_FavouriteKeyword;
        private System.Windows.Forms.Button btn_FavouriteAddKeyword;
        private System.Windows.Forms.Button btn_FavouriteRemoveKeyword;
        private System.Windows.Forms.ListBox lbx_FavouriteKeywords;
        private System.Windows.Forms.Button btn_FavouriteClearAll;
        private System.Windows.Forms.Button btn_FavouriteAccept;
        private System.Windows.Forms.Button btn_FavouriteDiscard;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Cancel;
    }
}
