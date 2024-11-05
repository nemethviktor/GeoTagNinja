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
            this.tbx_Sublocation = new System.Windows.Forms.TextBox();
            this.tbx_City = new System.Windows.Forms.TextBox();
            this.tbx_State = new System.Windows.Forms.TextBox();
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
            this.lbl_Sublocation = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cbx_Favourites
            // 
            this.cbx_Favourites.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_Favourites.FormattingEnabled = true;
            resources.ApplyResources(this.cbx_Favourites, "cbx_Favourites");
            this.cbx_Favourites.Name = "cbx_Favourites";
            this.cbx_Favourites.SelectedIndexChanged += new System.EventHandler(this.cbx_favouriteName_SelectedIndexChanged);
            // 
            // btn_Close
            // 
            this.btn_Close.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.btn_Close, "btn_Close");
            this.btn_Close.Name = "btn_Close";
            this.btn_Close.UseVisualStyleBackColor = true;
            this.btn_Close.Click += new System.EventHandler(this.btn_Close_Click);
            // 
            // lbl_Favourites
            // 
            resources.ApplyResources(this.lbl_Favourites, "lbl_Favourites");
            this.lbl_Favourites.Name = "lbl_Favourites";
            // 
            // tbx_Sublocation
            // 
            resources.ApplyResources(this.tbx_Sublocation, "tbx_Sublocation");
            this.tbx_Sublocation.Name = "tbx_Sublocation";
            this.tbx_Sublocation.TextChanged += new System.EventHandler(this.any_tbx_TextChanged);
            // 
            // tbx_City
            // 
            resources.ApplyResources(this.tbx_City, "tbx_City");
            this.tbx_City.Name = "tbx_City";
            this.tbx_City.TextChanged += new System.EventHandler(this.any_tbx_TextChanged);
            // 
            // tbx_State
            // 
            resources.ApplyResources(this.tbx_State, "tbx_State");
            this.tbx_State.Name = "tbx_State";
            this.tbx_State.TextChanged += new System.EventHandler(this.any_tbx_TextChanged);
            // 
            // lbl_City
            // 
            resources.ApplyResources(this.lbl_City, "lbl_City");
            this.lbl_City.Name = "lbl_City";
            // 
            // lbl_State
            // 
            resources.ApplyResources(this.lbl_State, "lbl_State");
            this.lbl_State.Name = "lbl_State";
            // 
            // btn_Delete
            // 
            resources.ApplyResources(this.btn_Delete, "btn_Delete");
            this.btn_Delete.Name = "btn_Delete";
            this.btn_Delete.UseVisualStyleBackColor = true;
            this.btn_Delete.Click += new System.EventHandler(this.btn_Delete_Click);
            // 
            // btn_Rename
            // 
            resources.ApplyResources(this.btn_Rename, "btn_Rename");
            this.btn_Rename.Name = "btn_Rename";
            this.btn_Rename.UseVisualStyleBackColor = true;
            this.btn_Rename.Click += new System.EventHandler(this.btn_Rename_Click);
            // 
            // tbx_GPSAltitude
            // 
            resources.ApplyResources(this.tbx_GPSAltitude, "tbx_GPSAltitude");
            this.tbx_GPSAltitude.Name = "tbx_GPSAltitude";
            this.tbx_GPSAltitude.ReadOnly = true;
            // 
            // tbx_GPSLongitude
            // 
            resources.ApplyResources(this.tbx_GPSLongitude, "tbx_GPSLongitude");
            this.tbx_GPSLongitude.Name = "tbx_GPSLongitude";
            this.tbx_GPSLongitude.ReadOnly = true;
            // 
            // tbx_GPSLatitude
            // 
            resources.ApplyResources(this.tbx_GPSLatitude, "tbx_GPSLatitude");
            this.tbx_GPSLatitude.Name = "tbx_GPSLatitude";
            this.tbx_GPSLatitude.ReadOnly = true;
            // 
            // lbl_GPSAltitude
            // 
            resources.ApplyResources(this.lbl_GPSAltitude, "lbl_GPSAltitude");
            this.lbl_GPSAltitude.Name = "lbl_GPSAltitude";
            // 
            // lbl_GPSLongitude
            // 
            resources.ApplyResources(this.lbl_GPSLongitude, "lbl_GPSLongitude");
            this.lbl_GPSLongitude.Name = "lbl_GPSLongitude";
            // 
            // lbl_GPSLatitude
            // 
            resources.ApplyResources(this.lbl_GPSLatitude, "lbl_GPSLatitude");
            this.lbl_GPSLatitude.Name = "lbl_GPSLatitude";
            // 
            // btn_Save
            // 
            resources.ApplyResources(this.btn_Save, "btn_Save");
            this.btn_Save.Name = "btn_Save";
            this.btn_Save.UseVisualStyleBackColor = true;
            this.btn_Save.Click += new System.EventHandler(this.btn_Save_Click);
            // 
            // lbl_Country
            // 
            resources.ApplyResources(this.lbl_Country, "lbl_Country");
            this.lbl_Country.Name = "lbl_Country";
            // 
            // cbx_Country
            // 
            this.cbx_Country.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.cbx_Country.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
            this.cbx_Country.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_Country.FormattingEnabled = true;
            resources.ApplyResources(this.cbx_Country, "cbx_Country");
            this.cbx_Country.Name = "cbx_Country";
            this.cbx_Country.Sorted = true;
            // 
            // lbl_Sublocation
            // 
            resources.ApplyResources(this.lbl_Sublocation, "lbl_Sublocation");
            this.lbl_Sublocation.Name = "lbl_Sublocation";
            // 
            // FrmManageFavourites
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btn_Close;
            this.ControlBox = false;
            this.Controls.Add(this.lbl_Sublocation);
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
            this.Controls.Add(this.tbx_Sublocation);
            this.Controls.Add(this.tbx_City);
            this.Controls.Add(this.tbx_State);
            this.Controls.Add(this.lbl_City);
            this.Controls.Add(this.lbl_State);
            this.Controls.Add(this.lbl_Favourites);
            this.Controls.Add(this.btn_Close);
            this.Controls.Add(this.cbx_Favourites);
            this.Name = "FrmManageFavourites";
            this.ShowInTaskbar = false;
            this.Load += new System.EventHandler(this.FrmManageFavourites_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox cbx_Favourites;
        private System.Windows.Forms.Button btn_Close;
        private System.Windows.Forms.Label lbl_Favourites;
        internal System.Windows.Forms.TextBox tbx_Sublocation;
        internal System.Windows.Forms.TextBox tbx_City;
        internal System.Windows.Forms.TextBox tbx_State;
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
        private System.Windows.Forms.Label lbl_Sublocation;
    }
}