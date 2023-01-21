using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja;

internal static partial class HelperStatic
{
    private static readonly object TableLock = new();
    internal static HashSet<string> FilesBeingProcessed = new();
    internal static bool FileListBeingUpdated;
    internal static bool FilesAreBeingSaved;

    /// <summary>
    ///     A "coalesce" function.
    /// </summary>
    /// <param name="strings">Array of string values to be queried</param>
    /// <returns>The first non-null value</returns>
    internal static string GenericCoalesce(params string[] strings)
    {
        return strings.FirstOrDefault(predicate: s => !string.IsNullOrWhiteSpace(value: s));
    }

    private static string GenericStringToDateTimeBackToString(string dateTimeToConvert)
    {
        bool isDT = DateTime.TryParse(s: dateTimeToConvert, result: out DateTime tryDataValueDT);
        string tryDataValueStr = tryDataValueDT.ToString(format: CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern);
        if (isDT)
        {
            return tryDataValueStr;
        }

        return "-";
    }

    private static DateTime? GenericStringToDateTime(string dateTimeToConvert)
    {
        bool isDT = DateTime.TryParse(s: dateTimeToConvert, result: out DateTime tryDataValueDT);
        if (isDT)
        {
            return tryDataValueDT;
        }

        return null;
    }

    /// <summary>
    ///     Wrangles the actual coordinate out of a point. (e.g. 4.54 East to -4.54)
    /// </summary>
    /// <param name="point">This is a raw coordinate. Could contain numbers or things like "East" on top of numbers</param>
    /// <returns>Double - an actual coordinate</returns>
    public static double GenericAdjustLatLongNegative(string point)
    {
        string pointOrig = point.Replace(oldValue: " ", newValue: "")
            .Replace(oldChar: ',', newChar: '.');
        // WGS84 DM --> logic here is, before I have to spend hours digging this crap again...
        // degree stays as-is, the totality of the rest gets divided by 60.
        // so 41,53.23922526N becomes 41 + (53.53.23922526)/60) = 41.88732
        double pointVal = 0.0;
        if (pointOrig.Count(predicate: f => f == '.') == 2)
        {
            bool degreeParse = int.TryParse(s: pointOrig.Split('.')[0], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out int degree);
            bool minuteParse = double.TryParse(s: Regex.Replace(input: pointOrig.Split('.')[1] + "." + pointOrig.Split('.')[2], pattern: "[SWNE\"-]", replacement: ""), style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out double minute);
            minute = minute / 60;
            pointVal = degree + minute;
        }
        else
        {
            pointVal = double.Parse(s: Regex.Replace(input: pointOrig, pattern: "[SWNE\"-]", replacement: ""), style: NumberStyles.Any, provider: CultureInfo.InvariantCulture);
        }

        pointVal = Math.Round(value: pointVal, digits: 6);
        int multiplier = point.Contains(value: "S") || point.Contains(value: "W")
            ? -1
            : 1; //handle south and west

        return pointVal * multiplier;
    }

    /// <summary>
    ///     Joins two datatables. Logically similar to a SQL join.
    /// </summary>
    /// <param name="t1">Name of input table</param>
    /// <param name="t2">Name of input table</param>
    /// <param name="joinOn">Column Name to join on</param>
    /// <returns>A joined datatable</returns>
    private static DataTable GenericJoinDataTables(DataTable t1,
                                                   DataTable t2,
                                                   params Func<DataRow, DataRow, bool>[] joinOn)
    {
        // via https://stackoverflow.com/a/11505884/3968494
        // usage
        // var test = JoinDataTables(transactionInfo, transactionItems,
        // (row1, row2) =>
        // row1.Field<int>("TransactionID") == row2.Field<int>("TransactionID"));

        DataTable result = new();
        foreach (DataColumn col in t1.Columns)
        {
            if (result.Columns[name: col.ColumnName] == null)
            {
                result.Columns.Add(columnName: col.ColumnName, type: col.DataType);
            }
        }

        foreach (DataColumn col in t2.Columns)
        {
            if (result.Columns[name: col.ColumnName] == null)
            {
                result.Columns.Add(columnName: col.ColumnName, type: col.DataType);
            }
        }

        foreach (DataRow row1 in t1.Rows)
        {
            EnumerableRowCollection<DataRow> joinRows = t2.AsEnumerable()
                .Where(predicate: row2 =>
                {
                    foreach (Func<DataRow, DataRow, bool> parameter in joinOn)
                    {
                        if (!parameter(arg1: row1, arg2: row2))
                        {
                            return false;
                        }
                    }

                    return true;
                });
            foreach (DataRow fromRow in joinRows)
            {
                DataRow insertRow = result.NewRow();
                foreach (DataColumn col1 in t1.Columns)
                {
                    insertRow[columnName: col1.ColumnName] = row1[columnName: col1.ColumnName];
                }

                foreach (DataColumn col2 in t2.Columns)
                {
                    insertRow[columnName: col2.ColumnName] = fromRow[columnName: col2.ColumnName];
                }

                result.Rows.Add(row: insertRow);
            }
        }

        return result;
    }

