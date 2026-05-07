namespace GeoTagNinja.View.ListView
{
    partial class FileListView
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up used resources.
        /// </summary>
        /// <param name="disposing">True if managed resources are to be deleted; otherwise False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code

        /// <summary> 
        /// Required method for designer support.
        /// The content of the method must not be changed using the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FileListView
            // 
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.FileList_ColumnClick);
            this.ColumnReordered += new System.Windows.Forms.ColumnReorderedEventHandler(this.FileList_ColumnReordered);
            this.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.FileList_ColumnWidthChanging);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
