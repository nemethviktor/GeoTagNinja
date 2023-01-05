using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace GeoTagNinja;

public partial class FrmManageFavourites : Form
{
    public FrmManageFavourites()
    {
        InitializeComponent();
    }

    private void FrmManageFavourites_Load(object sender,
                                          EventArgs e)
    {
        // this just pulls the form's name -- logged inside
        HelperStatic.GenericReturnControlText(cItem: this, senderForm: this);

        LoadFavouritesList();

        cbx_Favourites.SelectedIndex = 0;

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
                cItem.Text = HelperStatic.DataReadDTObjectText(objectType: cItem.GetType()
                                                                   .ToString()
                                                                   .Split('.')
                                                                   .Last(), objectName: cItem.Name);
            }
        }
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

                lstOriginals.Add(item: new KeyValuePair<string, string>(key: cItem.Name, value: cItem.Text));
            }
        }

        _frmNowLoadingFavouriteData = false;
    }

    private void btn_Save_Click(object sender,
                                EventArgs e)
    {
        string oldName = cbx_Favourites.Text;
        HelperStatic.DataWriteSQLiteUpdateFavourite(favouriteName: cbx_Favourites.Text,
                                                    city: tbx_City.Text,
                                                    state: tbx_State.Text,
                                                    subLocation: tbx_Sub_location.Text);

        MessageBox.Show(text: HelperStatic.GenericGetMessageBoxText(messageBoxName: "mbx_FrmMainApp_InfoFavouriteSaved"), caption: "Info", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);

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
            _dtFavourites = HelperStatic.DataReadSQLiteFavourites();
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
                    string oldText = HelperStatic.DataGetFirstOrDefaultFromKVPList(lstIn: lstOriginals, keyEqualsWhat: cItem.Name);
                    if (oldText != txt.Text)
                    {
                        txt.Font = new Font(prototype: txt.Font, newStyle: FontStyle.Bold);
                    }
                    else
                    {
                        txt.Font = new Font(prototype: txt.Font, newStyle: FontStyle.Regular);
                    }
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
            // update in sqlite
            HelperStatic.DataRenameSQLiteFavourite(oldName: oldName, newName: newName);

            // update in dropdown
            LoadFavouritesList();

            cbx_Favourites.Text = newName;
        }
    }


    private void btn_Delete_Click(object sender,
                                  EventArgs e)
    {
        HelperStatic.DataDeleteSQLiteFavourite(favouriteName: cbx_Favourites.Text);

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
        _dtFavourites = HelperStatic.DataReadSQLiteFavourites();
        foreach (DataRow drRow in _dtFavourites.Rows)
        {
            cbx_Favourites.Items.Add(item: drRow[columnName: "favouriteName"]
                                         .ToString());
        }
    }

    #region Variables

    private static bool _frmNowLoadingFavouriteData;
    private DataTable _dtFavourites = new();
    private readonly List<KeyValuePair<string, string>> lstOriginals = new();

    #endregion
}