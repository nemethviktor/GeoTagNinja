namespace GeoTagNinja
{
    partial class FrmCustomiseFormControls
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
            this.cbx_ControlItem = new System.Windows.Forms.ComboBox();
            this.pgr_Main = new System.Windows.Forms.PropertyGrid();
            this.lbl_ControlName = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cbx_ControlItem
            // 
            this.cbx_ControlItem.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbx_ControlItem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbx_ControlItem.Location = new System.Drawing.Point(13, 12);
            this.cbx_ControlItem.Name = "cbx_ControlItem";
            this.cbx_ControlItem.Size = new System.Drawing.Size(499, 21);
            this.cbx_ControlItem.TabIndex = 0;
            this.cbx_ControlItem.DropDown += new System.EventHandler(this.cbx_ControlItem_DropDown);
            this.cbx_ControlItem.SelectedIndexChanged += new System.EventHandler(this.cbx_ControlItem_SelectedIndexChanged);
            // 
            // pgr_Main
            // 
            this.pgr_Main.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pgr_Main.Location = new System.Drawing.Point(13, 71);
            this.pgr_Main.Name = "pgr_Main";
            this.pgr_Main.Size = new System.Drawing.Size(499, 367);
            this.pgr_Main.TabIndex = 1;
            // 
            // lbl_ControlName
            // 
            this.lbl_ControlName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_ControlName.Location = new System.Drawing.Point(13, 40);
            this.lbl_ControlName.Name = "lbl_ControlName";
            this.lbl_ControlName.Size = new System.Drawing.Size(499, 18);
            this.lbl_ControlName.TabIndex = 2;
            this.lbl_ControlName.Text = "(item name here)";
            // 
            // FrmCustomiseFormControls
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(524, 450);
            this.Controls.Add(this.lbl_ControlName);
            this.Controls.Add(this.pgr_Main);
            this.Controls.Add(this.cbx_ControlItem);
            this.Name = "FrmCustomiseFormControls";
            this.Text = "frmCustomiseFormControls";
            this.Load += new System.EventHandler(this.FrmCustomiseFormControls_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cbx_ControlItem;
        private System.Windows.Forms.PropertyGrid pgr_Main;
        private System.Windows.Forms.Label lbl_ControlName;
    }
}