using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja;

internal static partial class HelperStatic
{
    #region Generic

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
            int degree;
            double minute;
            bool degreeParse = int.TryParse(s: pointOrig.Split('.')[0], style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out degree);
            bool minuteParse = double.TryParse(s: Regex.Replace(input: pointOrig.Split('.')[1] + "." + pointOrig.Split('.')[2], pattern: "[SWNE\"-]", replacement: ""), style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out minute);
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
    internal static DataTable GenericJoinDataTables(DataTable t1,
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
        return DataReadSQLiteObjectText(
            languageName: FrmMainApp.AppLanguage,
            objectType: "messageBox",
            objectName: messageBoxName
        );
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
            FrmMainApp FrmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
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

            ;
            return returnString;
        }
    }

    /// <summary>
    ///     This (mostly) sets the various texts for most Controls in various forms, especially labels and buttons/boxes.
    /// </summary>
    /// <param name="cItem">The Control whose details need adjusting</param>
    internal static void GenericReturnControlText(Control cItem,
                                                  Form senderForm)
    {
        if (
            cItem.GetType() == typeof(Label) ||
            cItem.GetType() == typeof(GroupBox) ||
            cItem.GetType() == typeof(Button) ||
            cItem.GetType() == typeof(CheckBox) ||
            cItem.GetType() == typeof(TabPage) ||
            cItem.GetType() == typeof(RichTextBox) ||
            cItem.GetType() == typeof(RadioButton) // ||
        )
        {
            // for some reason there is no .Last() being offered here
            cItem.Text = DataReadSQLiteObjectText(
                languageName: FrmMainApp.AppLanguage,
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
        else if (cItem.GetType() == typeof(TextBox) || cItem.GetType() == typeof(ComboBox))
        {
            if (senderForm.Name == "FrmSettings")
            {
                cItem.Text = DataReadSQLiteSettings(
                    tableName: "settings",
                    settingTabPage: cItem.Parent.Name,
                    settingId: cItem.Name
                );
            }
        }
    }

    /// <summary>
    ///     A centralised way to interact with datatables containing exif data. Checks if the table already contains an element
    ///     for the given combination and if so deletes it, then writes the new data.
    /// </summary>
    /// <param name="dt">Name of the datatable. Realistically this is one of the three "queue" DTs</param>
    /// <param name="filePath">Path of file</param>
    /// <param name="settingId">Name of the column or tag (e.g. GPSLatitude)</param>
    /// <param name="settingValue">Value to write</param>
    internal static void GenericUpdateAddToDataTable(DataTable dt,
                                                     string filePath,
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
                    thisDr[columnName: "filePath"]
                        .ToString() ==
                    filePath &&
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
            newDr[columnName: "filePath"] = filePath;
            newDr[columnName: "settingId"] = settingId;
            newDr[columnName: "settingValue"] = settingValue;
            dt.Rows.Add(row: newDr);
            dt.AcceptChanges();
        }
    }

    /// <summary>
    ///     Checks for new versions of GTN and eT.
    /// </summary>
    internal static async Task GenericCheckForNewVersions()
    {
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

        if (nowUnixTime > lastCheckUnixTime + 604800) //604800 is a week's worth of seconds
        {
            // get current & newest exiftool version -- do this here at the end so it doesn't hold up the process
            decimal currentExifToolVersionLocal = await ExifGetExifToolVersion();
            decimal newestExifToolVersionOnline = API_ExifGetExifToolVersionFromWeb();
            decimal currentExifToolVersionInSQL;
            string strCurrentExifToolVersionInSQL = DataReadSQLiteSettings(
                tableName: "settings",
                settingTabPage: "generic",
                settingId: "exifToolVer"
            );

            if (!decimal.TryParse(s: strCurrentExifToolVersionInSQL, style: NumberStyles.Any, provider: CultureInfo.InvariantCulture, result: out currentExifToolVersionInSQL))
            {
                currentExifToolVersionInSQL = currentExifToolVersionLocal;
            }

            if (newestExifToolVersionOnline > currentExifToolVersionLocal && newestExifToolVersionOnline > currentExifToolVersionInSQL && currentExifToolVersionLocal + newestExifToolVersionOnline > 0)
            {
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
                }
            }

            // current version may be something like "0.5.8251.40825"
            // Assembly.GetExecutingAssembly().GetName().Version.Build is just "8251"
            int currentGTNVersionBuild = Assembly.GetExecutingAssembly()
                .GetName()
                .Version.Build;

            s_APIOkay = true;
            DataTable dt_APIGTNVersion = DTFromAPI_GetGTNVersion();
            // newest may be something like "v0.5.8251"
            string newestGTNVersionFull = dt_APIGTNVersion.Rows[index: 0][columnName: "version"]
                .ToString()
                .Replace(oldValue: "v", newValue: "");
            int newestGTNVersion = 0;

            bool intParse;
            intParse = int.TryParse(s: newestGTNVersionFull.Split('.')
                                        .Last(), result: out newestGTNVersion);

            if (newestGTNVersion > currentGTNVersionBuild)
            {
                if (MessageBox.Show(text: GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoNewGTNVersionExists") + newestGTNVersion, caption: "Info", buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Asterisk) == DialogResult.Yes)
                {
                    Process.Start(fileName: "https://github.com/nemethviktor/GeoTagNinja/releases/download/" + dt_APIGTNVersion.Rows[index: 0][columnName: "version"] + "/GeoTagNinja_Setup.msi");
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
    }

    /// <summary>
    ///     This creates the DataTables for the main Form - been moved out here because it's otherwise tedious to keep track
    ///     of.
    /// </summary>
    public static void GenericCreateDataTables()
    {
        // dt_fileDataCopyPool
        FrmMainApp.DtFileDataCopyPool = new DataTable();
        FrmMainApp.DtFileDataCopyPool.Clear();
        FrmMainApp.DtFileDataCopyPool.Columns.Add(columnName: "settingId");
        FrmMainApp.DtFileDataCopyPool.Columns.Add(columnName: "settingValue");

        // dt_fileDataToWriteStage1PreQueue 
        FrmMainApp.DtFileDataToWriteStage1PreQueue = new DataTable();
        FrmMainApp.DtFileDataToWriteStage1PreQueue.Clear();
        FrmMainApp.DtFileDataToWriteStage1PreQueue.Columns.Add(columnName: "filePath");
        FrmMainApp.DtFileDataToWriteStage1PreQueue.Columns.Add(columnName: "settingId");
        FrmMainApp.DtFileDataToWriteStage1PreQueue.Columns.Add(columnName: "settingValue");

        // dt_fileDataToWriteStage2QueuePendingSave 
        FrmMainApp.DtFileDataToWriteStage2QueuePendingSave = new DataTable();
        FrmMainApp.DtFileDataToWriteStage2QueuePendingSave.Clear();
        FrmMainApp.DtFileDataToWriteStage2QueuePendingSave.Columns.Add(columnName: "filePath");
        FrmMainApp.DtFileDataToWriteStage2QueuePendingSave.Columns.Add(columnName: "settingId");
        FrmMainApp.DtFileDataToWriteStage2QueuePendingSave.Columns.Add(columnName: "settingValue");

        // dt_fileDataToWriteStage3ReadyToWrite 
        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite = new DataTable();
        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Clear();
        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Columns.Add(columnName: "filePath");
        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Columns.Add(columnName: "settingId");
        FrmMainApp.DtFileDataToWriteStage3ReadyToWrite.Columns.Add(columnName: "settingValue");

        // DtFilesSeenInThisSession
        FrmMainApp.DtFilesSeenInThisSession = new DataTable();
        FrmMainApp.DtFilesSeenInThisSession.Clear();
        FrmMainApp.DtFilesSeenInThisSession.Columns.Add(columnName: "filePath");
        FrmMainApp.DtFilesSeenInThisSession.Columns.Add(columnName: "fileDateTime");

        // DtFileDataSeenInThisSession
        FrmMainApp.DtFileDataSeenInThisSession = new DataTable();
        FrmMainApp.DtFileDataSeenInThisSession.Clear();
        FrmMainApp.DtFileDataSeenInThisSession.Columns.Add(columnName: "filePath");
        FrmMainApp.DtFileDataSeenInThisSession.Columns.Add(columnName: "settingId");
        FrmMainApp.DtFileDataSeenInThisSession.Columns.Add(columnName: "settingValue");
    }

    internal static void GenericLockLockFile(string fileNameWithOutPath)
    {
        FilesBeingProcessed.Add(item: fileNameWithOutPath);
    }

    internal static void GenericLockUnLockFile(string fileNameWithOutPath)
    {
        FilesBeingProcessed.Remove(item: fileNameWithOutPath);
    }

    internal static bool GenericLockCheckLockFile(string fileNameWithOutPath)
    {
        return FilesBeingProcessed.Contains(item: fileNameWithOutPath);
    }

    #endregion
}