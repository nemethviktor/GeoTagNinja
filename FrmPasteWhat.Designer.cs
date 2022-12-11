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
            this.btn_OK = new System.Windows.Forms.Button();
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.gbx_LocationData = new System.Windows.Forms.GroupBox();
            this.ckb_Sub_location = new System.Windows.Forms.CheckBox();
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
            this.gbx_LocationData.SuspendLayout();
            this.gbx_GPSData.SuspendLayout();
            this.gbx_Dates.SuspendLayout();
            this.gbx_CreateDate.SuspendLayout();
            this.gbx_TakenDate.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_OK
            // 
            this.btn_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btn_OK.Location = new System.Drawing.Point(627, 607);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(75, 23);
            this.btn_OK.TabIndex = 16;
            this.btn_OK.Text = "btn_OK";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_OK_Click);
            // 
            // btn_Cancel
            // 
            this.btn_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Cancel.Location = new System.Drawing.Point(708, 607);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(75, 23);
            this.btn_Cancel.TabIndex = 17;
            this.btn_Cancel.Text = "btn_Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = true;
            this.btn_Cancel.Click += new System.EventHandler(this.btn_Cancel_Click);
            // 
            // gbx_LocationData
            // 
            this.gbx_LocationData.Controls.Add(this.ckb_Sub_location);
            this.gbx_LocationData.Controls.Add(this.ckb_State);
            this.gbx_LocationData.Controls.Add(this.ckb_Country);
            this.gbx_LocationData.Controls.Add(this.btn_LocationData_None);
            this.gbx_LocationData.Controls.Add(this.btn_LocationData_All);
            this.gbx_LocationData.Controls.Add(this.ckb_City);
            this.gbx_LocationData.Location = new System.Drawing.Point(26, 383);
            this.gbx_LocationData.Name = "gbx_LocationData";
            this.gbx_LocationData.Size = new System.Drawing.Size(386, 199);
            this.gbx_LocationData.TabIndex = 18;
            this.gbx_LocationData.TabStop = false;
            this.gbx_LocationData.Text = "gbx_LocationData";
            // 
            // ckb_Sub_location
            // 
            this.ckb_Sub_location.AutoSize = true;
            this.ckb_Sub_location.Location = new System.Drawing.Point(25, 131);
            this.ckb_Sub_location.Name = "ckb_Sub_location";
            this.ckb_Sub_location.Size = new System.Drawing.Size(112, 17);
            this.ckb_Sub_location.TabIndex = 7;
            this.ckb_Sub_location.Text = "ckb_Sub_location";
            this.ckb_Sub_location.UseVisualStyleBackColor = true;
            // 
            // ckb_State
            // 
            this.ckb_State.AutoSize = true;
            this.ckb_State.Location = new System.Drawing.Point(25, 98);
            this.ckb_State.Name = "ckb_State";
            this.ckb_State.Size = new System.Drawing.Size(75, 17);
            this.ckb_State.TabIndex = 6;
            this.ckb_State.Text = "ckb_State";
            this.ckb_State.UseVisualStyleBackColor = true;
            // 
            // ckb_Country
            // 
            this.ckb_Country.AutoSize = true;
            this.ckb_Country.Location = new System.Drawing.Point(25, 65);
            this.ckb_Country.Name = "ckb_Country";
            this.ckb_Country.Size = new System.Drawing.Size(86, 17);
            this.ckb_Country.TabIndex = 5;
            this.ckb_Country.Text = "ckb_Country";
            this.ckb_Country.UseVisualStyleBackColor = true;
            // 
            // btn_LocationData_None
            // 
            this.btn_LocationData_None.Location = new System.Drawing.Point(272, 158);
            this.btn_LocationData_None.Name = "btn_LocationData_None";
            this.btn_LocationData_None.Size = new System.Drawing.Size(75, 23);
            this.btn_LocationData_None.TabIndex = 4;
            this.btn_LocationData_None.Text = "btn_LocationData_None";
            this.btn_LocationData_None.UseVisualStyleBackColor = true;
            this.btn_LocationData_None.Click += new System.EventHandler(this.btn_LocationData_None_Click);
            // 
            // btn_LocationData_All
            // 
            this.btn_LocationData_All.Location = new System.Drawing.Point(191, 158);
            this.btn_LocationData_All.Name = "btn_LocationData_All";
            this.btn_LocationData_All.Size = new System.Drawing.Size(75, 23);
            this.btn_LocationData_All.TabIndex = 3;
            this.btn_LocationData_All.Text = "btn_LocationData_All";
            this.btn_LocationData_All.UseVisualStyleBackColor = true;
            this.btn_LocationData_All.Click += new System.EventHandler(this.btn_LocationData_All_Click);
            // 
            // ckb_City
            // 
            this.ckb_City.AutoSize = true;
            this.ckb_City.Location = new System.Drawing.Point(25, 32);
            this.ckb_City.Name = "ckb_City";
            this.ckb_City.Size = new System.Drawing.Size(67, 17);
            this.ckb_City.TabIndex = 0;
            this.ckb_City.Text = "ckb_City";
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
            this.gbx_GPSData.Location = new System.Drawing.Point(26, 30);
            this.gbx_GPSData.Name = "gbx_GPSData";
            this.gbx_GPSData.Size = new System.Drawing.Size(386, 347);
            this.gbx_GPSData.TabIndex = 19;
            this.gbx_GPSData.TabStop = false;
            this.gbx_GPSData.Text = "gbx_GPSData";
            // 
            // ckb_GPSDestLongitude
            // 
            this.ckb_GPSDestLongitude.AutoSize = true;
            this.ckb_GPSDestLongitude.Location = new System.Drawing.Point(25, 196);
            this.ckb_GPSDestLongitude.Name = "ckb_GPSDestLongitude";
            this.ckb_GPSDestLongitude.Size = new System.Drawing.Size(141, 17);
            this.ckb_GPSDestLongitude.TabIndex = 9;
            this.ckb_GPSDestLongitude.Text = "ckb_GPSDestLongitude";
            this.ckb_GPSDestLongitude.UseVisualStyleBackColor = true;
            // 
            // ckb_GPSSpeed
            // 
            this.ckb_GPSSpeed.AutoSize = true;
            this.ckb_GPSSpeed.Location = new System.Drawing.Point(25, 278);
            this.ckb_GPSSpeed.Name = "ckb_GPSSpeed";
            this.ckb_GPSSpeed.Size = new System.Drawing.Size(103, 17);
            this.ckb_GPSSpeed.TabIndex = 8;
            this.ckb_GPSSpeed.Text = "ckb_GPSSpeed";
            this.ckb_GPSSpeed.UseVisualStyleBackColor = true;
            // 
            // ckb_GPSImgDirection
            // 
            this.ckb_GPSImgDirection.AutoSize = true;
            this.ckb_GPSImgDirection.Location = new System.Drawing.Point(25, 237);
            this.ckb_GPSImgDirection.Name = "ckb_GPSImgDirection";
            this.ckb_GPSImgDirection.Size = new System.Drawing.Size(131, 17);
            this.ckb_GPSImgDirection.TabIndex = 6;
            this.ckb_GPSImgDirection.Text = "ckb_GPSImgDirection";
            this.ckb_GPSImgDirection.UseVisualStyleBackColor = true;
            // 
            // ckb_GPSDestLatitude
            // 
            this.ckb_GPSDestLatitude.AutoSize = true;
            this.ckb_GPSDestLatitude.Location = new System.Drawing.Point(25, 155);
            this.ckb_GPSDestLatitude.Name = "ckb_GPSDestLatitude";
            this.ckb_GPSDestLatitude.Size = new System.Drawing.Size(132, 17);
            this.ckb_GPSDestLatitude.TabIndex = 5;
            this.ckb_GPSDestLatitude.Text = "ckb_GPSDestLatitude";
            this.ckb_GPSDestLatitude.UseVisualStyleBackColor = true;
            // 
            // ckb_GPSAltitude
            // 
            this.ckb_GPSAltitude.AutoSize = true;
            this.ckb_GPSAltitude.Location = new System.Drawing.Point(25, 114);
            this.ckb_GPSAltitude.Name = "ckb_GPSAltitude";
            this.ckb_GPSAltitude.Size = new System.Drawing.Size(107, 17);
            this.ckb_GPSAltitude.TabIndex = 4;
            this.ckb_GPSAltitude.Text = "ckb_GPSAltitude";
            this.ckb_GPSAltitude.UseVisualStyleBackColor = true;
            // 
            // ckb_GPSLongitude
            // 
            this.ckb_GPSLongitude.AutoSize = true;
            this.ckb_GPSLongitude.Location = new System.Drawing.Point(25, 73);
            this.ckb_GPSLongitude.Name = "ckb_GPSLongitude";
            this.ckb_GPSLongitude.Size = new System.Drawing.Size(119, 17);
            this.ckb_GPSLongitude.TabIndex = 3;
            this.ckb_GPSLongitude.Text = "ckb_GPSLongitude";
            this.ckb_GPSLongitude.UseVisualStyleBackColor = true;
            // 
            // btn_GPSData_None
            // 
            this.btn_GPSData_None.Location = new System.Drawing.Point(272, 305);
            this.btn_GPSData_None.Name = "btn_GPSData_None";
            this.btn_GPSData_None.Size = new System.Drawing.Size(75, 23);
            this.btn_GPSData_None.TabIndex = 2;
            this.btn_GPSData_None.Text = "btn_GPSData_None";
            this.btn_GPSData_None.UseVisualStyleBackColor = true;
            this.btn_GPSData_None.Click += new System.EventHandler(this.btn_GPSData_None_Click);
            // 
            // btn_GPSData_All
            // 
            this.btn_GPSData_All.Location = new System.Drawing.Point(191, 305);
            this.btn_GPSData_All.Name = "btn_GPSData_All";
            this.btn_GPSData_All.Size = new System.Drawing.Size(75, 23);
            this.btn_GPSData_All.TabIndex = 1;
            this.btn_GPSData_All.Text = "btn_GPSData_All";
            this.btn_GPSData_All.UseVisualStyleBackColor = true;
            this.btn_GPSData_All.Click += new System.EventHandler(this.btn_GPSData_All_Click);
            // 
            // ckb_GPSLatitude
            // 
            this.ckb_GPSLatitude.AutoSize = true;
            this.ckb_GPSLatitude.Location = new System.Drawing.Point(25, 32);
            this.ckb_GPSLatitude.Name = "ckb_GPSLatitude";
            this.ckb_GPSLatitude.Size = new System.Drawing.Size(110, 17);
            this.ckb_GPSLatitude.TabIndex = 0;
            this.ckb_GPSLatitude.Text = "ckb_GPSLatitude";
            this.ckb_GPSLatitude.UseVisualStyleBackColor = true;
            // 
            // gbx_Dates
            // 
            this.gbx_Dates.Controls.Add(this.gbx_CreateDate);
            this.gbx_Dates.Controls.Add(this.gbx_TakenDate);
            this.gbx_Dates.Controls.Add(this.ckb_OffsetTime);
            this.gbx_Dates.Controls.Add(this.btn_Dates_None);
            this.gbx_Dates.Controls.Add(this.btn_Dates_All);
            this.gbx_Dates.Location = new System.Drawing.Point(436, 30);
            this.gbx_Dates.Name = "gbx_Dates";
            this.gbx_Dates.Size = new System.Drawing.Size(386, 347);
            this.gbx_Dates.TabIndex = 19;
            this.gbx_Dates.TabStop = false;
            this.gbx_Dates.Text = "gbx_Dates";
            // 
            // gbx_CreateDate
            // 
            this.gbx_CreateDate.Controls.Add(this.ckb_CreateDate);
            this.gbx_CreateDate.Controls.Add(this.rbt_PasteCreateDateShift);
            this.gbx_CreateDate.Controls.Add(this.rbt_PasteCreateDateActual);
            this.gbx_CreateDate.Location = new System.Drawing.Point(14, 140);
            this.gbx_CreateDate.Name = "gbx_CreateDate";
            this.gbx_CreateDate.Size = new System.Drawing.Size(333, 115);
            this.gbx_CreateDate.TabIndex = 24;
            this.gbx_CreateDate.TabStop = false;
            this.gbx_CreateDate.Text = "gbx_CreateDate";
            // 
            // ckb_CreateDate
            // 
            this.ckb_CreateDate.AutoSize = true;
            this.ckb_CreateDate.Location = new System.Drawing.Point(11, 29);
            this.ckb_CreateDate.Name = "ckb_CreateDate";
            this.ckb_CreateDate.Size = new System.Drawing.Size(104, 17);
            this.ckb_CreateDate.TabIndex = 14;
            this.ckb_CreateDate.Text = "ckb_CreateDate";
            this.ckb_CreateDate.UseVisualStyleBackColor = true;
            this.ckb_CreateDate.CheckedChanged += new System.EventHandler(this.ckb_CreateDate_CheckedChanged);
            // 
            // rbt_PasteCreateDateShift
            // 
            this.rbt_PasteCreateDateShift.AutoSize = true;
            this.rbt_PasteCreateDateShift.Location = new System.Drawing.Point(41, 75);
            this.rbt_PasteCreateDateShift.Name = "rbt_PasteCreateDateShift";
            this.rbt_PasteCreateDateShift.Size = new System.Drawing.Size(145, 17);
            this.rbt_PasteCreateDateShift.TabIndex = 13;
            this.rbt_PasteCreateDateShift.TabStop = true;
            this.rbt_PasteCreateDateShift.Text = "rbt_PasteCreateDateShift";
            this.rbt_PasteCreateDateShift.UseVisualStyleBackColor = true;
            this.rbt_PasteCreateDateShift.CheckedChanged += new System.EventHandler(this.rbt_PasteCreateDateShift_CheckedChanged);
            // 
            // rbt_PasteCreateDateActual
            // 
            this.rbt_PasteCreateDateActual.AutoSize = true;
            this.rbt_PasteCreateDateActual.Location = new System.Drawing.Point(41, 52);
            this.rbt_PasteCreateDateActual.Name = "rbt_PasteCreateDateActual";
            this.rbt_PasteCreateDateActual.Size = new System.Drawing.Size(154, 17);
            this.rbt_PasteCreateDateActual.TabIndex = 12;
            this.rbt_PasteCreateDateActual.TabStop = true;
            this.rbt_PasteCreateDateActual.Text = "rbt_PasteCreateDateActual";
            this.rbt_PasteCreateDateActual.UseVisualStyleBackColor = true;
            // 
            // gbx_TakenDate
            // 
            this.gbx_TakenDate.Controls.Add(this.rbt_PasteTakenDateShift);
            this.gbx_TakenDate.Controls.Add(this.rbt_PasteTakenDateActual);
            this.gbx_TakenDate.Controls.Add(this.ckb_TakenDate);
            this.gbx_TakenDate.Location = new System.Drawing.Point(14, 19);
            this.gbx_TakenDate.Name = "gbx_TakenDate";
            this.gbx_TakenDate.Size = new System.Drawing.Size(333, 115);
            this.gbx_TakenDate.TabIndex = 23;
            this.gbx_TakenDate.TabStop = false;
            this.gbx_TakenDate.Text = "gbx_TakenDate";
            // 
            // rbt_PasteTakenDateShift
            // 
            this.rbt_PasteTakenDateShift.AutoSize = true;
            this.rbt_PasteTakenDateShift.Location = new System.Drawing.Point(41, 78);
            this.rbt_PasteTakenDateShift.Name = "rbt_PasteTakenDateShift";
            this.rbt_PasteTakenDateShift.Size = new System.Drawing.Size(145, 17);
            this.rbt_PasteTakenDateShift.TabIndex = 12;
            this.rbt_PasteTakenDateShift.TabStop = true;
            this.rbt_PasteTakenDateShift.Text = "rbt_PasteTakenDateShift";
            this.rbt_PasteTakenDateShift.UseVisualStyleBackColor = true;
            this.rbt_PasteTakenDateShift.CheckedChanged += new System.EventHandler(this.rbt_PasteTakenDateShift_CheckedChanged);
            // 
            // rbt_PasteTakenDateActual
            // 
            this.rbt_PasteTakenDateActual.AutoSize = true;
            this.rbt_PasteTakenDateActual.Location = new System.Drawing.Point(41, 55);
            this.rbt_PasteTakenDateActual.Name = "rbt_PasteTakenDateActual";
            this.rbt_PasteTakenDateActual.Size = new System.Drawing.Size(154, 17);
            this.rbt_PasteTakenDateActual.TabIndex = 11;
            this.rbt_PasteTakenDateActual.TabStop = true;
            this.rbt_PasteTakenDateActual.Text = "rbt_PasteTakenDateActual";
            this.rbt_PasteTakenDateActual.UseVisualStyleBackColor = true;
            // 
            // ckb_TakenDate
            // 
            this.ckb_TakenDate.AutoSize = true;
            this.ckb_TakenDate.Location = new System.Drawing.Point(11, 32);
            this.ckb_TakenDate.Name = "ckb_TakenDate";
            this.ckb_TakenDate.Size = new System.Drawing.Size(104, 17);
            this.ckb_TakenDate.TabIndex = 9;
            this.ckb_TakenDate.Text = "ckb_TakenDate";
            this.ckb_TakenDate.UseVisualStyleBackColor = true;
            this.ckb_TakenDate.CheckedChanged += new System.EventHandler(this.ckb_TakenDate_CheckedChanged);
            // 
            // ckb_OffsetTime
            // 
            this.ckb_OffsetTime.AutoSize = true;
            this.ckb_OffsetTime.Location = new System.Drawing.Point(25, 270);
            this.ckb_OffsetTime.Name = "ckb_OffsetTime";
            this.ckb_OffsetTime.Size = new System.Drawing.Size(101, 17);
            this.ckb_OffsetTime.TabIndex = 6;
            this.ckb_OffsetTime.Text = "ckb_OffsetTime";
            this.ckb_OffsetTime.UseVisualStyleBackColor = true;
            // 
            // btn_Dates_None
            // 
            this.btn_Dates_None.Location = new System.Drawing.Point(272, 305);
            this.btn_Dates_None.Name = "btn_Dates_None";
            this.btn_Dates_None.Size = new System.Drawing.Size(75, 23);
            this.btn_Dates_None.TabIndex = 4;
            this.btn_Dates_None.Text = "btn_Dates_None";
            this.btn_Dates_None.UseVisualStyleBackColor = true;
            this.btn_Dates_None.Click += new System.EventHandler(this.btn_Dates_None_Click);
            // 
            // btn_Dates_All
            // 
            this.btn_Dates_All.Location = new System.Drawing.Point(191, 305);
            this.btn_Dates_All.Name = "btn_Dates_All";
            this.btn_Dates_All.Size = new System.Drawing.Size(75, 23);
            this.btn_Dates_All.TabIndex = 3;
            this.btn_Dates_All.Text = "btn_Dates_All";
            this.btn_Dates_All.UseVisualStyleBackColor = true;
            this.btn_Dates_All.Click += new System.EventHandler(this.btn_Dates_All_Click);
            // 
            // FrmPasteWhat
            // 
            this.AcceptButton = this.btn_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.btn_Cancel;
            this.ClientSize = new System.Drawing.Size(844, 642);
            this.Controls.Add(this.gbx_Dates);
            this.Controls.Add(this.gbx_GPSData);
            this.Controls.Add(this.gbx_LocationData);
            this.Controls.Add(this.btn_OK);
            this.Controls.Add(this.btn_Cancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimizeBox = false;
            this.Name = "FrmPasteWhat";
            this.ShowInTaskbar = false;
            this.Text = "FrmPasteWhat";
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

        private System.Windows.Forms.Button btn_OK;
        private System.Windows.Forms.Button btn_Cancel;
        private System.Windows.Forms.GroupBox gbx_LocationData;
        private System.Windows.Forms.Button btn_LocationData_None;
        private System.Windows.Forms.Button btn_LocationData_All;
        private System.Windows.Forms.CheckBox ckb_City;
        private System.Windows.Forms.GroupBox gbx_GPSData;
        private System.Windows.Forms.Button btn_GPSData_None;
        private System.Windows.Forms.Button btn_GPSData_All;
        private System.Windows.Forms.CheckBox ckb_GPSLatitude;
        private System.Windows.Forms.CheckBox ckb_Sub_location;
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
    }
}