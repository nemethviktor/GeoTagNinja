using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinFormsDarkThemerNinja;

namespace GeoTagNinja.View.ListView;

public partial class FileListView : System.Windows.Forms.ListView
{
    // Default values to set for entries
    private const string UNKNOWN_VALUE_FILE = "-";

    private readonly FrmMainApp _frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
    // Special value for coordinates as they are a pair.
    private const string UNKNOWN_VALUE_FILE_COORDINATES = "-;-";

    // Note - if this is changed, all checks for unknown need to be udpated
    // because currently this works via lvi.replace and check versus ""
    // but replace did not take ""
    private const string UNKNOWN_VALUE_DIR = "";

    internal static readonly int ThumbnailSize = 128;

    /// <summary>
    ///     Constructor
    /// </summary>
    public FileListView()
    {
        Log.Info(message: "Creating List View ...");
        InitializeComponent();

        // Bucket for Tiles/Large Icons
        LargeImageList = new ImageList
        {
            ImageSize = new Size(width: ThumbnailSize, height: ThumbnailSize),
            ColorDepth = ColorDepth.Depth32Bit
        };

        // Bucket for Details view (Standard 16x16 or 32x32 icons)
        SmallImageList = new ImageList
        {
            ImageSize = new Size(width: 32, height: 32), // Standard detail icon size
            ColorDepth = ColorDepth.Depth32Bit
        };

        LabelEdit = true; // Allows the user to click the text or press F2 to edit

        BeforeLabelEdit += FileListView_BeforeLabelEdit;
        AfterLabelEdit += FileListView_AfterLabelEdit;

    }

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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
    }

    #region Internal Variables

    internal Dictionary<string, DirectoryElement> LastScrollDict = [];

    /// <summary>
    /// Handle to the system image list used for retrieving shell icons.
    /// </summary>
    /// <remarks>Obtained from the shell (for example via SHGetImageList). This handle is shared by the
    /// system; do not destroy it unless ownership is explicitly transferred.</remarks>
    private IntPtr _hSysImgList;

    /// <summary>
    ///     Class containing all the relevant column names to be used
    ///     when e.g. querying for information. Do not use translations here.
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

    private DirectoryElementCollection _masterCollection = [];

    #region Filter strings

    /// <summary>
    /// Specifies available operators for constructing filter expressions, including equality, negation, emptiness
    /// checks, and substring containment.
    /// </summary>
    /// <remarks>Used primarily for string-based filtering and for building or parsing filter
    /// expressions.</remarks>
    private enum FilterOperator
    {
        Unknown,
        Is,
        IsNot,
        IsEmpty,
        IsNotEmpty,
        Contains,
        DoesNotContain
    }

    #endregion

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
        if (columnHeader.Name == HelperVariables.COL_NAME_PREFIX + FileListColumns.FILENAME)
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
            return directoryElement.GetAttributeValueAsString(
                attribute: atrb,
                notFoundValue: nfVal,
                nowSavingExif: false);
        }

        if (ColumnToAttributeMap.TryGetValue(
            key: columnHeader.Name.Substring(HelperVariables.COL_NAME_PREFIX.Length),
            value: out SourcesAndAttributes.ElementAttribute attribute))
        {
            return DefaultStrGetter(atrb: attribute);
        }

        // Handle special cases.
        return columnHeader.Name.Substring(HelperVariables.COL_NAME_PREFIX.Length) switch
        {
            FileListColumns.COORDINATES => ModelToColumnValueTransformations.M2C_CoordinatesInclDest(
                column: FileListColumns.COORDINATES,
                item: directoryElement,
                notFoundValue: nfVal),
            FileListColumns.DEST_COORDINATES => ModelToColumnValueTransformations.M2C_CoordinatesInclDest(
                column: FileListColumns.DEST_COORDINATES,
                item: directoryElement,
                notFoundValue: nfVal),
            _ => nfVal,
        };
    }

    /// <summary>
    ///     Adds a new listitem to lvw_FileList listview
    /// </summary>
    /// <param name="directoryElement">New DE to add</param>
    // Inside FileListView.cs
    public ListViewItem AddListItem(DirectoryElement directoryElement)
    {
        if (InvokeRequired)
        {
            return (ListViewItem)Invoke(new Func<DirectoryElement, ListViewItem>(AddListItem), directoryElement);
        }

        _masterCollection ??= [];
        if (!_masterCollection.Contains(directoryElement))
        {
            _masterCollection.Add(directoryElement);
        }

        ListViewItem lvi = new(text: directoryElement.ItemNameWithoutPath)
        {
            Tag = directoryElement,
            Name = directoryElement.FileNameWithPath
        };

        // 1. Check for custom icons (Folders, Drives, etc.)
        if (directoryElement.Thumbnail != null)
        {
            string imageKey = directoryElement.Type == DirectoryElement.ElementType.Drive
                ? $"Drive_{directoryElement.ItemNameWithoutPath}"
                : directoryElement.Type.ToString();

            // Add to LargeImageList
            if (LargeImageList != null && !LargeImageList.Images.ContainsKey(key: imageKey))
            {
                LargeImageList.Images.Add(key: imageKey, image: directoryElement.Thumbnail);
            }

            // ADD TO SMALLIMAGELIST (Fixes the Details view issue)
            if (SmallImageList != null && !SmallImageList.Images.ContainsKey(key: imageKey))
            {
                SmallImageList.Images.Add(key: imageKey, image: directoryElement.Thumbnail);
            }

            lvi.ImageKey = imageKey;
        }
        else
        {
            // 2. Fallback to Shell Icons
            NativeMethods.SHFILEINFO shfi = new();
            uint flags = NativeMethods.SHGFI_SYSICONINDEX | NativeMethods.SHGFI_SMALLICON;
            string path = (directoryElement.Type == DirectoryElement.ElementType.MyComputer)
                ? directoryElement.ItemNameWithoutPath
                : directoryElement.FileNameWithPath;

            _ = NativeMethods.SHGetFileInfo(pszPath: path, dwFileAttributes: 0, psfi: ref shfi, cbSizeFileInfo: (uint)Marshal.SizeOf(shfi), uFlags: flags);
            lvi.ImageIndex = shfi.iIcon;
        }

        // 4. Fill SubItems (Metadata)
        // If it's already hydrated (e.g., during a filter redraw), fill the actual values
        for (int i = 1; i < Columns.Count; i++)
        {
            string colValue = directoryElement.IsHydrated
                ? PickModelValueForColumn(directoryElement, Columns[i])
                : (directoryElement.Type == DirectoryElement.ElementType.File ? UNKNOWN_VALUE_FILE : UNKNOWN_VALUE_DIR);

            _ = lvi.SubItems.Add(text: colValue);
        }

        // 5. Apply "Dirty" state (Red colour)
        if (directoryElement.HasDirtyAttributes())
        {
            lvi.ForeColor = Color.Red;
        }

        // 6. Finalise
        _ = Items.Add(lvi);

        // Only sync/background-task for folders/drives if they aren't ready
        if (directoryElement.Type != DirectoryElement.ElementType.File && directoryElement.Thumbnail == null && HelperVariables.UserSettingShowThumbnails)
        {
            SyncElementThumbnail(directoryElement: directoryElement);
        }

        return lvi;
    }
    public void UpdateListItemData(DirectoryElement de)
    {
        if (InvokeRequired)
        {
            _ = Invoke(new Action<DirectoryElement>(UpdateListItemData), de);
            return;
        }

        // Find the ListViewItem that represents this DirectoryElement
        ListViewItem lvi = Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == de);
        if (lvi == null)
        {
            return;
        }

        if (de.Type == DirectoryElement.ElementType.File)
        {
            // Update subitems based on the new metadata
            for (int i = 1; i < Columns.Count; i++)
            {
                // Re-use the PickModelValueForColumn logic
                lvi.SubItems[i].Text = PickModelValueForColumn(de, Columns[i]);
            }
        }
        else
        {
            // Iterate through sub-items (starting from index 1 to skip the name) and clear them.
            for (int i = 1; i < lvi.SubItems.Count; i++)
            {
                lvi.SubItems[index: i].Text = string.Empty;
            }
            return;

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
                        key: columnHeader.Name.Substring(HelperVariables.COL_NAME_PREFIX.Length)))
                {
                    colOrderIndexInt =
                        _cfg_Col_Order_Default[
                            key: columnHeader.Name.Substring(HelperVariables.COL_NAME_PREFIX.Length)];
                }
            }

            colOrderIndex.Add(item: colOrderIndexInt);

            Log.Trace(message: $"columnHeader: {columnHeader.Name} - colOrderIndex: {colOrderIndexInt}");

            // Read and process width
            settingIdToSend = $"{Name}_{columnHeader.Name}_width";
            string colWidth = HelperDataApplicationSettings.DataReadSQLiteSettings(dataTable: HelperVariables.DtHelperDataApplicationLayout,
                settingTabPage: "lvw_FileList",
                settingId: settingIdToSend);

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
    /// <remarks>This method deselects only items representing files, leaving other lvi types unaffected.
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
                Name = HelperVariables.COL_NAME_PREFIX + clhName
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
            Themer.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_FrmMainApp_ErrorLanguageFileColumnHeaders",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                icon: MessageBoxIcon.Error,
                buttons: MessageBoxButtons.OK);
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

    #region File operations

    /// <summary>
    ///     Processes specialized key inputs. Forces the termination of active label edits 
    ///     when the Escape key is pressed to ensure UI consistency.
    /// </summary>
    /// <param name="e">The <see cref="KeyEventArgs"/> containing the event data.</param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // 1. Handle Rename Trigger (F2)
        if (e.KeyCode == Keys.F2 && SelectedItems.Count == 1)
        {
            e.Handled = true;
            SelectedItems[index: 0].BeginEdit();
        }
        // 2. Handle Deletion (Delete)
        else if (e.KeyCode == Keys.Delete && SelectedItems.Count > 0)
        {
            e.Handled = true;
            DeleteSelectedFiles();
        }
        // 3. Handle Cancellation (Escape) - The reliable version
        else if (e.KeyCode == Keys.Escape)
        {
            // Check if there is an active edit occurring
            // We can check if a focus is on the internal edit box or just toggle
            if (LabelEdit)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                // Toggling LabelEdit forces the internal EditControl to close immediately
                // and triggers AfterLabelEdit with a null label (cancelling it).
                LabelEdit = false;
                LabelEdit = true;
            }
        }

        base.OnKeyDown(e: e);
    }

    /// <summary>
    ///     Prepares the list view item for editing by removing the file extension from the label,
    ///     ensuring the user only modifies the base filename.
    /// </summary>
    private void FileListView_BeforeLabelEdit(object sender, LabelEditEventArgs e)
    {
        ListViewItem lvi = Items[index: e.Item];
        DirectoryElement de = (DirectoryElement)lvi.Tag;
        if (de.Type is not DirectoryElement.ElementType.File and not DirectoryElement.ElementType.SubDirectory)
        {
            e.CancelEdit = true;
        }

        // Only strip extensions for actual files
        if (de.Type == DirectoryElement.ElementType.File)
        {
            string baseName = Path.GetFileNameWithoutExtension(path: de.FileNameWithPath);

            // This changes the text in the edit box only
            lvi.Text = baseName;
        }
    }

    /// <summary>
    /// Finalises the rename operation. Re-attaches the extension if the operation was successful,
    /// and ensures the full original name is restored if the user cancels (e.g. by pressing Escape).
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="LabelEditEventArgs"/> containing the label data.</param>
    private void FileListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
    {
        ListViewItem lvi = Items[index: e.Item];
        DirectoryElement de = (DirectoryElement)lvi.Tag;

        // 1. HANDLE CANCELLATION (The Escape Key Fix)
        // If e.Label is null, the user cancelled. We must restore the full name 
        // because BeforeLabelEdit stripped the extension from the UI.
        if (e.Label == null)
        {
            lvi.Text = de.ItemNameWithoutPath;
            e.CancelEdit = true;
            return;
        }

        try
        {
            string? directory = Path.GetDirectoryName(path: de.FileNameWithPath);
            if (string.IsNullOrEmpty(value: directory))
            {
                lvi.Text = de.ItemNameWithoutPath;
                e.CancelEdit = true;
                return;
            }

            string originalExtension = Path.GetExtension(path: de.FileNameWithPath);
            string newBaseName = e.Label.Trim();

            // 2. CHECK IF NAME ACTUALLY CHANGED
            // If the user entered the same name (without extension) as before, treat as a cancel.
            if (string.Equals(a: newBaseName, b: Path.GetFileNameWithoutExtension(path: de.FileNameWithPath), comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                lvi.Text = de.ItemNameWithoutPath;
                e.CancelEdit = true;
                return;
            }

            // 3. EXTENSION STITCHING
            string newFileName = newBaseName.EndsWith(value: originalExtension, comparisonType: StringComparison.OrdinalIgnoreCase)
                ? newBaseName
                : newBaseName + originalExtension;

            string newPath = Path.Combine(path1: directory, path2: newFileName);

            // 4. VALIDATION & DISK MOVE
            if (File.Exists(path: newPath))
            {
                throw new IOException(message: "A file with that name already exists.");
            }

            File.Move(sourceFileName: de.FileNameWithPath, destFileName: newPath);

            // 5. SIDECAR SYNC
            if (de.SidecarFile != null && de.SidecarFile.Exists)
            {
                string newBaseWithoutExt = Path.GetFileNameWithoutExtension(path: newFileName);
                string newSidecarPath = Path.Combine(path1: directory, path2: newBaseWithoutExt + ".xmp");
                File.Move(sourceFileName: de.SidecarFile.FullName, destFileName: newSidecarPath);
            }

            // 6. UPDATE MODEL & UI
            de.UpdatePathAfterRename(newPath: newPath);
            lvi.Text = newFileName;
            lvi.Name = newPath;
            e.CancelEdit = true; // Tell WinForms NOT to use the raw e.Label

            Log.Info(message: $"Renamed successfully: {newFileName}");
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex, message: $"Rename failed for {de.ItemNameWithoutPath}");
            _ = MessageBox.Show(text: ex.Message, caption: "Rename Error", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);

            // Revert UI to safety
            lvi.Text = de.ItemNameWithoutPath;
            e.CancelEdit = true;
        }
    }

    /// <summary>
    ///     Evaluates the current selection and deletes valid files and subdirectories.
    ///     Structural elements like Drives or My Computer are ignored for safety.
    ///     Supports Recycle Bin integration and permanent deletion via Shift+Delete.
    /// </summary>
    private void DeleteSelectedFiles()
    {
        // 1. Filter the selection to only include deletable types (Files and SubDirectories)
        List<ListViewItem> deletableItems = SelectedItems.Cast<ListViewItem>()
            .Where(lvi => lvi.Tag is DirectoryElement de &&
                         (de.Type == DirectoryElement.ElementType.File ||
                          de.Type == DirectoryElement.ElementType.SubDirectory))
            .ToList();

        if (deletableItems.Count == 0)
        {
            Log.Warn(message: "Delete requested, but no deletable items (files/folders) were selected.");
            return;
        }

        // 2. Determine deletion mode (Shift + Delete logic)
        bool isShiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;
        Microsoft.VisualBasic.FileIO.RecycleOption recycleOption = isShiftPressed
            ? Microsoft.VisualBasic.FileIO.RecycleOption.DeletePermanently
            : Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin;

        string actionVerb = isShiftPressed ? "PERMANENTLY delete" : "recycle";
        DialogResult result = MessageBox.Show(
            text: $"Are you sure you want to {actionVerb} {deletableItems.Count} item(s)?",
            caption: "Confirm Deletion",
            buttons: MessageBoxButtons.YesNo,
            icon: isShiftPressed ? MessageBoxIcon.Stop : MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        BeginUpdate();
        try
        {
            foreach (ListViewItem lvi in deletableItems)
            {
                DirectoryElement de = (DirectoryElement)lvi.Tag;
                try
                {
                    // 3. Delete based on Type
                    if (de.Type == DirectoryElement.ElementType.File)
                    {
                        if (File.Exists(path: de.FileNameWithPath))
                        {
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                                file: de.FileNameWithPath,
                                showUI: Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                recycle: recycleOption);
                        }

                        // Handle the Sidecar (XMP)
                        if (de.SidecarFile != null && de.SidecarFile.Exists)
                        {
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                                file: de.SidecarFile.FullName,
                                showUI: Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                recycle: recycleOption);
                        }
                    }
                    else if (de.Type == DirectoryElement.ElementType.SubDirectory)
                    {
                        if (Directory.Exists(path: de.FileNameWithPath))
                        {
                            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                                directory: de.FileNameWithPath,
                                showUI: Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                recycle: recycleOption);
                        }
                    }

                    // 4. Update memory and UI only if the physical operation succeeded
                    _ = (_masterCollection?.Remove(item: de));
                    Items.Remove(item: lvi);
                }
                catch (Exception ex)
                {
                    Log.Error(exception: ex, message: $"Deletion failed for: {de.ItemNameWithoutPath}");
                }
            }
        }
        finally
        {
            EndUpdate();
        }
    }

    #endregion

    #region Updating & Filtering

    /// <summary>
    ///     Returns the translated string for a given FilterOperator.
    /// </summary>
    private string GetTextFromOperator(FilterOperator op)
    {
        return op switch
        {
            FilterOperator.Is => HelperControlAndMessageBoxHandling.ReturnControlText("Generic_Is", HelperControlAndMessageBoxHandling.FakeControlTypes.Generic),
            FilterOperator.IsNot => HelperControlAndMessageBoxHandling.ReturnControlText("Generic_IsNot", HelperControlAndMessageBoxHandling.FakeControlTypes.Generic),
            FilterOperator.IsEmpty => HelperControlAndMessageBoxHandling.ReturnControlText("Generic_IsEmpty", HelperControlAndMessageBoxHandling.FakeControlTypes.Generic),
            FilterOperator.IsNotEmpty => HelperControlAndMessageBoxHandling.ReturnControlText("Generic_IsNotEmpty", HelperControlAndMessageBoxHandling.FakeControlTypes.Generic),
            FilterOperator.Contains => HelperControlAndMessageBoxHandling.ReturnControlText("Generic_Contains", HelperControlAndMessageBoxHandling.FakeControlTypes.Generic),
            FilterOperator.DoesNotContain => HelperControlAndMessageBoxHandling.ReturnControlText("Generic_DoesntContain", HelperControlAndMessageBoxHandling.FakeControlTypes.Generic),
            _ => string.Empty
        };
    }

    /// <summary>
    /// Filters the list based on the clicked column.
    /// </summary>
    /// <param name="columnIndex">The index of the column to filter</param>
    /// <param name="operatorValue">The filter operation (is, contains, etc)</param>
    /// <param name="searchValue">The text to match</param>
    private void ApplyAutoFilter(int columnIndex, FilterOperator operatorValue, string searchValue)
    {
        // 1. Guard: Use the existing check
        FrmMainApp frmMain = (FrmMainApp)Application.OpenForms["FrmMainApp"];
        if (frmMain != null && !frmMain.EnsureMetadataIsReadyAndWarnUserIfNot())
        {
            return;
        }

        if (_masterCollection == null)
        {
            return;
        }

        ColumnHeader header = Columns[columnIndex];
        string search = (searchValue ?? "").Trim().ToLower(CultureInfo.InvariantCulture);
        string fileNameHeaderName = HelperVariables.COL_NAME_PREFIX + FileListColumns.FILENAME;

        // 2. Perform Filter
        List<DirectoryElement> filtered = _masterCollection.Where(de =>
        {
            // ALWAYS include folders, drives, and the parent directory
            if (de.Type != DirectoryElement.ElementType.File)
            {
                return true;
            }

            // Perform the existing logic for files
            string rawVal = (header.Name == fileNameHeaderName)
                ? (de.ItemNameWithoutPath ?? "")
                : PickModelValueForColumn(de, header);

            string val = (rawVal ?? "").Trim().ToLower(CultureInfo.InvariantCulture);
            bool isValueEmpty = string.IsNullOrWhiteSpace(val) || val == UNKNOWN_VALUE_FILE || val == UNKNOWN_VALUE_FILE_COORDINATES;

            return operatorValue switch
            {
                FilterOperator.IsEmpty => isValueEmpty,
                FilterOperator.IsNotEmpty => !isValueEmpty,
                FilterOperator.Is => val == search,
                FilterOperator.IsNot => val != search,
                FilterOperator.Contains => val.Contains(search),
                FilterOperator.DoesNotContain => !val.Contains(search),
                _ => true
            };
        }).ToList();

        // 3. THE MISSING LINK: Actually update the UI with the result
        UpdateListViewWithFilteredData(filtered);
    }

    /// <summary>
    ///     Redraws the ListView with a filtered subset of data, 
    ///     ensuring metadata and "Dirty" (Red) states are preserved.
    /// </summary>
    private void UpdateListViewWithFilteredData(List<DirectoryElement> filteredItems)
    {
        if (InvokeRequired)
        {
            _ = Invoke(new Action(() => UpdateListViewWithFilteredData(filteredItems: filteredItems)));
            return;
        }

        Log.Info(message: $"UI UPDATE: Redrawing list with {filteredItems.Count} items.");

        BeginUpdate();
        try
        {
            Items.Clear();
            foreach (DirectoryElement de in filteredItems)
            {
                // 1. Add the item (this handles the icon and basic placeholders)
                ListViewItem lvi = AddListItem(directoryElement: de);

                // 2. Restore metadata if it exists
                if (de.Type == DirectoryElement.ElementType.File && de.IsHydrated)
                {
                    for (int i = 1; i < Columns.Count; i++)
                    {
                        lvi.SubItems[i].Text = PickModelValueForColumn(directoryElement: de, columnHeader: Columns[i]);
                    }
                }

                // 3. RESTORE THE "DIRTY" COLOUR
                // If the element has unsaved changes, make it red again.
                if (de.HasDirtyAttributes())
                {
                    lvi.ForeColor = Color.Red;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, message: "Error during ListView redraw.");
        }
        finally
        {
            EndUpdate();
            Invalidate();
        }
    }

    /// <summary>
    ///     Reloads the listview's items with data from the DirectoryElementCollection
    ///     Note that the DirectoryElementCollection is assumed to be
    ///     in scope of the FileListView. Calling FileListView.Clear will
    ///     also clear it.
    /// </summary>
    public void ReloadFromDEs(IEnumerable<DirectoryElement> elements)
    {
        if (InvokeRequired)
        {
            _ = Invoke(new Action(() => ReloadFromDEs(elements: elements)));
            return;
        }

        BeginUpdate();
        try
        {
            Items.Clear();

            foreach (DirectoryElement de in elements)
            {
                // 1. Add the base item
                ListViewItem lvi = AddListItem(directoryElement: de);

                // 2. Restore metadata if hydrated
                if (de.Type == DirectoryElement.ElementType.File && de.IsHydrated)
                {
                    for (int i = 1; i < Columns.Count; i++)
                    {
                        lvi.SubItems[i].Text = PickModelValueForColumn(directoryElement: de, columnHeader: Columns[i]);
                    }
                }

                // 3. RESTORE THE RED COLOUR
                // This is the missing piece for Clear All
                if (de.HasDirtyAttributes())
                {
                    lvi.ForeColor = Color.Red;
                }
            }
        }
        finally
        {
            EndUpdate();
            Invalidate();
        }
    }

    /// <summary>
    /// Synchronizes the ListViewItem's icon with the DirectoryElement.
    /// Ensures thread-safety and prevents icon inheritance by explicitly clearing keys if null.
    /// </summary>
    /// <param name="directoryElement">The data directoryElement providing the image or type.</param>
    public void SyncElementThumbnail(DirectoryElement directoryElement)
    {
        // 1. Thread-safety check
        if (InvokeRequired)
        {
            _ = Invoke(new Action(() => SyncElementThumbnail(directoryElement: directoryElement)));
            return;
        }

        if (directoryElement == null)
        {
            return;
        }

        // 2. Locate the specific UI row
        ListViewItem? lvi = null;
        string guid = null;
        if (directoryElement.Type == DirectoryElement.ElementType.File)
        {
            // Files use GUID because they have metadata
            guid = directoryElement.GetAttributeValueAsString(attribute: SourcesAndAttributes.ElementAttribute.GUID);
            lvi = FindItemByGuid(guid: guid);
        }
        else
        {
            // Drives/Folders/MyComputer don't have GUIDs, so we find them by reference
            lvi = Items.Cast<ListViewItem>()
                           .FirstOrDefault(i => i.Tag == directoryElement);
        }

        // Still not found? Try a final fallback by path (useful for structural items)
        lvi ??= Items.Cast<ListViewItem>()
                       .FirstOrDefault(i => i.Tag is DirectoryElement de
                                        && de.FileNameWithPath == directoryElement.FileNameWithPath);

        if (lvi == null)
        {
            return;
        }

        // 3. Initialize the ImageList if missing
        LargeImageList ??= new ImageList
        {
            ImageSize = new Size(width: ThumbnailSize, height: ThumbnailSize),
            ColorDepth = ColorDepth.Depth32Bit
        };

        // 4. Handle Icon Assignment
        // If Thumbnail is null, we MUST set ImageIndex to -1 to stop ghosting/inheritance.
        if (directoryElement.Thumbnail == null)
        {
            lvi.ImageKey = string.Empty;
            lvi.ImageIndex = -1;
        }
        else
        {
            // Use GUID as unique key for files, or ElementType string for system icons
            string imageKey = (directoryElement.Type == DirectoryElement.ElementType.File)
                ? guid
                : directoryElement.Type.ToString();

            if (!LargeImageList.Images.ContainsKey(key: imageKey))
            {
                LargeImageList.Images.Add(key: imageKey, image: directoryElement.Thumbnail);
            }

            lvi.ImageKey = imageKey;
        }
    }

    /// <summary>
    /// Searches the current ListView items for a specific row associated with the given GUID.
    /// </summary>
    /// <param name="guid">The unique identifier (GUID) of the DirectoryElement to find.</param>
    /// <returns>
    /// The <see cref="ListViewItem"/> associated with the GUID if found; otherwise, <c>null</c>.
    /// </returns>
    public ListViewItem? FindItemByGuid(string guid)
    {
        if (string.IsNullOrWhiteSpace(value: guid))
        {
            return null;
        }

        // We iterate through the Items collection of the ListView.
        // Note: Since this is a UI control, this must be called from the UI thread 
        // or via an Invoke/BeginInvoke call.
        foreach (ListViewItem lvi in Items)
        {
            // 1. Ensure the Tag is not null and is indeed a DirectoryElement.
            if (lvi.Tag is DirectoryElement element)
            {
                // 2. Retrieve the GUID from the directoryElement's attributes.
                string elementGuid = element.GetAttributeValueAsString(
                    attribute: SourcesAndAttributes.ElementAttribute.GUID);

                // 3. Compare using InvariantCultureIgnoreCase to avoid casing mismatches.
                if (string.Equals(
                    a: elementGuid,
                    b: guid,
                    comparisonType: StringComparison.InvariantCultureIgnoreCase))
                {
                    return lvi;
                }
            }
        }

        // Return null if no matching item is currently visible in the ListView.
        return null;
    }

    /// <summary>
    ///     Clears the FileListView.
    ///     Should be used instead Items.Clear, etc.
    /// </summary>
    public void ClearData()
    {
        _masterCollection.Clear();
        Items.Clear();
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
    ///     Scrolls the list so the target item is positioned consistently,
    ///     avoiding the "just barely on screen" confusion of EnsureVisible.
    /// </summary>
    public void ScrollToItemCentred(ListViewItem item)
    {
        if (item == null)
        {
            return;
        }

        // 1. Ensure it's on screen at all
        item.EnsureVisible();

        // 2. The "Top Item" Trick: 
        // If you want the item at the VERY TOP of the list:
        TopItem = item;

        // 3. If you want it in the middle (requires more math/API), 
        // but usually TopItem is what users expect when returning to a spot.
    }

    /// <summary>
    ///     Deals with invoking the listview (from outside the thread) and updating the colour of a particular row (Item) to
    ///     the assigned colour.
    /// </summary>
    /// <param name="directoryElement">The particular ListViewItem (by directoryElement/Tag) that needs updating</param>
    /// <param name="color">Parameter to assign a particular colour (prob red or black) to the whole row</param>
    public void UpdateDirectoryElementItemColour(DirectoryElement directoryElement,
        Color color)
    {
        // If the current thread is not the UI thread, InvokeRequired will be true
        if (InvokeRequired)
        {
            _ = Invoke(method: (Action)(() =>
                UpdateDirectoryElementItemColour(directoryElement: directoryElement, color: color)));
            return;
        }

        ListViewItem lvi = FindItemByDirectoryElement(directoryElement: directoryElement);
        _ = (lvi?.ForeColor = color);
    }

    /// <summary>
    ///     Instead of trying to find items by their names (texts) we find them by Tag. This is needed because if the user is
    ///     in Flat mode then it's wholly possible to have identical files across folders.
    /// </summary>
    /// <param name="directoryElement"></param>
    /// <returns></returns>
    public ListViewItem FindItemByDirectoryElement(DirectoryElement directoryElement)
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

    /// <summary>
    /// Processes Windows messages to implement custom context-menu behavior.
    /// Specifically handles the WM_CONTEXTMENU message (0x007B) by decoding
    /// the screen coordinates encoded in <paramref name="m"/>.LParam, converting
    /// them to client coordinates, and invoking <see cref="ShowFilterMenu(Point)"/>
    /// to display the filter/context menu at that location. Messages other than
    /// WM_CONTEXTMENU are forwarded to the base window procedure.
    /// </summary>
    /// <param name="m">The Windows message being processed.</param>
    /// <remarks>
    /// The X and Y coordinates are extracted from the 32-bit LParam by splitting
    /// it into two 16-bit values using bit shifts and byte conversion. The
    /// method converts the screen coordinates to client coordinates via
    /// <see cref="PointToClient(Point)"/> before calling <see cref="ShowFilterMenu(Point)"/>.
    /// </remarks>
    protected override void WndProc(ref Message m)
    {
        // WM_CONTEXTMENU
        if (m.Msg == 0x007B)
        {
            // Get screen coordinates from LParam
            int x = BitConverter.ToInt16(new byte[] { (byte)((int)m.LParam & 0xFF), (byte)(((int)m.LParam >> 8) & 0xFF) }, 0);
            int y = BitConverter.ToInt16(new byte[] { (byte)(((int)m.LParam >> 16) & 0xFF), (byte)(((int)m.LParam >> 24) & 0xFF) }, 0);

            Point clientPoint = PointToClient(new Point(x, y));

            // Handle coordinates that might be outside client area (like the header)
            ShowFilterMenu(clientPoint);
            return;
        }
        base.WndProc(ref m);
    }

    #endregion

    #region Filtering Menu (only)

    /// <summary>
    /// Display a filter context menu at the specified location that provides per-column filter commands, imports
    /// standard actions from the main form, localizes menu items, and shows the menu.
    /// </summary>
    /// <remarks>Builds a Filter submenu containing emptiness checks, current-value shortcuts (when
    /// available), standard text operations, and a Clear All option. For operations that require user input, prompts
    /// using Microsoft.VisualBasic.Interaction.InputBox and aborts if the user provides no value. Applies filters via
    /// ApplyAutoFilter, may reload the master collection to clear filters, clones/imports items from
    /// FrmMainApp.cms_FileListView, calls LocaliseDynamicMenu, and then displays the menu.</remarks>
    /// <param name="location">Location within the control where the context menu is displayed and used for hit-testing to determine the target
    /// column and item.</param>
    private void ShowFilterMenu(Point location)
    {
        ContextMenuStrip menu = new();
        ListViewHitTestInfo hitInfo = HitTest(location);
        int columnIndex = GetColumnAtX(x: location.X);

        if (columnIndex != -1)
        {
            ColumnHeader header = Columns[columnIndex];
            string colName = header.Text;
            string clickedCellValue = (hitInfo.Item != null && hitInfo.SubItem != null && hitInfo.Item.SubItems.Count > columnIndex)
                                       ? hitInfo.Item.SubItems[columnIndex].Text
                                       : string.Empty;

            // 1. Create the Filter Sub-Menu
            ToolStripMenuItem filterSubMenu = new(text: $"Filter: {colName}") { Name = "cmi_Filter_SubMenu" };

            // --- LOCAL HELPER ---
            void AddFilterItem(FilterOperator op, string? value)
            {
                FilterOperator capturedOp = op;
                string? capturedValue = value;
                string opText = GetTextFromOperator(op: capturedOp);

                // If value is null, this is a generic "Is..." or "Contains..." call that needs user input
                string displayText = string.IsNullOrEmpty(value: capturedValue) && capturedOp != FilterOperator.IsEmpty && capturedOp != FilterOperator.IsNotEmpty
                    ? $"{opText}..."
                    : string.IsNullOrEmpty(value: capturedValue) ? $"{opText}" : $"{opText} \"{capturedValue}\"";
                _ = filterSubMenu.DropDownItems.Add(text: displayText, image: null, onClick: (s, ev) =>
                {
                    string finalSearchValue = capturedValue ?? "";

                    // If no value was provided (the "..." items), ask the user for one
                    if (capturedValue == null)
                    {
                        // Using the Microsoft.VisualBasic Interaction.InputBox 
                        // Ensure you have the reference to Microsoft.VisualBasic in the project
                        finalSearchValue = Microsoft.VisualBasic.Interaction.InputBox(
                            Prompt: $"{opText}:",
                            Title: "Filter",
                            DefaultResponse: "");

                        // If the user hits 'Cancel', the InputBox returns an empty string. 
                        // You might want to abort if they didn't type anything.
                        if (string.IsNullOrEmpty(finalSearchValue))
                        {
                            return;
                        }
                    }

                    ApplyAutoFilter(
                        columnIndex: columnIndex,
                        operatorValue: capturedOp,
                        searchValue: finalSearchValue);
                });
            }

            // 2. Is Empty / Is Not Empty
            AddFilterItem(op: FilterOperator.IsEmpty, value: "");
            AddFilterItem(op: FilterOperator.IsNotEmpty, value: "");
            _ = filterSubMenu.DropDownItems.Add(new ToolStripSeparator());

            // 3. Current Value Logic
            if (!string.IsNullOrEmpty(value: clickedCellValue) && clickedCellValue != UNKNOWN_VALUE_FILE)
            {
                AddFilterItem(op: FilterOperator.Is, value: clickedCellValue);
                AddFilterItem(op: FilterOperator.IsNot, value: clickedCellValue);
                _ = filterSubMenu.DropDownItems.Add(new ToolStripSeparator());
            }

            // 4. Standard Text Operations
            FilterOperator[] operations = {
            FilterOperator.Is,
            FilterOperator.IsNot,
            FilterOperator.Contains,
            FilterOperator.DoesNotContain
        };

            foreach (FilterOperator op in operations)
            {
                // The local helper now handles the capture correctly
                AddFilterItem(op: op, value: null);
            }

            _ = filterSubMenu.DropDownItems.Add(new ToolStripSeparator());

            // 5. Clear Logic
            string clearText = HelperControlAndMessageBoxHandling.ReturnControlText("Generic_ClearAll", HelperControlAndMessageBoxHandling.FakeControlTypes.Generic);
            _ = filterSubMenu.DropDownItems.Add(text: clearText ?? "Clear All Filters", image: null, onClick: (s, ev) =>
            {
                if (_masterCollection != null)
                {
                    ReloadFromDEs(_masterCollection);
                }
            });

            _ = menu.Items.Add(filterSubMenu);
            _ = menu.Items.Add(new ToolStripSeparator());
        }

        // --- Import standard actions from FrmMainApp ---
        if (Application.OpenForms["FrmMainApp"] is FrmMainApp frmMain && frmMain.cms_FileListView != null)
        {
            foreach (ToolStripItem item in frmMain.cms_FileListView.Items)
            {
                if (item is ToolStripSeparator)
                {
                    _ = menu.Items.Add(new ToolStripSeparator());
                }
                else if (item is ToolStripMenuItem tsmi)
                {
                    _ = menu.Items.Add(CloneToolStripItem(tsmi));
                }
            }
        }

        LocaliseDynamicMenu(menu: menu);
        menu.Show(control: this, location);
    }

    private ToolStripMenuItem CloneToolStripItem(ToolStripMenuItem source)
    {
        ToolStripMenuItem clone = new()
        {
            Text = source.Text,
            Name = source.Name,
            Image = source.Image,
            Enabled = source.Enabled,
            Tag = source.Tag,
            BackColor = source.BackColor,
            ForeColor = source.ForeColor,
        };

        // Manually hook into the original's click event logic
        // This is a bit of a trick: we invoke the original lvi's PerformClick
        clone.Click += (s, e) => source.PerformClick();

        if (source.HasDropDownItems)
        {
            foreach (ToolStripItem subItem in source.DropDownItems)
            {
                if (subItem is ToolStripSeparator)
                {
                    _ = clone.DropDownItems.Add(new ToolStripSeparator());
                }
                else if (subItem is ToolStripMenuItem subTsmi)
                {
                    _ = clone.DropDownItems.Add(value: CloneToolStripItem(subTsmi));
                }
            }
        }

        return clone;
    }

    private void LocaliseDynamicMenu(ContextMenuStrip menu)
    {
        foreach (ToolStripItem item in menu.Items)
        {
            if (item is ToolStripMenuItem && !string.IsNullOrEmpty(item.Name))
            {
                string localizedText = HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: item.Name,
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.ToolStripMenuItem);

                if (!string.IsNullOrEmpty(localizedText))
                {
                    item.Text = localizedText;
                }
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern int GetScrollPos(IntPtr hWnd, int nBar);
    private const int SB_HORZ = 0;

    private int GetColumnAtX(int x)
    {
        // Factor in the horizontal scroll position
        int scrollX = GetScrollPos(Handle, SB_HORZ);
        int adjustedX = x + scrollX;

        int currentX = 0;
        List<ColumnHeader> orderedCols = Columns.Cast<ColumnHeader>()
                                               .OrderBy(c => c.DisplayIndex)
                                               .ToList();

        foreach (ColumnHeader col in orderedCols)
        {
            currentX += col.Width;
            if (adjustedX <= currentX)
            {
                return col.Index;
            }
        }
        return -1;
    }

    #endregion
}