    /// <summary>
    ///     This is a special member of the objectMapping/Language and really should sit there not here.
    ///     Messageboxes are a bit more complicate to work with than "simple" objects and this takes their language-value and
    ///     returns it efficiently
    /// </summary>
    /// <param name="messageBoxName">A pseudonym for the messagebox whose value is requested.</param>
    /// <returns>Messagebox text contents</returns>
    internal static string GenericGetMessageBoxText(string messageBoxName)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        return DataReadDTObjectText(
            objectType: "messageBox",
            objectName: messageBoxName
        );
    }

    /// <summary>
    ///     This (mostly) sets the various texts for most Controls in various forms, especially labels and buttons/boxes.
    /// </summary>
    /// <param name="cItem">The Control whose details need adjusting</param>
    /// <param name="senderForm"></param>
    /// <param name="parentNameToUse"></param>
    internal static void GenericReturnControlText(Control cItem,
                                                  Form senderForm,
                                                  string parentNameToUse = null)
    {
        if (parentNameToUse == null && !(cItem is Form))
        {
            parentNameToUse = cItem.Parent.Name;
        }

        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

        if (
            cItem is Label ||
            cItem is GroupBox ||
            cItem is Button ||
            cItem is CheckBox ||
            cItem is TabPage ||
            cItem is RichTextBox ||
            cItem is RadioButton
            //||
        )
        {
            FrmMainApp.Logger.Trace(message: "Starting - cItem: " + cItem.Name);
            // for some reason there is no .Last() being offered here
            cItem.Text = DataReadDTObjectText(
                objectType: cItem.GetType()
                    .ToString()
                    .Split('.')[cItem.GetType()
                                    .ToString()
                                    .Split('.')
                                    .Length -
                                1],
                objectName: cItem.Name
            );
        }
        else if (cItem is Form)
        {
            cItem.Text = DataReadDTObjectText(
                objectType: "Form",
                objectName: cItem.Name);
        }
        else if (cItem is TextBox || cItem is ComboBox)
        {
            if (senderForm.Name == "FrmSettings")
            {
                cItem.Text = DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: parentNameToUse,
                    settingId: cItem.Name
                );
            }
        }
        else if (cItem is NumericUpDown)
        {
            if (senderForm.Name == "FrmSettings")
            {
                NumericUpDown nud = (NumericUpDown)cItem;
                FrmMainApp.Logger.Trace(message: "Starting - cItem: " + nud.Name);
                _ = decimal.TryParse(s: DataReadSQLiteSettings(
                                         tableName: "settings",
                                         settingTabPage: parentNameToUse,
                                         settingId: cItem.Name
                                     ), result: out decimal outVal);

                // if this doesn't exist, it'd return 0, which is illegal because the min-values can be higher than that
                nud.Value = Math.Max(val1: nud.Minimum, val2: outVal);
                nud.Text = outVal.ToString(provider: CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>
    ///     A centralised way to interact with datatables containing exif data. Checks if the table already contains an element
    ///     for the given combination and if so deletes it, then writes the new data.
    /// </summary>
    /// <param name="dt">Name of the datatable. Realistically this is one of the three "queue" DTs</param>
    /// <param name="fileNameWithoutPath"></param>
    /// <param name="settingId">Name of the column or tag (e.g. GPSLatitude)</param>
    /// <param name="settingValue">Value to write</param>
    internal static void GenericUpdateAddToDataTable(DataTable dt,
                                                     string fileNameWithoutPath,
                                                     string settingId,
                                                     string settingValue)
    {
        lock (TableLock)
        {
            // delete any existing rows with the current combination
            for (int i = dt.Rows.Count - 1; i >= 0; i--)
            {
                DataRow thisDr = dt.Rows[index: i];
                if (
                    thisDr[columnName: "fileNameWithoutPath"]
                        .ToString() ==
                    fileNameWithoutPath &&
                    thisDr[columnName: "settingId"]
                        .ToString() ==
                    settingId
                )
                {
                    thisDr.Delete();
                }
            }

            dt.AcceptChanges();

            // add new
            DataRow newDr = dt.NewRow();
            newDr[columnName: "fileNameWithoutPath"] = fileNameWithoutPath;
            newDr[columnName: "settingId"] = settingId;
            newDr[columnName: "settingValue"] = settingValue;
            dt.Rows.Add(row: newDr);
            dt.AcceptChanges();
        }
    }

    /// <summary>
    ///     Updates the sessions storage for the Toponomy DT
    /// </summary>
    /// <param name="lat">string value of lat</param>
    /// <param name="lng">string value of lng</param>
    /// <param name="adminName1">Value to write</param>
    /// <param name="adminName2">Value to write</param>
    /// <param name="adminName3">Value to write</param>
    /// <param name="adminName4">Value to write</param>
    /// <param name="toponymName">Value to write</param>
    /// <param name="countryCode">Value to write</param>
    /// <param name="altitude">Value to write</param>
    /// <param name="timezoneId">Value to write</param>
    private static void GenericUpdateAddToDataTableTopopnomy(
        string lat,
        string lng,
        string adminName1,
        string adminName2,
        string adminName3,
        string adminName4,
        string toponymName,
        string countryCode,
        string altitude,
        string timezoneId
    )
    {
        lock (TableLock)
        {
            // delete any existing rows with the current combination
            for (int i = FrmMainApp.DtToponomySessionData.Rows.Count - 1; i >= 0; i--)
            {
                DataRow thisDr = FrmMainApp.DtToponomySessionData.Rows[index: i];
                if (
                    thisDr[columnName: "lat"]
                        .ToString() ==
                    lat &&
                    thisDr[columnName: "lng"]
                        .ToString() ==
                    lng
                )
                {
                    thisDr.Delete();
                }
            }

            FrmMainApp.DtToponomySessionData.AcceptChanges();

            // add new
            DataRow newDr = FrmMainApp.DtToponomySessionData.NewRow();
            newDr[columnName: "lat"] = lat;
            newDr[columnName: "lng"] = lng;
            newDr[columnName: "AdminName1"] = adminName1;
            newDr[columnName: "AdminName2"] = adminName2;
            newDr[columnName: "AdminName3"] = adminName3;
            newDr[columnName: "AdminName4"] = adminName4;
            newDr[columnName: "ToponymName"] = toponymName;
            newDr[columnName: "CountryCode"] = countryCode;
            newDr[columnName: "GPSAltitude"] = altitude;
            newDr[columnName: "timezoneId"] = timezoneId;

            FrmMainApp.DtToponomySessionData.Rows.Add(row: newDr);
            FrmMainApp.DtToponomySessionData.AcceptChanges();
        }
    }

    /// <summary>
    ///     Checks for new versions of GTN and eT.
    /// </summary>
    internal static async Task GenericCheckForNewVersions()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        // check when the last polling took place
        long nowUnixTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        long lastCheckUnixTime = 0;

        string strLastOnlineVersionCheck = DataReadSQLiteSettings(
            tableName: "settings",
            settingTabPage: "generic",
            settingId: "onlineVersionCheckDate"
        );

        if (strLastOnlineVersionCheck == null)
        {
            lastCheckUnixTime = nowUnixTime;
            // write back to SQL so it doesn't remain blank
            DataWriteSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "onlineVersionCheckDate",
                settingValue: nowUnixTime.ToString(provider: CultureInfo.InvariantCulture)
            );
        }
        else
        {
            lastCheckUnixTime = long.Parse(s: strLastOnlineVersionCheck);
        }

        FrmMainApp.Logger.Trace(message: "nowUnixTime > lastCheckUnixTime:" + (nowUnixTime - lastCheckUnixTime));
        int checkUpdateVal = 604800; //604800 is a week's worth of seconds
        #if DEBUG
        checkUpdateVal = 1;
        #endif

        if (nowUnixTime > lastCheckUnixTime + checkUpdateVal)
        {
            FrmMainApp.Logger.Trace(message: "Checking for new versions.");

            // get current & newest exiftool version -- do this here at the end so it doesn't hold up the process
            ///////////////

            string exiftoolCmd = "-ver";
            await RunExifTool(exiftoolCmd: exiftoolCmd,
                              frmMainAppInstance: null,
                              initiator: "GenericCheckForNewVersions");
            decimal newestExifToolVersionOnline = API_ExifGetExifToolVersionFromWeb();

            FrmMainApp.Logger.Trace(message: "currentExifToolVersionLocal: " + _currentExifToolVersionLocal + " / newestExifToolVersionOnline: " + newestExifToolVersionOnline);

            string strCurrentExifToolVersionInSQL = DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "exifToolVer"
            );

            FrmMainApp.Logger.Trace(message: "strCurrentExifToolVersionInSQL: " + strCurrentExifToolVersionInSQL);

            if (!decimal.TryParse(s: strCurrentExifToolVersionInSQL, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out decimal currentExifToolVersionInSQL))
            {
                currentExifToolVersionInSQL = _currentExifToolVersionLocal;
            }

            // shouldn't really happen but...
            if (_currentExifToolVersionLocal != currentExifToolVersionInSQL)
            {
                // write current to SQL
                DataWriteSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "generic",
                    settingId: "exifToolVer",
                    settingValue: _currentExifToolVersionLocal.ToString(provider: CultureInfo.InvariantCulture)
                );

                currentExifToolVersionInSQL = _currentExifToolVersionLocal;
            }

            if (newestExifToolVersionOnline > _currentExifToolVersionLocal && newestExifToolVersionOnline > currentExifToolVersionInSQL && _currentExifToolVersionLocal + newestExifToolVersionOnline > 0)
            {
                FrmMainApp.Logger.Trace(message: "Writing new version to SQL: " + newestExifToolVersionOnline.ToString(provider: CultureInfo.InvariantCulture));
                // write current to SQL
                DataWriteSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "generic",
                    settingId: "exifToolVer",
                    settingValue: newestExifToolVersionOnline.ToString(provider: CultureInfo.InvariantCulture)
                );

                if (MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoNewExifToolVersionExists") + newestExifToolVersionOnline.ToString(provider: CultureInfo.InvariantCulture), caption: "Info", buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Asterisk) == DialogResult.Yes)
                {
                    Process.Start(fileName: "https://exiftool.org/exiftool-" + newestExifToolVersionOnline.ToString(provider: CultureInfo.InvariantCulture) + ".zip");
                    FrmMainApp.Logger.Trace(message: "User Launched Browser to Download");
                }
                else
                {
                    FrmMainApp.Logger.Trace(message: "User Declined Launch to Download");
                }
            }

            // current version may be something like "0.5.8251.40825"
            // Assembly.GetExecutingAssembly().GetName().Version.Build is just "8251"
            int currentGTNVersionBuild = Assembly.GetExecutingAssembly()
                .GetName()
                .Version.Build;

            SApiOkay = true;
            DataTable dtApigtnVersion = DTFromAPI_GetGTNVersion();
            // newest may be something like "v0.5.8251"
            try // could be offline etc
            {
                string newestGTNVersionFull = dtApigtnVersion.Rows[index: 0][columnName: "version"]
                    .ToString()
                    .Replace(oldValue: "v", newValue: "");

                int newestGTNVersion = 0;

                int.TryParse(s: newestGTNVersionFull.Split('.')
                                 .Last(), result: out newestGTNVersion);

                if (newestGTNVersion > currentGTNVersionBuild)
                {
                    if (MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoNewGTNVersionExists") + newestGTNVersion, caption: "Info", buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Asterisk) == DialogResult.Yes)
                    {
                        Process.Start(fileName: "https://github.com/nemethviktor/GeoTagNinja/releases/download/" + dtApigtnVersion.Rows[index: 0][columnName: "version"] + "/GeoTagNinja_Setup.msi");
                    }
                }

                // write back to SQL
                DataWriteSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: "generic",
                    settingId: "onlineVersionCheckDate",
                    settingValue: nowUnixTime.ToString()
                );
            }
            catch
            {
                // nothing
            }
        }
        else
        {
            FrmMainApp.Logger.Trace(message: "Not checking for new versions.");
        }
    }

    /// <summary>
    ///     This creates the DataTables for the main Form - been moved out here because it's otherwise tedious to keep track
    ///     of.
    /// </summary>
    public static void GenericCreateDataTables()
    {
        FrmMainApp.Logger.Debug(message: "Starting");

        // DtLanguageLabels
        FrmMainApp.DtLanguageLabels = new DataTable();
        FrmMainApp.DtLanguageLabels.Clear();
        FrmMainApp.DtLanguageLabels.Columns.Add(columnName: "languageName");
        FrmMainApp.DtLanguageLabels.Columns.Add(columnName: "objectType");
        FrmMainApp.DtLanguageLabels.Columns.Add(columnName: "objectName");
        FrmMainApp.DtLanguageLabels.Columns.Add(columnName: "objectText");

        // DtFileDataCopyPool
        FrmMainApp.DtFileDataCopyPool = new DataTable();
        FrmMainApp.DtFileDataCopyPool.Clear();
        FrmMainApp.DtFileDataCopyPool.Columns.Add(columnName: "fileNameWithoutPath");
        FrmMainApp.DtFileDataCopyPool.Columns.Add(columnName: "settingId");
        FrmMainApp.DtFileDataCopyPool.Columns.Add(columnName: "settingValue");

        // DtFileDataPastePool 
        FrmMainApp.DtFileDataPastePool = new DataTable();
        FrmMainApp.DtFileDataPastePool.Clear();
        FrmMainApp.DtFileDataPastePool.Columns.Add(columnName: "settingId");
        FrmMainApp.DtFileDataPastePool.Columns.Add(columnName: "settingValue");

        // DtFileDataToWriteStage1PreQueue 
        FrmMainApp.DtFileDataToWriteStage1PreQueue = new DataTable();
        FrmMainApp.DtFileDataToWriteStage1PreQueue.Clear();
        FrmMainApp.DtFileDataToWriteStage1PreQueue.Columns.Add(columnName: "fileNameWithoutPath");
        FrmMainApp.DtFileDataToWriteStage1PreQueue.Columns.Add(columnName: "settingId");
        FrmMainApp.DtFileDataToWriteStage1PreQueue.Columns.Add(columnName: "settingValue");

        // DtFileDataToWriteStage2QueuePendingSave 
        FrmMainApp.DtFileDataToWriteStage2QueuePendingSave = new DataTable();
        FrmMainApp.DtFileDataToWriteStage2QueuePendingSave.Clear();
        FrmMainApp.DtFileDataToWriteStage2QueuePendingSave.Columns.Add(columnName: "fileNameWithoutPath");
        FrmMainApp.DtFileDataToWriteStage2QueuePendingSave.Columns.Add(columnName: "settingId");
        FrmMainApp.DtFileDataToWriteStage2QueuePendingSave.Columns.Add(columnName: "settingValue");

        // DtFileDataToWriteStage3ReadyToWrite 
        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite = new DataTable();
        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Clear();
        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Columns.Add(columnName: "fileNameWithoutPath");
        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Columns.Add(columnName: "settingId");
        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Columns.Add(columnName: "settingValue");
        // DtFileDataSeenInThisSession
        FrmMainApp.DtFileDataSeenInThisSession = new DataTable();
        FrmMainApp.DtFileDataSeenInThisSession.Clear();
        FrmMainApp.DtFileDataSeenInThisSession.Columns.Add(columnName: "fileNameWithPath");
        FrmMainApp.DtFileDataSeenInThisSession.Columns.Add(columnName: "settingId");
        FrmMainApp.DtFileDataSeenInThisSession.Columns.Add(columnName: "settingValue");

        // DtOriginalTakenDate
        FrmMainApp.DtOriginalTakenDate = new DataTable();
        FrmMainApp.DtOriginalTakenDate.Clear();
        FrmMainApp.DtOriginalTakenDate.Columns.Add(columnName: "fileNameWithoutPath");
        FrmMainApp.DtOriginalTakenDate.Columns.Add(columnName: "settingId");
        FrmMainApp.DtOriginalTakenDate.Columns.Add(columnName: "settingValue");

        // DtOriginalCreateDate
        FrmMainApp.DtOriginalCreateDate = new DataTable();
        FrmMainApp.DtOriginalCreateDate.Clear();
        FrmMainApp.DtOriginalCreateDate.Columns.Add(columnName: "fileNameWithoutPath");
        FrmMainApp.DtOriginalCreateDate.Columns.Add(columnName: "settingId");
        FrmMainApp.DtOriginalCreateDate.Columns.Add(columnName: "settingValue");

        // DtToponomySessionData;
        FrmMainApp.DtToponomySessionData = new DataTable();
        FrmMainApp.DtToponomySessionData.Clear();
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "lat");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "lng");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "AdminName1");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "AdminName2");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "AdminName3");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "AdminName4");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "ToponymName");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "CountryCode");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "GPSAltitude");
        FrmMainApp.DtToponomySessionData.Columns.Add(columnName: "timezoneId");
    }

    /// <summary>
    ///     Adds fileNameWithoutPath to FilesBeingProcessed
    /// </summary>
    /// <param name="fileNameWithoutPath">The file name without the path.</param>
    internal static void GenericLockLockFile(string fileNameWithoutPath)
    {
        FilesBeingProcessed.Add(item: fileNameWithoutPath);
    }

    /// <summary>
    ///     Removes fileNameWithoutPath from FilesBeingProcessed
    /// </summary>
    /// <param name="fileNameWithoutPath">The file name without the path.</param>
    internal static void GenericLockUnLockFile(string fileNameWithoutPath)
    {
        FilesBeingProcessed.Remove(item: fileNameWithoutPath);
    }

    /// <summary>
    ///     Checks if a file is currently locked by any other running operation - checks if the fileNameWithoutPath is
    ///     currently in FilesBeingProcessed
    /// </summary>
    /// <param name="fileNameWithoutPath">The file name without the path.</param>
    /// <returns>A true/false</returns>
    internal static bool GenericLockCheckLockFile(string fileNameWithoutPath)
    {
        return FilesBeingProcessed.Contains(item: fileNameWithoutPath);
    }

    /// <summary>
    ///     Triggers the "create preview" process for the file it's sent to check
    /// </summary>
    /// <param name="fileNameWithPath">Filename w/ path to check</param>
    /// <param name="initiator"></param>
    /// <returns></returns>
    internal static async Task GenericCreateImagePreview(string fileNameWithPath,
                                                         string initiator)
    {
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        FrmEditFileData frmEditFileDataInstance = (FrmEditFileData)Application.OpenForms[name: "FrmEditFileData"];
        Image img = null;
        FileInfo fi = new(fileName: fileNameWithPath);
        string generatedFileName = null;

        if (initiator == "FrmMainApp" && frmMainAppInstance != null)
        {
            frmMainAppInstance.pbx_imagePreview.Image = null;
            generatedFileName = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: frmMainAppInstance.lvw_FileList.SelectedItems[index: 0]
                                                                                              .Text +
                                                                                          ".jpg");
        }
        else if (initiator == "FrmEditFileData" && frmEditFileDataInstance != null)
        {
            frmEditFileDataInstance.pbx_imagePreview.Image = null;
            generatedFileName = Path.Combine(path1: FrmMainApp.UserDataFolderPath, path2: frmEditFileDataInstance.lvw_FileListEditImages.SelectedItems[index: 0]
                                                                                              .Text +
                                                                                          ".jpg");
        }

        //sometimes the file doesn't get created. (ie exiftool may fail to extract a preview.)
        string pbxErrorMsg = DataReadDTObjectText(
            objectType: "PictureBox",
            objectName: "pbx_imagePreviewCouldNotRetrieve"
        );

        try
        {
            // via https://stackoverflow.com/a/6576645/3968494
            using FileStream stream = new(path: fileNameWithPath, mode: FileMode.Open, access: FileAccess.Read);
            img = Image.FromStream(stream: stream);
        }
        catch
        {
            // nothing.
        }

        if (img == null)
        {
            // don't run the thing again if file has already been generated
            if (!File.Exists(path: generatedFileName))
            {
                await ExifGetImagePreviews(fileNameWithoutPath: fileNameWithPath);
            }

            if (File.Exists(path: generatedFileName))
            {
                try
                {
                    using FileStream stream = new(path: generatedFileName, mode: FileMode.Open, access: FileAccess.Read);
                    img = Image.FromStream(stream: stream);

                    ExifRotate(img: img);
                }
                catch
                {
                    // nothing
                }
            }
        }

        if (img != null)
        {
            if (initiator == "FrmMainApp" && frmMainAppInstance != null)
            {
                frmMainAppInstance.pbx_imagePreview.Image = img;
            }
            else if (initiator == "FrmEditFileData" && frmEditFileDataInstance != null)
            {
                frmEditFileDataInstance.pbx_imagePreview.Image = img;
            }
        }

        else
        {
            if (initiator == "FrmMainApp" && frmMainAppInstance != null)
            {
                frmMainAppInstance.pbx_imagePreview.SetErrorMessage(message: pbxErrorMsg);
            }
            else if (initiator == "FrmEditFileData" && frmEditFileDataInstance != null)
            {
                // frmEditFileDataInstance.pbx_imagePreview.SetErrorMessage(message: pbxErrorMsg); // <- nonesuch.
            }
        }
    }

    /// <summary>
    ///     Custom-made equivalent for a dialogbox w/ checkbox
    /// </summary>
    internal static class GenericCheckboxDialog
    {
        /// <summary>
        ///     A custom dialogbox-like form that includes a checkbox too.
        ///     TODO: make it more reusable. Atm it's a bit fixed as there's only 1 place that calls it. Basically a "source"
        ///     parameter needs to be added in at some stage.
        /// </summary>
        /// <param name="labelText">String of the "main" message.</param>
        /// <param name="caption">Caption of the box - the one that appears on the top.</param>
        /// <param name="checkboxText">Text of the checkbox.</param>
        /// <param name="returnCheckboxText">A yes-no style logic that gets returned/amended to the return string if checked.</param>
        /// <param name="button1Text">Label of the button</param>
        /// <param name="returnButton1Text">String val of what's sent further if the btn is pressed</param>
        /// <param name="button2Text">Same as above</param>
        /// <param name="returnButton2Text">Same as above</param>
        /// <returns>A string that can be reused. Needs fine-tuning in the future as it's single-purpose atm. Lazy. </returns>
        internal static string ShowDialogWithCheckBox(string labelText,
                                                      string caption,
                                                      string checkboxText,
                                                      string returnCheckboxText,
                                                      string button1Text,
                                                      string returnButton1Text,
                                                      string button2Text,
                                                      string returnButton2Text)
        {
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
            string returnString = "";
            Form promptBox = new();
            promptBox.Text = caption;
            promptBox.ControlBox = false;
            promptBox.FormBorderStyle = FormBorderStyle.Fixed3D;
            FlowLayoutPanel panel = new();

            Label lblText = new();
            lblText.Text = labelText;
            lblText.AutoSize = true;
            panel.SetFlowBreak(control: lblText, value: true);
            panel.Controls.Add(value: lblText);

            Button btnYes = new()
                { Text = button1Text };
            btnYes.Click += (sender,
                             e) =>
            {
                returnString = returnButton1Text;
                promptBox.Close();
            };
            btnYes.Location = new Point(x: 10, y: lblText.Bottom + 5);
            btnYes.AutoSize = true;
            panel.Controls.Add(value: btnYes);

            Button btnNo = new()
                { Text = button2Text };
            btnNo.Click += (sender,
                            e) =>
            {
                returnString = returnButton2Text;
                promptBox.Close();
            };

            btnNo.Location = new Point(x: btnYes.Width + 20, y: lblText.Bottom + 5);
            btnNo.AutoSize = true;
            panel.SetFlowBreak(control: btnNo, value: true);
            panel.Controls.Add(value: btnNo);

            CheckBox chk = new();
            chk.Text = checkboxText;
            chk.AutoSize = true;
            chk.Location = new Point(x: 10, y: btnYes.Bottom + 5);

            panel.Controls.Add(value: chk);
            panel.Padding = new Padding(all: 5);
            panel.AutoSize = true;

            promptBox.Controls.Add(value: panel);
            promptBox.Size = new Size(width: lblText.Width + 40, height: chk.Bottom + 50);
            promptBox.ShowInTaskbar = false;

            promptBox.StartPosition = FormStartPosition.CenterScreen;
            promptBox.ShowDialog();

            if (chk.Checked)
            {
                returnString += returnCheckboxText;
            }

            // in case of idiots break glass -- basically if someone ALT+F4s then we reset stuff to "no".
            if (!returnString.Contains(value: returnButton1Text) && !returnString.Contains(value: returnButton2Text))
            {
                returnString = returnButton2Text;
            }

            return returnString;
        }
    }
}