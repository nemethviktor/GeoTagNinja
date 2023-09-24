using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using NLog;

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
    public const string UNKNOWN_VALUE_FILE = "-";

    // Note - if this is changed, all checks for unknown need to be udpated
    // because currently this works via item.replace and check versus ""
    // but replace did not take ""
    public const string UNKNOWN_VALUE_DIR = "";

    /// <summary>
    ///     Every column has this prefix for its name when it is created.
    /// </summary>
    public const string COL_NAME_PREFIX = "clh_";


    /// <summary>
    ///     Constructor
    /// </summary>
    public FileListView()
    {
        Logger.Info(message: "Creating List View ...");
        InitializeComponent();
    }

    /// <summary>
    ///     Class containing native method (shell32, etc) definitions in order
    ///     to retrieve file and directory information.
    ///     This is to deal with the icons in listview
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


    /// <summary>
    ///     Class containing all the relevant column names to be used
    ///     when e.g. querying for information.
    /// </summary>
    public static class FileListColumns
    {
        public const string FILENAME = "FileName";
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
        public const string SUB_LOCATION = "Sub_location";
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
        public const string OFFSET_TIME = "OffsetTime";
    }

#region Internal Variables

    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     The used application language
    /// </summary>
    private string _AppLanguage = "";

    /// <summary>
    ///     The list of columns to show (without prefix)
    /// </summary>
    internal List<string> _cfg_Col_Names = new();

    /// <summary>
    ///     The default order of the columns to show (without prefix)
    /// </summary>
    internal static Dictionary<string, int> _cfg_Col_Order_Default = new();

    /// <summary>
    ///     The used sorter
    /// </summary>
    internal ListViewColumnSorter LvwColumnSorter;

    /// <summary>
    ///     Tracks if the initializer ReadAndApplySetting was called.
    /// </summary>
    internal bool _isInitialized;

    /// <summary>
    ///     Counter for files in the list - incremented in addListItem method.
    /// </summary>
    internal int _fileCount = -1;

    /// <summary>
    ///     Pointer to the SHFILEINFO Structure that is initialized to be
    ///     used for this list view.
    /// </summary>
    private NativeMethods.SHFILEINFOW shfi;

#endregion


#region External Visible Properties

    /// <summary>
    ///     The list of directory elements to display.
    /// </summary>
    public DirectoryElementCollection DirectoryElements { get; private set; } = new();

    /// <summary>
    ///     The number of elements of type file in the view as
    ///     loaded.
    /// </summary>
    public int FileCount => _fileCount;

#endregion


