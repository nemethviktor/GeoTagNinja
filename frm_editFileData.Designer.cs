namespace GeoTagNinja
{
    partial class frm_editFileData
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
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.btn_OK = new System.Windows.Forms.Button();
            this.pbx_imgPreview = new System.Windows.Forms.PictureBox();
            this.lvw_FileListEditImages = new System.Windows.Forms.ListView();
            this.clh_FileName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tpg_Location = new System.Windows.Forms.TabPage();
            this.gbx_LocationData = new System.Windows.Forms.GroupBox();
            this.btn_getAllFromWeb_Toponomy = new System.Windows.Forms.Button();
            this.btn_getFromWeb_Toponomy = new System.Windows.Forms.Button();
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
            this.btn_getFromWeb_Altitude = new System.Windows.Forms.Button();
            this.tbx_GPSAltitude = new System.Windows.Forms.TextBox();
            this.tbx_GPSImgDirection = new System.Windows.Forms.TextBox();
            this.tbx_GPSLongitude = new System.Windows.Forms.TextBox();
            this.tbx_GPSLatitude = new System.Windows.Forms.TextBox();
            this.lbl_GPSAltitude = new System.Windows.Forms.Label();
            this.lbl_GPSImgDirection = new System.Windows.Forms.Label();
            this.lbl_GPSLongitude = new System.Windows.Forms.Label();
            this.lbl_GPSLatitude = new System.Windows.Forms.Label();
            this.btn_getAllFromWeb_Altitude = new System.Windows.Forms.Button();
            this.lbl_Decimal = new System.Windows.Forms.Label();
            this.tcr_EditData = new System.Windows.Forms.TabControl();
            ((System.ComponentModel.ISupportInitialize)(this.pbx_imgPreview)).BeginInit();
            this.tpg_Location.SuspendLayout();
            this.gbx_LocationData.SuspendLayout();
            this.gbx_GPSData.SuspendLayout();
            this.tcr_EditData.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_Cancel
            // 
            this.btn_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Cancel.Location = new System.Drawing.Point(1113, 879);
            this.btn_Cancel.Margin = new System.Windows.Forms.Padding(6);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(138, 42);
            this.btn_Cancel.TabIndex = 13;
            this.btn_Cancel.Text = "btn_Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = true;
            this.btn_Cancel.Click += new System.EventHandler(this.btn_Cancel_Click);
            // 
            // btn_OK
            // 
            this.btn_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btn_OK.Location = new System.Drawing.Point(964, 879);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(6);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(138, 42);
            this.btn_OK.TabIndex = 12;
            this.btn_OK.Text = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // pbx_imgPreview
            // 
            this.pbx_imgPreview.Location = new System.Drawing.Point(24, 474);
            this.pbx_imgPreview.Margin = new System.Windows.Forms.Padding(6);
            this.pbx_imgPreview.Name = "pbx_imgPreview";
            this.pbx_imgPreview.Size = new System.Drawing.Size(482, 388);
            this.pbx_imgPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbx_imgPreview.TabIndex = 2;
            this.pbx_imgPreview.TabStop = false;
            // 
            // lvw_FileListEditImages
            // 
            this.lvw_FileListEditImages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.clh_FileName});
            this.lvw_FileListEditImages.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lvw_FileListEditImages.HideSelection = false;
            this.lvw_FileListEditImages.LabelWrap = false;
            this.lvw_FileListEditImages.Location = new System.Drawing.Point(22, 61);
            this.lvw_FileListEditImages.Margin = new System.Windows.Forms.Padding(6);
            this.lvw_FileListEditImages.MultiSelect = false;
            this.lvw_FileListEditImages.Name = "lvw_FileListEditImages";
            this.lvw_FileListEditImages.Size = new System.Drawing.Size(479, 364);
            this.lvw_FileListEditImages.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.lvw_FileListEditImages.TabIndex = 0;
            this.lvw_FileListEditImages.UseCompatibleStateImageBehavior = false;
            this.lvw_FileListEditImages.View = System.Windows.Forms.View.Details;
            this.lvw_FileListEditImages.MouseClick += new System.Windows.Forms.MouseEventHandler(this.lvw_FileListEditImages_MouseClick);
            // 
            // clh_FileName
            // 
            this.clh_FileName.Text = "FileName";
            this.clh_FileName.Width = 200;
            // 
            // tpg_Location
            // 
            this.tpg_Location.Controls.Add(this.gbx_LocationData);
            this.tpg_Location.Controls.Add(this.gbx_GPSData);
            this.tpg_Location.Location = new System.Drawing.Point(4, 33);
            this.tpg_Location.Margin = new System.Windows.Forms.Padding(4);
            this.tpg_Location.Name = "tpg_Location";
            this.tpg_Location.Padding = new System.Windows.Forms.Padding(4);
            this.tpg_Location.Size = new System.Drawing.Size(722, 812);
            this.tpg_Location.TabIndex = 0;
            this.tpg_Location.Text = "tpg_Location";
            this.tpg_Location.UseVisualStyleBackColor = true;
            // 
            // gbx_LocationData
            // 
            this.gbx_LocationData.Controls.Add(this.btn_getAllFromWeb_Toponomy);
            this.gbx_LocationData.Controls.Add(this.btn_getFromWeb_Toponomy);
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
            this.gbx_LocationData.Location = new System.Drawing.Point(24, 414);
            this.gbx_LocationData.Margin = new System.Windows.Forms.Padding(4);
            this.gbx_LocationData.Name = "gbx_LocationData";
            this.gbx_LocationData.Padding = new System.Windows.Forms.Padding(4);
            this.gbx_LocationData.Size = new System.Drawing.Size(667, 380);
            this.gbx_LocationData.TabIndex = 1;
            this.gbx_LocationData.TabStop = false;
            this.gbx_LocationData.Text = "gbx_LocationData";
            // 
            // btn_getAllFromWeb_Toponomy
            // 
            this.btn_getAllFromWeb_Toponomy.Location = new System.Drawing.Point(244, 312);
            this.btn_getAllFromWeb_Toponomy.Margin = new System.Windows.Forms.Padding(6);
            this.btn_getAllFromWeb_Toponomy.Name = "btn_getAllFromWeb_Toponomy";
            this.btn_getAllFromWeb_Toponomy.Size = new System.Drawing.Size(206, 42);
            this.btn_getAllFromWeb_Toponomy.TabIndex = 12;
            this.btn_getAllFromWeb_Toponomy.Text = "btn_getAllFromWeb_Toponomy";
            this.btn_getAllFromWeb_Toponomy.UseVisualStyleBackColor = true;
            this.btn_getAllFromWeb_Toponomy.Click += new System.EventHandler(this.btn_getFromWeb_Click);
            // 
            // btn_getFromWeb_Toponomy
            // 
            this.btn_getFromWeb_Toponomy.Location = new System.Drawing.Point(26, 312);
            this.btn_getFromWeb_Toponomy.Margin = new System.Windows.Forms.Padding(6);
            this.btn_getFromWeb_Toponomy.Name = "btn_getFromWeb_Toponomy";
            this.btn_getFromWeb_Toponomy.Size = new System.Drawing.Size(206, 42);
            this.btn_getFromWeb_Toponomy.TabIndex = 11;
            this.btn_getFromWeb_Toponomy.Text = "btn_getFromWeb_Toponomy";
            this.btn_getFromWeb_Toponomy.UseVisualStyleBackColor = true;
            this.btn_getFromWeb_Toponomy.Click += new System.EventHandler(this.btn_getFromWeb_Click);
            // 
            // tbx_Sub_location
            // 
            this.tbx_Sub_location.Location = new System.Drawing.Point(227, 246);
            this.tbx_Sub_location.Margin = new System.Windows.Forms.Padding(6);
            this.tbx_Sub_location.Name = "tbx_Sub_location";
            this.tbx_Sub_location.Size = new System.Drawing.Size(389, 29);
            this.tbx_Sub_location.TabIndex = 10;
            this.tbx_Sub_location.TextChanged += new System.EventHandler(this.tbx_cbx_Any_TextChanged);
            this.tbx_Sub_location.Enter += new System.EventHandler(this.tbx_cbx_Any_Enter);
            // 
            // tbx_City
            // 
            this.tbx_City.Location = new System.Drawing.Point(227, 198);
            this.tbx_City.Margin = new System.Windows.Forms.Padding(6);
            this.tbx_City.Name = "tbx_City";
            this.tbx_City.Size = new System.Drawing.Size(389, 29);
            this.tbx_City.TabIndex = 9;
            this.tbx_City.TextChanged += new System.EventHandler(this.tbx_cbx_Any_TextChanged);
            this.tbx_City.Enter += new System.EventHandler(this.tbx_cbx_Any_Enter);
            // 
            // tbx_State
            // 
            this.tbx_State.Location = new System.Drawing.Point(227, 150);
            this.tbx_State.Margin = new System.Windows.Forms.Padding(6);
            this.tbx_State.Name = "tbx_State";
            this.tbx_State.Size = new System.Drawing.Size(389, 29);
            this.tbx_State.TabIndex = 8;
            this.tbx_State.TextChanged += new System.EventHandler(this.tbx_cbx_Any_TextChanged);
            this.tbx_State.Enter += new System.EventHandler(this.tbx_cbx_Any_Enter);
            // 
            // cbx_Country
            // 
            this.cbx_Country.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cbx_Country.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbx_Country.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_Country.FormattingEnabled = true;
            this.cbx_Country.Items.AddRange(new object[] {
            "",
            "Afghanistan",
            "Åland Islands",
            "Albania",
            "Algeria",
            "American Samoa",
            "Andorra",
            "Angola",
            "Anguilla",
            "Antarctica",
            "Antigua and Barbuda",
            "Argentina",
            "Armenia",
            "Aruba",
            "Australia",
            "Austria",
            "Azerbaijan",
            "Bahamas (the)",
            "Bahrain",
            "Bangladesh",
            "Barbados",
            "Belarus",
            "Belgium",
            "Belize",
            "Benin",
            "Bermuda",
            "Bhutan",
            "Bolivia (Plurinational State of)",
            "Bonaire",
            "Bosnia and Herzegovina",
            "Botswana",
            "Bouvet Island",
            "Brazil",
            "British Indian Ocean Territory (the)",
            "Brunei Darussalam",
            "Bulgaria",
            "Burkina Faso",
            "Burundi",
            "Cabo Verde",
            "Cambodia",
            "Cameroon",
            "Canada",
            "Cayman Islands (the)",
            "Central African Republic (the)",
            "Chad",
            "Chile",
            "China",
            "Christmas Island",
            "Cocos (Keeling) Islands (the)",
            "Colombia",
            "Comoros (the)",
            "Congo (the Democratic Republic of the)",
            "Congo (the)",
            "Cook Islands (the)",
            "Costa Rica",
            "Côte d\'Ivoire",
            "Croatia",
            "Cuba",
            "Curaçao",
            "Cyprus",
            "Czechia",
            "Denmark",
            "Djibouti",
            "Dominica",
            "Dominican Republic (the)",
            "Ecuador",
            "Egypt",
            "El Salvador",
            "Equatorial Guinea",
            "Eritrea",
            "Estonia",
            "Eswatini",
            "Ethiopia",
            "Falkland Islands (the) [Malvinas]",
            "Faroe Islands (the)",
            "Fiji",
            "Finland",
            "France",
            "French Guiana",
            "French Polynesia",
            "French Southern Territories (the)",
            "Gabon",
            "Gambia (the)",
            "Georgia",
            "Germany",
            "Ghana",
            "Gibraltar",
            "Greece",
            "Greenland",
            "Grenada",
            "Guadeloupe",
            "Guam",
            "Guatemala",
            "Guernsey",
            "Guinea",
            "Guinea-Bissau",
            "Guyana",
            "Haiti",
            "Heard Island and McDonald Islands",
            "Holy See (the)",
            "Honduras",
            "Hong Kong",
            "Hungary",
            "Iceland",
            "India",
            "Indonesia",
            "Iran (Islamic Republic of)",
            "Iraq",
            "Ireland",
            "Isle of Man",
            "Israel",
            "Italy",
            "Jamaica",
            "Japan",
            "Jersey",
            "Jordan",
            "Kazakhstan",
            "Kenya",
            "Kiribati",
            "Korea (the Democratic People\'s Republic of)",
            "Korea (the Republic of)",
            "Kuwait",
            "Kyrgyzstan",
            "Lao People\'s Democratic Republic (the)",
            "Latvia",
            "Lebanon",
            "Lesotho",
            "Liberia",
            "Libya",
            "Liechtenstein",
            "Lithuania",
            "Luxembourg",
            "Macao",
            "Madagascar",
            "Malawi",
            "Malaysia",
            "Maldives",
            "Mali",
            "Malta",
            "Marshall Islands (the)",
            "Martinique",
            "Mauritania",
            "Mauritius",
            "Mayotte",
            "Mexico",
            "Micronesia (Federated States of)",
            "Moldova (the Republic of)",
            "Monaco",
            "Mongolia",
            "Montenegro",
            "Montserrat",
            "Morocco",
            "Mozambique",
            "Myanmar",
            "Namibia",
            "Nauru",
            "Nepal",
            "Netherlands (the)",
            "New Caledonia",
            "New Zealand",
            "Nicaragua",
            "Niger (the)",
            "Nigeria",
            "Niue",
            "Norfolk Island",
            "Northern Mariana Islands (the)",
            "Norway",
            "Oman",
            "Pakistan",
            "Palau",
            "Palestine",
            "Panama",
            "Papua New Guinea",
            "Paraguay",
            "Peru",
            "Philippines (the)",
            "Pitcairn",
            "Poland",
            "Portugal",
            "Puerto Rico",
            "Qatar",
            "Republic of North Macedonia",
            "Réunion",
            "Romania",
            "Russian Federation (the)",
            "Rwanda",
            "Saint Barthélemy",
            "Saint Helena",
            "Saint Kitts and Nevis",
            "Saint Lucia",
            "Saint Martin (French part)",
            "Saint Pierre and Miquelon",
            "Saint Vincent and the Grenadines",
            "Samoa",
            "San Marino",
            "Sao Tome and Principe",
            "Saudi Arabia",
            "Senegal",
            "Serbia",
            "Seychelles",
            "Sierra Leone",
            "Singapore",
            "Sint Maarten (Dutch part)",
            "Slovakia",
            "Slovenia",
            "Solomon Islands",
            "Somalia",
            "South Africa",
            "South Georgia and the South Sandwich Islands",
            "South Sudan",
            "Spain",
            "Sri Lanka",
            "Sudan (the)",
            "Suriname",
            "Svalbard and Jan Mayen",
            "Sweden",
            "Switzerland",
            "Syrian Arab Republic",
            "Taiwan (Province of China)",
            "Tajikistan",
            "Tanzania",
            "Thailand",
            "Timor-Leste",
            "Togo",
            "Tokelau",
            "Tonga",
            "Trinidad and Tobago",
            "Tunisia",
            "Turkey",
            "Turkmenistan",
            "Turks and Caicos Islands (the)",
            "Tuvalu",
            "Uganda",
            "Ukraine",
            "United Arab Emirates (the)",
            "United Kingdom of Great Britain and Northern Ireland (the)",
            "United States Minor Outlying Islands (the)",
            "United States of America (the)",
            "Uruguay",
            "Uzbekistan",
            "Vanuatu",
            "Venezuela (Bolivarian Republic of)",
            "Viet Nam",
            "Virgin Islands (British)",
            "Virgin Islands (U.S.)",
            "Wallis and Futuna",
            "Western Sahara",
            "Yemen",
            "Zambia",
            "Zimbabwe"});
            this.cbx_Country.Location = new System.Drawing.Point(227, 100);
            this.cbx_Country.Margin = new System.Windows.Forms.Padding(6);
            this.cbx_Country.Name = "cbx_Country";
            this.cbx_Country.Size = new System.Drawing.Size(389, 32);
            this.cbx_Country.Sorted = true;
            this.cbx_Country.TabIndex = 7;
            this.cbx_Country.SelectedValueChanged += new System.EventHandler(this.tbx_cbx_Any_TextChanged);
            this.cbx_Country.Enter += new System.EventHandler(this.tbx_cbx_Any_Enter);
            // 
            // cbx_CountryCode
            // 
            this.cbx_CountryCode.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cbx_CountryCode.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbx_CountryCode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_CountryCode.FormattingEnabled = true;
            this.cbx_CountryCode.Items.AddRange(new object[] {
            "",
            "ABW",
            "AFG",
            "AGO",
            "AIA",
            "ALA",
            "ALB",
            "AND",
            "ARE",
            "ARG",
            "ARM",
            "ASM",
            "ATA",
            "ATF",
            "ATG",
            "AUS",
            "AUT",
            "AZE",
            "BDI",
            "BEL",
            "BEN",
            "BFA",
            "BGD",
            "BGR",
            "BHR",
            "BHS",
            "BIH",
            "BLM",
            "BLR",
            "BLZ",
            "BMU",
            "BOL",
            "BQ",
            "BRA",
            "BRB",
            "BRN",
            "BTN",
            "BVT",
            "BWA",
            "CAF",
            "CAN",
            "CCK",
            "CHE",
            "CHL",
            "CHN",
            "CIV",
            "CMR",
            "COD",
            "COG",
            "COK",
            "COL",
            "COM",
            "CPV",
            "CRI",
            "CUB",
            "CUW",
            "CXR",
            "CYM",
            "CYP",
            "CZE",
            "DEU",
            "DJI",
            "DMA",
            "DNK",
            "DOM",
            "DZA",
            "ECU",
            "EGY",
            "ERI",
            "ESH",
            "ESP",
            "EST",
            "ETH",
            "FIN",
            "FJI",
            "FLK",
            "FRA",
            "FRO",
            "FSM",
            "GAB",
            "GBR",
            "GEO",
            "GGY",
            "GHA",
            "GIB",
            "GIN",
            "GLP",
            "GMB",
            "GNB",
            "GNQ",
            "GRC",
            "GRD",
            "GRL",
            "GTM",
            "GUF",
            "GUM",
            "GUY",
            "HKG",
            "HMD",
            "HND",
            "HRV",
            "HTI",
            "HUN",
            "IDN",
            "IMN",
            "IND",
            "IOT",
            "IRL",
            "IRN",
            "IRQ",
            "ISL",
            "ISR",
            "ITA",
            "JAM",
            "JEY",
            "JOR",
            "JPN",
            "KAZ",
            "KEN",
            "KGZ",
            "KHM",
            "KIR",
            "KNA",
            "KOR",
            "KWT",
            "LAO",
            "LBN",
            "LBR",
            "LBY",
            "LCA",
            "LIE",
            "LKA",
            "LSO",
            "LTU",
            "LUX",
            "LVA",
            "MAC",
            "MAF",
            "MAR",
            "MCO",
            "MDA",
            "MDG",
            "MDV",
            "MEX",
            "MHL",
            "MKD",
            "MLI",
            "MLT",
            "MMR",
            "MNE",
            "MNG",
            "MNP",
            "MOZ",
            "MRT",
            "MSR",
            "MTQ",
            "MUS",
            "MWI",
            "MYS",
            "MYT",
            "NAM",
            "NCL",
            "NER",
            "NFK",
            "NGA",
            "NIC",
            "NIU",
            "NLD",
            "NOR",
            "NPL",
            "NRU",
            "NZL",
            "OMN",
            "PAK",
            "PAN",
            "PCN",
            "PER",
            "PHL",
            "PLW",
            "PNG",
            "POL",
            "PRI",
            "PRK",
            "PRT",
            "PRY",
            "PS",
            "PYF",
            "QAT",
            "REU",
            "ROU",
            "RUS",
            "RWA",
            "SAU",
            "SDN",
            "SEN",
            "SGP",
            "SGS",
            "SH",
            "SJM",
            "SLB",
            "SLE",
            "SLV",
            "SMR",
            "SOM",
            "SPM",
            "SRB",
            "SSD",
            "STP",
            "SUR",
            "SVK",
            "SVN",
            "SWE",
            "SWZ",
            "SXM",
            "SYC",
            "SYR",
            "TCA",
            "TCD",
            "TGO",
            "THA",
            "TJK",
            "TKL",
            "TKM",
            "TLS",
            "TON",
            "TTO",
            "TUN",
            "TUR",
            "TUV",
            "TWN",
            "TZ",
            "UGA",
            "UKR",
            "UMI",
            "URY",
            "USA",
            "UZB",
            "VAT",
            "VCT",
            "VEN",
            "VGB",
            "VIR",
            "VNM",
            "VUT",
            "WLF",
            "WSM",
            "YEM",
            "ZAF",
            "ZMB",
            "ZWE"});
            this.cbx_CountryCode.Location = new System.Drawing.Point(227, 50);
            this.cbx_CountryCode.Margin = new System.Windows.Forms.Padding(6);
            this.cbx_CountryCode.Name = "cbx_CountryCode";
            this.cbx_CountryCode.Size = new System.Drawing.Size(121, 32);
            this.cbx_CountryCode.Sorted = true;
            this.cbx_CountryCode.TabIndex = 6;
            this.cbx_CountryCode.SelectedValueChanged += new System.EventHandler(this.tbx_cbx_Any_TextChanged);
            this.cbx_CountryCode.Enter += new System.EventHandler(this.tbx_cbx_Any_Enter);
            // 
            // lbl_Sub_location
            // 
            this.lbl_Sub_location.AutoSize = true;
            this.lbl_Sub_location.Location = new System.Drawing.Point(20, 251);
            this.lbl_Sub_location.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl_Sub_location.Name = "lbl_Sub_location";
            this.lbl_Sub_location.Size = new System.Drawing.Size(156, 25);
            this.lbl_Sub_location.TabIndex = 0;
            this.lbl_Sub_location.Text = "lbl_Sub_location";
            // 
            // lbl_City
            // 
            this.lbl_City.AutoSize = true;
            this.lbl_City.Location = new System.Drawing.Point(20, 203);
            this.lbl_City.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl_City.Name = "lbl_City";
            this.lbl_City.Size = new System.Drawing.Size(76, 25);
            this.lbl_City.TabIndex = 0;
            this.lbl_City.Text = "lbl_City";
            // 
            // lbl_State
            // 
            this.lbl_State.AutoSize = true;
            this.lbl_State.Location = new System.Drawing.Point(20, 155);
            this.lbl_State.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl_State.Name = "lbl_State";
            this.lbl_State.Size = new System.Drawing.Size(88, 25);
            this.lbl_State.TabIndex = 0;
            this.lbl_State.Text = "lbl_State";
            // 
            // lbl_Country
            // 
            this.lbl_Country.AutoSize = true;
            this.lbl_Country.Location = new System.Drawing.Point(20, 105);
            this.lbl_Country.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl_Country.Name = "lbl_Country";
            this.lbl_Country.Size = new System.Drawing.Size(111, 25);
            this.lbl_Country.TabIndex = 0;
            this.lbl_Country.Text = "lbl_Country";
            // 
            // lbl_CountryCode
            // 
            this.lbl_CountryCode.AutoSize = true;
            this.lbl_CountryCode.Location = new System.Drawing.Point(20, 55);
            this.lbl_CountryCode.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl_CountryCode.Name = "lbl_CountryCode";
            this.lbl_CountryCode.Size = new System.Drawing.Size(159, 25);
            this.lbl_CountryCode.TabIndex = 0;
            this.lbl_CountryCode.Text = "lbl_CountryCode";
            // 
            // gbx_GPSData
            // 
            this.gbx_GPSData.Controls.Add(this.btn_getFromWeb_Altitude);
            this.gbx_GPSData.Controls.Add(this.tbx_GPSAltitude);
            this.gbx_GPSData.Controls.Add(this.tbx_GPSImgDirection);
            this.gbx_GPSData.Controls.Add(this.tbx_GPSLongitude);
            this.gbx_GPSData.Controls.Add(this.tbx_GPSLatitude);
            this.gbx_GPSData.Controls.Add(this.lbl_GPSAltitude);
            this.gbx_GPSData.Controls.Add(this.lbl_GPSImgDirection);
            this.gbx_GPSData.Controls.Add(this.lbl_GPSLongitude);
            this.gbx_GPSData.Controls.Add(this.lbl_GPSLatitude);
            this.gbx_GPSData.Controls.Add(this.btn_getAllFromWeb_Altitude);
            this.gbx_GPSData.Controls.Add(this.lbl_Decimal);
            this.gbx_GPSData.Location = new System.Drawing.Point(24, 78);
            this.gbx_GPSData.Margin = new System.Windows.Forms.Padding(4);
            this.gbx_GPSData.Name = "gbx_GPSData";
            this.gbx_GPSData.Padding = new System.Windows.Forms.Padding(4);
            this.gbx_GPSData.Size = new System.Drawing.Size(667, 290);
            this.gbx_GPSData.TabIndex = 0;
            this.gbx_GPSData.TabStop = false;
            this.gbx_GPSData.Text = "gbx_GPSData";
            // 
            // btn_getFromWeb_Altitude
            // 
            this.btn_getFromWeb_Altitude.Location = new System.Drawing.Point(446, 135);
            this.btn_getFromWeb_Altitude.Margin = new System.Windows.Forms.Padding(6);
            this.btn_getFromWeb_Altitude.Name = "btn_getFromWeb_Altitude";
            this.btn_getFromWeb_Altitude.Size = new System.Drawing.Size(206, 42);
            this.btn_getFromWeb_Altitude.TabIndex = 13;
            this.btn_getFromWeb_Altitude.Text = "btn_getFromWeb_Altitude";
            this.btn_getFromWeb_Altitude.UseVisualStyleBackColor = true;
            this.btn_getFromWeb_Altitude.Click += new System.EventHandler(this.btn_getFromWeb_Click);
            // 
            // tbx_GPSAltitude
            // 
            this.tbx_GPSAltitude.Location = new System.Drawing.Point(227, 212);
            this.tbx_GPSAltitude.Margin = new System.Windows.Forms.Padding(6);
            this.tbx_GPSAltitude.Name = "tbx_GPSAltitude";
            this.tbx_GPSAltitude.Size = new System.Drawing.Size(180, 29);
            this.tbx_GPSAltitude.TabIndex = 4;
            this.tbx_GPSAltitude.TextChanged += new System.EventHandler(this.tbx_cbx_Any_TextChanged);
            this.tbx_GPSAltitude.Enter += new System.EventHandler(this.tbx_cbx_Any_Enter);
            // 
            // tbx_GPSImgDirection
            // 
            this.tbx_GPSImgDirection.Location = new System.Drawing.Point(227, 164);
            this.tbx_GPSImgDirection.Margin = new System.Windows.Forms.Padding(6);
            this.tbx_GPSImgDirection.Name = "tbx_GPSImgDirection";
            this.tbx_GPSImgDirection.Size = new System.Drawing.Size(180, 29);
            this.tbx_GPSImgDirection.TabIndex = 3;
            this.tbx_GPSImgDirection.TextChanged += new System.EventHandler(this.tbx_cbx_Any_TextChanged);
            this.tbx_GPSImgDirection.Enter += new System.EventHandler(this.tbx_cbx_Any_Enter);
            // 
            // tbx_GPSLongitude
            // 
            this.tbx_GPSLongitude.Location = new System.Drawing.Point(227, 116);
            this.tbx_GPSLongitude.Margin = new System.Windows.Forms.Padding(6);
            this.tbx_GPSLongitude.Name = "tbx_GPSLongitude";
            this.tbx_GPSLongitude.Size = new System.Drawing.Size(180, 29);
            this.tbx_GPSLongitude.TabIndex = 2;
            this.tbx_GPSLongitude.TextChanged += new System.EventHandler(this.tbx_cbx_Any_TextChanged);
            this.tbx_GPSLongitude.Enter += new System.EventHandler(this.tbx_cbx_Any_Enter);
            // 
            // tbx_GPSLatitude
            // 
            this.tbx_GPSLatitude.Location = new System.Drawing.Point(227, 65);
            this.tbx_GPSLatitude.Margin = new System.Windows.Forms.Padding(6);
            this.tbx_GPSLatitude.Name = "tbx_GPSLatitude";
            this.tbx_GPSLatitude.Size = new System.Drawing.Size(180, 29);
            this.tbx_GPSLatitude.TabIndex = 1;
            this.tbx_GPSLatitude.TextChanged += new System.EventHandler(this.tbx_cbx_Any_TextChanged);
            this.tbx_GPSLatitude.Enter += new System.EventHandler(this.tbx_cbx_Any_Enter);
            // 
            // lbl_GPSAltitude
            // 
            this.lbl_GPSAltitude.AutoSize = true;
            this.lbl_GPSAltitude.Location = new System.Drawing.Point(26, 216);
            this.lbl_GPSAltitude.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lbl_GPSAltitude.Name = "lbl_GPSAltitude";
            this.lbl_GPSAltitude.Size = new System.Drawing.Size(149, 25);
            this.lbl_GPSAltitude.TabIndex = 12;
            this.lbl_GPSAltitude.Text = "lbl_GPSAltitude";
            // 
            // lbl_GPSImgDirection
            // 
            this.lbl_GPSImgDirection.AutoSize = true;
            this.lbl_GPSImgDirection.Location = new System.Drawing.Point(26, 170);
            this.lbl_GPSImgDirection.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lbl_GPSImgDirection.Name = "lbl_GPSImgDirection";
            this.lbl_GPSImgDirection.Size = new System.Drawing.Size(192, 25);
            this.lbl_GPSImgDirection.TabIndex = 12;
            this.lbl_GPSImgDirection.Text = "lbl_GPSImgDirection";
            // 
            // lbl_GPSLongitude
            // 
            this.lbl_GPSLongitude.AutoSize = true;
            this.lbl_GPSLongitude.Location = new System.Drawing.Point(26, 122);
            this.lbl_GPSLongitude.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lbl_GPSLongitude.Name = "lbl_GPSLongitude";
            this.lbl_GPSLongitude.Size = new System.Drawing.Size(170, 25);
            this.lbl_GPSLongitude.TabIndex = 12;
            this.lbl_GPSLongitude.Text = "lbl_GPSLongitude";
            // 
            // lbl_GPSLatitude
            // 
            this.lbl_GPSLatitude.AutoSize = true;
            this.lbl_GPSLatitude.Location = new System.Drawing.Point(26, 70);
            this.lbl_GPSLatitude.Margin = new System.Windows.Forms.Padding(6, 0, 6, 0);
            this.lbl_GPSLatitude.Name = "lbl_GPSLatitude";
            this.lbl_GPSLatitude.Size = new System.Drawing.Size(153, 25);
            this.lbl_GPSLatitude.TabIndex = 12;
            this.lbl_GPSLatitude.Text = "lbl_GPSLatitude";
            // 
            // btn_getAllFromWeb_Altitude
            // 
            this.btn_getAllFromWeb_Altitude.Location = new System.Drawing.Point(446, 199);
            this.btn_getAllFromWeb_Altitude.Margin = new System.Windows.Forms.Padding(6);
            this.btn_getAllFromWeb_Altitude.Name = "btn_getAllFromWeb_Altitude";
            this.btn_getAllFromWeb_Altitude.Size = new System.Drawing.Size(206, 42);
            this.btn_getAllFromWeb_Altitude.TabIndex = 5;
            this.btn_getAllFromWeb_Altitude.Text = "btn_getAllFromWeb_Altitude";
            this.btn_getAllFromWeb_Altitude.UseVisualStyleBackColor = true;
            this.btn_getAllFromWeb_Altitude.Click += new System.EventHandler(this.btn_getFromWeb_Click);
            // 
            // lbl_Decimal
            // 
            this.lbl_Decimal.AutoSize = true;
            this.lbl_Decimal.Location = new System.Drawing.Point(238, 30);
            this.lbl_Decimal.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl_Decimal.Name = "lbl_Decimal";
            this.lbl_Decimal.Size = new System.Drawing.Size(112, 25);
            this.lbl_Decimal.TabIndex = 1;
            this.lbl_Decimal.Text = "lbl_Decimal";
            // 
            // tcr_EditData
            // 
            this.tcr_EditData.Controls.Add(this.tpg_Location);
            this.tcr_EditData.Location = new System.Drawing.Point(532, 20);
            this.tcr_EditData.Margin = new System.Windows.Forms.Padding(4);
            this.tcr_EditData.Name = "tcr_EditData";
            this.tcr_EditData.SelectedIndex = 0;
            this.tcr_EditData.Size = new System.Drawing.Size(730, 849);
            this.tcr_EditData.TabIndex = 0;
            // 
            // frm_editFileData
            // 
            this.AcceptButton = this.btn_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.btn_Cancel;
            this.ClientSize = new System.Drawing.Size(1274, 994);
            this.ControlBox = false;
            this.Controls.Add(this.lvw_FileListEditImages);
            this.Controls.Add(this.pbx_imgPreview);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Cancel);
            this.Controls.Add(this.tcr_EditData);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(24, 1000);
            this.Name = "frm_editFileData";
            this.Text = "Edit Data";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.frm_editFileData_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbx_imgPreview)).EndInit();
            this.tpg_Location.ResumeLayout(false);
            this.gbx_LocationData.ResumeLayout(false);
            this.gbx_LocationData.PerformLayout();
            this.gbx_GPSData.ResumeLayout(false);
            this.gbx_GPSData.PerformLayout();
            this.tcr_EditData.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btn_Cancel;
        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.PictureBox pbx_imgPreview;
        public System.Windows.Forms.ColumnHeader clh_FileName;
        internal System.Windows.Forms.ListView lvw_FileListEditImages;
        private System.Windows.Forms.TabPage tpg_Location;
        private System.Windows.Forms.GroupBox gbx_LocationData;
        private System.Windows.Forms.Button btn_getFromWeb_Toponomy;
        internal System.Windows.Forms.TextBox tbx_Sub_location;
        internal System.Windows.Forms.TextBox tbx_City;
        internal System.Windows.Forms.TextBox tbx_State;
        private System.Windows.Forms.ComboBox cbx_Country;
        private System.Windows.Forms.ComboBox cbx_CountryCode;
        private System.Windows.Forms.Label lbl_Sub_location;
        private System.Windows.Forms.Label lbl_City;
        private System.Windows.Forms.Label lbl_State;
        private System.Windows.Forms.Label lbl_Country;
        private System.Windows.Forms.Label lbl_CountryCode;
        private System.Windows.Forms.TabControl tcr_EditData;
        private System.Windows.Forms.GroupBox gbx_GPSData;
        private System.Windows.Forms.TextBox tbx_GPSAltitude;
        private System.Windows.Forms.TextBox tbx_GPSImgDirection;
        private System.Windows.Forms.TextBox tbx_GPSLongitude;
        private System.Windows.Forms.TextBox tbx_GPSLatitude;
        private System.Windows.Forms.Label lbl_GPSAltitude;
        private System.Windows.Forms.Label lbl_GPSImgDirection;
        private System.Windows.Forms.Label lbl_GPSLongitude;
        private System.Windows.Forms.Label lbl_GPSLatitude;
        private System.Windows.Forms.Button btn_getAllFromWeb_Altitude;
        private System.Windows.Forms.Label lbl_Decimal;
        private System.Windows.Forms.Button btn_getFromWeb_Altitude;
        private System.Windows.Forms.Button btn_getAllFromWeb_Toponomy;
    }
}