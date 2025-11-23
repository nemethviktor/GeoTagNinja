using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using NLog;
using Svg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HelperControlAndMessageBoxCustomMessageBoxManager =
    GeoTagNinja.Helpers.HelperControlAndMessageBoxCustomMessageBoxManager;
using Image = System.Drawing.Image;

namespace GeoTagNinja.View.ListView;
/*
 * TODOs
 * * Check if to migrate Context Menu into this class
 * * Hide Items.Clear
 * * When EXIFTool, etc do not use the value of column "Text" anymore,
 *   remove adding item extension and showing file system dir names
 */

public partial class FileListView : System.Windows.Forms.ListView
{
    // Default values to set for entries
    private const string UNKNOWN_VALUE_FILE = "-";

    // Note - if this is changed, all checks for unknown need to be udpated
    // because currently this works via item.replace and check versus ""
    // but replace did not take ""
    private const string UNKNOWN_VALUE_DIR = "";

    /// <summary>
    ///     Every column has this prefix for its name when it is created.
    /// </summary>
    public const string COL_NAME_PREFIX = "clh_";

    internal static readonly int ThumbnailSize = 128;

    /// <summary>
    ///     Constructor
    /// </summary>
    public FileListView()
    {
        Log.Info(message: "Creating List View ...");
        InitializeComponent();
    }

    private IntPtr _hSysImgList;

    /// <summary>
    ///     Class containing native method (shell32, etc) definitions in order
    ///     to retrieve file and directory information.
    ///     This is to deal with the icons in listview
    ///     from https://stackoverflow.com/a/37806517/3968494
    /// </summary>
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
        public readonly struct Shfileinfow
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

    /// <summary>
    ///     Class containing all the relevant column names to be used
    ///     when e.g. querying for information.
    /// </summary>
    public static class FileListColumns
    {
        public const string FILENAME = "FileName";
        public const string FOLDER = "Folder";
        public const string GUID = "GUID";
        public const string GPS_ALTITUDE = "GPSAltitude";
        public const string GPS_ALTITUDE_REF = "GPSAltitudeRef";
        public const string GPS_DEST_LATITUDE = "GPSDestLatitude";
        public const string GPS_DEST_LATITUDE_REF = "GPSDestLatitudeRef";
        public const string GPS_DEST_LONGITUDE = "GPSDestLongitude";
        public const string GPS_DEST_LONGITUDE_REF = "GPSDestLongitudeRef";
        public const string GPS_IMGDIRECTION = "GPSImgDirection";
        public const string GPS_IMGDIRECTION_REF = "GPSImgDirectionRef";
        public const string GPS_LATITUDE = "GPSLatitude";
        public const string GPS_LATITUDE_REF = "GPSLatitudeRef";
        public const string GPS_LONGITUDE = "GPSLongitude";
        public const string GPS_LONGITUDE_REF = "GPSLongitudeRef";
        public const string GPS_SPEED = "GPSSpeed";
        public const string GPS_SPEED_REF = "GPSSpeedRef";
        public const string COORDINATES = "Coordinates";
        public const string DEST_COORDINATES = "DestCoordinates";
        public const string CITY = "City";
        public const string COUNTRY_CODE = "CountryCode";
        public const string COUNTRY = "Country";
        public const string STATE = "State";
        public const string Sublocation = "Sublocation";
        public const string MAKE = "Make";
        public const string MODEL = "Model";
        public const string RATING = "Rating";
        public const string EXPOSURETIME = "ExposureTime";
        public const string FNUMBER = "Fnumber";
        public const string FOCAL_LENGTH = "FocalLength";
        public const string FOCAL_LENGTH_IN_35MM_FORMAT = "FocalLengthIn35mmFormat";
        public const string ISO = "ISO";
        public const string LENS_SPEC = "LensSpec";
        public const string TAKEN_DATE = "TakenDate";
        public const string CREATE_DATE = "CreateDate";
        public const string GPS_DATETIME = "GPSDateTime";
        public const string OFFSET_TIME = "OffsetTime";
        public const string IPTC_KEYWORDS = "IPTCKeywords";
        public const string XML_SUBJECTS = "XMLSubjects";
        public const string GPSDOP = "GPSDOP";
        public const string GPSHPOSITIONINGERROR = "GPSHPositioningError";
    }

    #region Internal Variables

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     The used application language
    /// </summary>
    private string _AppLanguage = "";

    /// <summary>
    ///     The list of columns to show (without prefix)
    /// </summary>
    internal List<string> _cfg_Col_Names = [];

    /// <summary>
    ///     The default order of the columns to show (without prefix)
    /// </summary>
    internal static Dictionary<string, int> _cfg_Col_Order_Default = [];

    /// <summary>
    ///     The used sorter
    /// </summary>
    private ListViewColumnSorter LvwColumnSorter;

    /// <summary>
    ///     Tracks if the initializer ReadAndApplySetting was called.
    /// </summary>
    private bool _isInitialized;

