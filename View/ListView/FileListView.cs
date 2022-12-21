using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using GeoTagNinja.Model;
using NLog;
using static System.Environment;

namespace GeoTagNinja.View.ListView;
/*
 * TODOs
 * * Check if to migrate Context Menu into this class
 * * Hide Items.Clear
 */

public partial class FileListView : System.Windows.Forms.ListView
{
    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    internal DataTable _objectNames;

    internal ListViewColumnSorter LvwColumnSorter;


    public FileListView()
    {
        Logger.Info(message: "Creating List View ...");
        InitializeComponent();
        SetStyle();
    }

    /// <summary>
    ///     The list of directory elements to display.
    /// </summary>
    public DirectoryElementCollection DirectoryElements { get; private set; } = new();


    #region Internal Update Logic

    /// <summary>
    ///     Adds a new listitem to lvw_FileList listview
    /// </summary>
    /// <param name="fileNameWithoutPath">Name of file to be added</param>
    private void addListItem(DirectoryElement item)
    {
        #region icon handlers

        //https://stackoverflow.com/a/37806517/3968494
        NativeMethods.SHFILEINFOW shfi = new();
        IntPtr hSysImgList = NativeMethods.SHGetFileInfo(pszPath: "",
                                                         dwFileAttributes: 0,
                                                         psfi: ref shfi,
                                                         cbSizeFileInfo: (uint)Marshal.SizeOf(structure: shfi),
                                                         uFlags: NativeMethods.SHGFI_SYSICONINDEX | NativeMethods.SHGFI_SMALLICON);
        Debug.Assert(condition: hSysImgList != IntPtr.Zero); // cross our fingers and hope to succeed!

        // Set the ListView control to use that image list.
        IntPtr hOldImgList = NativeMethods.SendMessage(hWnd: Handle,
                                                       msg: NativeMethods.LVM_SETIMAGELIST,
                                                       wParam: NativeMethods.LVSIL_SMALL,
                                                       lParam: hSysImgList);

        // If the ListView control already had an image list, delete the old one.
        if (hOldImgList != IntPtr.Zero)
        {
            NativeMethods.ImageList_Destroy(hImageList: hOldImgList);
        }

        // Get the items from the file system, and add each of them to the ListView,
        // complete with their corresponding name and icon indices.
        IntPtr himl;
        if (item.FullPathAndName != SpecialFolder.MyComputer.ToString())
        {
            himl = NativeMethods.SHGetFileInfo(pszPath: item.FullPathAndName,
                                               dwFileAttributes: 0,
                                               psfi: ref shfi,
                                               cbSizeFileInfo: (uint)Marshal.SizeOf(structure: shfi),
                                               uFlags: NativeMethods.SHGFI_DISPLAYNAME | NativeMethods.SHGFI_SYSICONINDEX | NativeMethods.SHGFI_SMALLICON);
        }
        else
        {
            himl = NativeMethods.SHGetFileInfo(pszPath: item.ItemName,
                                               dwFileAttributes: 0,
                                               psfi: ref shfi,
                                               cbSizeFileInfo: (uint)Marshal.SizeOf(structure: shfi),
                                               uFlags: NativeMethods.SHGFI_DISPLAYNAME | NativeMethods.SHGFI_SYSICONINDEX | NativeMethods.SHGFI_SMALLICON);
        }

        //Debug.Assert(himl == hSysImgList); // should be the same imagelist as the one we set

        #endregion

        List<string> subItemList = new();
        if (item.Type == DirectoryElement.ElementType.File)
        {
            foreach (ColumnHeader columnHeader in Columns)
            {
                if (columnHeader.Name != "clh_FileName")
                {
                    subItemList.Add(item: "-");
                }
            }
            // For each non-file (i.e. dirs), create empty sub items (needed for sorting)
        }
        else
        {
            foreach (ColumnHeader columnHeader in Columns)
            {
                if (columnHeader.Name != "clh_FileName")
                {
                    subItemList.Add(item: "");
                }
            }
        }

        ListViewItem lvi = new();

        // dev comment --> https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetfileinfow
        // SHGFI_DISPLAYNAME (0x000000200)
        // Retrieve the display name for the file, which is the name as it appears in Windows Explorer.
        // The name is copied to the szDisplayName member of the structure specified in psfi.
        // The returned display name uses the long file name, if there is one, rather than the 8.3 form of the file name.
        // [!!!!] Note that the display name can be affected by settings such as whether extensions are shown.

        // TLDR if Windows User has "show extensions" set to OFF in Windows Explorer, they won't show here either.
        // The repercussions of that is w/o an extension fileinfo.exists will return false and exiftool won't run/find it.

        // With that in mind if we're missing the extension then we'll force it back on.
        if (!string.IsNullOrEmpty(value: item.Extension))
        {
            if (shfi.szDisplayName.Contains(value: item.Extension))
            {
                lvi.Text = shfi.szDisplayName;
                item.DisplayName = shfi.szDisplayName;
            }
            else
            {
                lvi.Text = shfi.szDisplayName + item.Extension;
                item.DisplayName = shfi.szDisplayName + item.Extension;
            }
        }
        else
        {
            // this should prevent showing silly string values for special folders (like if your Pictures folder has been moved to say Digi, it'd have shown "Digi" but since that doesn't exist per se it'd have caused an error.
            // same for non-English places. E.g. "Documents and Settings" in HU would be displayed as "Felhasználók" but that folder is still actually called Documents and Settings, but the label is "fake".
            if (item.Type == DirectoryElement.ElementType.SubDirectory ||
                item.Type == DirectoryElement.ElementType.ParentDirectory)
            {
                lvi.Text = item.ItemName;
                item.DisplayName = item.ItemName;
            }
            else
            {
                lvi.Text = shfi.szDisplayName;
                item.DisplayName = shfi.szDisplayName;
            }
        }

        lvi.ImageIndex = shfi.iIcon;
        if (item.Type == DirectoryElement.ElementType.File)
        {
            if (lvi.Index % 10 == 0)
            {
                Application.DoEvents();
                // not adding the xmp here because the current code logic would pull a "unified" data point.                         

                ScrollToDataPoint(itemText: item.ItemName);
            }

            UpdateItemColour(itemText: item.ItemName, color: Color.Gray);
        }

        // don't add twice. this could happen if user does F5 too fast/too many times/is derp. (mostly the last one.)
        if (FindItemWithText(text: lvi.Text) == null)
        {
            lvi.Tag = item;
            Items.Add(value: lvi)
                .SubItems.AddRange(items: subItemList.ToArray());
        }
    }

