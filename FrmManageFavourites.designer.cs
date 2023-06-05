namespace GeoTagNinja
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmManageFavourites));
            this.cbx_Favourites = new System.Windows.Forms.ComboBox();
            this.btn_Close = new System.Windows.Forms.Button();
            this.lbl_Favourites = new System.Windows.Forms.Label();
            this.tbx_Sub_location = new System.Windows.Forms.TextBox();
            this.tbx_City = new System.Windows.Forms.TextBox();
            this.tbx_State = new System.Windows.Forms.TextBox();
            this.lbl_Sub_location = new System.Windows.Forms.Label();
            this.lbl_City = new System.Windows.Forms.Label();
            this.lbl_State = new System.Windows.Forms.Label();
            this.btn_Delete = new System.Windows.Forms.Button();
            this.btn_Rename = new System.Windows.Forms.Button();
            this.tbx_GPSAltitude = new System.Windows.Forms.TextBox();
            this.tbx_GPSLongitude = new System.Windows.Forms.TextBox();
            this.tbx_GPSLatitude = new System.Windows.Forms.TextBox();
            this.lbl_GPSAltitude = new System.Windows.Forms.Label();
            this.lbl_GPSLongitude = new System.Windows.Forms.Label();
            this.lbl_GPSLatitude = new System.Windows.Forms.Label();
            this.btn_Save = new System.Windows.Forms.Button();
            this.lbl_Country = new System.Windows.Forms.Label();
            this.cbx_Country = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // cbx_Favourites
            // 
            this.cbx_Favourites.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_Favourites.FormattingEnabled = true;
            this.cbx_Favourites.Location = new System.Drawing.Point(40, 41);
            this.cbx_Favourites.Name = "cbx_Favourites";
            this.cbx_Favourites.Size = new System.Drawing.Size(442, 21);
            this.cbx_Favourites.TabIndex = 0;
            this.cbx_Favourites.SelectedIndexChanged += new System.EventHandler(this.cbx_favouriteName_SelectedIndexChanged);
            // 
            // btn_Close
            // 
            this.btn_Close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Close.Location = new System.Drawing.Point(332, 347);
            this.btn_Close.Name = "btn_Close";
            this.btn_Close.Size = new System.Drawing.Size(150, 23);
            this.btn_Close.TabIndex = 3;
            this.btn_Close.Text = "btn_Close";
            this.btn_Close.UseVisualStyleBackColor = true;
            this.btn_Close.Click += new System.EventHandler(this.btn_Close_Click);
            // 
            // lbl_Favourites
            // 
            this.lbl_Favourites.AutoSize = true;
            this.lbl_Favourites.Location = new System.Drawing.Point(40, 18);
            this.lbl_Favourites.Name = "lbl_Favourites";
            this.lbl_Favourites.Size = new System.Drawing.Size(72, 13);
            this.lbl_Favourites.TabIndex = 4;
            this.lbl_Favourites.Text = "lbl_Favourites";
            // 
            // tbx_Sub_location
            // 
            this.tbx_Sub_location.Location = new System.Drawing.Point(179, 305);
            this.tbx_Sub_location.Name = "tbx_Sub_location";
            this.tbx_Sub_location.Size = new System.Drawing.Size(303, 20);
            this.tbx_Sub_location.TabIndex = 30;
            this.tbx_Sub_location.TextChanged += new System.EventHandler(this.any_tbx_TextChanged);
            // 
            // tbx_City
            // 
            this.tbx_City.Location = new System.Drawing.Point(179, 279);
            this.tbx_City.Name = "tbx_City";
            this.tbx_City.Size = new System.Drawing.Size(303, 20);
            this.tbx_City.TabIndex = 29;
            this.tbx_City.TextChanged += new System.EventHandler(this.any_tbx_TextChanged);
            // 
            // tbx_State
            // 
            this.tbx_State.Location = new System.Drawing.Point(179, 253);
            this.tbx_State.Name = "tbx_State";
            this.tbx_State.Size = new System.Drawing.Size(303, 20);
            this.tbx_State.TabIndex = 28;
            this.tbx_State.TextChanged += new System.EventHandler(this.any_tbx_TextChanged);
            // 
            // lbl_Sub_location
            // 
            this.lbl_Sub_location.AutoSize = true;
            this.lbl_Sub_location.Location = new System.Drawing.Point(34, 308);
            this.lbl_Sub_location.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Sub_location.Name = "lbl_Sub_location";
            this.lbl_Sub_location.Size = new System.Drawing.Size(85, 13);
            this.lbl_Sub_location.TabIndex = 21;
            this.lbl_Sub_location.Text = "lbl_Sub_location";
            // 
            // lbl_City
            // 
            this.lbl_City.AutoSize = true;
            this.lbl_City.Location = new System.Drawing.Point(34, 282);
            this.lbl_City.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_City.Name = "lbl_City";
            this.lbl_City.Size = new System.Drawing.Size(40, 13);
            this.lbl_City.TabIndex = 22;
            this.lbl_City.Text = "lbl_City";
            // 
            // lbl_State
            // 
            this.lbl_State.AutoSize = true;
            this.lbl_State.Location = new System.Drawing.Point(34, 256);
            this.lbl_State.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_State.Name = "lbl_State";
            this.lbl_State.Size = new System.Drawing.Size(48, 13);
            this.lbl_State.TabIndex = 23;
            this.lbl_State.Text = "lbl_State";
            // 
            // btn_Delete
            // 
            this.btn_Delete.Location = new System.Drawing.Point(353, 78);
            this.btn_Delete.Name = "btn_Delete";
            this.btn_Delete.Size = new System.Drawing.Size(105, 23);
            this.btn_Delete.TabIndex = 32;
            this.btn_Delete.Text = "btn_Delete";
            this.btn_Delete.UseVisualStyleBackColor = true;
            this.btn_Delete.Click += new System.EventHandler(this.btn_Delete_Click);
            // 
            // btn_Rename
            // 
            this.btn_Rename.Location = new System.Drawing.Point(214, 78);
            this.btn_Rename.Name = "btn_Rename";
            this.btn_Rename.Size = new System.Drawing.Size(105, 23);
            this.btn_Rename.TabIndex = 33;
            this.btn_Rename.Text = "btn_Rename";
            this.btn_Rename.UseVisualStyleBackColor = true;
            this.btn_Rename.Click += new System.EventHandler(this.btn_Rename_Click);
            // 
            // tbx_GPSAltitude
            // 
            this.tbx_GPSAltitude.Location = new System.Drawing.Point(179, 180);
            this.tbx_GPSAltitude.Name = "tbx_GPSAltitude";
            this.tbx_GPSAltitude.ReadOnly = true;
            this.tbx_GPSAltitude.Size = new System.Drawing.Size(303, 20);
            this.tbx_GPSAltitude.TabIndex = 37;
            // 
            // tbx_GPSLongitude
            // 
            this.tbx_GPSLongitude.Location = new System.Drawing.Point(179, 150);
            this.tbx_GPSLongitude.Name = "tbx_GPSLongitude";
            this.tbx_GPSLongitude.ReadOnly = true;
            this.tbx_GPSLongitude.Size = new System.Drawing.Size(303, 20);
            this.tbx_GPSLongitude.TabIndex = 35;
            // 
            // tbx_GPSLatitude
            // 
            this.tbx_GPSLatitude.Location = new System.Drawing.Point(179, 122);
            this.tbx_GPSLatitude.Name = "tbx_GPSLatitude";
            this.tbx_GPSLatitude.ReadOnly = true;
            this.tbx_GPSLatitude.Size = new System.Drawing.Size(303, 20);
            this.tbx_GPSLatitude.TabIndex = 34;
            // 
            // lbl_GPSAltitude
            // 
            this.lbl_GPSAltitude.AutoSize = true;
            this.lbl_GPSAltitude.Location = new System.Drawing.Point(34, 182);
            this.lbl_GPSAltitude.Name = "lbl_GPSAltitude";
            this.lbl_GPSAltitude.Size = new System.Drawing.Size(80, 13);
            this.lbl_GPSAltitude.TabIndex = 38;
            this.lbl_GPSAltitude.Text = "lbl_GPSAltitude";
            // 
            // lbl_GPSLongitude
            // 
            this.lbl_GPSLongitude.AutoSize = true;
            this.lbl_GPSLongitude.Location = new System.Drawing.Point(34, 153);
            this.lbl_GPSLongitude.Name = "lbl_GPSLongitude";
            this.lbl_GPSLongitude.Size = new System.Drawing.Size(92, 13);
            this.lbl_GPSLongitude.TabIndex = 40;
            this.lbl_GPSLongitude.Text = "lbl_GPSLongitude";
            // 
            // lbl_GPSLatitude
            // 
            this.lbl_GPSLatitude.AutoSize = true;
            this.lbl_GPSLatitude.Location = new System.Drawing.Point(34, 125);
            this.lbl_GPSLatitude.Name = "lbl_GPSLatitude";
            this.lbl_GPSLatitude.Size = new System.Drawing.Size(83, 13);
            this.lbl_GPSLatitude.TabIndex = 41;
            this.lbl_GPSLatitude.Text = "lbl_GPSLatitude";
            // 
            // btn_Save
            // 
            this.btn_Save.Location = new System.Drawing.Point(164, 347);
            this.btn_Save.Name = "btn_Save";
            this.btn_Save.Size = new System.Drawing.Size(150, 23);
            this.btn_Save.TabIndex = 42;
            this.btn_Save.Text = "btn_Save";
            this.btn_Save.UseVisualStyleBackColor = true;
            this.btn_Save.Click += new System.EventHandler(this.btn_Save_Click);
            // 
            // lbl_Country
            // 
            this.lbl_Country.AutoSize = true;
            this.lbl_Country.Location = new System.Drawing.Point(34, 227);
            this.lbl_Country.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lbl_Country.Name = "lbl_Country";
            this.lbl_Country.Size = new System.Drawing.Size(59, 13);
            this.lbl_Country.TabIndex = 43;
            this.lbl_Country.Text = "lbl_Country";
            // 
            // cbx_Country
            // 
            this.cbx_Country.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cbx_Country.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbx_Country.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_Country.FormattingEnabled = true;
            this.cbx_Country.Location = new System.Drawing.Point(179, 224);
            this.cbx_Country.Name = "cbx_Country";
            this.cbx_Country.Size = new System.Drawing.Size(303, 21);
            this.cbx_Country.Sorted = true;
            this.cbx_Country.TabIndex = 44;
            // 
            // FrmManageFavourites
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btn_Close;
            this.ClientSize = new System.Drawing.Size(567, 393);
            this.ControlBox = false;
            this.Controls.Add(this.cbx_Country);
            this.Controls.Add(this.lbl_Country);
            this.Controls.Add(this.btn_Save);
            this.Controls.Add(this.tbx_GPSAltitude);
            this.Controls.Add(this.tbx_GPSLongitude);
            this.Controls.Add(this.tbx_GPSLatitude);
            this.Controls.Add(this.lbl_GPSAltitude);
            this.Controls.Add(this.lbl_GPSLongitude);
            this.Controls.Add(this.lbl_GPSLatitude);
            this.Controls.Add(this.btn_Rename);
            this.Controls.Add(this.btn_Delete);
            this.Controls.Add(this.tbx_Sub_location);
            this.Controls.Add(this.tbx_City);
            this.Controls.Add(this.tbx_State);
            this.Controls.Add(this.lbl_Sub_location);
            this.Controls.Add(this.lbl_City);
            this.Controls.Add(this.lbl_State);
            this.Controls.Add(this.lbl_Favourites);
            this.Controls.Add(this.btn_Close);
            this.Controls.Add(this.cbx_Favourites);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FrmManageFavourites";
            this.ShowInTaskbar = false;
            this.Text = "FrmManageFavourites";
            this.Load += new System.EventHandler(this.FrmManageFavourites_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbx_Favourites;
        private System.Windows.Forms.Button btn_Close;
        private System.Windows.Forms.Label lbl_Favourites;
        internal System.Windows.Forms.TextBox tbx_Sub_location;
        internal System.Windows.Forms.TextBox tbx_City;
        internal System.Windows.Forms.TextBox tbx_State;
        private System.Windows.Forms.Label lbl_Sub_location;
        private System.Windows.Forms.Label lbl_City;
        private System.Windows.Forms.Label lbl_State;
        private System.Windows.Forms.Button btn_Delete;
        private System.Windows.Forms.Button btn_Rename;
        internal System.Windows.Forms.TextBox tbx_GPSAltitude;
        internal System.Windows.Forms.TextBox tbx_GPSLongitude;
        internal System.Windows.Forms.TextBox tbx_GPSLatitude;
        private System.Windows.Forms.Label lbl_GPSAltitude;
        private System.Windows.Forms.Label lbl_GPSLongitude;
        private System.Windows.Forms.Label lbl_GPSLatitude;
        private System.Windows.Forms.Button btn_Save;
        private System.Windows.Forms.Label lbl_Country;
        internal System.Windows.Forms.ComboBox cbx_Country;
    }
}