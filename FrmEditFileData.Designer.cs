using GeoTagNinja.Helpers;

namespace GeoTagNinja
{
    partial class FrmEditFileData
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmEditFileData));
            this.pbx_imagePreview = new System.Windows.Forms.PictureBox();
            this.tpg_Location = new System.Windows.Forms.TabPage();
            this.gbx_LocationData = new System.Windows.Forms.GroupBox();
            this.gbx_GetToponomy = new System.Windows.Forms.GroupBox();
            this.btn_getFromWeb_Toponomy = new System.Windows.Forms.Button();
            this.btn_getAllFromWeb_Toponomy = new System.Windows.Forms.Button();
            this.tbx_CreateDate = new System.Windows.Forms.TextBox();
            this.pbx_OffsetTimeInfo = new System.Windows.Forms.PictureBox();
            this.ckb_UseDST = new System.Windows.Forms.CheckBox();
            this.tbx_OffsetTime = new System.Windows.Forms.TextBox();
            this.cbx_OffsetTimeList = new System.Windows.Forms.ComboBox();
            this.lbl_OffsetTime = new System.Windows.Forms.Label();
            this.tbx_Sub_location = new System.Windows.Forms.TextBox();
            this.tbx_City = new System.Windows.Forms.TextBox();
            this.tbx_State = new System.Windows.Forms.TextBox();
            this.cbx_Country = new System.Windows.Forms.ComboBox();
            this.cbx_CountryCode = new System.Windows.Forms.ComboBox();
            this.lbl_Sub_location = new System.Windows.Forms.Label();
            this.lbl_City = new System.Windows.Forms.Label();
            this.lbl_State = new System.Windows.Forms.Label();
            this.lbl_Country = new System.Windows.Forms.Label();
            this.lbl_CountryCode = new System.Windows.Forms.Label();
            this.gbx_GPSData = new System.Windows.Forms.GroupBox();
            this.nud_GPSAltitude = new System.Windows.Forms.NumericUpDown();
            this.nud_GPSLongitude = new System.Windows.Forms.NumericUpDown();
            this.nud_GPSLatitude = new System.Windows.Forms.NumericUpDown();
            this.lbl_GPSAltitude = new System.Windows.Forms.Label();
            this.lbl_GPSLongitude = new System.Windows.Forms.Label();
            this.lbl_GPSLatitude = new System.Windows.Forms.Label();
            this.lbl_Decimal = new System.Windows.Forms.Label();
            this.btn_RemoveGeoData = new System.Windows.Forms.Button();
            this.tcr_EditData = new System.Windows.Forms.TabControl();
            this.tpg_DateTime = new System.Windows.Forms.TabPage();
            this.gbx_CreateDate = new System.Windows.Forms.GroupBox();
            this.btn_InsertFromTakenDate = new System.Windows.Forms.Button();
            this.lbl_CreateDateSecondsShift = new System.Windows.Forms.Label();
            this.lbl_CreateDateMinutesShift = new System.Windows.Forms.Label();
            this.lbl_CreateDateHoursShift = new System.Windows.Forms.Label();
            this.lbl_CreateDateDaysShift = new System.Windows.Forms.Label();
            this.nud_CreateDateSecondsShift = new System.Windows.Forms.NumericUpDown();
            this.nud_CreateDateMinutesShift = new System.Windows.Forms.NumericUpDown();
            this.nud_CreateDateHoursShift = new System.Windows.Forms.NumericUpDown();
            this.nud_CreateDateDaysShift = new System.Windows.Forms.NumericUpDown();
            this.dtp_CreateDate = new System.Windows.Forms.DateTimePicker();
            this.rbt_CreateDateTimeShift = new System.Windows.Forms.RadioButton();
            this.rbt_CreateDateSetToFixedDate = new System.Windows.Forms.RadioButton();
            this.btn_InsertCreateDate = new System.Windows.Forms.Button();
            this.gbx_TakenDate = new System.Windows.Forms.GroupBox();
            this.lbl_TakenDateSecondsShift = new System.Windows.Forms.Label();
            this.lbl_TakenDateMinutesShift = new System.Windows.Forms.Label();
            this.lbl_TakenDateHoursShift = new System.Windows.Forms.Label();
            this.lbl_TakenDateDaysShift = new System.Windows.Forms.Label();
            this.nud_TakenDateSecondsShift = new System.Windows.Forms.NumericUpDown();
            this.nud_TakenDateMinutesShift = new System.Windows.Forms.NumericUpDown();
            this.nud_TakenDateHoursShift = new System.Windows.Forms.NumericUpDown();
            this.nud_TakenDateDaysShift = new System.Windows.Forms.NumericUpDown();
            this.dtp_TakenDate = new System.Windows.Forms.DateTimePicker();
            this.rbt_TakenDateTimeShift = new System.Windows.Forms.RadioButton();
            this.rbt_TakenDateSetToFixedDate = new System.Windows.Forms.RadioButton();
            this.btn_InsertTakenDate = new System.Windows.Forms.Button();
            this.igl_TabPages = new System.Windows.Forms.ImageList(this.components);
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_SetCurrentValues = new System.Windows.Forms.Button();
            this.ttp_OffsetTime = new System.Windows.Forms.ToolTip(this.components);
            this.gbx_EditImageList = new System.Windows.Forms.GroupBox();
            this.lvw_FileListEditImages = new System.Windows.Forms.ListView();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_imagePreview)).BeginInit();
            this.tpg_Location.SuspendLayout();
            this.gbx_LocationData.SuspendLayout();
            this.gbx_GetToponomy.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_OffsetTimeInfo)).BeginInit();
            this.gbx_GPSData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_GPSAltitude)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_GPSLongitude)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_GPSLatitude)).BeginInit();
            this.tcr_EditData.SuspendLayout();
            this.tpg_DateTime.SuspendLayout();
            this.gbx_CreateDate.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_CreateDateSecondsShift)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_CreateDateMinutesShift)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_CreateDateHoursShift)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_CreateDateDaysShift)).BeginInit();
            this.gbx_TakenDate.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TakenDateSecondsShift)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TakenDateMinutesShift)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TakenDateHoursShift)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TakenDateDaysShift)).BeginInit();
            this.gbx_EditImageList.SuspendLayout();
            this.SuspendLayout();
            // 
            // pbx_imagePreview
            // 
            this.pbx_imagePreview.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pbx_imagePreview.Location = new System.Drawing.Point(12, 338);
            this.pbx_imagePreview.Name = "pbx_imagePreview";
            this.pbx_imagePreview.Size = new System.Drawing.Size(438, 219);
            this.pbx_imagePreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbx_imagePreview.TabIndex = 2;
            this.pbx_imagePreview.TabStop = false;
            // 
            // tpg_Location
            // 
            this.tpg_Location.Controls.Add(this.gbx_LocationData);
            this.tpg_Location.Controls.Add(this.gbx_GPSData);
            this.tpg_Location.Controls.Add(this.btn_RemoveGeoData);
            this.tpg_Location.ImageKey = "PublishOnDemand.png";
            this.tpg_Location.Location = new System.Drawing.Point(4, 23);
            this.tpg_Location.Margin = new System.Windows.Forms.Padding(2);
            this.tpg_Location.Name = "tpg_Location";
            this.tpg_Location.Padding = new System.Windows.Forms.Padding(2);
            this.tpg_Location.Size = new System.Drawing.Size(557, 491);
            this.tpg_Location.TabIndex = 0;
            this.tpg_Location.Text = "tpg_Location";
            this.tpg_Location.UseVisualStyleBackColor = true;
            // 
            // gbx_LocationData
            // 
            this.gbx_LocationData.Controls.Add(this.gbx_GetToponomy);
            this.gbx_LocationData.Controls.Add(this.tbx_CreateDate);
            this.gbx_LocationData.Controls.Add(this.pbx_OffsetTimeInfo);
            this.gbx_LocationData.Controls.Add(this.ckb_UseDST);
            this.gbx_LocationData.Controls.Add(this.tbx_OffsetTime);
            this.gbx_LocationData.Controls.Add(this.cbx_OffsetTimeList);
            this.gbx_LocationData.Controls.Add(this.lbl_OffsetTime);
            this.gbx_LocationData.Controls.Add(this.tbx_Sub_location);
            this.gbx_LocationData.Controls.Add(this.tbx_City);
            this.gbx_LocationData.Controls.Add(this.tbx_State);
            this.gbx_LocationData.Controls.Add(this.cbx_Country);
            this.gbx_LocationData.Controls.Add(this.cbx_CountryCode);
            this.gbx_LocationData.Controls.Add(this.lbl_Sub_location);
            this.gbx_LocationData.Controls.Add(this.lbl_City);
            this.gbx_LocationData.Controls.Add(this.lbl_State);
            this.gbx_LocationData.Controls.Add(this.lbl_Country);
            this.gbx_LocationData.Controls.Add(this.lbl_CountryCode);
            this.gbx_LocationData.Location = new System.Drawing.Point(13, 189);
            this.gbx_LocationData.Margin = new System.Windows.Forms.Padding(2);
            this.gbx_LocationData.Name = "gbx_LocationData";
            this.gbx_LocationData.Padding = new System.Windows.Forms.Padding(2);
            this.gbx_LocationData.Size = new System.Drawing.Size(518, 257);
            this.gbx_LocationData.TabIndex = 1;
            this.gbx_LocationData.TabStop = false;
            this.gbx_LocationData.Text = "gbx_LocationData";
            // 
            // gbx_GetToponomy
            // 
            this.gbx_GetToponomy.Controls.Add(this.btn_getFromWeb_Toponomy);
            this.gbx_GetToponomy.Controls.Add(this.btn_getAllFromWeb_Toponomy);
            this.gbx_GetToponomy.Location = new System.Drawing.Point(313, 60);
            this.gbx_GetToponomy.Name = "gbx_GetToponomy";
            this.gbx_GetToponomy.Size = new System.Drawing.Size(200, 100);
            this.gbx_GetToponomy.TabIndex = 20;
            this.gbx_GetToponomy.TabStop = false;
            this.gbx_GetToponomy.Text = "gbx_GetToponomy";
            // 
            // btn_getFromWeb_Toponomy
            // 
            this.btn_getFromWeb_Toponomy.Location = new System.Drawing.Point(22, 24);
            this.btn_getFromWeb_Toponomy.Name = "btn_getFromWeb_Toponomy";
            this.btn_getFromWeb_Toponomy.Size = new System.Drawing.Size(160, 23);
            this.btn_getFromWeb_Toponomy.TabIndex = 11;
            this.btn_getFromWeb_Toponomy.Text = "btn_getFromWeb_Toponomy";
            this.btn_getFromWeb_Toponomy.UseVisualStyleBackColor = true;
            this.btn_getFromWeb_Toponomy.Click += new System.EventHandler(this.btn_getFromWeb_Click);
            // 
            // btn_getAllFromWeb_Toponomy
            // 
            this.btn_getAllFromWeb_Toponomy.Location = new System.Drawing.Point(22, 62);
            this.btn_getAllFromWeb_Toponomy.Name = "btn_getAllFromWeb_Toponomy";
            this.btn_getAllFromWeb_Toponomy.Size = new System.Drawing.Size(160, 23);
            this.btn_getAllFromWeb_Toponomy.TabIndex = 12;
            this.btn_getAllFromWeb_Toponomy.Text = "btn_getAllFromWeb_Toponomy";
            this.btn_getAllFromWeb_Toponomy.UseVisualStyleBackColor = true;
            this.btn_getAllFromWeb_Toponomy.Click += new System.EventHandler(this.btn_getFromWeb_Click);
            // 
            // tbx_CreateDate
            // 
            this.tbx_CreateDate.Enabled = false;
            this.tbx_CreateDate.Location = new System.Drawing.Point(298, 210);
            this.tbx_CreateDate.Name = "tbx_CreateDate";
            this.tbx_CreateDate.ReadOnly = true;
            this.tbx_CreateDate.Size = new System.Drawing.Size(160, 20);
            this.tbx_CreateDate.TabIndex = 19;
            this.tbx_CreateDate.Visible = false;
            // 
            // pbx_OffsetTimeInfo
            // 
            this.pbx_OffsetTimeInfo.Image = ((System.Drawing.Image)(resources.GetObject("pbx_OffsetTimeInfo.Image")));
            this.pbx_OffsetTimeInfo.Location = new System.Drawing.Point(124, 180);
            this.pbx_OffsetTimeInfo.Name = "pbx_OffsetTimeInfo";
            this.pbx_OffsetTimeInfo.Size = new System.Drawing.Size(16, 16);
            this.pbx_OffsetTimeInfo.TabIndex = 18;
            this.pbx_OffsetTimeInfo.TabStop = false;
            this.pbx_OffsetTimeInfo.MouseHover += new System.EventHandler(this.pbx_OffsetTimeInfo_MouseHover);
            // 
            // ckb_UseDST
            // 
            this.ckb_UseDST.AutoSize = true;
            this.ckb_UseDST.Location = new System.Drawing.Point(14, 210);
            this.ckb_UseDST.Name = "ckb_UseDST";
            this.ckb_UseDST.Size = new System.Drawing.Size(91, 17);
            this.ckb_UseDST.TabIndex = 17;
            this.ckb_UseDST.Text = "ckb_UseDST";
            this.ckb_UseDST.UseVisualStyleBackColor = true;
            this.ckb_UseDST.CheckedChanged += new System.EventHandler(this.ckb_UseDST_CheckedChanged);
            // 
            // tbx_OffsetTime
            // 
            this.tbx_OffsetTime.Location = new System.Drawing.Point(163, 210);
            this.tbx_OffsetTime.Name = "tbx_OffsetTime";
            this.tbx_OffsetTime.ReadOnly = true;
            this.tbx_OffsetTime.Size = new System.Drawing.Size(79, 20);
            this.tbx_OffsetTime.TabIndex = 16;
            this.tbx_OffsetTime.TextChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            // 
            // cbx_OffsetTimeList
            // 
            this.cbx_OffsetTimeList.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cbx_OffsetTimeList.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbx_OffsetTimeList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_OffsetTimeList.FormattingEnabled = true;
            this.cbx_OffsetTimeList.Location = new System.Drawing.Point(163, 177);
            this.cbx_OffsetTimeList.Name = "cbx_OffsetTimeList";
            this.cbx_OffsetTimeList.Size = new System.Drawing.Size(342, 21);
            this.cbx_OffsetTimeList.Sorted = true;
            this.cbx_OffsetTimeList.TabIndex = 15;
            this.cbx_OffsetTimeList.SelectedIndexChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            // 
            // lbl_OffsetTime
            // 
            this.lbl_OffsetTime.AutoSize = true;
            this.lbl_OffsetTime.Location = new System.Drawing.Point(11, 180);
            this.lbl_OffsetTime.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_OffsetTime.Name = "lbl_OffsetTime";
            this.lbl_OffsetTime.Size = new System.Drawing.Size(74, 13);
            this.lbl_OffsetTime.TabIndex = 14;
            this.lbl_OffsetTime.Text = "lbl_OffsetTime";
            // 
            // tbx_Sub_location
            // 
            this.tbx_Sub_location.Location = new System.Drawing.Point(124, 147);
            this.tbx_Sub_location.Name = "tbx_Sub_location";
            this.tbx_Sub_location.Size = new System.Drawing.Size(176, 20);
            this.tbx_Sub_location.TabIndex = 10;
            this.tbx_Sub_location.TextChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            // 
            // tbx_City
            // 
            this.tbx_City.Location = new System.Drawing.Point(124, 113);
            this.tbx_City.Name = "tbx_City";
            this.tbx_City.Size = new System.Drawing.Size(176, 20);
            this.tbx_City.TabIndex = 9;
            this.tbx_City.TextChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            // 
            // tbx_State
            // 
            this.tbx_State.Location = new System.Drawing.Point(124, 87);
            this.tbx_State.Name = "tbx_State";
            this.tbx_State.Size = new System.Drawing.Size(176, 20);
            this.tbx_State.TabIndex = 8;
            this.tbx_State.TextChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            // 
            // cbx_Country
            // 
            this.cbx_Country.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cbx_Country.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbx_Country.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_Country.FormattingEnabled = true;
            this.cbx_Country.Location = new System.Drawing.Point(124, 57);
            this.cbx_Country.Name = "cbx_Country";
            this.cbx_Country.Size = new System.Drawing.Size(176, 21);
            this.cbx_Country.Sorted = true;
            this.cbx_Country.TabIndex = 7;
            this.cbx_Country.SelectedValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            // 
            // cbx_CountryCode
            // 
            this.cbx_CountryCode.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cbx_CountryCode.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbx_CountryCode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_CountryCode.FormattingEnabled = true;
            this.cbx_CountryCode.Location = new System.Drawing.Point(124, 27);
            this.cbx_CountryCode.Name = "cbx_CountryCode";
            this.cbx_CountryCode.Size = new System.Drawing.Size(68, 21);
            this.cbx_CountryCode.Sorted = true;
            this.cbx_CountryCode.TabIndex = 6;
            this.cbx_CountryCode.SelectedValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            // 
            // lbl_Sub_location
            // 
            this.lbl_Sub_location.AutoSize = true;
            this.lbl_Sub_location.Location = new System.Drawing.Point(11, 150);
            this.lbl_Sub_location.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Sub_location.Name = "lbl_Sub_location";
            this.lbl_Sub_location.Size = new System.Drawing.Size(85, 13);
            this.lbl_Sub_location.TabIndex = 0;
            this.lbl_Sub_location.Text = "lbl_Sub_location";
            // 
            // lbl_City
            // 
            this.lbl_City.AutoSize = true;
            this.lbl_City.Location = new System.Drawing.Point(11, 120);
            this.lbl_City.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_City.Name = "lbl_City";
            this.lbl_City.Size = new System.Drawing.Size(40, 13);
            this.lbl_City.TabIndex = 0;
            this.lbl_City.Text = "lbl_City";
            // 
            // lbl_State
            // 
            this.lbl_State.AutoSize = true;
            this.lbl_State.Location = new System.Drawing.Point(11, 90);
            this.lbl_State.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_State.Name = "lbl_State";
            this.lbl_State.Size = new System.Drawing.Size(48, 13);
            this.lbl_State.TabIndex = 0;
            this.lbl_State.Text = "lbl_State";
            // 
            // lbl_Country
            // 
            this.lbl_Country.AutoSize = true;
            this.lbl_Country.Location = new System.Drawing.Point(12, 60);
            this.lbl_Country.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Country.Name = "lbl_Country";
            this.lbl_Country.Size = new System.Drawing.Size(59, 13);
            this.lbl_Country.TabIndex = 0;
            this.lbl_Country.Text = "lbl_Country";
            // 
            // lbl_CountryCode
            // 
            this.lbl_CountryCode.AutoSize = true;
            this.lbl_CountryCode.Location = new System.Drawing.Point(11, 30);
            this.lbl_CountryCode.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_CountryCode.Name = "lbl_CountryCode";
            this.lbl_CountryCode.Size = new System.Drawing.Size(84, 13);
            this.lbl_CountryCode.TabIndex = 0;
            this.lbl_CountryCode.Text = "lbl_CountryCode";
            // 
            // gbx_GPSData
            // 
            this.gbx_GPSData.Controls.Add(this.nud_GPSAltitude);
            this.gbx_GPSData.Controls.Add(this.nud_GPSLongitude);
            this.gbx_GPSData.Controls.Add(this.nud_GPSLatitude);
            this.gbx_GPSData.Controls.Add(this.lbl_GPSAltitude);
            this.gbx_GPSData.Controls.Add(this.lbl_GPSLongitude);
            this.gbx_GPSData.Controls.Add(this.lbl_GPSLatitude);
            this.gbx_GPSData.Controls.Add(this.lbl_Decimal);
            this.gbx_GPSData.Location = new System.Drawing.Point(13, 17);
            this.gbx_GPSData.Margin = new System.Windows.Forms.Padding(2);
            this.gbx_GPSData.Name = "gbx_GPSData";
            this.gbx_GPSData.Padding = new System.Windows.Forms.Padding(2);
            this.gbx_GPSData.Size = new System.Drawing.Size(518, 157);
            this.gbx_GPSData.TabIndex = 0;
            this.gbx_GPSData.TabStop = false;
            this.gbx_GPSData.Text = "gbx_GPSData";
            // 
            // nud_GPSAltitude
            // 
            this.nud_GPSAltitude.DecimalPlaces = 2;
            this.nud_GPSAltitude.Location = new System.Drawing.Point(124, 117);
            this.nud_GPSAltitude.Maximum = new decimal(new int[] {
            65536,
            0,
            0,
            0});
            this.nud_GPSAltitude.Name = "nud_GPSAltitude";
            this.nud_GPSAltitude.Size = new System.Drawing.Size(120, 20);
            this.nud_GPSAltitude.TabIndex = 15;
            this.nud_GPSAltitude.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.nud_GPSAltitude.Enter += new System.EventHandler(this.any_nud_Enter);
            this.nud_GPSAltitude.Leave += new System.EventHandler(this.any_nud_Leave);
            // 
            // nud_GPSLongitude
            // 
            this.nud_GPSLongitude.DecimalPlaces = 6;
            this.nud_GPSLongitude.Location = new System.Drawing.Point(124, 65);
            this.nud_GPSLongitude.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
            this.nud_GPSLongitude.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
            this.nud_GPSLongitude.Name = "nud_GPSLongitude";
            this.nud_GPSLongitude.Size = new System.Drawing.Size(120, 20);
            this.nud_GPSLongitude.TabIndex = 14;
            this.nud_GPSLongitude.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.nud_GPSLongitude.Enter += new System.EventHandler(this.any_nud_Enter);
            this.nud_GPSLongitude.Leave += new System.EventHandler(this.any_nud_Leave);
            // 
            // nud_GPSLatitude
            // 
            this.nud_GPSLatitude.DecimalPlaces = 6;
            this.nud_GPSLatitude.Location = new System.Drawing.Point(124, 36);
            this.nud_GPSLatitude.Maximum = new decimal(new int[] {
            180,
            0,
            0,
            0});
            this.nud_GPSLatitude.Minimum = new decimal(new int[] {
            180,
            0,
            0,
            -2147483648});
            this.nud_GPSLatitude.Name = "nud_GPSLatitude";
            this.nud_GPSLatitude.Size = new System.Drawing.Size(120, 20);
            this.nud_GPSLatitude.TabIndex = 13;
            this.nud_GPSLatitude.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.nud_GPSLatitude.Enter += new System.EventHandler(this.any_nud_Enter);
            this.nud_GPSLatitude.Leave += new System.EventHandler(this.any_nud_Leave);
            // 
            // lbl_GPSAltitude
            // 
            this.lbl_GPSAltitude.AutoSize = true;
            this.lbl_GPSAltitude.Location = new System.Drawing.Point(14, 117);
            this.lbl_GPSAltitude.Name = "lbl_GPSAltitude";
            this.lbl_GPSAltitude.Size = new System.Drawing.Size(80, 13);
            this.lbl_GPSAltitude.TabIndex = 12;
            this.lbl_GPSAltitude.Text = "lbl_GPSAltitude";
            // 
            // lbl_GPSLongitude
            // 
            this.lbl_GPSLongitude.AutoSize = true;
            this.lbl_GPSLongitude.Location = new System.Drawing.Point(14, 66);
            this.lbl_GPSLongitude.Name = "lbl_GPSLongitude";
            this.lbl_GPSLongitude.Size = new System.Drawing.Size(92, 13);
            this.lbl_GPSLongitude.TabIndex = 12;
            this.lbl_GPSLongitude.Text = "lbl_GPSLongitude";
            // 
            // lbl_GPSLatitude
            // 
            this.lbl_GPSLatitude.AutoSize = true;
            this.lbl_GPSLatitude.Location = new System.Drawing.Point(14, 38);
            this.lbl_GPSLatitude.Name = "lbl_GPSLatitude";
            this.lbl_GPSLatitude.Size = new System.Drawing.Size(83, 13);
            this.lbl_GPSLatitude.TabIndex = 12;
            this.lbl_GPSLatitude.Text = "lbl_GPSLatitude";
            // 
            // lbl_Decimal
            // 
            this.lbl_Decimal.AutoSize = true;
            this.lbl_Decimal.Location = new System.Drawing.Point(130, 16);
            this.lbl_Decimal.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Decimal.Name = "lbl_Decimal";
            this.lbl_Decimal.Size = new System.Drawing.Size(61, 13);
            this.lbl_Decimal.TabIndex = 1;
            this.lbl_Decimal.Text = "lbl_Decimal";
            // 
            // btn_RemoveGeoData
            // 
            this.btn_RemoveGeoData.Location = new System.Drawing.Point(146, 455);
            this.btn_RemoveGeoData.Name = "btn_RemoveGeoData";
            this.btn_RemoveGeoData.Size = new System.Drawing.Size(325, 23);
            this.btn_RemoveGeoData.TabIndex = 13;
            this.btn_RemoveGeoData.Text = "btn_RemoveGeoData";
            this.btn_RemoveGeoData.UseVisualStyleBackColor = true;
            this.btn_RemoveGeoData.Click += new System.EventHandler(this.btn_RemoveGeoData_Click);
            // 
            // tcr_EditData
            // 
            this.tcr_EditData.Controls.Add(this.tpg_Location);
            this.tcr_EditData.Controls.Add(this.tpg_DateTime);
            this.tcr_EditData.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tcr_EditData.ImageList = this.igl_TabPages;
            this.tcr_EditData.Location = new System.Drawing.Point(455, 11);
            this.tcr_EditData.Margin = new System.Windows.Forms.Padding(2);
            this.tcr_EditData.Name = "tcr_EditData";
            this.tcr_EditData.SelectedIndex = 0;
            this.tcr_EditData.Size = new System.Drawing.Size(565, 518);
            this.tcr_EditData.TabIndex = 0;
            
            // 
            // tpg_DateTime
            // 
            this.tpg_DateTime.Controls.Add(this.gbx_CreateDate);
            this.tpg_DateTime.Controls.Add(this.gbx_TakenDate);
            this.tpg_DateTime.ImageKey = "DateTimeAxis.png";
            this.tpg_DateTime.Location = new System.Drawing.Point(4, 23);
            this.tpg_DateTime.Name = "tpg_DateTime";
            this.tpg_DateTime.Size = new System.Drawing.Size(557, 491);
            this.tpg_DateTime.TabIndex = 1;
            this.tpg_DateTime.Text = "tpg_DateTime";
            this.tpg_DateTime.UseVisualStyleBackColor = true;
            // 
            // gbx_CreateDate
            // 
            this.gbx_CreateDate.Controls.Add(this.btn_InsertFromTakenDate);
            this.gbx_CreateDate.Controls.Add(this.lbl_CreateDateSecondsShift);
            this.gbx_CreateDate.Controls.Add(this.lbl_CreateDateMinutesShift);
            this.gbx_CreateDate.Controls.Add(this.lbl_CreateDateHoursShift);
            this.gbx_CreateDate.Controls.Add(this.lbl_CreateDateDaysShift);
            this.gbx_CreateDate.Controls.Add(this.nud_CreateDateSecondsShift);
            this.gbx_CreateDate.Controls.Add(this.nud_CreateDateMinutesShift);
            this.gbx_CreateDate.Controls.Add(this.nud_CreateDateHoursShift);
            this.gbx_CreateDate.Controls.Add(this.nud_CreateDateDaysShift);
            this.gbx_CreateDate.Controls.Add(this.dtp_CreateDate);
            this.gbx_CreateDate.Controls.Add(this.rbt_CreateDateTimeShift);
            this.gbx_CreateDate.Controls.Add(this.rbt_CreateDateSetToFixedDate);
            this.gbx_CreateDate.Controls.Add(this.btn_InsertCreateDate);
            this.gbx_CreateDate.Location = new System.Drawing.Point(17, 234);
            this.gbx_CreateDate.Margin = new System.Windows.Forms.Padding(2);
            this.gbx_CreateDate.Name = "gbx_CreateDate";
            this.gbx_CreateDate.Padding = new System.Windows.Forms.Padding(2);
            this.gbx_CreateDate.Size = new System.Drawing.Size(501, 198);
            this.gbx_CreateDate.TabIndex = 14;
            this.gbx_CreateDate.TabStop = false;
            this.gbx_CreateDate.Text = "gbx_CreateDate";
            // 
            // btn_InsertFromTakenDate
            // 
            this.btn_InsertFromTakenDate.Location = new System.Drawing.Point(204, 60);
            this.btn_InsertFromTakenDate.Name = "btn_InsertFromTakenDate";
            this.btn_InsertFromTakenDate.Size = new System.Drawing.Size(145, 23);
            this.btn_InsertFromTakenDate.TabIndex = 38;
            this.btn_InsertFromTakenDate.Text = "btn_InsertFromTakenDate";
            this.btn_InsertFromTakenDate.UseVisualStyleBackColor = true;
            this.btn_InsertFromTakenDate.Click += new System.EventHandler(this.btn_InsertFromTakenDate_Click);
            // 
            // lbl_CreateDateSecondsShift
            // 
            this.lbl_CreateDateSecondsShift.AutoSize = true;
            this.lbl_CreateDateSecondsShift.Location = new System.Drawing.Point(269, 122);
            this.lbl_CreateDateSecondsShift.Name = "lbl_CreateDateSecondsShift";
            this.lbl_CreateDateSecondsShift.Size = new System.Drawing.Size(140, 13);
            this.lbl_CreateDateSecondsShift.TabIndex = 37;
            this.lbl_CreateDateSecondsShift.Text = "lbl_CreateDateSecondsShift";
            // 
            // lbl_CreateDateMinutesShift
            // 
            this.lbl_CreateDateMinutesShift.AutoSize = true;
            this.lbl_CreateDateMinutesShift.Location = new System.Drawing.Point(189, 122);
            this.lbl_CreateDateMinutesShift.Name = "lbl_CreateDateMinutesShift";
            this.lbl_CreateDateMinutesShift.Size = new System.Drawing.Size(135, 13);
            this.lbl_CreateDateMinutesShift.TabIndex = 36;
            this.lbl_CreateDateMinutesShift.Text = "lbl_CreateDateMinutesShift";
            // 
            // lbl_CreateDateHoursShift
            // 
            this.lbl_CreateDateHoursShift.AutoSize = true;
            this.lbl_CreateDateHoursShift.Location = new System.Drawing.Point(118, 122);
            this.lbl_CreateDateHoursShift.Name = "lbl_CreateDateHoursShift";
            this.lbl_CreateDateHoursShift.Size = new System.Drawing.Size(126, 13);
            this.lbl_CreateDateHoursShift.TabIndex = 35;
            this.lbl_CreateDateHoursShift.Text = "lbl_CreateDateHoursShift";
            // 
            // lbl_CreateDateDaysShift
            // 
            this.lbl_CreateDateDaysShift.AutoSize = true;
            this.lbl_CreateDateDaysShift.Location = new System.Drawing.Point(56, 122);
            this.lbl_CreateDateDaysShift.Name = "lbl_CreateDateDaysShift";
            this.lbl_CreateDateDaysShift.Size = new System.Drawing.Size(122, 13);
            this.lbl_CreateDateDaysShift.TabIndex = 34;
            this.lbl_CreateDateDaysShift.Text = "lbl_CreateDateDaysShift";
            // 
            // nud_CreateDateSecondsShift
            // 
            this.nud_CreateDateSecondsShift.Location = new System.Drawing.Point(282, 148);
            this.nud_CreateDateSecondsShift.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
            this.nud_CreateDateSecondsShift.Minimum = new decimal(new int[] {
            59,
            0,
            0,
            -2147483648});
            this.nud_CreateDateSecondsShift.Name = "nud_CreateDateSecondsShift";
            this.nud_CreateDateSecondsShift.Size = new System.Drawing.Size(36, 20);
            this.nud_CreateDateSecondsShift.TabIndex = 33;
            this.nud_CreateDateSecondsShift.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.nud_CreateDateSecondsShift.Enter += new System.EventHandler(this.any_nud_Enter);
            this.nud_CreateDateSecondsShift.Leave += new System.EventHandler(this.any_nud_Leave);
            // 
            // nud_CreateDateMinutesShift
            // 
            this.nud_CreateDateMinutesShift.Location = new System.Drawing.Point(202, 148);
            this.nud_CreateDateMinutesShift.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
            this.nud_CreateDateMinutesShift.Minimum = new decimal(new int[] {
            59,
            0,
            0,
            -2147483648});
            this.nud_CreateDateMinutesShift.Name = "nud_CreateDateMinutesShift";
            this.nud_CreateDateMinutesShift.Size = new System.Drawing.Size(36, 20);
            this.nud_CreateDateMinutesShift.TabIndex = 32;
            this.nud_CreateDateMinutesShift.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.nud_CreateDateMinutesShift.Enter += new System.EventHandler(this.any_nud_Enter);
            this.nud_CreateDateMinutesShift.Leave += new System.EventHandler(this.any_nud_Leave);
            // 
            // nud_CreateDateHoursShift
            // 
            this.nud_CreateDateHoursShift.Location = new System.Drawing.Point(131, 148);
            this.nud_CreateDateHoursShift.Maximum = new decimal(new int[] {
            23,
            0,
            0,
            0});
            this.nud_CreateDateHoursShift.Minimum = new decimal(new int[] {
            23,
            0,
            0,
            -2147483648});
            this.nud_CreateDateHoursShift.Name = "nud_CreateDateHoursShift";
            this.nud_CreateDateHoursShift.Size = new System.Drawing.Size(36, 20);
            this.nud_CreateDateHoursShift.TabIndex = 31;
            this.nud_CreateDateHoursShift.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.nud_CreateDateHoursShift.Enter += new System.EventHandler(this.any_nud_Enter);
            this.nud_CreateDateHoursShift.Leave += new System.EventHandler(this.any_nud_Leave);
            // 
            // nud_CreateDateDaysShift
            // 
            this.nud_CreateDateDaysShift.Location = new System.Drawing.Point(66, 148);
            this.nud_CreateDateDaysShift.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.nud_CreateDateDaysShift.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            -2147483648});
            this.nud_CreateDateDaysShift.Name = "nud_CreateDateDaysShift";
            this.nud_CreateDateDaysShift.Size = new System.Drawing.Size(36, 20);
            this.nud_CreateDateDaysShift.TabIndex = 30;
            this.nud_CreateDateDaysShift.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.nud_CreateDateDaysShift.Enter += new System.EventHandler(this.any_nud_Enter);
            this.nud_CreateDateDaysShift.Leave += new System.EventHandler(this.any_nud_Leave);
            // 
            // dtp_CreateDate
            // 
            this.dtp_CreateDate.CustomFormat = "";
            this.dtp_CreateDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtp_CreateDate.Location = new System.Drawing.Point(25, 59);
            this.dtp_CreateDate.MaxDate = new System.DateTime(2069, 12, 31, 0, 0, 0, 0);
            this.dtp_CreateDate.MinDate = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
            this.dtp_CreateDate.Name = "dtp_CreateDate";
            this.dtp_CreateDate.Size = new System.Drawing.Size(164, 20);
            this.dtp_CreateDate.TabIndex = 29;
            this.dtp_CreateDate.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.dtp_CreateDate.Enter += new System.EventHandler(this.dtp_CreateDate_Enter);
            this.dtp_CreateDate.Leave += new System.EventHandler(this.dtp_CreateDate_Leave);
            // 
            // rbt_CreateDateTimeShift
            // 
            this.rbt_CreateDateTimeShift.AutoSize = true;
            this.rbt_CreateDateTimeShift.Location = new System.Drawing.Point(15, 91);
            this.rbt_CreateDateTimeShift.Name = "rbt_CreateDateTimeShift";
            this.rbt_CreateDateTimeShift.Size = new System.Drawing.Size(141, 17);
            this.rbt_CreateDateTimeShift.TabIndex = 28;
            this.rbt_CreateDateTimeShift.Text = "rbt_CreateDateTimeShift";
            this.rbt_CreateDateTimeShift.UseVisualStyleBackColor = true;
            this.rbt_CreateDateTimeShift.CheckedChanged += new System.EventHandler(this.rbt_CreateDateTimeShift_CheckedChanged);
            // 
            // rbt_CreateDateSetToFixedDate
            // 
            this.rbt_CreateDateSetToFixedDate.AutoSize = true;
            this.rbt_CreateDateSetToFixedDate.Checked = true;
            this.rbt_CreateDateSetToFixedDate.Location = new System.Drawing.Point(15, 36);
            this.rbt_CreateDateSetToFixedDate.Name = "rbt_CreateDateSetToFixedDate";
            this.rbt_CreateDateSetToFixedDate.Size = new System.Drawing.Size(174, 17);
            this.rbt_CreateDateSetToFixedDate.TabIndex = 27;
            this.rbt_CreateDateSetToFixedDate.TabStop = true;
            this.rbt_CreateDateSetToFixedDate.Text = "rbt_CreateDateSetToFixedDate";
            this.rbt_CreateDateSetToFixedDate.UseVisualStyleBackColor = true;
            this.rbt_CreateDateSetToFixedDate.CheckedChanged += new System.EventHandler(this.rbt_CreateDateSetToFixedDate_CheckedChanged);
            // 
            // btn_InsertCreateDate
            // 
            this.btn_InsertCreateDate.Location = new System.Drawing.Point(237, 30);
            this.btn_InsertCreateDate.Name = "btn_InsertCreateDate";
            this.btn_InsertCreateDate.Size = new System.Drawing.Size(112, 23);
            this.btn_InsertCreateDate.TabIndex = 26;
            this.btn_InsertCreateDate.Text = "btn_InsertCreateDate";
            this.btn_InsertCreateDate.UseVisualStyleBackColor = true;
            this.btn_InsertCreateDate.Click += new System.EventHandler(this.btn_InsertCreateDate_Click);
            // 
            // gbx_TakenDate
            // 
            this.gbx_TakenDate.Controls.Add(this.lbl_TakenDateSecondsShift);
            this.gbx_TakenDate.Controls.Add(this.lbl_TakenDateMinutesShift);
            this.gbx_TakenDate.Controls.Add(this.lbl_TakenDateHoursShift);
            this.gbx_TakenDate.Controls.Add(this.lbl_TakenDateDaysShift);
            this.gbx_TakenDate.Controls.Add(this.nud_TakenDateSecondsShift);
            this.gbx_TakenDate.Controls.Add(this.nud_TakenDateMinutesShift);
            this.gbx_TakenDate.Controls.Add(this.nud_TakenDateHoursShift);
            this.gbx_TakenDate.Controls.Add(this.nud_TakenDateDaysShift);
            this.gbx_TakenDate.Controls.Add(this.dtp_TakenDate);
            this.gbx_TakenDate.Controls.Add(this.rbt_TakenDateTimeShift);
            this.gbx_TakenDate.Controls.Add(this.rbt_TakenDateSetToFixedDate);
            this.gbx_TakenDate.Controls.Add(this.btn_InsertTakenDate);
            this.gbx_TakenDate.Location = new System.Drawing.Point(17, 22);
            this.gbx_TakenDate.Margin = new System.Windows.Forms.Padding(2);
            this.gbx_TakenDate.Name = "gbx_TakenDate";
            this.gbx_TakenDate.Padding = new System.Windows.Forms.Padding(2);
            this.gbx_TakenDate.Size = new System.Drawing.Size(501, 185);
            this.gbx_TakenDate.TabIndex = 1;
            this.gbx_TakenDate.TabStop = false;
            this.gbx_TakenDate.Text = "gbx_TakenDate";
            // 
            // lbl_TakenDateSecondsShift
            // 
            this.lbl_TakenDateSecondsShift.AutoSize = true;
            this.lbl_TakenDateSecondsShift.Location = new System.Drawing.Point(271, 111);
            this.lbl_TakenDateSecondsShift.Name = "lbl_TakenDateSecondsShift";
            this.lbl_TakenDateSecondsShift.Size = new System.Drawing.Size(140, 13);
            this.lbl_TakenDateSecondsShift.TabIndex = 25;
            this.lbl_TakenDateSecondsShift.Text = "lbl_TakenDateSecondsShift";
            // 
            // lbl_TakenDateMinutesShift
            // 
            this.lbl_TakenDateMinutesShift.AutoSize = true;
            this.lbl_TakenDateMinutesShift.Location = new System.Drawing.Point(191, 111);
            this.lbl_TakenDateMinutesShift.Name = "lbl_TakenDateMinutesShift";
            this.lbl_TakenDateMinutesShift.Size = new System.Drawing.Size(135, 13);
            this.lbl_TakenDateMinutesShift.TabIndex = 24;
            this.lbl_TakenDateMinutesShift.Text = "lbl_TakenDateMinutesShift";
            // 
            // lbl_TakenDateHoursShift
            // 
            this.lbl_TakenDateHoursShift.AutoSize = true;
            this.lbl_TakenDateHoursShift.Location = new System.Drawing.Point(120, 111);
            this.lbl_TakenDateHoursShift.Name = "lbl_TakenDateHoursShift";
            this.lbl_TakenDateHoursShift.Size = new System.Drawing.Size(126, 13);
            this.lbl_TakenDateHoursShift.TabIndex = 23;
            this.lbl_TakenDateHoursShift.Text = "lbl_TakenDateHoursShift";
            // 
            // lbl_TakenDateDaysShift
            // 
            this.lbl_TakenDateDaysShift.AutoSize = true;
            this.lbl_TakenDateDaysShift.Location = new System.Drawing.Point(58, 111);
            this.lbl_TakenDateDaysShift.Name = "lbl_TakenDateDaysShift";
            this.lbl_TakenDateDaysShift.Size = new System.Drawing.Size(122, 13);
            this.lbl_TakenDateDaysShift.TabIndex = 22;
            this.lbl_TakenDateDaysShift.Text = "lbl_TakenDateDaysShift";
            // 
            // nud_TakenDateSecondsShift
            // 
            this.nud_TakenDateSecondsShift.Location = new System.Drawing.Point(284, 137);
            this.nud_TakenDateSecondsShift.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
            this.nud_TakenDateSecondsShift.Minimum = new decimal(new int[] {
            59,
            0,
            0,
            -2147483648});
            this.nud_TakenDateSecondsShift.Name = "nud_TakenDateSecondsShift";
            this.nud_TakenDateSecondsShift.Size = new System.Drawing.Size(36, 20);
            this.nud_TakenDateSecondsShift.TabIndex = 21;
            this.nud_TakenDateSecondsShift.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.nud_TakenDateSecondsShift.Enter += new System.EventHandler(this.any_nud_Enter);
            this.nud_TakenDateSecondsShift.Leave += new System.EventHandler(this.any_nud_Enter);
            // 
            // nud_TakenDateMinutesShift
            // 
            this.nud_TakenDateMinutesShift.Location = new System.Drawing.Point(204, 137);
            this.nud_TakenDateMinutesShift.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
            this.nud_TakenDateMinutesShift.Minimum = new decimal(new int[] {
            59,
            0,
            0,
            -2147483648});
            this.nud_TakenDateMinutesShift.Name = "nud_TakenDateMinutesShift";
            this.nud_TakenDateMinutesShift.Size = new System.Drawing.Size(36, 20);
            this.nud_TakenDateMinutesShift.TabIndex = 20;
            this.nud_TakenDateMinutesShift.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.nud_TakenDateMinutesShift.Enter += new System.EventHandler(this.any_nud_Enter);
            this.nud_TakenDateMinutesShift.Leave += new System.EventHandler(this.any_nud_Enter);
            // 
            // nud_TakenDateHoursShift
            // 
            this.nud_TakenDateHoursShift.Location = new System.Drawing.Point(133, 137);
            this.nud_TakenDateHoursShift.Maximum = new decimal(new int[] {
            23,
            0,
            0,
            0});
            this.nud_TakenDateHoursShift.Minimum = new decimal(new int[] {
            23,
            0,
            0,
            -2147483648});
            this.nud_TakenDateHoursShift.Name = "nud_TakenDateHoursShift";
            this.nud_TakenDateHoursShift.Size = new System.Drawing.Size(36, 20);
            this.nud_TakenDateHoursShift.TabIndex = 19;
            this.nud_TakenDateHoursShift.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.nud_TakenDateHoursShift.Enter += new System.EventHandler(this.any_nud_Enter);
            this.nud_TakenDateHoursShift.Leave += new System.EventHandler(this.any_nud_Enter);
            // 
            // nud_TakenDateDaysShift
            // 
            this.nud_TakenDateDaysShift.Location = new System.Drawing.Point(68, 137);
            this.nud_TakenDateDaysShift.Maximum = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.nud_TakenDateDaysShift.Minimum = new decimal(new int[] {
            30,
            0,
            0,
            -2147483648});
            this.nud_TakenDateDaysShift.Name = "nud_TakenDateDaysShift";
            this.nud_TakenDateDaysShift.Size = new System.Drawing.Size(36, 20);
            this.nud_TakenDateDaysShift.TabIndex = 18;
            this.nud_TakenDateDaysShift.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.nud_TakenDateDaysShift.Enter += new System.EventHandler(this.any_nud_Enter);
            this.nud_TakenDateDaysShift.Leave += new System.EventHandler(this.any_nud_Enter);
            // 
            // dtp_TakenDate
            // 
            this.dtp_TakenDate.CustomFormat = "";
            this.dtp_TakenDate.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtp_TakenDate.Location = new System.Drawing.Point(27, 48);
            this.dtp_TakenDate.MaxDate = new System.DateTime(2069, 12, 31, 0, 0, 0, 0);
            this.dtp_TakenDate.MinDate = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
            this.dtp_TakenDate.Name = "dtp_TakenDate";
            this.dtp_TakenDate.Size = new System.Drawing.Size(164, 20);
            this.dtp_TakenDate.TabIndex = 17;
            this.dtp_TakenDate.ValueChanged += new System.EventHandler(this.tbx_cbx_dtp_nud_Any_TextChanged);
            this.dtp_TakenDate.Enter += new System.EventHandler(this.any_nud_Enter);
            this.dtp_TakenDate.Leave += new System.EventHandler(this.any_nud_Leave);
            // 
            // rbt_TakenDateTimeShift
            // 
            this.rbt_TakenDateTimeShift.AutoSize = true;
            this.rbt_TakenDateTimeShift.Location = new System.Drawing.Point(17, 80);
            this.rbt_TakenDateTimeShift.Name = "rbt_TakenDateTimeShift";
            this.rbt_TakenDateTimeShift.Size = new System.Drawing.Size(141, 17);
            this.rbt_TakenDateTimeShift.TabIndex = 15;
            this.rbt_TakenDateTimeShift.Text = "rbt_TakenDateTimeShift";
            this.rbt_TakenDateTimeShift.UseVisualStyleBackColor = true;
            this.rbt_TakenDateTimeShift.CheckedChanged += new System.EventHandler(this.rbt_TakenDateTimeShift_CheckedChanged);
            // 
            // rbt_TakenDateSetToFixedDate
            // 
            this.rbt_TakenDateSetToFixedDate.AutoSize = true;
            this.rbt_TakenDateSetToFixedDate.Checked = true;
            this.rbt_TakenDateSetToFixedDate.Location = new System.Drawing.Point(17, 25);
            this.rbt_TakenDateSetToFixedDate.Name = "rbt_TakenDateSetToFixedDate";
            this.rbt_TakenDateSetToFixedDate.Size = new System.Drawing.Size(174, 17);
            this.rbt_TakenDateSetToFixedDate.TabIndex = 14;
            this.rbt_TakenDateSetToFixedDate.TabStop = true;
            this.rbt_TakenDateSetToFixedDate.Text = "rbt_TakenDateSetToFixedDate";
            this.rbt_TakenDateSetToFixedDate.UseVisualStyleBackColor = true;
            this.rbt_TakenDateSetToFixedDate.CheckedChanged += new System.EventHandler(this.rbt_TakenDateSetToFixedDate_CheckedChanged);
            // 
            // btn_InsertTakenDate
            // 
            this.btn_InsertTakenDate.Location = new System.Drawing.Point(239, 19);
            this.btn_InsertTakenDate.Name = "btn_InsertTakenDate";
            this.btn_InsertTakenDate.Size = new System.Drawing.Size(112, 23);
            this.btn_InsertTakenDate.TabIndex = 5;
            this.btn_InsertTakenDate.Text = "btn_InsertTakenDate";
            this.btn_InsertTakenDate.UseVisualStyleBackColor = true;
            this.btn_InsertTakenDate.Click += new System.EventHandler(this.btn_InsertTakenDate_Click);
            // 
            // igl_TabPages
            // 
            this.igl_TabPages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("igl_TabPages.ImageStream")));
            this.igl_TabPages.TransparentColor = System.Drawing.Color.Transparent;
            this.igl_TabPages.Images.SetKeyName(0, "DateTimeAxis.png");
            this.igl_TabPages.Images.SetKeyName(1, "PublishOnDemand.png");
            // 
            // btn_Cancel
            // 
            this.btn_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Cancel.Location = new System.Drawing.Point(893, 590);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(84, 23);
            this.btn_Cancel.TabIndex = 15;
            this.btn_Cancel.Text = "btn_Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = true;
            this.btn_Cancel.Click += new System.EventHandler(this.btn_Cancel_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btn_OK.Location = new System.Drawing.Point(800, 590);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(84, 23);
            this.btn_OK.TabIndex = 14;
            this.btn_OK.Text = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_SetCurrentValues
            // 
            this.btn_SetCurrentValues.Location = new System.Drawing.Point(715, 534);
            this.btn_SetCurrentValues.Name = "btn_SetCurrentValues";
            this.btn_SetCurrentValues.Size = new System.Drawing.Size(288, 23);
            this.btn_SetCurrentValues.TabIndex = 16;
            this.btn_SetCurrentValues.Text = "btn_SetCurrentValues";
            this.btn_SetCurrentValues.UseVisualStyleBackColor = true;
            this.btn_SetCurrentValues.Click += new System.EventHandler(this.btn_SetCurrentValues_Click);
            // 
            // ttp_OffsetTime
            // 
            this.ttp_OffsetTime.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            // 
            // gbx_EditImageList
            // 
            this.gbx_EditImageList.Controls.Add(this.lvw_FileListEditImages);
            this.gbx_EditImageList.Location = new System.Drawing.Point(12, 12);
            this.gbx_EditImageList.Name = "gbx_EditImageList";
            this.gbx_EditImageList.Size = new System.Drawing.Size(438, 312);
            this.gbx_EditImageList.TabIndex = 17;
            this.gbx_EditImageList.TabStop = false;
            this.gbx_EditImageList.Text = "gbx_EditImageList";
            // 
            // lvw_FileListEditImages
            // 
            this.lvw_FileListEditImages.FullRowSelect = true;
            this.lvw_FileListEditImages.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lvw_FileListEditImages.HideSelection = false;
            this.lvw_FileListEditImages.LabelWrap = false;
            this.lvw_FileListEditImages.Location = new System.Drawing.Point(6, 22);
            this.lvw_FileListEditImages.MultiSelect = false;
            this.lvw_FileListEditImages.Name = "lvw_FileListEditImages";
            this.lvw_FileListEditImages.OwnerDraw = true;
            this.lvw_FileListEditImages.Size = new System.Drawing.Size(426, 284);
            this.lvw_FileListEditImages.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lvw_FileListEditImages.TabIndex = 1;
            this.lvw_FileListEditImages.UseCompatibleStateImageBehavior = false;
            this.lvw_FileListEditImages.View = System.Windows.Forms.View.Details;
            this.lvw_FileListEditImages.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.ListView_DrawColumnHeader);
            this.lvw_FileListEditImages.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.ListView_DrawItem);
            this.lvw_FileListEditImages.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.ListView_DrawSubItem);
            this.lvw_FileListEditImages.SelectedIndexChanged += new System.EventHandler(this.lvw_FileListEditImages_SelectedIndexChanged);
            // 
            // FrmEditFileData
            // 
            this.AcceptButton = this.btn_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.btn_Cancel;
            this.ClientSize = new System.Drawing.Size(1031, 634);
            this.Controls.Add(this.gbx_EditImageList);
            this.Controls.Add(this.btn_SetCurrentValues);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Cancel);
            this.Controls.Add(this.pbx_imagePreview);
            this.Controls.Add(this.tcr_EditData);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(20, 490);
            this.Name = "FrmEditFileData";
            this.ShowInTaskbar = false;
            this.Text = "FrmEditFileData";
            this.Load += new System.EventHandler(this.FrmEditFileData_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbx_imagePreview)).EndInit();
            this.tpg_Location.ResumeLayout(false);
            this.gbx_LocationData.ResumeLayout(false);
            this.gbx_LocationData.PerformLayout();
            this.gbx_GetToponomy.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbx_OffsetTimeInfo)).EndInit();
            this.gbx_GPSData.ResumeLayout(false);
            this.gbx_GPSData.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_GPSAltitude)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_GPSLongitude)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_GPSLatitude)).EndInit();
            this.tcr_EditData.ResumeLayout(false);
            this.tpg_DateTime.ResumeLayout(false);
            this.gbx_CreateDate.ResumeLayout(false);
            this.gbx_CreateDate.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_CreateDateSecondsShift)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_CreateDateMinutesShift)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_CreateDateHoursShift)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_CreateDateDaysShift)).EndInit();
            this.gbx_TakenDate.ResumeLayout(false);
            this.gbx_TakenDate.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TakenDateSecondsShift)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TakenDateMinutesShift)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TakenDateHoursShift)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TakenDateDaysShift)).EndInit();
            this.gbx_EditImageList.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.PictureBox pbx_imagePreview;
        private System.Windows.Forms.TabPage tpg_Location;
        private System.Windows.Forms.Button btn_RemoveGeoData;
        private System.Windows.Forms.Button btn_getAllFromWeb_Toponomy;
        private System.Windows.Forms.Button btn_getFromWeb_Toponomy;
        internal System.Windows.Forms.TextBox tbx_Sub_location;
        internal System.Windows.Forms.TextBox tbx_City;
        internal System.Windows.Forms.TextBox tbx_State;
        internal System.Windows.Forms.ComboBox cbx_Country;
        internal System.Windows.Forms.ComboBox cbx_CountryCode;
        private System.Windows.Forms.Label lbl_Sub_location;
        private System.Windows.Forms.Label lbl_City;
        private System.Windows.Forms.Label lbl_State;
        private System.Windows.Forms.Label lbl_Country;
        private System.Windows.Forms.Label lbl_CountryCode;
        private System.Windows.Forms.Label lbl_GPSAltitude;
        private System.Windows.Forms.Label lbl_GPSLongitude;
        private System.Windows.Forms.Label lbl_GPSLatitude;
        private System.Windows.Forms.Label lbl_Decimal;
        private System.Windows.Forms.TabControl tcr_EditData;
        private System.Windows.Forms.Button btn_Cancel;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.ImageList igl_TabPages;
        private System.Windows.Forms.TabPage tpg_DateTime;
        private System.Windows.Forms.GroupBox gbx_CreateDate;
        private System.Windows.Forms.GroupBox gbx_TakenDate;
        private System.Windows.Forms.Button btn_InsertTakenDate;
        private System.Windows.Forms.Button btn_SetCurrentValues;
        private System.Windows.Forms.RadioButton rbt_TakenDateTimeShift;
        private System.Windows.Forms.RadioButton rbt_TakenDateSetToFixedDate;
        private System.Windows.Forms.DateTimePicker dtp_TakenDate;
        private System.Windows.Forms.Label lbl_TakenDateSecondsShift;
        private System.Windows.Forms.Label lbl_TakenDateMinutesShift;
        private System.Windows.Forms.Label lbl_TakenDateHoursShift;
        private System.Windows.Forms.Label lbl_TakenDateDaysShift;
        private System.Windows.Forms.NumericUpDown nud_TakenDateSecondsShift;
        private System.Windows.Forms.NumericUpDown nud_TakenDateMinutesShift;
        private System.Windows.Forms.NumericUpDown nud_TakenDateHoursShift;
        private System.Windows.Forms.NumericUpDown nud_TakenDateDaysShift;
        private System.Windows.Forms.Label lbl_CreateDateSecondsShift;
        private System.Windows.Forms.Label lbl_CreateDateMinutesShift;
        private System.Windows.Forms.Label lbl_CreateDateHoursShift;
        private System.Windows.Forms.Label lbl_CreateDateDaysShift;
        private System.Windows.Forms.NumericUpDown nud_CreateDateSecondsShift;
        private System.Windows.Forms.NumericUpDown nud_CreateDateMinutesShift;
        private System.Windows.Forms.NumericUpDown nud_CreateDateHoursShift;
        private System.Windows.Forms.NumericUpDown nud_CreateDateDaysShift;
        private System.Windows.Forms.DateTimePicker dtp_CreateDate;
        private System.Windows.Forms.RadioButton rbt_CreateDateTimeShift;
        private System.Windows.Forms.RadioButton rbt_CreateDateSetToFixedDate;
        private System.Windows.Forms.Button btn_InsertCreateDate;
        private System.Windows.Forms.Button btn_InsertFromTakenDate;
        internal System.Windows.Forms.ComboBox cbx_OffsetTimeList;
        private System.Windows.Forms.Label lbl_OffsetTime;
        private System.Windows.Forms.TextBox tbx_OffsetTime;
        private System.Windows.Forms.PictureBox pbx_OffsetTimeInfo;
        private System.Windows.Forms.CheckBox ckb_UseDST;
        private System.Windows.Forms.ToolTip ttp_OffsetTime;
        private System.Windows.Forms.TextBox tbx_CreateDate;
        private System.Windows.Forms.GroupBox gbx_EditImageList;
        internal System.Windows.Forms.ListView lvw_FileListEditImages;
        private System.Windows.Forms.GroupBox gbx_GetToponomy;
        internal System.Windows.Forms.NumericUpDown nud_GPSAltitude;
        internal System.Windows.Forms.NumericUpDown nud_GPSLongitude;
        internal System.Windows.Forms.NumericUpDown nud_GPSLatitude;
        internal System.Windows.Forms.GroupBox gbx_LocationData;
        internal System.Windows.Forms.GroupBox gbx_GPSData;
    }
}