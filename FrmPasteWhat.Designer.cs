namespace GeoTagNinja
{
    partial class FrmPasteWhat
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmPasteWhat));
            this.btn_Generic_OK = new System.Windows.Forms.Button();
            this.btn_Generic_Cancel = new System.Windows.Forms.Button();
            this.gbx_LocationData = new System.Windows.Forms.GroupBox();
            this.ckb_Sublocation = new System.Windows.Forms.CheckBox();
            this.ckb_State = new System.Windows.Forms.CheckBox();
            this.ckb_Country = new System.Windows.Forms.CheckBox();
            this.btn_LocationData_None = new System.Windows.Forms.Button();
            this.btn_LocationData_All = new System.Windows.Forms.Button();
            this.ckb_City = new System.Windows.Forms.CheckBox();
            this.gbx_GPSData = new System.Windows.Forms.GroupBox();
            this.ckb_GPSDestLongitude = new System.Windows.Forms.CheckBox();
            this.ckb_GPSSpeed = new System.Windows.Forms.CheckBox();
            this.ckb_GPSImgDirection = new System.Windows.Forms.CheckBox();
            this.ckb_GPSDestLatitude = new System.Windows.Forms.CheckBox();
            this.ckb_GPSAltitude = new System.Windows.Forms.CheckBox();
            this.ckb_GPSLongitude = new System.Windows.Forms.CheckBox();
            this.btn_GPSData_None = new System.Windows.Forms.Button();
            this.btn_GPSData_All = new System.Windows.Forms.Button();
            this.ckb_GPSLatitude = new System.Windows.Forms.CheckBox();
            this.gbx_Dates = new System.Windows.Forms.GroupBox();
            this.gbx_CreateDate = new System.Windows.Forms.GroupBox();
            this.ckb_CreateDate = new System.Windows.Forms.CheckBox();
            this.rbt_PasteCreateDateShift = new System.Windows.Forms.RadioButton();
            this.rbt_PasteCreateDateActual = new System.Windows.Forms.RadioButton();
            this.gbx_TakenDate = new System.Windows.Forms.GroupBox();
            this.rbt_PasteTakenDateShift = new System.Windows.Forms.RadioButton();
            this.rbt_PasteTakenDateActual = new System.Windows.Forms.RadioButton();
            this.ckb_TakenDate = new System.Windows.Forms.CheckBox();
            this.ckb_OffsetTime = new System.Windows.Forms.CheckBox();
            this.btn_Dates_None = new System.Windows.Forms.Button();
            this.btn_Dates_All = new System.Windows.Forms.Button();
            this.btn_AllData_None = new System.Windows.Forms.Button();
            this.btn_AllData_All = new System.Windows.Forms.Button();
            this.btn_PullMostRecentPasteSettings = new System.Windows.Forms.Button();
            this.gbx_LocationData.SuspendLayout();
            this.gbx_GPSData.SuspendLayout();
            this.gbx_Dates.SuspendLayout();
            this.gbx_CreateDate.SuspendLayout();
            this.gbx_TakenDate.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_Generic_OK
            // 
            this.btn_Generic_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            resources.ApplyResources(this.btn_Generic_OK, "btn_Generic_OK");
            this.btn_Generic_OK.Name = "btn_Generic_OK";
            this.btn_Generic_OK.UseVisualStyleBackColor = true;
            this.btn_Generic_OK.Click += new System.EventHandler(this.btn_Generic_OK_Click);
            // 
            // btn_Generic_Cancel
            // 
            this.btn_Generic_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            resources.ApplyResources(this.btn_Generic_Cancel, "btn_Generic_Cancel");
            this.btn_Generic_Cancel.Name = "btn_Generic_Cancel";
            this.btn_Generic_Cancel.UseVisualStyleBackColor = true;
            this.btn_Generic_Cancel.Click += new System.EventHandler(this.btn_Generic_Cancel_Click);
            // 
            // gbx_LocationData
            // 
            this.gbx_LocationData.Controls.Add(this.ckb_Sublocation);
            this.gbx_LocationData.Controls.Add(this.ckb_State);
            this.gbx_LocationData.Controls.Add(this.ckb_Country);
            this.gbx_LocationData.Controls.Add(this.btn_LocationData_None);
            this.gbx_LocationData.Controls.Add(this.btn_LocationData_All);
            this.gbx_LocationData.Controls.Add(this.ckb_City);
            resources.ApplyResources(this.gbx_LocationData, "gbx_LocationData");
            this.gbx_LocationData.Name = "gbx_LocationData";
            this.gbx_LocationData.TabStop = false;
            // 
            // ckb_Sublocation
            // 
            resources.ApplyResources(this.ckb_Sublocation, "ckb_Sublocation");
            this.ckb_Sublocation.Name = "ckb_Sublocation";
            this.ckb_Sublocation.UseVisualStyleBackColor = true;
            // 
            // ckb_State
            // 
            resources.ApplyResources(this.ckb_State, "ckb_State");
            this.ckb_State.Name = "ckb_State";
            this.ckb_State.UseVisualStyleBackColor = true;
            // 
            // ckb_Country
            // 
            resources.ApplyResources(this.ckb_Country, "ckb_Country");
            this.ckb_Country.Name = "ckb_Country";
            this.ckb_Country.UseVisualStyleBackColor = true;
            // 
            // btn_LocationData_None
            // 
            resources.ApplyResources(this.btn_LocationData_None, "btn_LocationData_None");
            this.btn_LocationData_None.Name = "btn_LocationData_None";
            this.btn_LocationData_None.UseVisualStyleBackColor = true;
            this.btn_LocationData_None.Click += new System.EventHandler(this.btn_LocationData_None_Click);
            // 
            // btn_LocationData_All
            // 
            resources.ApplyResources(this.btn_LocationData_All, "btn_LocationData_All");
            this.btn_LocationData_All.Name = "btn_LocationData_All";
            this.btn_LocationData_All.UseVisualStyleBackColor = true;
            this.btn_LocationData_All.Click += new System.EventHandler(this.btn_LocationData_All_Click);
            // 
            // ckb_City
            // 
            resources.ApplyResources(this.ckb_City, "ckb_City");
            this.ckb_City.Name = "ckb_City";
            this.ckb_City.UseVisualStyleBackColor = true;
            // 
            // gbx_GPSData
            // 
            this.gbx_GPSData.Controls.Add(this.ckb_GPSDestLongitude);
            this.gbx_GPSData.Controls.Add(this.ckb_GPSSpeed);
            this.gbx_GPSData.Controls.Add(this.ckb_GPSImgDirection);
            this.gbx_GPSData.Controls.Add(this.ckb_GPSDestLatitude);
            this.gbx_GPSData.Controls.Add(this.ckb_GPSAltitude);
            this.gbx_GPSData.Controls.Add(this.ckb_GPSLongitude);
            this.gbx_GPSData.Controls.Add(this.btn_GPSData_None);
            this.gbx_GPSData.Controls.Add(this.btn_GPSData_All);
            this.gbx_GPSData.Controls.Add(this.ckb_GPSLatitude);
            resources.ApplyResources(this.gbx_GPSData, "gbx_GPSData");
            this.gbx_GPSData.Name = "gbx_GPSData";
            this.gbx_GPSData.TabStop = false;
            // 
            // ckb_GPSDestLongitude
            // 
            resources.ApplyResources(this.ckb_GPSDestLongitude, "ckb_GPSDestLongitude");
            this.ckb_GPSDestLongitude.Name = "ckb_GPSDestLongitude";
            this.ckb_GPSDestLongitude.UseVisualStyleBackColor = true;
            // 
            // ckb_GPSSpeed
            // 
            resources.ApplyResources(this.ckb_GPSSpeed, "ckb_GPSSpeed");
            this.ckb_GPSSpeed.Name = "ckb_GPSSpeed";
            this.ckb_GPSSpeed.UseVisualStyleBackColor = true;
            // 
            // ckb_GPSImgDirection
            // 
            resources.ApplyResources(this.ckb_GPSImgDirection, "ckb_GPSImgDirection");
            this.ckb_GPSImgDirection.Name = "ckb_GPSImgDirection";
            this.ckb_GPSImgDirection.UseVisualStyleBackColor = true;
            // 
            // ckb_GPSDestLatitude
            // 
            resources.ApplyResources(this.ckb_GPSDestLatitude, "ckb_GPSDestLatitude");
            this.ckb_GPSDestLatitude.Name = "ckb_GPSDestLatitude";
            this.ckb_GPSDestLatitude.UseVisualStyleBackColor = true;
            // 
            // ckb_GPSAltitude
            // 
            resources.ApplyResources(this.ckb_GPSAltitude, "ckb_GPSAltitude");
            this.ckb_GPSAltitude.Name = "ckb_GPSAltitude";
            this.ckb_GPSAltitude.UseVisualStyleBackColor = true;
            // 
            // ckb_GPSLongitude
            // 
            resources.ApplyResources(this.ckb_GPSLongitude, "ckb_GPSLongitude");
            this.ckb_GPSLongitude.Name = "ckb_GPSLongitude";
            this.ckb_GPSLongitude.UseVisualStyleBackColor = true;
            // 
            // btn_GPSData_None
            // 
            resources.ApplyResources(this.btn_GPSData_None, "btn_GPSData_None");
            this.btn_GPSData_None.Name = "btn_GPSData_None";
            this.btn_GPSData_None.UseVisualStyleBackColor = true;
            this.btn_GPSData_None.Click += new System.EventHandler(this.btn_GPSData_None_Click);
            // 
            // btn_GPSData_All
            // 
            resources.ApplyResources(this.btn_GPSData_All, "btn_GPSData_All");
            this.btn_GPSData_All.Name = "btn_GPSData_All";
            this.btn_GPSData_All.UseVisualStyleBackColor = true;
            this.btn_GPSData_All.Click += new System.EventHandler(this.btn_GPSData_All_Click);
            // 
            // ckb_GPSLatitude
            // 
            resources.ApplyResources(this.ckb_GPSLatitude, "ckb_GPSLatitude");
            this.ckb_GPSLatitude.Name = "ckb_GPSLatitude";
            this.ckb_GPSLatitude.UseVisualStyleBackColor = true;
            // 
            // gbx_Dates
            // 
            this.gbx_Dates.Controls.Add(this.gbx_CreateDate);
            this.gbx_Dates.Controls.Add(this.gbx_TakenDate);
            this.gbx_Dates.Controls.Add(this.ckb_OffsetTime);
            this.gbx_Dates.Controls.Add(this.btn_Dates_None);
            this.gbx_Dates.Controls.Add(this.btn_Dates_All);
            resources.ApplyResources(this.gbx_Dates, "gbx_Dates");
            this.gbx_Dates.Name = "gbx_Dates";
            this.gbx_Dates.TabStop = false;
            // 
            // gbx_CreateDate
            // 
            this.gbx_CreateDate.Controls.Add(this.ckb_CreateDate);
            this.gbx_CreateDate.Controls.Add(this.rbt_PasteCreateDateShift);
            this.gbx_CreateDate.Controls.Add(this.rbt_PasteCreateDateActual);
            resources.ApplyResources(this.gbx_CreateDate, "gbx_CreateDate");
            this.gbx_CreateDate.Name = "gbx_CreateDate";
            this.gbx_CreateDate.TabStop = false;
            // 
            // ckb_CreateDate
            // 
            resources.ApplyResources(this.ckb_CreateDate, "ckb_CreateDate");
            this.ckb_CreateDate.Name = "ckb_CreateDate";
            this.ckb_CreateDate.UseVisualStyleBackColor = true;
            this.ckb_CreateDate.CheckedChanged += new System.EventHandler(this.ckb_CreateDate_CheckedChanged);
            // 
            // rbt_PasteCreateDateShift
            // 
            resources.ApplyResources(this.rbt_PasteCreateDateShift, "rbt_PasteCreateDateShift");
            this.rbt_PasteCreateDateShift.Name = "rbt_PasteCreateDateShift";
            this.rbt_PasteCreateDateShift.TabStop = true;
            this.rbt_PasteCreateDateShift.UseVisualStyleBackColor = true;
            this.rbt_PasteCreateDateShift.CheckedChanged += new System.EventHandler(this.rbt_PasteCreateDateShift_CheckedChanged);
            // 
            // rbt_PasteCreateDateActual
            // 
            resources.ApplyResources(this.rbt_PasteCreateDateActual, "rbt_PasteCreateDateActual");
            this.rbt_PasteCreateDateActual.Name = "rbt_PasteCreateDateActual";
            this.rbt_PasteCreateDateActual.TabStop = true;
            this.rbt_PasteCreateDateActual.UseVisualStyleBackColor = true;
            // 
            // gbx_TakenDate
            // 
            this.gbx_TakenDate.Controls.Add(this.rbt_PasteTakenDateShift);
            this.gbx_TakenDate.Controls.Add(this.rbt_PasteTakenDateActual);
            this.gbx_TakenDate.Controls.Add(this.ckb_TakenDate);
            resources.ApplyResources(this.gbx_TakenDate, "gbx_TakenDate");
            this.gbx_TakenDate.Name = "gbx_TakenDate";
            this.gbx_TakenDate.TabStop = false;
            // 
            // rbt_PasteTakenDateShift
            // 
            resources.ApplyResources(this.rbt_PasteTakenDateShift, "rbt_PasteTakenDateShift");
            this.rbt_PasteTakenDateShift.Name = "rbt_PasteTakenDateShift";
            this.rbt_PasteTakenDateShift.TabStop = true;
            this.rbt_PasteTakenDateShift.UseVisualStyleBackColor = true;
            this.rbt_PasteTakenDateShift.CheckedChanged += new System.EventHandler(this.rbt_PasteTakenDateShift_CheckedChanged);
            // 
            // rbt_PasteTakenDateActual
            // 
            resources.ApplyResources(this.rbt_PasteTakenDateActual, "rbt_PasteTakenDateActual");
            this.rbt_PasteTakenDateActual.Name = "rbt_PasteTakenDateActual";
            this.rbt_PasteTakenDateActual.TabStop = true;
            this.rbt_PasteTakenDateActual.UseVisualStyleBackColor = true;
            // 
            // ckb_TakenDate
            // 
            resources.ApplyResources(this.ckb_TakenDate, "ckb_TakenDate");
            this.ckb_TakenDate.Name = "ckb_TakenDate";
            this.ckb_TakenDate.UseVisualStyleBackColor = true;
            this.ckb_TakenDate.CheckedChanged += new System.EventHandler(this.ckb_TakenDate_CheckedChanged);
            // 
            // ckb_OffsetTime
            // 
            resources.ApplyResources(this.ckb_OffsetTime, "ckb_OffsetTime");
            this.ckb_OffsetTime.Name = "ckb_OffsetTime";
            this.ckb_OffsetTime.UseVisualStyleBackColor = true;
            // 
            // btn_Dates_None
            // 
            resources.ApplyResources(this.btn_Dates_None, "btn_Dates_None");
            this.btn_Dates_None.Name = "btn_Dates_None";
            this.btn_Dates_None.UseVisualStyleBackColor = true;
            this.btn_Dates_None.Click += new System.EventHandler(this.btn_Dates_None_Click);
            // 
            // btn_Dates_All
            // 
            resources.ApplyResources(this.btn_Dates_All, "btn_Dates_All");
            this.btn_Dates_All.Name = "btn_Dates_All";
            this.btn_Dates_All.UseVisualStyleBackColor = true;
            this.btn_Dates_All.Click += new System.EventHandler(this.btn_Dates_All_Click);
            // 
            // btn_AllData_None
            // 
            resources.ApplyResources(this.btn_AllData_None, "btn_AllData_None");
            this.btn_AllData_None.Name = "btn_AllData_None";
            this.btn_AllData_None.UseVisualStyleBackColor = true;
            this.btn_AllData_None.Click += new System.EventHandler(this.btn_AllData_None_Click);
            // 
            // btn_AllData_All
            // 
            resources.ApplyResources(this.btn_AllData_All, "btn_AllData_All");
            this.btn_AllData_All.Name = "btn_AllData_All";
            this.btn_AllData_All.UseVisualStyleBackColor = true;
            this.btn_AllData_All.Click += new System.EventHandler(this.btn_AllData_All_Click);
            // 
            // btn_PullMostRecentPasteSettings
            // 
            resources.ApplyResources(this.btn_PullMostRecentPasteSettings, "btn_PullMostRecentPasteSettings");
            this.btn_PullMostRecentPasteSettings.Name = "btn_PullMostRecentPasteSettings";
            this.btn_PullMostRecentPasteSettings.UseVisualStyleBackColor = true;
            this.btn_PullMostRecentPasteSettings.Click += new System.EventHandler(this.btn_PullMostRecentPasteSettings_Click);
            // 
            // FrmPasteWhat
            // 
            this.AcceptButton = this.btn_Generic_OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btn_Generic_Cancel;
            this.ControlBox = false;
            this.Controls.Add(this.btn_PullMostRecentPasteSettings);
            this.Controls.Add(this.btn_AllData_None);
            this.Controls.Add(this.btn_AllData_All);
            this.Controls.Add(this.gbx_Dates);
            this.Controls.Add(this.gbx_GPSData);
            this.Controls.Add(this.gbx_LocationData);
            this.Controls.Add(this.btn_Generic_OK);
            this.Controls.Add(this.btn_Generic_Cancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MinimizeBox = false;
            this.Name = "FrmPasteWhat";
            this.ShowInTaskbar = false;
            this.Load += new System.EventHandler(this.FrmPasteWhat_Load);
            this.gbx_LocationData.ResumeLayout(false);
            this.gbx_LocationData.PerformLayout();
            this.gbx_GPSData.ResumeLayout(false);
            this.gbx_GPSData.PerformLayout();
            this.gbx_Dates.ResumeLayout(false);
            this.gbx_Dates.PerformLayout();
            this.gbx_CreateDate.ResumeLayout(false);
            this.gbx_CreateDate.PerformLayout();
            this.gbx_TakenDate.ResumeLayout(false);
            this.gbx_TakenDate.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn_Generic_OK;
        private System.Windows.Forms.Button btn_Generic_Cancel;
        private System.Windows.Forms.GroupBox gbx_LocationData;
        private System.Windows.Forms.Button btn_LocationData_None;
        private System.Windows.Forms.Button btn_LocationData_All;
        private System.Windows.Forms.CheckBox ckb_City;
        private System.Windows.Forms.GroupBox gbx_GPSData;
        private System.Windows.Forms.Button btn_GPSData_None;
        private System.Windows.Forms.Button btn_GPSData_All;
        private System.Windows.Forms.CheckBox ckb_GPSLatitude;
        private System.Windows.Forms.CheckBox ckb_Sublocation;
        private System.Windows.Forms.CheckBox ckb_State;
        private System.Windows.Forms.CheckBox ckb_Country;
        private System.Windows.Forms.CheckBox ckb_GPSSpeed;
        private System.Windows.Forms.CheckBox ckb_GPSImgDirection;
        private System.Windows.Forms.CheckBox ckb_GPSDestLatitude;
        private System.Windows.Forms.CheckBox ckb_GPSAltitude;
        private System.Windows.Forms.CheckBox ckb_GPSLongitude;
        private System.Windows.Forms.CheckBox ckb_GPSDestLongitude;
        private System.Windows.Forms.GroupBox gbx_Dates;
        private System.Windows.Forms.Button btn_Dates_None;
        private System.Windows.Forms.Button btn_Dates_All;
        private System.Windows.Forms.CheckBox ckb_OffsetTime;
        private System.Windows.Forms.GroupBox gbx_CreateDate;
        private System.Windows.Forms.CheckBox ckb_CreateDate;
        private System.Windows.Forms.RadioButton rbt_PasteCreateDateShift;
        private System.Windows.Forms.RadioButton rbt_PasteCreateDateActual;
        private System.Windows.Forms.GroupBox gbx_TakenDate;
        private System.Windows.Forms.RadioButton rbt_PasteTakenDateShift;
        private System.Windows.Forms.RadioButton rbt_PasteTakenDateActual;
        private System.Windows.Forms.CheckBox ckb_TakenDate;
        private System.Windows.Forms.Button btn_AllData_None;
        private System.Windows.Forms.Button btn_AllData_All;
        private System.Windows.Forms.Button btn_PullMostRecentPasteSettings;
    }
}