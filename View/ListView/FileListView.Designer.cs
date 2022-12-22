namespace GeoTagNinja.View.ListView
{
    partial class FileListView
    {
        /// <summary> 
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
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
        /// Erforderliche Methode für die Designerunterstützung. 
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FileListView
            // 
            this.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.FileList_ColumnClick);
            this.ColumnReordered += new System.Windows.Forms.ColumnReorderedEventHandler(this.FileList_ColumnReordered);
            this.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.FileList_ColumnWidthChanging);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