#region Internal Update Logic

    internal static string ElementAttributeToColumnHeaderName(SourcesAndAttributes.ElementAttribute elementAttribute)
    {
        switch (elementAttribute)
        {
            case SourcesAndAttributes.ElementAttribute.GUID:
                return COL_NAME_PREFIX + FileListColumns.GUID;
            case SourcesAndAttributes.ElementAttribute.GPSAltitude:
                return COL_NAME_PREFIX + FileListColumns.GPS_ALTITUDE;
            case SourcesAndAttributes.ElementAttribute.GPSAltitudeRef:
                return COL_NAME_PREFIX + FileListColumns.GPS_ALTITUDE_REF;
            case SourcesAndAttributes.ElementAttribute.GPSDestLatitude:
                return COL_NAME_PREFIX + FileListColumns.GPS_DEST_LATITUDE;
            case SourcesAndAttributes.ElementAttribute.GPSDestLatitudeRef:
                return COL_NAME_PREFIX + FileListColumns.GPS_DEST_LATITUDE_REF;
            case SourcesAndAttributes.ElementAttribute.GPSDestLongitude:
                return COL_NAME_PREFIX + FileListColumns.GPS_DEST_LONGITUDE;
            case SourcesAndAttributes.ElementAttribute.GPSDestLongitudeRef:
                return COL_NAME_PREFIX + FileListColumns.GPS_DEST_LONGITUDE_REF;
            case SourcesAndAttributes.ElementAttribute.GPSImgDirection:
                return COL_NAME_PREFIX + FileListColumns.GPS_IMGDIRECTION;
            case SourcesAndAttributes.ElementAttribute.GPSImgDirectionRef:
                return COL_NAME_PREFIX + FileListColumns.GPS_IMGDIRECTION_REF;
            case SourcesAndAttributes.ElementAttribute.GPSLatitude:
                return COL_NAME_PREFIX + FileListColumns.GPS_LATITUDE;
            case SourcesAndAttributes.ElementAttribute.GPSLatitudeRef:
                return COL_NAME_PREFIX + FileListColumns.GPS_LATITUDE_REF;
            case SourcesAndAttributes.ElementAttribute.GPSLongitude:
                return COL_NAME_PREFIX + FileListColumns.GPS_LONGITUDE;
            case SourcesAndAttributes.ElementAttribute.GPSLongitudeRef:
                return COL_NAME_PREFIX + FileListColumns.GPS_LONGITUDE_REF;
            case SourcesAndAttributes.ElementAttribute.GPSSpeed:
                return COL_NAME_PREFIX + FileListColumns.GPS_SPEED;
            case SourcesAndAttributes.ElementAttribute.GPSSpeedRef:
                return COL_NAME_PREFIX + FileListColumns.GPS_SPEED_REF;
            case SourcesAndAttributes.ElementAttribute.Coordinates:
                return COL_NAME_PREFIX + FileListColumns.COORDINATES;
            case SourcesAndAttributes.ElementAttribute.DestCoordinates:
                return COL_NAME_PREFIX + FileListColumns.DEST_COORDINATES;
            case SourcesAndAttributes.ElementAttribute.City:
                return COL_NAME_PREFIX + FileListColumns.CITY;
            case SourcesAndAttributes.ElementAttribute.CountryCode:
                return COL_NAME_PREFIX + FileListColumns.COUNTRY_CODE;
            case SourcesAndAttributes.ElementAttribute.Country:
                return COL_NAME_PREFIX + FileListColumns.COUNTRY;
            case SourcesAndAttributes.ElementAttribute.State:
                return COL_NAME_PREFIX + FileListColumns.STATE;
            case SourcesAndAttributes.ElementAttribute.Sub_location:
                return COL_NAME_PREFIX + FileListColumns.SUB_LOCATION;
            case SourcesAndAttributes.ElementAttribute.Make:
                return COL_NAME_PREFIX + FileListColumns.MAKE;
            case SourcesAndAttributes.ElementAttribute.Model:
                return COL_NAME_PREFIX + FileListColumns.MODEL;
            case SourcesAndAttributes.ElementAttribute.Rating:
                return COL_NAME_PREFIX + FileListColumns.RATING;
            case SourcesAndAttributes.ElementAttribute.ExposureTime:
                return COL_NAME_PREFIX + FileListColumns.EXPOSURETIME;
            case SourcesAndAttributes.ElementAttribute.Fnumber:
                return COL_NAME_PREFIX + FileListColumns.FNUMBER;
            case SourcesAndAttributes.ElementAttribute.FocalLength:
                return COL_NAME_PREFIX + FileListColumns.FOCAL_LENGTH;
            case SourcesAndAttributes.ElementAttribute.FocalLengthIn35mmFormat:
                return COL_NAME_PREFIX + FileListColumns.FOCAL_LENGTH_IN_35MM_FORMAT;
            case SourcesAndAttributes.ElementAttribute.ISO:
                return COL_NAME_PREFIX + FileListColumns.ISO;
            case SourcesAndAttributes.ElementAttribute.LensSpec:
                return COL_NAME_PREFIX + FileListColumns.LENS_SPEC;
            case SourcesAndAttributes.ElementAttribute.TakenDate:
                return COL_NAME_PREFIX + FileListColumns.TAKEN_DATE;
            case SourcesAndAttributes.ElementAttribute.CreateDate:
                return COL_NAME_PREFIX + FileListColumns.CREATE_DATE;
            case SourcesAndAttributes.ElementAttribute.OffsetTime:
                return COL_NAME_PREFIX + FileListColumns.OFFSET_TIME;

            default:
                throw new ArgumentException(message: "Unimplemented column name");
                break;
        }
    }

    internal static SourcesAndAttributes.ElementAttribute ColumnHeaderToElementAttribute(ColumnHeader columnHeader)
    {
        switch (columnHeader.Name.Substring(startIndex: COL_NAME_PREFIX.Length))
        {
            case FileListColumns.GUID:
                return SourcesAndAttributes.ElementAttribute.GUID;
            case FileListColumns.GPS_ALTITUDE:
                return SourcesAndAttributes.ElementAttribute.GPSAltitude;
            case FileListColumns.GPS_ALTITUDE_REF:
                return SourcesAndAttributes.ElementAttribute.GPSAltitudeRef;
            case FileListColumns.GPS_DEST_LATITUDE:
                return SourcesAndAttributes.ElementAttribute.GPSDestLatitude;
            case FileListColumns.GPS_DEST_LATITUDE_REF:
                return SourcesAndAttributes.ElementAttribute.GPSDestLatitudeRef;
            case FileListColumns.GPS_DEST_LONGITUDE:
                return SourcesAndAttributes.ElementAttribute.GPSDestLongitude;
            case FileListColumns.GPS_DEST_LONGITUDE_REF:
                return SourcesAndAttributes.ElementAttribute.GPSDestLongitudeRef;
            case FileListColumns.GPS_IMGDIRECTION:
                return SourcesAndAttributes.ElementAttribute.GPSImgDirection;
            case FileListColumns.GPS_IMGDIRECTION_REF:
                return SourcesAndAttributes.ElementAttribute.GPSImgDirectionRef;
            case FileListColumns.GPS_LATITUDE:
                return SourcesAndAttributes.ElementAttribute.GPSLatitude;
            case FileListColumns.GPS_LATITUDE_REF:
                return SourcesAndAttributes.ElementAttribute.GPSLatitudeRef;
            case FileListColumns.GPS_LONGITUDE:
                return SourcesAndAttributes.ElementAttribute.GPSLongitude;
            case FileListColumns.GPS_LONGITUDE_REF:
                return SourcesAndAttributes.ElementAttribute.GPSLongitudeRef;
            case FileListColumns.GPS_SPEED:
                return SourcesAndAttributes.ElementAttribute.GPSSpeed;
            case FileListColumns.GPS_SPEED_REF:
                return SourcesAndAttributes.ElementAttribute.GPSSpeedRef;
            case FileListColumns.COORDINATES:
                return SourcesAndAttributes.ElementAttribute.Coordinates;
            case FileListColumns.DEST_COORDINATES:
                return SourcesAndAttributes.ElementAttribute.DestCoordinates;
            case FileListColumns.CITY:
                return SourcesAndAttributes.ElementAttribute.City;
            case FileListColumns.COUNTRY_CODE:
                return SourcesAndAttributes.ElementAttribute.CountryCode;
            case FileListColumns.COUNTRY:
                return SourcesAndAttributes.ElementAttribute.Country;
            case FileListColumns.STATE:
                return SourcesAndAttributes.ElementAttribute.State;
            case FileListColumns.SUB_LOCATION:
                return SourcesAndAttributes.ElementAttribute.Sub_location;
            case FileListColumns.MAKE:
                return SourcesAndAttributes.ElementAttribute.Make;
            case FileListColumns.MODEL:
                return SourcesAndAttributes.ElementAttribute.Model;
            case FileListColumns.RATING:
                return SourcesAndAttributes.ElementAttribute.Rating;
            case FileListColumns.EXPOSURETIME:
                return SourcesAndAttributes.ElementAttribute.ExposureTime;
            case FileListColumns.FNUMBER:
                return SourcesAndAttributes.ElementAttribute.Fnumber;
            case FileListColumns.FOCAL_LENGTH:
                return SourcesAndAttributes.ElementAttribute.FocalLength;
            case FileListColumns.FOCAL_LENGTH_IN_35MM_FORMAT:
                return SourcesAndAttributes.ElementAttribute.FocalLengthIn35mmFormat;
            case FileListColumns.ISO:
                return SourcesAndAttributes.ElementAttribute.ISO;
            case FileListColumns.LENS_SPEC:
                return SourcesAndAttributes.ElementAttribute.LensSpec;
            case FileListColumns.TAKEN_DATE:
                return SourcesAndAttributes.ElementAttribute.TakenDate;
            case FileListColumns.CREATE_DATE:
                return SourcesAndAttributes.ElementAttribute.CreateDate;
            case FileListColumns.OFFSET_TIME:
                return SourcesAndAttributes.ElementAttribute.OffsetTime;
            default:
                throw new ArgumentException(message: "Unimplemented column name");
                break;
        }
    }

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
            throw new ArgumentException(message: "The contents of the filename column cannot be requested from the method 'pickModelValueForColumn'.");
        }

        // Set the value if no model value is found
        string nfVal = UNKNOWN_VALUE_DIR;
        if (directoryElement.Type == DirectoryElement.ElementType.File)
        {
            nfVal = UNKNOWN_VALUE_FILE;
        }

        string DefaultStrGetter(SourcesAndAttributes.ElementAttribute atrb)
        {
            return directoryElement.GetAttributeValueString(attribute: atrb, notFoundValue: nfVal);
        }

        switch (columnHeader.Name.Substring(startIndex: 4))
        {
            case FileListColumns.GUID:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GUID);
            case FileListColumns.GPS_ALTITUDE:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSAltitude);
            case FileListColumns.GPS_ALTITUDE_REF:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSAltitudeRef);
            case FileListColumns.GPS_DEST_LATITUDE:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSDestLatitude);
            case FileListColumns.GPS_DEST_LATITUDE_REF:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSDestLatitudeRef);
            case FileListColumns.GPS_DEST_LONGITUDE:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSDestLongitude);
            case FileListColumns.GPS_DEST_LONGITUDE_REF:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSDestLongitudeRef);
            case FileListColumns.GPS_IMGDIRECTION:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSImgDirection);
            case FileListColumns.GPS_IMGDIRECTION_REF:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSImgDirectionRef);
            case FileListColumns.GPS_LATITUDE:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSLatitude);
            case FileListColumns.GPS_LATITUDE_REF:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSLatitudeRef);
            case FileListColumns.GPS_LONGITUDE:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSLongitude);
            case FileListColumns.GPS_LONGITUDE_REF:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSLongitudeRef);
            case FileListColumns.GPS_SPEED:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSSpeed);
            case FileListColumns.GPS_SPEED_REF:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.GPSSpeedRef);
            case FileListColumns.COORDINATES:
                return ModelToColumnValueTransformations.M2C_CoordinatesInclDest(column: FileListColumns.COORDINATES, item: directoryElement, nfVal: nfVal);
            case FileListColumns.DEST_COORDINATES:
                return ModelToColumnValueTransformations.M2C_CoordinatesInclDest(column: FileListColumns.DEST_COORDINATES, item: directoryElement, nfVal: nfVal);
            case FileListColumns.CITY:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.City);
            case FileListColumns.COUNTRY_CODE:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.CountryCode);
            case FileListColumns.COUNTRY:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.Country);
            case FileListColumns.STATE:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.State);
            case FileListColumns.SUB_LOCATION:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.Sub_location);
            case FileListColumns.MAKE:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.Make);
            case FileListColumns.MODEL:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.Model);
            case FileListColumns.RATING:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.Rating);
            case FileListColumns.EXPOSURETIME:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.ExposureTime);
            case FileListColumns.FNUMBER:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.Fnumber);
            case FileListColumns.FOCAL_LENGTH:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.FocalLength);
            case FileListColumns.FOCAL_LENGTH_IN_35MM_FORMAT:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.FocalLengthIn35mmFormat);
            case FileListColumns.ISO:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.ISO);
            case FileListColumns.LENS_SPEC:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.LensSpec);
            case FileListColumns.TAKEN_DATE:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.TakenDate);
            case FileListColumns.CREATE_DATE:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.CreateDate);
            case FileListColumns.OFFSET_TIME:
                return DefaultStrGetter(atrb: SourcesAndAttributes.ElementAttribute.OffsetTime);

            default:
                return nfVal;
        }
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

        IntPtr himl;

        if (directoryElement.Type != DirectoryElement.ElementType.MyComputer)
        {
            himl = NativeMethods.SHGetFileInfo(pszPath: directoryElement.FileNameWithPath,
                                               dwFileAttributes: 0,
                                               psfi: ref shfi,
                                               cbSizeFileInfo: (uint)Marshal.SizeOf(structure: shfi),
                                               uFlags: NativeMethods.SHGFI_DISPLAYNAME | NativeMethods.SHGFI_SYSICONINDEX | NativeMethods.SHGFI_SMALLICON);
        }
        else
        {
            himl = NativeMethods.SHGetFileInfo(pszPath: directoryElement.ItemNameWithoutPath,
                                               dwFileAttributes: 0,
                                               psfi: ref shfi,
                                               cbSizeFileInfo: (uint)Marshal.SizeOf(structure: shfi),
                                               uFlags: NativeMethods.SHGFI_DISPLAYNAME | NativeMethods.SHGFI_SYSICONINDEX | NativeMethods.SHGFI_SMALLICON);
        }

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
            if (shfi.szDisplayName.ToLower()
                    .Contains(value: directoryElement.Extension.ToLower()))
            {
                lvi.Text = shfi.szDisplayName;
            }
            else
            {
                lvi.Text = shfi.szDisplayName + "." + directoryElement.Extension;
            }
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
            if (directoryElement.Type == DirectoryElement.ElementType.SubDirectory ||
                directoryElement.Type == DirectoryElement.ElementType.MyComputer ||
                directoryElement.Type == DirectoryElement.ElementType.ParentDirectory)
            {
                lvi.Text = directoryElement.ItemNameWithoutPath;
            }
            else
            {
                lvi.Text = shfi.szDisplayName;
            }
        }

    #endregion

        // Set the icon to use out of the explorer icons
        lvi.ImageIndex = shfi.iIcon;

        // Set the values for the columns
        List<string> subItemList = new();
        if (directoryElement.Type == DirectoryElement.ElementType.File)
        {
            foreach (ColumnHeader columnHeader in Columns)
            {
                if (columnHeader.Name != COL_NAME_PREFIX + FileListColumns.FILENAME)
                {
                    subItemList.Add(item: PickModelValueForColumn(directoryElement: directoryElement, columnHeader: columnHeader));
                }
            }

            // For each non-file (i.e. dirs), create empty sub items (needed for sorting)
        }
        else
        {
            foreach (ColumnHeader columnHeader in Columns)
            {
                if (columnHeader.Name == COL_NAME_PREFIX + FileListColumns.FILENAME)
                {
                    // nothing
                }
                else if (columnHeader.Name == COL_NAME_PREFIX + FileListColumns.GUID)
                {
                    subItemList.Add(item: PickModelValueForColumn(directoryElement: directoryElement, columnHeader: columnHeader));
                }
                else
                {
                    subItemList.Add(item: UNKNOWN_VALUE_DIR);
                }
            }
        }

        // don't add twice. this could happen if user does F5 too fast/too many times/is derp. (mostly the last one.)
        if (FindItemWithText(text: lvi.Text) == null)
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

            colOrderIndexInt = Convert.ToInt16(value: HelperDataApplicationSettings.DataReadSQLiteSettings(
                                                   tableName: "applayout",
                                                   settingTabPage: "lvw_FileList",
                                                   settingId: settingIdToSend));

            // If no user preset is found, retrieve the default
            // col order value
            if (colOrderIndexInt == 0)
            {
                if (_cfg_Col_Order_Default.ContainsKey(key: columnHeader.Name.Substring(startIndex: 4)))
                {
                    colOrderIndexInt = _cfg_Col_Order_Default[key: columnHeader.Name.Substring(startIndex: 4)];
                }
            }

            colOrderIndex.Add(item: colOrderIndexInt);

            Logger.Trace(message: "columnHeader: " +
                                  columnHeader.Name +
                                  " - colOrderIndex: " +
                                  colOrderIndexInt);

            // Read and process width
            settingIdToSend = Name + "_" + columnHeader.Name + "_width";
            colWidth = HelperDataApplicationSettings.DataReadSQLiteSettings(
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
    private void ColOrderAndWidth_Write()
    {
        string settingIdToSend;
        foreach (ColumnHeader columnHeader in Columns)
        {
            settingIdToSend = Name + "_" + columnHeader.Name + "_index";
            HelperDataApplicationSettings.DataWriteSQLiteSettings(
                tableName: "applayout",
                settingTabPage: "lvw_FileList",
                settingId: settingIdToSend,
                settingValue: columnHeader.DisplayIndex.ToString()
            );

            settingIdToSend = Name + "_" + columnHeader.Name + "_width";
            HelperDataApplicationSettings.DataWriteSQLiteSettings(
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


#region Further Settings Stuff

    /// <summary>
    ///     Setup the columns as read from the data table.
    /// </summary>
    private void SetupColumns()
    {
        Logger.Debug(message: "Starting");
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
            Columns.Add(value: clh);
            Logger.Trace(message: "Added column: " + clhName);
        }

        // Encapsulate locatization - in case it fails above column setup still there...
        try
        {
            foreach (ColumnHeader clh in Columns)
            {
                Logger.Trace(message: "Loading localization for: " + clh.Name);
                clh.Text = HelperDataLanguageTZ.DataReadDTObjectText(
                    objectType: "ColumnHeader",
                    objectName: clh.Name
                );
                Logger.Trace(message: "Loaded localization: " + clh.Name + " --> " + clh.Text);
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(message: "Error: " + ex.Message);
            CustomMessageBox.CustomMessageBox customMessageBox = new(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_FrmMainApp_ErrorLanguageFileColumnHeaders") +
                      ex.Message,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Error.ToString()),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Error);
            customMessageBox.ShowDialog();
        }
    }


    private void SetStyle()
    {
        // Set up the ListView control's basic properties.
        // Set its theme so it will look like the one used by Explorer.
        NativeMethods.SetWindowTheme(hWnd: Handle, pszSubAppName: "Explorer", pszSubIdList: null);
    }

    private void InitializeImageList()
    {
        //https://stackoverflow.com/a/37806517/3968494
        shfi = new NativeMethods.SHFILEINFOW();
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
    }


    /// <summary>
    ///     Initialize the list view.
    ///     Must be called before items are added to it.
    /// </summary>
    /// <param name="appLanguage">The application language to use</param>
    /// <param name="objectNames">
    ///     A data table containing the list of
    ///     columns to be used in column "objectName" and the default ordering
    ///     of these in column "sqlOrder"
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///     If this method is called
    ///     more than once.
    /// </exception>
    public void ReadAndApplySetting(string appLanguage)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException(message: "Trying to initialize the FileListView more than once.");
        }

        Logger.Debug(message: "Starting");
        _AppLanguage = appLanguage;

        SetupColumns();

        // Create the sorter for the list view
        LvwColumnSorter = new ListViewColumnSorter();
        ListViewItemSorter = LvwColumnSorter;

        // Apply column order and size
        ColOrderAndWidth_Read();

        // Finally set style and icons
        SetStyle();
        InitializeImageList(); // must be here - if called in constructor, it won't work

        _isInitialized = true;
    }

    /// <summary>
    ///     Can be called to make the FileListView persist its user
    ///     settings (like column order and width).
    /// </summary>
    public void PersistSettings()
    {
        ColOrderAndWidth_Write();
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
        // Temp. disable sorting of the list view
        Logger.Trace(message: "Disable ListViewItemSorter");
        SuspendColumnSorting();

        DirectoryElements = directoryElements;
        _fileCount = 0;

        foreach (DirectoryElement item in DirectoryElements)
        {
            AddListItem(directoryElement: item);
            if (item.Type == DirectoryElement.ElementType.File)
            {
                _fileCount++;
            }
        }

        // Resume sorting...
        Logger.Trace(message: "Enable ListViewItemSorter");
        ResumeColumnSorting();
        Sort();
    }


    /// <summary>
    ///     Clears the FileListView.
    ///     Should be used instead Items.Clear, etc. as it correctly handles
    ///     all due other things to do for clearing, like clearing the
    ///     the Directory Elements collection.
    /// </summary>
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