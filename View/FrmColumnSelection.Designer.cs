namespace GeoTagNinja.View
{
    partial class FrmColumnSelection
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
            this.clb_ColList = new System.Windows.Forms.CheckedListBox();
            this.lbl_SelectColsTitle = new System.Windows.Forms.Label();
            this.ckb_DeSelectAll = new System.Windows.Forms.CheckBox();
            this.btn_Cancel = new System.Windows.Forms.Button();
            this.tlp_Buttons = new System.Windows.Forms.TableLayoutPanel();
            this.btn_OK = new System.Windows.Forms.Button();
            this.tlp_Buttons.SuspendLayout();
            this.SuspendLayout();
            // 
            // clb_ColList
            // 
            this.clb_ColList.CheckOnClick = true;
            this.clb_ColList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.clb_ColList.FormattingEnabled = true;
            this.clb_ColList.Location = new System.Drawing.Point(5, 42);
            this.clb_ColList.Name = "clb_ColList";
            this.clb_ColList.Size = new System.Drawing.Size(182, 222);
            this.clb_ColList.TabIndex = 0;
            // 
            // lbl_SelectColsTitle
            // 
            this.lbl_SelectColsTitle.AutoSize = true;
            this.lbl_SelectColsTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbl_SelectColsTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbl_SelectColsTitle.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lbl_SelectColsTitle.Location = new System.Drawing.Point(5, 5);
            this.lbl_SelectColsTitle.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.lbl_SelectColsTitle.MinimumSize = new System.Drawing.Size(0, 20);
            this.lbl_SelectColsTitle.Name = "lbl_SelectColsTitle";
            this.lbl_SelectColsTitle.Size = new System.Drawing.Size(107, 20);
            this.lbl_SelectColsTitle.TabIndex = 1;
            this.lbl_SelectColsTitle.Text = "Select Columns";
            this.lbl_SelectColsTitle.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // ckb_DeSelectAll
            // 
            this.ckb_DeSelectAll.AutoSize = true;
            this.ckb_DeSelectAll.CausesValidation = false;
            this.ckb_DeSelectAll.Dock = System.Windows.Forms.DockStyle.Top;
            this.ckb_DeSelectAll.Location = new System.Drawing.Point(5, 25);
            this.ckb_DeSelectAll.Name = "ckb_DeSelectAll";
            this.ckb_DeSelectAll.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.ckb_DeSelectAll.Size = new System.Drawing.Size(182, 17);
            this.ckb_DeSelectAll.TabIndex = 2;
            this.ckb_DeSelectAll.Text = "(Un)Check All";
            this.ckb_DeSelectAll.UseVisualStyleBackColor = true;
            this.ckb_DeSelectAll.CheckedChanged += new System.EventHandler(this.ckb_DeSelectAll_CheckedChanged);
            // 
            // btn_Cancel
            // 
            this.btn_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Cancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_Cancel.Location = new System.Drawing.Point(0, 0);
            this.btn_Cancel.Margin = new System.Windows.Forms.Padding(0);
            this.btn_Cancel.Name = "btn_Cancel";
            this.btn_Cancel.Size = new System.Drawing.Size(91, 23);
            this.btn_Cancel.TabIndex = 3;
            this.btn_Cancel.Text = "Cancel";
            this.btn_Cancel.UseVisualStyleBackColor = true;
            this.btn_Cancel.Click += new System.EventHandler(this.btn_Generic_Cancel_Click);
            // 
            // tlp_Buttons
            // 
            this.tlp_Buttons.ColumnCount = 2;
            this.tlp_Buttons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlp_Buttons.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlp_Buttons.Controls.Add(this.btn_Cancel, 0, 0);
            this.tlp_Buttons.Controls.Add(this.btn_OK, 1, 0);
            this.tlp_Buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.tlp_Buttons.Location = new System.Drawing.Point(5, 264);
            this.tlp_Buttons.Margin = new System.Windows.Forms.Padding(0);
            this.tlp_Buttons.Name = "tlp_Buttons";
            this.tlp_Buttons.RowCount = 1;
            this.tlp_Buttons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlp_Buttons.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tlp_Buttons.Size = new System.Drawing.Size(182, 23);
            this.tlp_Buttons.TabIndex = 4;
            // 
            // btn_OK
            // 
            this.btn_OK.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_OK.Location = new System.Drawing.Point(91, 0);
            this.btn_OK.Margin = new System.Windows.Forms.Padding(0);
            this.btn_OK.Name = "btn_OK";
            this.btn_OK.Size = new System.Drawing.Size(91, 23);
            this.btn_OK.TabIndex = 4;
            this.btn_OK.Text = "Ok";
            this.btn_OK.UseVisualStyleBackColor = true;
            this.btn_OK.Click += new System.EventHandler(this.btn_Generic_OK_Click);
            // 
            // FrmColumnSelection
            // 
            this.AcceptButton = this.btn_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btn_Cancel;
            this.ClientSize = new System.Drawing.Size(192, 292);
            this.ControlBox = false;
            this.Controls.Add(this.clb_ColList);
            this.Controls.Add(this.tlp_Buttons);
            this.Controls.Add(this.ckb_DeSelectAll);
            this.Controls.Add(this.lbl_SelectColsTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "FrmColumnSelection";
            this.Opacity = 0.95D;
            this.Padding = new System.Windows.Forms.Padding(5);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.TopMost = true;
            this.tlp_Buttons.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckedListBox clb_ColList;
        private System.Windows.Forms.Label lbl_SelectColsTitle;
        private System.Windows.Forms.CheckBox ckb_DeSelectAll;
        private System.Windows.Forms.Button btn_Cancel;
        private System.Windows.Forms.TableLayoutPanel tlp_Buttons;
        private System.Windows.Forms.Button btn_OK;
    }
}