    /// <summary>
    ///     Pointer to the SHFILEINFO Structure that is initialized to be
    ///     used for this list view.
    /// </summary>
    private NativeMethods.SHFILEINFOW shfi;

    private static readonly Dictionary<string, SourcesAndAttributes.ElementAttribute>
        ColumnToAttributeMap = new()
        {
            {
                FileListColumns.GUID,
                SourcesAndAttributes.ElementAttribute.GUID
            },
            {
                FileListColumns.GPS_ALTITUDE,
                SourcesAndAttributes.ElementAttribute.GPSAltitude
            },
            {
                FileListColumns.GPS_ALTITUDE_REF,
                SourcesAndAttributes.ElementAttribute.GPSAltitudeRef
            },
            {
                FileListColumns.GPS_DEST_LATITUDE,
                SourcesAndAttributes.ElementAttribute.GPSDestLatitude
            },
            {
                FileListColumns.GPS_DEST_LATITUDE_REF,
                SourcesAndAttributes.ElementAttribute.GPSDestLatitudeRef
            },
            {
                FileListColumns.GPS_DEST_LONGITUDE,
                SourcesAndAttributes.ElementAttribute.GPSDestLongitude
            },
            {
                FileListColumns.GPS_DEST_LONGITUDE_REF,
                SourcesAndAttributes.ElementAttribute.GPSDestLongitudeRef
            },
            {
                FileListColumns.GPS_IMGDIRECTION,
                SourcesAndAttributes.ElementAttribute.GPSImgDirection
            },
            {
                FileListColumns.GPS_IMGDIRECTION_REF,
                SourcesAndAttributes.ElementAttribute.GPSImgDirectionRef
            },
            {
                FileListColumns.GPS_LATITUDE,
                SourcesAndAttributes.ElementAttribute.GPSLatitude
            },
            {
                FileListColumns.GPS_LATITUDE_REF,
                SourcesAndAttributes.ElementAttribute.GPSLatitudeRef
            },
            {
                FileListColumns.GPS_LONGITUDE,
                SourcesAndAttributes.ElementAttribute.GPSLongitude
            },
            {
                FileListColumns.GPS_LONGITUDE_REF,
                SourcesAndAttributes.ElementAttribute.GPSLongitudeRef
            },
            {
                FileListColumns.GPS_SPEED,
                SourcesAndAttributes.ElementAttribute.GPSSpeed
            },
            {
                FileListColumns.GPS_SPEED_REF,
                SourcesAndAttributes.ElementAttribute.GPSSpeedRef
            },

            {
                FileListColumns.CITY,
                SourcesAndAttributes.ElementAttribute.City
            },
            {
                FileListColumns.COUNTRY_CODE,
                SourcesAndAttributes.ElementAttribute.CountryCode
            },
            {
                FileListColumns.COUNTRY,
                SourcesAndAttributes.ElementAttribute.Country
            },
            {
                FileListColumns.STATE,
                SourcesAndAttributes.ElementAttribute.State
            },
            {
                FileListColumns.Sublocation,
                SourcesAndAttributes.ElementAttribute.Sublocation
            },
            {
                FileListColumns.MAKE,
                SourcesAndAttributes.ElementAttribute.Make
            },
            {
                FileListColumns.MODEL,
                SourcesAndAttributes.ElementAttribute.Model
            },
            {
                FileListColumns.RATING,
                SourcesAndAttributes.ElementAttribute.Rating
            },
            {
                FileListColumns.EXPOSURETIME,
                SourcesAndAttributes.ElementAttribute.ExposureTime
            },
            {
                FileListColumns.FNUMBER,
                SourcesAndAttributes.ElementAttribute.Fnumber
            },
            {
                FileListColumns.FOCAL_LENGTH,
                SourcesAndAttributes.ElementAttribute.FocalLength
            },
            {
                FileListColumns.FOCAL_LENGTH_IN_35MM_FORMAT,
                SourcesAndAttributes.ElementAttribute.FocalLengthIn35mmFormat
            },
            {
                FileListColumns.ISO,
                SourcesAndAttributes.ElementAttribute.ISO
            },
            {
                FileListColumns.LENS_SPEC,
                SourcesAndAttributes.ElementAttribute.LensSpec
            },
            {
                FileListColumns.TAKEN_DATE,
                SourcesAndAttributes.ElementAttribute.TakenDate
            },
            {
                FileListColumns.CREATE_DATE,
                SourcesAndAttributes.ElementAttribute.CreateDate
            },
            {
                FileListColumns.GPS_DATETIME,
                SourcesAndAttributes.ElementAttribute.GPSDateTime
            },
            {
                FileListColumns.OFFSET_TIME,
                SourcesAndAttributes.ElementAttribute.OffsetTime
            },
            {
                FileListColumns.IPTC_KEYWORDS,
                SourcesAndAttributes.ElementAttribute.IPTCKeywords
            },
            {
                FileListColumns.XML_SUBJECTS,
                SourcesAndAttributes.ElementAttribute.XMLSubjects
            },
            {
                FileListColumns.GPSDOP,
                SourcesAndAttributes.ElementAttribute.GPSDOP
            },
            {
                FileListColumns.GPSHPOSITIONINGERROR,
                SourcesAndAttributes.ElementAttribute.GPSHPositioningError
            },
        };

