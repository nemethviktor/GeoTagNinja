using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GeoTagNinja.Helpers;
using GeoTagNinja.View.DialogAndMessageBoxes;
using Microsoft.VisualBasic;

namespace GeoTagNinja;

public partial class FrmManageFavourites : Form
{
    public FrmManageFavourites()
    {
        InitializeComponent();
        HelperControlThemeManager.SetThemeColour(themeColour: HelperVariables.UserSettingUseDarkMode
                                                     ? ThemeColour.Dark
                                                     : ThemeColour.Light, parentControl: this);
    }

    private void FrmManageFavourites_Load(object sender,
                                          EventArgs e)
    {
        HelperControlAndMessageBoxHandling.ReturnControlText(cItem: this, senderForm: this);

        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (
                cItem is Label ||
                cItem is GroupBox ||
                cItem is Button ||
                cItem is CheckBox ||
                cItem is TabPage ||
                cItem is RadioButton
            )
            {
                // gets logged inside.
                cItem.Text = HelperDataLanguageTZ.DataReadDTObjectText(
                    objectType: HelperDataLanguageTZ.GetControlType(
                        controlType: cItem.GetType()),
                    objectName: cItem.Name);
            }
            // there is only one dropdown atm.
            else if (cItem.Name == "cbx_Country")
            {
                FillCountryDropDown();
            }
        }

        LoadFavouritesList();
        cbx_Favourites.SelectedIndex = 0;
    }

    private void cbx_favouriteName_SelectedIndexChanged(object sender,
                                                        EventArgs e)
    {
        lstOriginals.Clear();
        _frmNowLoadingFavouriteData = true;
        string favouriteName = cbx_Favourites.Text;
        EnumerableRowCollection<DataRow> drDataTableData = from DataRow dataRow in _dtFavourites.AsEnumerable()
                                                           where dataRow.Field<string>(columnName: "favouriteName") == favouriteName
                                                           select dataRow;

        DataRow drFavouriteDataRow = drDataTableData.FirstOrDefault();

        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (
                cItem is TextBox
            )
            {
                // reset font to normal
                cItem.Font = new Font(prototype: cItem.Font, newStyle: FontStyle.Regular);
                string exifTag = cItem.Name.Substring(startIndex: 4);

                cItem.Text = drFavouriteDataRow[columnName: exifTag]
                    .ToString();

                lstOriginals.Add(key: cItem.Name, value: cItem.Text);
            }
            // there is only one dropdown atm.
            else if (cItem.Name == "cbx_Country")
            {
                string countryCode = cItem.Text = drFavouriteDataRow[columnName: "CountryCode"]
                    .ToString();
                string sqliteText = HelperDataLanguageTZ.DataReadDTCountryCodesNames(
                    queryWhat: "ISO_3166_1A3",
                    inputVal: countryCode,
                    returnWhat: "Country");

                if (cbx_Country.Items.Count == 0)
                {
                    FillCountryDropDown();
                }

                try
                {
                    cbx_Country.SelectedIndex = cbx_Country.Items.IndexOf(value: sqliteText);
                }
                catch
                {
                    cbx_Country.SelectedIndex = 0; // blank
                    break;
                }
            }
        }

        _frmNowLoadingFavouriteData = false;
    }

    private void btn_Save_Click(object sender,
                                EventArgs e)
    {
        string oldName = cbx_Favourites.Text;
        string countryCode = HelperDataLanguageTZ.DataReadDTCountryCodesNames(
            queryWhat: "Country",
            inputVal: cbx_Country.Text,
            returnWhat: "ISO_3166_1A3");

        HelperDataFavourites.DataWriteSQLiteUpdateFavourite(favouriteName: cbx_Favourites.Text,
                                                            countryCode: countryCode,
                                                            city: tbx_City.Text,
                                                            state: tbx_State.Text,
                                                            subLocation: tbx_Sub_location.Text);

        CustomMessageBox customMessageBox = new(
            text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                messageBoxName: "mbx_FrmMainApp_InfoFavouriteSaved"),
            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption
                   .Information.ToString()),
            buttons: MessageBoxButtons.OK,
            icon: MessageBoxIcon.Information);
        customMessageBox.ShowDialog();

        _frmNowLoadingFavouriteData = true;

        // reload table data
        LoadFavouritesList();

        // reset labels
        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem is TextBox txt)
            {
                txt.Font = new Font(prototype: txt.Font, newStyle: FontStyle.Regular);
            }
        }

        cbx_Favourites.Text = oldName;
        _frmNowLoadingFavouriteData = false;
    }


    private void btn_Close_Click(object sender,
                                 EventArgs e)
    {
        // refresh mainApp's dropdown
        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
        if (frmMainAppInstance != null)
        {
            frmMainAppInstance.cbx_Favourites.Items.Clear();
            _dtFavourites = HelperDataFavourites.DataReadSQLiteFavourites();
            foreach (DataRow drRow in _dtFavourites.Rows)
            {
                frmMainAppInstance.cbx_Favourites.Items.Add(item: drRow[columnName: "favouriteName"]
                                                                .ToString());
            }
        }

        Hide();
    }

    private void any_tbx_TextChanged(object sender,
                                     EventArgs e)
    {
        if (!_frmNowLoadingFavouriteData)
        {
            HelperNonStatic helperNonstatic = new();
            IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
            foreach (Control cItem in c)
            {
                if (cItem is TextBox txt)
                {
                    string oldText = HelperDataOtherDataRelated.DataGetFirstOrDefaultFromKVPList(lstIn: lstOriginals, keyEqualsWhat: cItem.Name);
                    txt.Font = oldText != txt.Text
                        ? new Font(prototype: txt.Font, newStyle: FontStyle.Bold)
                        : new Font(prototype: txt.Font, newStyle: FontStyle.Regular);
                }
            }
        }
    }

    private void btn_Rename_Click(object sender,
                                  EventArgs e)
    {
        string oldName = cbx_Favourites.Text;
        string newName = Interaction.InputBox(Prompt: btn_Rename.Text, DefaultResponse: cbx_Favourites.Text);
        if (oldName != newName && newName.Length > 0)
        {
            HelperDataFavourites.DataRenameSQLiteFavourite(oldName: oldName, newName: newName);

            // update in dropdown
            LoadFavouritesList();

            cbx_Favourites.Text = newName;
        }
    }

    private void btn_Delete_Click(object sender,
                                  EventArgs e)
    {
        HelperDataFavourites.DataDeleteSQLiteFavourite(favouriteName: cbx_Favourites.Text);

        LoadFavouritesList();
        try
        {
            cbx_Favourites.SelectedIndex = 0;
        }
        catch
        {
            // nothing
        }
    }

    /// <summary>
    ///     Fills up the cbx_favouriteName
    /// </summary>
    private void LoadFavouritesList()
    {
        cbx_Favourites.Items.Clear();
        _dtFavourites = HelperDataFavourites.DataReadSQLiteFavourites();
        foreach (DataRow drRow in _dtFavourites.Rows)
        {
            cbx_Favourites.Items.Add(item: drRow[columnName: "favouriteName"]
                                         .ToString());
        }
    }

    private void FillCountryDropDown()
    {
        cbx_Country.Items.Clear();
        foreach (string country in HelperGenericAncillaryListsArrays.GetCountries())
        {
            cbx_Country.Items.Add(item: country);
        }
    }

    #region Variables

    private static bool _frmNowLoadingFavouriteData;
    private DataTable _dtFavourites = new();
    private readonly Dictionary<string, string> lstOriginals = new();

    #endregion
}