    #endregion

    /// <summary>
    ///     this is to deal with the icons in listview
    ///     from https://stackoverflow.com/a/37806517/3968494
    /// </summary>
    [SuppressMessage(category: "ReSharper", checkId: "InconsistentNaming"), SuppressMessage(category: "ReSharper", checkId: "UnusedMember.Local"), SuppressMessage(category: "ReSharper", checkId: "IdentifierTypo"), SuppressMessage(category: "ReSharper", checkId: "StringLiteralTypo"), SuppressMessage(category: "ReSharper", checkId: "MemberCanBePrivate.Local"), SuppressMessage(category: "ReSharper", checkId: "FieldCanBeMadeReadOnly.Local")]
    private static class NativeMethods
    {
        public const uint LVM_FIRST = 0x1000;
        public const uint LVM_GETIMAGELIST = LVM_FIRST + 2;
        public const uint LVM_SETIMAGELIST = LVM_FIRST + 3;

        public const uint LVSIL_NORMAL = 0;
        public const uint LVSIL_SMALL = 1;
        public const uint LVSIL_STATE = 2;
        public const uint LVSIL_GROUPHEADER = 3;

        public const uint SHGFI_DISPLAYNAME = 0x200;
        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;
        public const uint SHGFI_SMALLICON = 0x1;
        public const uint SHGFI_SYSICONINDEX = 0x4000;

        [DllImport(dllName: "user32")]
        public static extern IntPtr SendMessage(IntPtr hWnd,
                                                uint msg,
                                                uint wParam,
                                                IntPtr lParam);

        [DllImport(dllName: "comctl32")]
        public static extern bool ImageList_Destroy(IntPtr hImageList);