    #endregion

    #region External Visible Properties

    /// <summary>
    ///     The list of directory elements to display.
    /// </summary>
    private DirectoryElementCollection DirectoryElements { get; set; } = [];

    /// <summary>
    ///     The number of elements of type file in the view as
    ///     loaded.
    /// </summary>
    public int FileCount { get; private set; } = -1;

    #endregion

    #region Internal Update Logic

    /// <summary>
    ///     Retrieves the value for the given column from the given
    ///     Directory Element (also does transformations if necessary).
    ///     Transformations are kept in class ModelToColumnValueTransformations.
    /// </summary>
    /// <param name="directoryElement">The Directory Element of which data is to be displayed</param>
    /// <param name="columnHeader">The column in this list view to get data for</param>
    /// <returns>The value for the column as a string</returns>
    private string PickModelValueForColumn(DirectoryElement directoryElement,
        ColumnHeader columnHeader)
    {
        // The displayed file name has to be derived using shell32.dll,
        // which is done in the actual addListItem method.
        if (columnHeader.Name == COL_NAME_PREFIX + FileListColumns.FILENAME)
        {
            throw new ArgumentException(
                message:
                "The contents of the filename column cannot be requested from the method 'pickModelValueForColumn'.");
        }

        // Set the value if no model value is found
        string nfVal = UNKNOWN_VALUE_DIR;
        if (directoryElement.Type == DirectoryElement.ElementType.File)
        {
            nfVal = UNKNOWN_VALUE_FILE;
        }

        string DefaultStrGetter(SourcesAndAttributes.ElementAttribute atrb)
        {
            return directoryElement.GetAttributeValueString(
                attribute: atrb, notFoundValue: nfVal, nowSavingExif: false);
        }

        if (ColumnToAttributeMap.TryGetValue(
                key: columnHeader.Name.Substring(startIndex: 4),
                value: out SourcesAndAttributes.ElementAttribute attribute))
        {
            return DefaultStrGetter(atrb: attribute);
        }

        // Handle special cases.
        return columnHeader.Name.Substring(startIndex: 4) switch
        {
            FileListColumns.COORDINATES => ModelToColumnValueTransformations.M2C_CoordinatesInclDest(
                                column: FileListColumns.COORDINATES, item: directoryElement,
                                nfVal: nfVal),
            FileListColumns.DEST_COORDINATES => ModelToColumnValueTransformations.M2C_CoordinatesInclDest(
                                column: FileListColumns.DEST_COORDINATES, item: directoryElement,
                                nfVal: nfVal),
            _ => nfVal,
        };
    }

