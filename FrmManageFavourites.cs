using GeoTagNinja.Helpers;
using GeoTagNinja.Model;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Themer = WinFormsDarkThemerNinja.Themer;


namespace GeoTagNinja;

public partial class FrmManageFavourites : Form
{
    private FrmMainApp _frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

    public FrmManageFavourites()
    {
        InitializeComponent();

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
                cItem is Button or
                CheckBox or
                GroupBox or
                Label or
                RadioButton or
                TabPage
            )
            {
                // gets logged inside.
                HelperControlAndMessageBoxHandling.FakeControlTypes fakeControlType = cItem switch
                {
                    Button => HelperControlAndMessageBoxHandling.FakeControlTypes.Button,
                    CheckBox => HelperControlAndMessageBoxHandling.FakeControlTypes.CheckBox,
                    GroupBox => HelperControlAndMessageBoxHandling.FakeControlTypes.GroupBox,
                    Label => HelperControlAndMessageBoxHandling.FakeControlTypes.Label,
                    RadioButton => HelperControlAndMessageBoxHandling.FakeControlTypes.RadioButton,
                    TabPage => HelperControlAndMessageBoxHandling.FakeControlTypes.TabPage,
                    _ => HelperControlAndMessageBoxHandling.FakeControlTypes.Undefined
                };

                cItem.Text = HelperControlAndMessageBoxHandling.ReturnControlText(controlName: cItem.Name,
                    fakeControlType: fakeControlType);
            }
            // there is only one dropdown atm.
            else if (cItem.Name == "cbx_Country")
            {
                FillCountryDropDown();
            }
        }

        RefreshCbxFavouritesItems();
        cbx_Favourites.SelectedIndex = 0;

        Themer.ApplyThemeToControl(
            control: this,
            themeStyle: HelperVariables.UserSettingUseDarkMode ?
            Themer.ThemeStyle.Custom :
            Themer.ThemeStyle.Default
            );
    }

    private void cbx_favouriteName_SelectedIndexChanged(object sender,
        EventArgs e)
    {
        _originalFavouritesPresented.Clear();
        _frmNowLoadingFavouriteData = true;
        string favouriteName = cbx_Favourites.Text;

        Favourite favourite = _frmMainAppInstance.GetFavouriteByName(favouriteName: favouriteName);
        if (favourite == null)
        {
            return;
        }

        HelperNonStatic helperNonstatic = new();
        IEnumerable<Control> c = helperNonstatic.GetAllControls(control: this);
        foreach (Control cItem in c)
        {
            if (cItem is TextBox)
            {
                // reset font to normal
                cItem.Font = new Font(prototype: cItem.Font, newStyle: FontStyle.Regular);
                string exifTag = cItem.Name.Substring(startIndex: 4);

                cItem.Text = cItem.Name switch
                {
                    "tbx_GPSAltitude" => favourite.GPSAltitude,
                    "tbx_GPSLongitude" => favourite.GPSLongitude,
                    "tbx_GPSLatitude" => favourite.GPSLatitude,
                    "tbx_City" => favourite.City,
                    "tbx_State" => favourite.State,
                    "tbx_Sublocation" => favourite.Sublocation,
                    _ => throw new ArgumentOutOfRangeException()
                };

                _originalFavouritesPresented.Add(key: cItem.Name, value: cItem.Text);
            }
            // there is only one dropdown atm.
            else if (cItem.Name == "cbx_Country")
            {
                string countryCode = favourite.CountryCode;
                string sqliteText = HelperDataLanguageTZ.DataReadDTCountryCodesNames(
                    queryWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A3,
                    inputVal: countryCode,
                    returnWhat: LanguageMappingQueryOrReturnWhat.Country);

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
            queryWhat: LanguageMappingQueryOrReturnWhat.Country,
            inputVal: cbx_Country.Text,
            returnWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A3);

        // no need to delete existing one
        Favourite favourite = _frmMainAppInstance.GetFavouriteByName(favouriteName: oldName);
        if (favourite != null)
        {
            favourite.CountryCode = countryCode;
            favourite.City = tbx_City.Text;
            favourite.State = tbx_State.Text;
            favourite.Sublocation = tbx_Sublocation.Text;
        }

        HelperDataFavourites.DataWriteSQLiteClearAndUpdateFavourites();
        Themer.ShowMessageBox(message:
            HelperControlAndMessageBoxHandling.ReturnControlText(
                controlName: "mbx_FrmMainApp_InfoFavouriteSaved",
                fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
            icon: MessageBoxIcon.Information,
            buttons: MessageBoxButtons.OK);

        _frmNowLoadingFavouriteData = true;

        // reload table data
        RefreshCbxFavouritesItems();

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

        _frmMainAppInstance?.ClearReloadFavouritesDropDownValues();

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
                    string oldText =
                        HelperDataOtherDataRelated.DataGetFirstOrDefaultFromKVPList(
                            kvpListIn: _originalFavouritesPresented,
                            keyEqualsWhat: cItem.Name);
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

        Favourite favourite = _frmMainAppInstance.GetFavouriteByName(favouriteName: oldName);

        if (oldName != newName &&
            newName.Length > 0)
        {
            favourite.FavouriteName = newName;
            HelperDataFavourites.DataWriteSQLiteClearAndUpdateFavourites();

            // update in dropdown
            RefreshCbxFavouritesItems();

            cbx_Favourites.Text = newName;
        }
    }

    private void btn_Delete_Click(object sender,
        EventArgs e)
    {
        _frmMainAppInstance.RemoveFavouriteByName(favouriteName: cbx_Favourites.Text);

        RefreshCbxFavouritesItems();
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
    private void RefreshCbxFavouritesItems()
    {
        cbx_Favourites.Items.Clear();
        foreach (Favourite favourite in FrmMainApp.Favourites)
        {
            _ = cbx_Favourites.Items.Add(item: favourite.FavouriteName);
        }
    }

    /// <summary>
    ///     Fills the country dropdown
    /// </summary>
    private void FillCountryDropDown()
    {
        cbx_Country.Items.Clear();
        foreach (string country in HelperGenericAncillaryListsArrays.GetCountries())
        {
            _ = cbx_Country.Items.Add(item: country);
        }
    }

    #region Variables

    private static bool _frmNowLoadingFavouriteData;
    private readonly Dictionary<string, string> _originalFavouritesPresented = [];

    #endregion
}