        [DllImport(dllName: "shell32", CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfo(string pszPath,
                                                  uint dwFileAttributes,
                                                  ref SHFILEINFOW psfi,
                                                  uint cbSizeFileInfo,
                                                  uint uFlags);

        [DllImport(dllName: "uxtheme", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd,
                                                string pszSubAppName,
                                                string pszSubIdList);


        [StructLayout(layoutKind: LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFOW
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(unmanagedType: UnmanagedType.ByValTStr, SizeConst = 260 * 2)]
            public string szDisplayName;

            [MarshalAs(unmanagedType: UnmanagedType.ByValTStr, SizeConst = 80 * 2)]
            public string szTypeName;
        }


        [StructLayout(layoutKind: LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct Shfileinfow
        {
            public readonly IntPtr hIcon;
            public readonly int iIcon;
            public readonly uint dwAttributes;

            [MarshalAs(unmanagedType: UnmanagedType.ByValTStr, SizeConst = 260 * 2)]
            public readonly string szDisplayName;

            [MarshalAs(unmanagedType: UnmanagedType.ByValTStr, SizeConst = 80 * 2)]
            public readonly string szTypeName;
        }
    }


    #region Column Size and Order

    /// <summary>
    ///     Reads the widths of individual CLHs from SQL, if not found assigns them "auto" (-2)
    /// </summary>
    /// <exception cref="InvalidOperationException">If it encounters a missing CLH</exception>
    private void ColOrderAndWidth_Read()
    {
        Logger.Debug(message: "Starting");

        BeginUpdate(); // stop drawing

        // While reading col widths, gather order data
        List<int> colOrderIndex = new();
        List<string> colOrderHeadername = new();

        string settingIdToSend;
        string colWidth = null;
        // logic: see if it's in SQL first...if not then set to Auto
        foreach (ColumnHeader columnHeader in Columns)
        {
            // columnHeader.Name doesn't get automatically recorded, i think that's a VSC bug.
            // anyway will introduce a breaking-line here for that.
            // oh and can't convert bool to str but it's not letting to deal w it otherwise anyway so going for length == 0 instead
            if (columnHeader.Name.Length == 0)
            {
                throw new InvalidOperationException(message: "columnHeader name missing");
            }

            // Read index / order
            settingIdToSend = Name + "_" + columnHeader.Name + "_index";
            colOrderHeadername.Add(item: columnHeader.Name);
            int colOrderIndexInt = 0;

            colOrderIndexInt = Convert.ToInt16(value: HelperStatic.DataReadSQLiteSettings(
                                                   tableName: "applayout",
                                                   settingTabPage: "lvw_FileList",
                                                   settingId: settingIdToSend));

            // this would be the default case 
            if (colOrderIndexInt == 0)
            {
                EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in _objectNames.AsEnumerable()
                                                                   where dataRow.Field<string>(columnName: "objectName") == columnHeader.Name.Substring(startIndex: 4)
                                                                   select dataRow;
                List<int> lstReturn = new();

                Parallel.ForEach(source: drDataTableData, body: dataRow =>
                    {
                        int settingValue = int.Parse(s: dataRow[columnName: "sqlOrder"]
                                                         .ToString());
                        lstReturn.Add(item: settingValue);
                    })
                    ;

                // basically fileName will always come 0.
                if (lstReturn.Count > 0)
                {
                    colOrderIndexInt = lstReturn[index: 0];
                }
            }

            colOrderIndex.Add(item: colOrderIndexInt);

            Logger.Trace(message: "columnHeader: " +
                                  columnHeader.Name +
                                  " - colOrderIndex: " +
                                  colOrderIndexInt);

            // Read and process width
            settingIdToSend = Name + "_" + columnHeader.Name + "_width";
            colWidth = HelperStatic.DataReadSQLiteSettings(
                tableName: "applayout",
                settingTabPage: "lvw_FileList",
                settingId: settingIdToSend
            );

            // We only set col width if there actually is a setting for it.
            // New columns thus will have a default size
            if (colWidth != null && colWidth.Length > 0)
            {
                columnHeader.Width = Convert.ToInt16(value: colWidth);
            }

            Logger.Trace(message: "columnHeader: " +
                                  columnHeader.Name +
                                  " - columnHeader.Width: " +
                                  columnHeader.Width);
        }

        // Finally set the column order - setting them from first to last col
        int[] arrColOrderIndex = colOrderIndex.ToArray();
        string[] arrColOrderHeadername = colOrderHeadername.ToArray();
        Array.Sort(keys: arrColOrderIndex, items: arrColOrderHeadername);
        for (int idx = 0; idx < arrColOrderHeadername.Length; idx++)
        {
            foreach (ColumnHeader columnHeader in Columns)
            {
                // We go for case-insensitive!
                if (string.Equals(a: columnHeader.Name, b: arrColOrderHeadername[idx], comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    columnHeader.DisplayIndex = idx;
                    Logger.Trace(message: "columnHeader: " +
                                          columnHeader.Name +
                                          " - columnHeader.DisplayIndex: " +
                                          columnHeader.DisplayIndex);
                    break;
                }
            }
        }

        EndUpdate(); // continue drawing
    }


    /// <summary>
    ///     Sends the CLH width and column order to SQL for writing.
    /// </summary>
    /// <param name="frmMainApp">Make a guess</param>
    private void ColOrderAndWidth_Write()
    {
        string settingIdToSend;
        foreach (ColumnHeader columnHeader in Columns)
        {
            settingIdToSend = Name + "_" + columnHeader.Name + "_index";
            HelperStatic.DataWriteSQLiteSettings(
                tableName: "applayout",
                settingTabPage: "lvw_FileList",
                settingId: settingIdToSend,
                settingValue: columnHeader.DisplayIndex.ToString()
            );

            settingIdToSend = Name + "_" + columnHeader.Name + "_width";
            HelperStatic.DataWriteSQLiteSettings(
                tableName: "applayout",
                settingTabPage: "lvw_FileList",
                settingId: settingIdToSend,
                settingValue: columnHeader.Width.ToString()
            );
        }
    }


    /// <summary>
    ///     Shows the dialog to selection which columns to show.
    /// </summary>
    public void ShowColumnSelectionDialog()
    {
        FrmColumnSelection frm_ColSel = new(
            ColList: Columns, AppLanguage: _AppLanguage);
        Point lvwLoc = PointToScreen(p: new Point(x: 0, y: 0));
        lvwLoc.Offset(dx: 20, dy: 10); // Relative to list view top left
        frm_ColSel.Location = lvwLoc; // in screen coords...
        frm_ColSel.ShowDialog(owner: this);
    }

    #endregion


    #region SettingsStuff

    private string _AppLanguage = "";
    private readonly List<string> _columnIndizes = new();


    /// <summary>
    ///     applies the names of columnheaders
    /// </summary>
    /// The list of columns as read from the defaults.
    private void SetupColumns()
    {
        Logger.Debug(message: "Starting");

        try
        {
            _objectNames.DefaultView.Sort = "sqlOrder";
            DataTable dt = _objectNames.DefaultView.ToTable();
            foreach (DataRow dr in dt.Rows)
            {
                ColumnHeader clh = new();
                string objectName = dr[columnName: "objectName"]
                    .ToString();
                clh.Name = "clh_" + objectName;
                Columns.Add(value: clh);
                _columnIndizes.Add(item: objectName);
                Logger.Trace(message: "Loading: " + objectName);
            }

            foreach (ColumnHeader clh in Columns)
            {
                clh.Text = HelperStatic.DataReadDTObjectText(
                    objectType: "ColumnHeader",
                    objectName: clh.Name
                );
                Logger.Trace(message: "Loading: " + clh.Name + " --> " + clh.Text);
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_ErrorLanguageFileColumnHeaders") + ex.Message, caption: "Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }


    public void ReadAndApplySetting(string appLanguage,
                                    DataTable objectNames)
    {
        Logger.Debug(message: "Starting");
        _AppLanguage = appLanguage;
        _objectNames = objectNames;
        SetupColumns();

        // Create the sorter for the list view
        LvwColumnSorter = new ListViewColumnSorter();
        ListViewItemSorter = LvwColumnSorter;

        // Apply column order and size
        ColOrderAndWidth_Read();
    }

    public void PersistSettings()
    {
        ColOrderAndWidth_Write();
    }


    private void SetStyle()
    {
        // Set up the ListView control's basic properties.
        // Set its theme so it will look like the one used by Explorer.
        NativeMethods.SetWindowTheme(hWnd: Handle, pszSubAppName: "Explorer", pszSubIdList: null);
    }

    #endregion


    #region Modes

    /// <summary>
    ///     Suspends sorting the list view
    /// </summary>
    public void SuspendColumnSorting()
    {
        ListViewItemSorter = null;
    }

    /// <summary>
    ///     Resume sorting the list view
    /// </summary>
    public void ResumeColumnSorting()
    {
        ListViewItemSorter = LvwColumnSorter;
    }

    #endregion


    #region Updating

    /// <summary>
    ///     Restaff the list view with the set of directory elements handed
    /// </summary>
    public void ReloadFromDEs(DirectoryElementCollection directoryElements)
    {
        // Temp. disable sorting of the list view
        Logger.Trace(message: "Disable ListViewItemSorter");
        SuspendColumnSorting();

        DirectoryElements = directoryElements;
        foreach (DirectoryElement item in DirectoryElements)
        {
            addListItem(item: item);
        }

        // Resume sorting...
        Logger.Trace(message: "Enable ListViewItemSorter");
        ResumeColumnSorting();
        Sort();
    }


    public void ClearData()
    {
        Items.Clear();
        DirectoryElements.Clear();
    }

    /// <summary>
    ///     Scrolls to the relevant line of the listview
    /// </summary>
    /// <param name="itemText">The particular ListViewItem (by text) that needs updating</param>
    public void ScrollToDataPoint(string itemText)
    {
        // If the current thread is not the UI thread, InvokeRequired will be true
        if (InvokeRequired)
        {
            Invoke(method: (Action)(() => ScrollToDataPoint(itemText: itemText)));
            return;
        }

        ListViewItem itemToModify = FindItemWithText(text: itemText);
        if (itemToModify != null)
        {
            EnsureVisible(index: itemToModify.Index);
        }
    }

    /// <summary>
    ///     Deals with invoking the listview (from outside the thread) and updating the colour of a particular row (Item) to
    ///     the assigned colour.
    /// </summary>
    /// <param name="lvw">The listView Control that needs updating. Most likely the one in the main Form</param>
    /// <param name="itemText">The particular ListViewItem (by text) that needs updating</param>
    /// <param name="color">Parameter to assign a particular colour (prob red or black) to the whole row</param>
    public void UpdateItemColour(string itemText,
                                 Color color)
    {
        // If the current thread is not the UI thread, InvokeRequired will be true
        if (InvokeRequired)
        {
            Invoke(method: (Action)(() => UpdateItemColour(itemText: itemText, color: color)));
            return;
        }

        ListViewItem itemToModify = FindItemWithText(text: itemText);
        if (itemToModify != null)
        {
            itemToModify.ForeColor = color;
        }
    }

    #endregion


    #region Handlers

    /// <summary>
    ///     Handles the sorting and reordering.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FileList_ColumnClick(object sender,
                                      ColumnClickEventArgs e)
    {
        if (e.Column == LvwColumnSorter.SortColumn)
        {
            // Column clicked is current sort column --> Reverse order
            if (LvwColumnSorter.SortOrder == SortOrder.Ascending)
            {
                LvwColumnSorter.SortOrder = SortOrder.Descending;
            }
            else
            {
                LvwColumnSorter.SortOrder = SortOrder.Ascending;
            }
        }
        else
        {
            LvwColumnSorter.SortColumn = e.Column;
            LvwColumnSorter.SortOrder = SortOrder.Ascending;
        }

        // Perform the sort with these new sort options.
        Sort();
    }

    private void FileList_ColumnWidthChanging(object sender,
                                              ColumnWidthChangingEventArgs e)
    {
        // Columns with width = 0 should stay hidden / may not be resized.
        if (Columns[index: e.ColumnIndex]
                .Width ==
            0)
        {
            e.Cancel = true;
            e.NewWidth = 0;
        }
    }

    private void FileList_ColumnReordered(object sender,
                                          ColumnReorderedEventArgs e)
    {
        // Prevent FileName column to be moved
        if (e.Header.Index == 0)
        {
            e.Cancel = true;
        }
    }

    #endregion
}