    /// <summary>
    ///     Adds a new listitem to lvw_FileList listview
    /// </summary>
    /// <param name="directoryElement">New DE to add</param>
    private void AddListItem(DirectoryElement directoryElement)
    {
        #region icon handlers

        //https://stackoverflow.com/a/37806517/3968494
        // Get the items from the file system, and add each of them to the ListView,
        // complete with their corresponding name and icon indices.

        IntPtr himl = directoryElement.Type != DirectoryElement.ElementType.MyComputer
            ? NativeMethods.SHGetFileInfo(pszPath: directoryElement.FileNameWithPath,
                dwFileAttributes: 0,
                psfi: ref shfi,
                cbSizeFileInfo: (uint)Marshal.SizeOf(
                    structure: shfi),
                uFlags: NativeMethods.SHGFI_DISPLAYNAME |
                        NativeMethods.SHGFI_SYSICONINDEX |
                        NativeMethods.SHGFI_SMALLICON)
            : NativeMethods.SHGetFileInfo(
                pszPath: directoryElement.ItemNameWithoutPath,
                dwFileAttributes: 0,
                psfi: ref shfi,
                cbSizeFileInfo: (uint)Marshal.SizeOf(structure: shfi),
                uFlags: NativeMethods.SHGFI_DISPLAYNAME |
                        NativeMethods.SHGFI_SYSICONINDEX |
                        NativeMethods.SHGFI_SMALLICON);

        //Debug.Assert(himl == hSysImgList); // should be the same imagelist as the one we set

        #endregion

        ListViewItem lvi = new();

        directoryElement.DisplayName = shfi.szDisplayName;

        #region Set LVI Text depending on whether displayname is usable for FS operations

        // dev comment --> https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetfileinfow
        // SHGFI_DISPLAYNAME (0x000000200)
        // Retrieve the display name for the file, which is the name as it appears in Windows Explorer.
        // The name is copied to the szDisplayName member of the structure specified in psfi.
        // The returned display name uses the long file name, if there is one, rather than the 8.3 form of the file name.
        // [!!!!] Note that the display name can be affected by settings such as whether extensions are shown.

        // TLDR if Windows User has "show extensions" set to OFF in Windows Explorer, they won't show here either.
        // The repercussions of that is w/o an extension fileinfo.exists will return false and exiftool won't run/find it.

        // With that in mind if we're missing the extension then we'll force it back on.
        if (!string.IsNullOrEmpty(value: directoryElement.Extension))
        {
            lvi.Text = shfi.szDisplayName.ToLower()
                    .Contains(value: directoryElement.Extension.ToLower())
                ? shfi.szDisplayName
                : $"{shfi.szDisplayName}.{directoryElement.Extension}";
        }
        else
        {
            // this should prevent showing silly string values for special folder
            // (like if your Pictures folder has been moved to say Digi, it'd have
            // shown "Digi" but since that doesn't exist per se it'd have caused
            // an error.
            // same for non-English places. E.g. "Documents and Settings" in HU
            // would be displayed as "Felhasználók" but that folder is still
            // actually called Documents and Settings, but the label is "fake".
            lvi.Text = directoryElement.Type is DirectoryElement.ElementType.SubDirectory or
                DirectoryElement.ElementType.MyComputer or
                DirectoryElement.ElementType.ParentDirectory
                ? directoryElement.ItemNameWithoutPath
                : shfi.szDisplayName;
        }

        #endregion

        // Set the icon to use out of the explorer icons
        lvi.ImageIndex = shfi.iIcon;

        // Set the values for the columns
        List<string> subItemList = [];
        if (directoryElement.Type == DirectoryElement.ElementType.File)
        {
            foreach (ColumnHeader columnHeader in Columns)
            {
                if (columnHeader.Name == COL_NAME_PREFIX + FileListColumns.FILENAME)
                {
                    continue;
                }

                if (columnHeader.Name == COL_NAME_PREFIX + FileListColumns.FOLDER)
                {
                    subItemList.Add(item: directoryElement.Folder);
                    continue;
                }

                subItemList.Add(item: PickModelValueForColumn(
                    directoryElement: directoryElement,
                    columnHeader: columnHeader));
            }

            // For each non-file (i.e. dirs), create empty sub items (needed for sorting)
        }
        else // ie != DirectoryElement.ElementType.File
        {
            foreach (ColumnHeader columnHeader in Columns)
            {
                if (columnHeader.Name == COL_NAME_PREFIX + FileListColumns.FILENAME)
                {
                    // nothing
                }
                else if (columnHeader.Name == COL_NAME_PREFIX + FileListColumns.GUID)
                {
                    subItemList.Add(item: PickModelValueForColumn(
                        directoryElement: directoryElement,
                        columnHeader: columnHeader));
                }
                else
                {
                    subItemList.Add(item: UNKNOWN_VALUE_DIR);
                }
            }
        }

        // don't add twice. this could happen if user does F5 too fast/too many times/is derp. (mostly the last one.)
        bool itemAlreadyInListview = Items.Cast<ListViewItem>()
                                          .Any(predicate: listViewItem => directoryElement == listViewItem.Tag);

        if (!itemAlreadyInListview)
        {
            lvi.Tag = directoryElement;
            Items.Add(value: lvi)
                 .SubItems.AddRange(items: subItemList.ToArray());
        }
    }

    #endregion

    #region Column Size and Order

    /// <summary>
    ///     Reads the widths of individual CLHs from SQL
    /// </summary>
    /// <exception cref="InvalidOperationException">If it encounters a missing CLH</exception>
    private void ColOrderAndWidth_Read()
    {
        Log.Info(message: "Starting");

        BeginUpdate(); // stop drawing

        // While reading col widths, gather order data
        List<int> colOrderIndex = [];
        List<string> colOrderHeadername = [];

        string settingIdToSend;
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
            settingIdToSend = $"{Name}_{columnHeader.Name}_index";
            colOrderHeadername.Add(item: columnHeader.Name);
            int colOrderIndexInt = 0;

            colOrderIndexInt = Convert.ToInt16(
                value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                    dataTable: HelperVariables.DtHelperDataApplicationLayout,
                    settingTabPage: "lvw_FileList",
                    settingId: settingIdToSend));

            // If no user preset is found, retrieve the default
            // col order value
            if (colOrderIndexInt == 0)
            {
                if (_cfg_Col_Order_Default.ContainsKey(
                        key: columnHeader.Name.Substring(startIndex: 4)))
                {
                    colOrderIndexInt =
                        _cfg_Col_Order_Default[
                            key: columnHeader.Name.Substring(startIndex: 4)];
                }
            }

            colOrderIndex.Add(item: colOrderIndexInt);

            Log.Trace(message: $"columnHeader: {columnHeader.Name} - colOrderIndex: {colOrderIndexInt}");

            // Read and process width
            settingIdToSend = $"{Name}_{columnHeader.Name}_width";
            string colWidth = HelperDataApplicationSettings.DataReadSQLiteSettings(
        dataTable: HelperVariables.DtHelperDataApplicationLayout,
        settingTabPage: "lvw_FileList",
        settingId: settingIdToSend
    );

            // We only set col width if there actually is a setting for it.
            // New columns thus will have a default size
            if (colWidth is { Length: > 0 })
            {
                columnHeader.Width = Convert.ToInt16(value: colWidth);
            }

            if (View == System.Windows.Forms.View.LargeIcon)
            {
                columnHeader.Width = columnHeader.Name == "clh_FileName" ? ThumbnailSize : 0;
            }

            Log.Trace(message: $"columnHeader: {columnHeader.Name} - columnHeader.Width: {columnHeader.Width}");
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
                if (string.Equals(a: columnHeader.Name, b: arrColOrderHeadername[idx],
                        comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    columnHeader.DisplayIndex = idx;
                    Log.Trace(message:
                        $"columnHeader: {columnHeader.Name} - columnHeader.DisplayIndex: {columnHeader.DisplayIndex}");
                    break;
                }
            }
        }

        EndUpdate(); // continue drawing
    }

    private void ToggleIndividualColumnVisibility(string columnHeaderName, bool setVisible)
    {
        foreach (ColumnHeader columnHeader in Columns)
        {
            if (columnHeader.Name == columnHeaderName)
            {
                if ((columnHeader.Width == 0) & setVisible)
                {
                    columnHeader.AutoResize(headerAutoResize: ColumnHeaderAutoResizeStyle.HeaderSize);
                }
                else if (columnHeader.Width > 0 &&
                         !setVisible)
                {
                    columnHeader.Width = 0;
                }

                return;
            }
        }
    }

    /// <summary>
    ///     Sends the CLH width and column order to SQL for writing.
    /// </summary>
    private void ColOrderAndWidth_Write()
    {
        List<AppSettingContainer> settingsToWrite = [];

        string settingIdToSend;
        foreach (ColumnHeader columnHeader in Columns)
        {
            settingIdToSend = $"{Name}_{columnHeader.Name}_index";
            settingsToWrite.Add(item: new AppSettingContainer
            {
                TableName = "applayout",
                SettingTabPage = "lvw_FileList",
                SettingId = settingIdToSend,
                SettingValue = columnHeader.DisplayIndex.ToString()
            });

            settingIdToSend = $"{Name}_{columnHeader.Name}_width";
            settingsToWrite.Add(item: new AppSettingContainer
            {
                TableName = "applayout",
                SettingTabPage = "lvw_FileList",
                SettingId = settingIdToSend,
                SettingValue = columnHeader.Width.ToString()
            });
        }

        HelperDataApplicationSettings.DataWriteSQLiteSettings(
            settingsToWrite: settingsToWrite);
    }

    /// <summary>
    ///     Shows the dialog to selection which columns to show.
    /// </summary>
    public void ShowColumnSelectionDialog()
    {
        FrmColumnSelection frm_ColSel = new(
            colList: Columns);
        Point lvwLoc = PointToScreen(p: new Point(x: 0, y: 0));
        lvwLoc.Offset(dx: 20, dy: 10); // Relative to list view top left
        frm_ColSel.Location = lvwLoc; // in screen coords...
        _ = frm_ColSel.ShowDialog(owner: this);
    }

    #endregion

    #region Selection 

    /// <summary>
    /// Selects all items in the list that represent files and triggers navigation to the selected items.
    /// </summary>
    /// <remarks>This method marks all file items as selected within the list view and initiates navigation
    /// actions in the associated application forms. Only items corresponding to files are affected; directory items
    /// remain unselected. Calling this method may result in changes to the current navigation state of the
    /// application.</remarks>
    public void SelectAllItems()
    {
        foreach (ListViewItem lvi in Items)
        {
            if (lvi.Tag is DirectoryElement directoryElement && directoryElement.Type == DirectoryElement.ElementType.File)
            {
                lvi.Selected = true;
            }
        }

        FileListViewMapNavigation.ListViewItemClickNavigate();
        FrmMainApp _frmMainApp =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        _frmMainApp.Request_Map_NavigateGo();

    }

    /// <summary>
    /// Clears the selection of all file items in the list view and updates navigation to reflect the change.
    /// </summary>
    /// <remarks>This method deselects only items representing files, leaving other item types unaffected.
    /// After clearing the selection, it triggers navigation updates to ensure the user interface reflects the current
    /// selection state. This method is typically used to reset the selection before performing navigation or other
    /// actions that require no files to be selected.</remarks>
    public void SelectNoItems()
    {
        foreach (ListViewItem lvi in Items)
        {
            if (lvi.Tag is DirectoryElement directoryElement && directoryElement.Type == DirectoryElement.ElementType.File)
            {
                lvi.Selected = false;
            }
        }

        FileListViewMapNavigation.ListViewItemClickNavigate();
        FrmMainApp _frmMainApp =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        _frmMainApp.Request_Map_NavigateGo();
    }

    /// <summary>
    /// Selects all file items in the list view that are currently unselected and deselects those that are selected,
    /// effectively inverting the selection state of file items.
    /// </summary>
    /// <remarks>This method only affects items representing files; directory items remain unchanged. After
    /// inverting the selection, the method triggers navigation updates to reflect the new selection state in related UI
    /// components.</remarks>
    public void SelectInverseItems()
    {
        foreach (ListViewItem lvi in Items)
        {
            if (lvi.Tag is DirectoryElement directoryElement && directoryElement.Type == DirectoryElement.ElementType.File)
            {
                lvi.Selected = !lvi.Selected;
            }
        }

        FileListViewMapNavigation.ListViewItemClickNavigate();
        FrmMainApp _frmMainApp =
            (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        _frmMainApp.Request_Map_NavigateGo();
    }

    #endregion

    #region Further Settings Stuff

    /// <summary>
    ///     Setup the columns as read from the data table.
    /// </summary>
    private void SetupColumns()
    {
        Log.Info(message: "Starting");
        List<SourcesAndAttributes.ElementAttribute> attributesWithValidOrderIDs = Enum
                                                            .GetValues(enumType: typeof(SourcesAndAttributes.ElementAttribute))
                                                            .Cast<SourcesAndAttributes.ElementAttribute>()
                                                            .Where(predicate: attribute =>
                                                                 SourcesAndAttributes.GetElementAttributesOrderID(
                                                                     attributeToFind: attribute) >
                                                                 0)
                                                            .ToList();

        foreach (SourcesAndAttributes.ElementAttribute attribute in
                 attributesWithValidOrderIDs)
        {
            string clhName = attribute.ToString();

            ColumnHeader clh = new()
            {
                Name = COL_NAME_PREFIX + clhName
            };
            _ = Columns.Add(value: clh);
            Log.Trace(message: $"Added column: {clhName}");
        }

        // Encapsulate locatization - in case it fails above column setup still there...
        try
        {
            foreach (ColumnHeader clh in Columns)
            {
                Log.Trace(message: $"Loading localization for: {clh.Name}");
                clh.Text = HelperControlAndMessageBoxHandling.ReturnControlText(
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.ColumnHeader,
                    controlName: clh.Name
                );
                Log.Trace(message: $"Loaded localization: {clh.Name} --> {clh.Text}");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(message: $"Error: {ex.Message}");
            HelperControlAndMessageBoxCustomMessageBoxManager.ShowMessageBox(
                controlName: "mbx_FrmMainApp_ErrorLanguageFileColumnHeaders",
                captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Error, buttons: MessageBoxButtons.OK,
                extraMessage: ex.Message);
        }
    }

    private void SetStyle()
    {
        // Set up the ListView control's basic properties.
        // Set its theme so it will look like the one used by Explorer.
        _ = NativeMethods.SetWindowTheme(hWnd: Handle, pszSubAppName: "Explorer",
            pszSubIdList: null);
    }

    private void InitializeImageList()
    {
        //https://stackoverflow.com/a/37806517/3968494
        shfi = new NativeMethods.SHFILEINFOW();
        _hSysImgList = NativeMethods.SHGetFileInfo(pszPath: "",
            dwFileAttributes: 0,
            psfi: ref shfi,
            cbSizeFileInfo: (uint)Marshal
               .SizeOf(structure: shfi),
            uFlags: NativeMethods
                       .SHGFI_SYSICONINDEX |
                    NativeMethods.SHGFI_SMALLICON);
        Debug.Assert(condition: _hSysImgList !=
                                IntPtr.Zero); // cross our fingers and hope to succeed!

        // Set the ListView control to use that image list.
        IntPtr hOldImgList = NativeMethods.SendMessage(hWnd: Handle,
            msg: NativeMethods
               .LVM_SETIMAGELIST,
            wParam: NativeMethods.LVSIL_SMALL,
            lParam: _hSysImgList);

        // If the ListView control already had an image list, delete the old one.
        if (hOldImgList != IntPtr.Zero)
        {
            _ = NativeMethods.ImageList_Destroy(hImageList: hOldImgList);
        }
    }

    /// <summary>
    ///     Initialize the list view.
    ///     Must be called before items are added to it.
    /// </summary>
    /// <param name="appLanguage">The application language to use</param>
    /// <exception cref="InvalidOperationException">
    ///     If this method is called
    ///     more than once.
    /// </exception>
    public void ReadAndApplySetting(string appLanguage)
    {
        //if (_isInitialized)
        //{
        //    throw new InvalidOperationException(
        //        message: "Trying to initialize the FileListView more than once.");
        //}

        Log.Info(message: "Starting");
        _AppLanguage = appLanguage;

        SetupColumns();

        // Create the sorter for the list view
        LvwColumnSorter = new ListViewColumnSorter(); // Don't remove this line.
        ListViewItemSorter = LvwColumnSorter;

        // Apply column order and size
        ColOrderAndWidth_Read();

        // Finally set style and icons
        SetStyle();
        if (View == System.Windows.Forms.View.Details)
        {
            InitializeImageList(); // must be here - if called in constructor, it won't work
        }

        _isInitialized = true;
    }

    /// <summary>
    ///     Can be called to make the FileListView persist its user
    ///     settings (like column order and width).
    /// </summary>
    public void PersistSettings()
    {
        // tile-view only shows the filename column, we don't want that saved.
        if (View == System.Windows.Forms.View.Details)
        {
            ColOrderAndWidth_Write();
        }
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
    ///     Reloads the listview's items with data from the DirectoryElementCollection
    ///     Note that the DirectoryElementCollection is assumed to be
    ///     in scope of the FileListView. Calling FileListView.Clear will
    ///     also clear it.
    /// </summary>
    public void ReloadFromDEs(DirectoryElementCollection directoryElements)
    {
        Application.DoEvents();
        BeginUpdate();
        FrmPleaseWaitBox _frmPleaseWaitBoxInstance =
            (FrmPleaseWaitBox)Application.OpenForms[name: "FrmPleaseWaitBox"];

        _frmPleaseWaitBoxInstance?.UpdateLabels(stage: FrmPleaseWaitBox.ActionStages.POPULATING_LISTVIEW);

        // Temp. disable sorting of the list view
        Log.Trace(message: "Disable ListViewItemSorter");
        SuspendColumnSorting();

        DirectoryElements = directoryElements;
        FileCount = 0;
        bool dotDotAdded = false;

        // don't parse the subfolders if we're not in Flat mode, it can be very time-consuming.
        List<string> subFolders = FrmMainApp.FlatMode
            ? HelperFileSystemOperators.GetDirectories(path: FrmMainApp.FolderName, searchPattern: "*",
                searchOption: SearchOption.AllDirectories)
            : [];

        int count = 1;
        int directoryElementCollectionLength = DirectoryElements.Count;
        // so now that we don't clear the DE collection we actually need to make sure these items exist in the current folder/scope.
        foreach (DirectoryElement directoryElement in DirectoryElements)
        {
            if (_frmPleaseWaitBoxInstance != null)
            {
                Application.DoEvents();
                _frmPleaseWaitBoxInstance.lbl_PleaseWaitBoxMessage.Text =
                    // ReSharper disable once LocalizableElement
                    $"{count} / {directoryElementCollectionLength}";
                count++;
            }

            // it's a file and we are in collectionMode
            bool aFileAndWeAreInCollectionMode = directoryElement.Type == DirectoryElement.ElementType.File &&
                                                 Program.CollectionModeEnabled;

            // it's a file and it exists in the current folder and we're not in Flat Mode...and the file factually exists. (like hasn't been deleted etc.)
            bool aFileAndWeAreNotInFlatModeButFileIsWithinMainFolder =
                directoryElement.Type == DirectoryElement.ElementType.File &&
                !FrmMainApp.FlatMode &&
                directoryElement.Folder + "\\" == FrmMainApp.FolderName &&
                File.Exists(path: directoryElement.FileNameWithPath);

            // it's a file and it's in a subfolder of the current FrmMainApp.FolderName folder and we are in Flat Mode
            bool aFileAndWeAreInFlatModeAndItsInARelevantSubfolder =
                    directoryElement.Type == DirectoryElement.ElementType.File &&
                    FrmMainApp.FlatMode &&
                    // this on its own won't work because if we have a 1.jpg in FrmMainApp.FolderName and another in FrmMainApp.FolderName\subFolder, it's still going to show due to how the logic is coded elsewhere.
                    Directory.GetFiles(path: FrmMainApp.FolderName, searchPattern: directoryElement.ItemNameWithoutPath,
                                  searchOption: SearchOption.AllDirectories)
                             .FirstOrDefault() != null &&
                    // thus we make sure that directoryElement.Folder is a subfolder of FrmMainApp.FolderName
                    (subFolders.Contains(value: directoryElement.Folder) ||
                     FrmMainApp.FolderName == directoryElement.Folder ||
                     FrmMainApp.FolderName == directoryElement.Folder + "\\")
                ;

            // it's an immediate subfolder within the current FrmMainApp.FolderName folder
            bool aSubfolderWithinMainFolder =
                directoryElement.Type == DirectoryElement.ElementType.SubDirectory &&
                // problem here is that if we have a C:\temp and a D:\temp then we can get a duplicate just by using the below logic
                Directory.Exists(path: Path.Combine(path1: FrmMainApp.FolderName,
                    path2: directoryElement.ItemNameWithoutPath)) &&
                // we check this is an immediate subfolder of the main folder.
                // ... there's some odd fuckery with the backslashes.
                (Directory.GetParent(path: directoryElement.FileNameWithPath).FullName + "\\" ==
                 FrmMainApp.FolderName ||
                 Directory.GetParent(path: directoryElement.FileNameWithPath).FullName == FrmMainApp.FolderName);

            // it's ".." and fileNameWithPath is the actual parent of the current FrmMainApp.FolderName folder.
            bool dotDotParent = directoryElement.Type == DirectoryElement.ElementType.ParentDirectory &&
                                (!FrmMainApp.FlatMode || (FrmMainApp.FlatMode &&
                                                          Directory.GetParent(path: FrmMainApp.FolderName)
                                                                  ?.Parent.FullName ==
                                                          directoryElement.FileNameWithPath));

            // we're My Computer and it's a Drive
            bool weAreInMyComputerAndItsADrive =
                FrmMainApp.FolderName == Environment.SpecialFolder.MyComputer.ToString() &&
                directoryElement.Type == DirectoryElement.ElementType.Drive;

            if (aFileAndWeAreInCollectionMode ||
                aFileAndWeAreNotInFlatModeButFileIsWithinMainFolder ||
                aFileAndWeAreInFlatModeAndItsInARelevantSubfolder ||
                aSubfolderWithinMainFolder ||
                dotDotParent ||
                weAreInMyComputerAndItsADrive)

            {
                // for some reason i keep getting the .. multiple times. 
                if ((dotDotParent && !dotDotAdded) ||
                    !dotDotParent)
                {
                    AddListItem(directoryElement: directoryElement);
                }

                if (dotDotParent)
                {
                    dotDotAdded = true;
                }

                // increment file count
                if (aFileAndWeAreInCollectionMode ||
                    aFileAndWeAreNotInFlatModeButFileIsWithinMainFolder ||
                    aFileAndWeAreInFlatModeAndItsInARelevantSubfolder)
                {
                    FileCount++;
                }
            }
        }

        ToggleIndividualColumnVisibility(
            columnHeaderName: COL_NAME_PREFIX + FileListColumns.FOLDER,
            setVisible: FrmMainApp.FlatMode || Program.CollectionModeEnabled
        );

        // Add images when required
        if (View == System.Windows.Forms.View.LargeIcon)
        {
            ImageList imgList = new()
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(width: (int)(ThumbnailSize * 0.9), height: (int)(ThumbnailSize * 0.9))
            };

            // mass-generate thumbs

            foreach (ListViewItem lvi in Items)
            {
                DirectoryElement de = lvi.Tag as DirectoryElement;
                string imageListKey = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                    path2: $"{de.ItemNameWithoutPath}.jpg");

                if (de.Thumbnail is not null)
                {
                    imgList.Images.Add(
                        key: imageListKey,
                        image: de.Thumbnail);
                }
            }

            LargeImageList = imgList;
            foreach (ListViewItem lvi in Items)
            {
                DirectoryElement de = lvi.Tag as DirectoryElement;
                lvi.ImageKey = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                    path2: $"{de.ItemNameWithoutPath}.jpg");
            }
        }

        // Resume sorting...
        Log.Trace(message: "Enable ListViewItemSorter");
        EndUpdate();
        ResumeColumnSorting();
        Sort();
    }

    /// <summary>
    ///     Converts svg files to image.
    /// </summary>
    /// <param name="imagePath"></param>
    /// <returns></returns>
    private Image ConvertSVGToImage(string imagePath)
    {
        SvgDocument svgDoc = SvgDocument.Open(path: imagePath);
        return new Bitmap(original: svgDoc.Draw(rasterWidth: ThumbnailSize, rasterHeight: ThumbnailSize));
    }

    /// <summary>
    ///     Clears the FileListView.
    ///     Should be used instead Items.Clear, etc.
    /// </summary>
    public void ClearData()
    {
        Items.Clear();
        // DirectoryElements.Clear();
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
            _ = Invoke(method: (Action)(() => ScrollToDataPoint(itemText: itemText)));
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
    /// <param name="directoryElement">The particular ListViewItem (by directoryElement/Tag) that needs updating</param>
    /// <param name="color">Parameter to assign a particular colour (prob red or black) to the whole row</param>
    public void UpdateItemColour(DirectoryElement directoryElement,
        Color color)
    {
        // If the current thread is not the UI thread, InvokeRequired will be true
        if (InvokeRequired)
        {
            _ = Invoke(method: (Action)(() =>
                UpdateItemColour(directoryElement: directoryElement, color: color)));
            return;
        }

        ListViewItem itemToModify = FindItemByDirectoryElement(directoryElement: directoryElement);
        _ = (itemToModify?.ForeColor = color);
    }

    /// <summary>
    ///     Instead of trying to find items by their names (texts) we find them by Tag. This is needed because if the user is
    ///     in Flat mode then it's wholly possible to have identical files across folders.
    /// </summary>
    /// <param name="directoryElement"></param>
    /// <returns></returns>
    private ListViewItem FindItemByDirectoryElement(DirectoryElement directoryElement)
    {
        return Items.Cast<ListViewItem>().FirstOrDefault(predicate: item => item.Tag == directoryElement);
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
            LvwColumnSorter.SortOrder = LvwColumnSorter.SortOrder == SortOrder.Ascending
                ? SortOrder.Descending
                : SortOrder.Ascending;
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