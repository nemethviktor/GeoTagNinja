using GeoTagNinja.Helpers;
using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.Exif;
using GeoTagNinja.Helpers.Geo;
using GeoTagNinja.Helpers.Localisation;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TimeZoneConverter;
using static GeoTagNinja.Helpers.UI.HelperControlAndMessageBoxHandling;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     The Manage Favourites dialog: a (optionally grouped) tree of every favourite on the left and the details of the
///     selected one on the right.
/// </summary>
/// <remarks>
///     <para>
///         Everything here edits a working copy of the favourites. Nothing reaches the application's list or SQLite
///         until OK; Cancel throws the copy away. Within the dialog, edits to the selected favourite are held in the
///         controls until Accept copies them into the working copy (or Discard puts the old values back), and moving
///         to another favourite with edits pending asks which of the two is wanted.
///     </para>
///     <para>
///         Position is never typed: a favourite gets its coordinates when it is created from the photo selected in
///         the main window, and keeps them.
///     </para>
/// </remarks>
public partial class FrmManageFavourites : Form
{
    private const int MaxNodesToExpandAll = 300;
    private const string NodePathSeparator = "\u001f";

    /// <summary>The favourites as edited so far: copies, so Cancel leaves the real list alone.</summary>
    private readonly List<Favourite> _favourites = [];

    private readonly HashSet<FavouriteGroupKey> _groupKeys = [.. FavouriteGrouping.DefaultKeys];

    private Favourite _current;
    private string _countFormat = "{0} Items ({1} Selected)";

    /// <summary>True while the controls are being filled from a favourite, so filling them is not read as an edit.</summary>
    private bool _loading;

    /// <summary>True while the tree is being rebuilt, so the selection it restores is not read as the user's click.</summary>
    private bool _rebuilding;

    /// <summary>True while country and country code are being set from one another.</summary>
    private bool _syncingCountry;

    private bool _editsPending;
    private bool _listChanged;

    public FrmManageFavourites()
    {
        InitializeComponent();
    }

    #region Form lifecycle

    private void FrmManageFavourites_Load(object sender,
                                          EventArgs e)
    {
        ApplyCaptions();

        foreach (Favourite favourite in FrmMainApp.Favourites)
        {
            _favourites.Add(item: favourite.Clone());
        }

        FillDropDowns();

        tmi_GroupByCountryCode.Checked = _groupKeys.Contains(item: FavouriteGroupKey.CountryCode);
        tmi_GroupByCountry.Checked = _groupKeys.Contains(item: FavouriteGroupKey.Country);
        tmi_GroupByState.Checked = _groupKeys.Contains(item: FavouriteGroupKey.State);
        tmi_GroupByCity.Checked = _groupKeys.Contains(item: FavouriteGroupKey.City);
        tmi_GroupByCoordinates.Checked = _groupKeys.Contains(item: FavouriteGroupKey.Coordinates);

        RebuildTree(select: null);
        trv_Favourites.SelectedNode = FirstLeaf(nodes: trv_Favourites.Nodes);

        ThemeHelper.ApplyTo(control: this);
        ThemeHelper.StyleMenu(menu: tsp_Favourites);
    }

    /// <summary>
    ///     The favourite at the top of the tree as it is shown (which, once grouped, is not the first by name).
    /// </summary>
    private static TreeNode FirstLeaf(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            TreeNode leaf = node.Tag is Favourite
                ? node
                : FirstLeaf(nodes: node.Nodes);
            if (leaf != null)
            {
                return leaf;
            }
        }

        return null;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e: e);

        // OK saves first and sets DialogResult.OK, so anything else that leaves changes behind is a way out without
        // saving: the Cancel button, Escape, the title bar.
        if (!e.Cancel &&
            DialogResult != DialogResult.OK &&
            (_listChanged || _editsPending) &&
            ThemeHelper.ShowMessageBoxWithResult(
                message: MessageText(name: "mbx_FrmManageFavourites_QuestionDiscardAll",
                    fallback: "Close without saving your changes to the favourites?"),
                buttons: MessageBoxButtons.YesNo,
                icon: MessageBoxIcon.Question) != DialogResult.Yes)
        {
            e.Cancel = true;
        }
    }

    private void btn_OK_Click(object sender,
                              EventArgs e)
    {
        if (_editsPending && !TryAccept())
        {
            return;
        }

        List<Favourite> previous = [.. FrmMainApp.Favourites];
        try
        {
            FrmMainApp.Favourites.Clear();
            foreach (Favourite favourite in _favourites)
            {
                _ = FrmMainApp.Favourites.Add(item: favourite);
            }

            Favourites.DataWriteSQLiteClearAndUpdateFavourites();
        }
        catch (Exception ex)
        {
            // the write is a transaction, so the database is as it was; put the in-memory list back to match.
            FrmMainApp.Favourites.Clear();
            foreach (Favourite favourite in previous)
            {
                _ = FrmMainApp.Favourites.Add(item: favourite);
            }

            ThemeHelper.ShowMessageBox(message: ex.Message, icon: MessageBoxIcon.Error);
            return;
        }

        FrmMainApp.Instance?.ClearReloadFavouritesDropDownValues();
        DialogResult = DialogResult.OK;
        Close();
    }

    #endregion

    #region Setting up

    /// <summary>
    ///     Puts the localised captions on the labels, buttons and menu items. Where a language has no caption the
    ///     English text from the designer stays.
    /// </summary>
    private void ApplyCaptions()
    {
        Text = Caption(controlName: "FrmManageFavourites", fakeControlType: FakeControlTypes.Form, fallback: Text);

        foreach (Control control in ControlTraversal.GetAllControls(control: this))
        {
            FakeControlTypes? fakeControlType = control switch
            {
                Button => FakeControlTypes.Button,
                CheckBox => FakeControlTypes.CheckBox,
                GroupBox => FakeControlTypes.GroupBox,
                Label => FakeControlTypes.Label,
                _ => null
            };

            if (fakeControlType != null && control != lbl_FavouritesCount)
            {
                control.Text = Caption(controlName: control.Name, fakeControlType: fakeControlType.Value,
                    fallback: control.Text);
            }
        }

        _countFormat = Caption(controlName: "lbl_FavouritesCount", fakeControlType: FakeControlTypes.Label,
            fallback: _countFormat);

        foreach (ToolStripItem item in new ToolStripItem[]
                 {
                     tsd_GroupBy, tmi_GroupByCountryCode, tmi_GroupByCountry, tmi_GroupByState, tmi_GroupByCity,
                     tmi_GroupByCoordinates, tmi_GroupByNone, tsb_Duplicate, tsb_Delete, tsb_AddFromSelection
                 })
        {
            item.Text = Caption(controlName: item.Name, fakeControlType: FakeControlTypes.ToolStripMenuItem,
                fallback: item.Text);
        }
    }

    private void FillDropDowns()
    {
        cbx_Country.Items.AddRange(items: Countries.GetCountries()
                                                   .Cast<object>()
                                                   .ToArray());
        cbx_CountryCode.Items.AddRange(items: Countries.GetCountryCodes()
                                                       .Cast<object>()
                                                       .ToArray());

        // IANA ids, which is what GeoNames returns and what the favourite stores; the box stays editable so a value
        // imported from GeoSetter that is not on the list is kept rather than lost.
        cbx_FavouriteTimeZone.Items.Add(item: "");
        cbx_FavouriteTimeZone.Items.AddRange(items: TZConvert.KnownIanaTimeZoneNames
                                                             .OrderBy(keySelector: name => name,
                                                                  comparer: StringComparer.Ordinal)
                                                             .Cast<object>()
                                                             .ToArray());
    }

    private static string Caption(string controlName,
                                  FakeControlTypes fakeControlType,
                                  string fallback)
    {
        string text = ReturnControlText(controlName: controlName, fakeControlType: fakeControlType);
        return string.IsNullOrWhiteSpace(value: text)
            ? fallback
            : text;
    }

    private static string MessageText(string name,
                                  string fallback)
    {
        return Caption(controlName: name, fakeControlType: FakeControlTypes.MessageBox, fallback: fallback);
    }

    #endregion

    #region The tree

    /// <summary>
    ///     Rebuilds the tree from the working copy, grouped as ticked in the Group By menu, and selects a favourite in
    ///     it. Does not touch the details panel, so edits pending on the selected favourite survive a regrouping.
    /// </summary>
    private void RebuildTree(Favourite select)
    {
        string blankLabel = Caption(controlName: "tmi_GroupByBlank", fakeControlType: FakeControlTypes.Generic,
            fallback: "(none)");

        List<(Favourite Favourite, List<string> Path)> placed = FavouriteGrouping
           .InDisplayOrder(favourites: _favourites)
           .Select(selector: favourite => (Favourite: favourite,
                Path: FavouriteGrouping.PathFor(favourite: favourite, keys: _groupKeys, blankLabel: blankLabel)))
           .OrderBy(keySelector: item => string.Join(separator: NodePathSeparator, values: item.Path),
                comparer: StringComparer.CurrentCultureIgnoreCase)
           .ToList();

        _rebuilding = true;
        trv_Favourites.BeginUpdate();
        try
        {
            trv_Favourites.Nodes.Clear();

            // one group node per distinct path, found by key rather than by scanning the siblings: a GeoSetter
            // import can bring thousands of favourites.
            Dictionary<string, TreeNode> groupNodes = new(comparer: StringComparer.CurrentCultureIgnoreCase);
            TreeNode selectNode = null;

            foreach ((Favourite favourite, List<string> path) in placed)
            {
                TreeNodeCollection level = trv_Favourites.Nodes;
                string key = "";
                foreach (string segment in path)
                {
                    key += NodePathSeparator + segment;
                    if (!groupNodes.TryGetValue(key: key, value: out TreeNode groupNode))
                    {
                        groupNode = level.Add(text: segment);
                        groupNodes[key: key] = groupNode;
                    }

                    level = groupNode.Nodes;
                }

                TreeNode leaf = level.Add(text: favourite.FavouriteName);
                leaf.Tag = favourite;
                if (ReferenceEquals(objA: favourite, objB: select))
                {
                    selectNode = leaf;
                }
            }

            if (placed.Count <= MaxNodesToExpandAll)
            {
                trv_Favourites.ExpandAll();
            }

            if (selectNode != null)
            {
                trv_Favourites.SelectedNode = selectNode;
                selectNode.EnsureVisible();
            }
        }
        finally
        {
            trv_Favourites.EndUpdate();
            _rebuilding = false;
        }

        if (select == null || trv_Favourites.SelectedNode == null)
        {
            // nothing is selected in the tree, so nothing should be showing as selected in the details either.
            LoadFavourite(favourite: null);
        }
        else if (!ReferenceEquals(objA: _current, objB: select))
        {
            LoadFavourite(favourite: select);
        }

        UpdateCount();
    }

    private void trv_Favourites_BeforeSelect(object sender,
                                             TreeViewCancelEventArgs e)
    {
        if (_rebuilding ||
            !_editsPending ||
            ReferenceEquals(objA: e.Node?.Tag, objB: _current))
        {
            return;
        }

        if (!ResolvePendingEdits())
        {
            e.Cancel = true;
        }
    }

    private void trv_Favourites_AfterSelect(object sender,
                                            TreeViewEventArgs e)
    {
        if (_rebuilding)
        {
            return;
        }

        LoadFavourite(favourite: e.Node?.Tag as Favourite);
        UpdateCount();
    }

    private void UpdateCount()
    {
        lbl_FavouritesCount.Text = string.Format(format: _countFormat, arg0: _favourites.Count,
            arg1: _current == null ? 0 : 1);
    }

    private void any_GroupByItem_Click(object sender,
                                       EventArgs e)
    {
        _groupKeys.Clear();
        AddIfChecked(item: tmi_GroupByCountryCode, key: FavouriteGroupKey.CountryCode);
        AddIfChecked(item: tmi_GroupByCountry, key: FavouriteGroupKey.Country);
        AddIfChecked(item: tmi_GroupByState, key: FavouriteGroupKey.State);
        AddIfChecked(item: tmi_GroupByCity, key: FavouriteGroupKey.City);
        AddIfChecked(item: tmi_GroupByCoordinates, key: FavouriteGroupKey.Coordinates);

        RebuildTree(select: _current);

        void AddIfChecked(ToolStripMenuItem item,
                          FavouriteGroupKey key)
        {
            if (item.Checked)
            {
                _ = _groupKeys.Add(item: key);
            }
        }
    }

    private void tmi_GroupByNone_Click(object sender,
                                       EventArgs e)
    {
        foreach (ToolStripMenuItem item in new[]
                 {
                     tmi_GroupByCountryCode, tmi_GroupByCountry, tmi_GroupByState, tmi_GroupByCity,
                     tmi_GroupByCoordinates
                 })
        {
            item.Checked = false;
        }

        _groupKeys.Clear();
        RebuildTree(select: _current);
    }

    #endregion

    #region Toolbar

    private void tsb_Duplicate_Click(object sender,
                                     EventArgs e)
    {
        if (_current == null || !ResolvePendingEdits())
        {
            return;
        }

        Favourite copy = _current.Clone();
        copy.FavouriteName = FavouriteGrouping.UniqueName(baseName: _current.FavouriteName,
            existingNames: _favourites.Select(selector: favourite => favourite.FavouriteName));

        AddAndSelect(favourite: copy);
    }

    private void tsb_Delete_Click(object sender,
                                  EventArgs e)
    {
        if (_current == null)
        {
            return;
        }

        _ = _favourites.Remove(item: _current);
        _listChanged = true;
        _current = null;
        SetEditsPending(pending: false);
        RebuildTree(select: null);
    }

    /// <summary>
    ///     Makes a new favourite from the photo currently selected in the main window: its position, altitude and
    ///     location names.
    /// </summary>
    private void tsb_AddFromSelection_Click(object sender,
                                            EventArgs e)
    {
        if (!ResolvePendingEdits())
        {
            return;
        }

        DirectoryElement directoryElement = FrmMainApp.Instance?.lvw_FileList.SelectedItems
                                                      .Cast<ListViewItem>()
                                                      .Select(selector: item => item.Tag as DirectoryElement)
                                                      .FirstOrDefault(predicate: element =>
                                                           element is { Type: DirectoryElement.ElementType.File });
        if (directoryElement == null)
        {
            ThemeHelper.ShowMessageBox(
                message: MessageText(name: "mbx_FrmMainApp_WarningNoItemSelected",
                    fallback: "Please select a file in the main window first."),
                icon: MessageBoxIcon.Warning);
            return;
        }

        Favourite favourite = Favourites.FromDirectoryElement(directoryElement: directoryElement,
            favouriteName: "");
        if (!FavouriteMatcher.TryGetPosition(favourite: favourite, latitude: out _, longitude: out _))
        {
            ThemeHelper.ShowMessageBox(
                message: MessageText(name: "mbx_FrmManageFavourites_WarningNoCoordinates",
                    fallback: "The selected file has no coordinates, so there is no position to save."),
                icon: MessageBoxIcon.Warning);
            return;
        }

        string suggestedName = string.IsNullOrWhiteSpace(value: favourite.City)
            ? Path.GetFileNameWithoutExtension(path: directoryElement.ItemNameWithoutPath)
            : favourite.City;
        favourite.FavouriteName = FavouriteGrouping.UniqueName(baseName: suggestedName,
            existingNames: _favourites.Select(selector: existing => existing.FavouriteName));

        AddAndSelect(favourite: favourite);
        tbx_FavouriteName.Focus();
        tbx_FavouriteName.SelectAll();
    }

    private void AddAndSelect(Favourite favourite)
    {
        _favourites.Add(item: favourite);
        _listChanged = true;
        _current = null;
        RebuildTree(select: favourite);
    }

    #endregion

    #region The details panel

    /// <summary>
    ///     Fills the details panel from a favourite, or empties and disables it for none.
    /// </summary>
    private void LoadFavourite(Favourite favourite)
    {
        _loading = true;
        try
        {
            _current = favourite;
            bool hasFavourite = favourite != null;

            gbx_FavouriteDetails.Enabled = hasFavourite;
            tsb_Duplicate.Enabled = hasFavourite;
            tsb_Delete.Enabled = hasFavourite;

            tbx_FavouriteName.Text = favourite?.FavouriteName ?? "";
            tbx_FavouriteCoordinates.Text = DescribePosition(favourite: favourite);
            nud_FavouriteRadius.Value = Math.Min(val1: Math.Max(val1: favourite?.Radius ?? 0, val2: 0),
                val2: (int)nud_FavouriteRadius.Maximum);
            ckb_FavouriteAutoAssign.Checked = favourite?.AutoAssign ?? false;
            ckb_FavouriteSnap.Checked = favourite?.Snap ?? false;
            ckb_FavouriteSnap.Enabled = ckb_FavouriteAutoAssign.Checked;
            tbx_FavouriteAltitude.Text = favourite?.GPSAltitude ?? "";
            cbx_FavouriteTimeZone.Text = favourite?.TimeZoneId ?? "";
            SetCountry(code: favourite?.CountryCode, name: favourite?.Country);
            tbx_State.Text = favourite?.State ?? "";
            tbx_City.Text = favourite?.City ?? "";
            tbx_Sublocation.Text = favourite?.Sublocation ?? "";

            lbx_FavouriteKeywords.Items.Clear();
            lbx_FavouriteKeywords.Items.AddRange(items: (favourite?.Keywords ?? [])
                                                       .Cast<object>()
                                                       .ToArray());
            tbx_FavouriteKeyword.Text = "";
        }
        finally
        {
            _loading = false;
        }

        SetEditsPending(pending: false);
    }

    private static string DescribePosition(Favourite favourite)
    {
        if (favourite == null)
        {
            return "";
        }

        return FavouriteMatcher.TryGetPosition(favourite: favourite, latitude: out double latitude,
            longitude: out double longitude)
            ? FormattableString.Invariant(formattable: $"{latitude:0.######}; {longitude:0.######}")
            : favourite.Coordinates ?? "";
    }

    /// <summary>
    ///     Shows a country: the code and the name are looked up from one another, and whichever the favourite has
    ///     that is not on the lists is shown as it is rather than dropped.
    /// </summary>
    private void SetCountry(string code,
                            string name)
    {
        _syncingCountry = true;
        try
        {
            code = code?.Trim() ?? "";
            name = name?.Trim() ?? "";

            if (name.Length == 0 && code.Length > 0)
            {
                name = LanguageTZ.DataReadDTCountryCodesNames(
                                      queryWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A3,
                                      inputVal: code,
                                      returnWhat: LanguageMappingQueryOrReturnWhat.Country) ??
                       "";
            }
            else if (code.Length == 0 && name.Length > 0)
            {
                code = LanguageTZ.DataReadDTCountryCodesNames(
                                      queryWhat: LanguageMappingQueryOrReturnWhat.Country,
                                      inputVal: name,
                                      returnWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A3) ??
                       "";
            }

            cbx_CountryCode.Text = code;
            cbx_Country.Text = name;
        }
        finally
        {
            _syncingCountry = false;
        }
    }

    private void cbx_Country_SelectedIndexChanged(object sender,
                                                  EventArgs e)
    {
        if (_syncingCountry || _loading)
        {
            return;
        }

        SetCountry(code: "", name: cbx_Country.Text);
        SetEditsPending(pending: true);
    }

    private void cbx_CountryCode_SelectedIndexChanged(object sender,
                                                      EventArgs e)
    {
        if (_syncingCountry || _loading)
        {
            return;
        }

        SetCountry(code: cbx_CountryCode.Text, name: "");
        SetEditsPending(pending: true);
    }

    private void any_Edit_Changed(object sender,
                                  EventArgs e)
    {
        if (!_loading)
        {
            SetEditsPending(pending: true);
        }
    }

    private void ckb_FavouriteAutoAssign_CheckedChanged(object sender,
                                                        EventArgs e)
    {
        // snapping only means something for photos that are auto-assigned.
        ckb_FavouriteSnap.Enabled = ckb_FavouriteAutoAssign.Checked;
        any_Edit_Changed(sender: sender, e: e);
    }

    private void SetEditsPending(bool pending)
    {
        _editsPending = pending && _current != null;
        btn_FavouriteAccept.Enabled = _editsPending;
        btn_FavouriteDiscard.Enabled = _editsPending;
    }

    #endregion

    #region Accept and discard

    private void btn_FavouriteAccept_Click(object sender,
                                           EventArgs e)
    {
        _ = TryAccept();
    }

    private void btn_FavouriteDiscard_Click(object sender,
                                            EventArgs e)
    {
        LoadFavourite(favourite: _current);
    }

    /// <summary>
    ///     Asks what to do with the edits pending on the selected favourite.
    /// </summary>
    /// <returns>False if the user backed out, or accepting failed, so whatever was about to happen should not.</returns>
    private bool ResolvePendingEdits()
    {
        if (!_editsPending)
        {
            return true;
        }

        switch (ThemeHelper.ShowMessageBoxWithResult(
                    message: MessageText(name: "mbx_FrmManageFavourites_QuestionAcceptChanges",
                        fallback: "Accept the changes to this favourite?"),
                    buttons: MessageBoxButtons.YesNoCancel,
                    icon: MessageBoxIcon.Question))
        {
            case DialogResult.Yes:
                return TryAccept();
            case DialogResult.No:
                LoadFavourite(favourite: _current);
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     Checks the edited values and, if they are fine, copies them into the selected favourite.
    /// </summary>
    /// <returns>False, after telling the user why, if a value is not acceptable.</returns>
    private bool TryAccept()
    {
        if (_current == null)
        {
            return true;
        }

        string name = tbx_FavouriteName.Text.Trim();
        if (name.Length == 0)
        {
            ThemeHelper.ShowMessageBox(
                message: MessageText(name: "mbx_FrmMainApp_InfoFavouriteNameCannotBeBlank",
                    fallback: "The favourite's name cannot be blank."),
                icon: MessageBoxIcon.Warning);
            tbx_FavouriteName.Focus();
            return false;
        }

        if (_favourites.Any(predicate: favourite => !ReferenceEquals(objA: favourite, objB: _current) &&
                                                    string.Equals(a: favourite.FavouriteName, b: name,
                                                        comparisonType: StringComparison.OrdinalIgnoreCase)))
        {
            ThemeHelper.ShowMessageBox(
                message: MessageText(name: "mbx_FrmManageFavourites_WarningNameTaken",
                    fallback: "Another favourite already has this name."),
                icon: MessageBoxIcon.Warning);
            tbx_FavouriteName.Focus();
            return false;
        }

        string altitude = "";
        string altitudeRef = _current.GPSAltitudeRef;
        string altitudeText = tbx_FavouriteAltitude.Text.Trim();
        if (altitudeText.Length > 0)
        {
            if (!AttributeValueFormatter.TryParseDouble(value: altitudeText, context: ValueFormatContext.Display,
                    result: out double altitudeValue))
            {
                ThemeHelper.ShowMessageBox(
                    message: MessageText(name: "mbx_FrmManageFavourites_WarningAltitudeInvalid",
                        fallback: "The altitude must be a number."),
                    icon: MessageBoxIcon.Warning);
                tbx_FavouriteAltitude.Focus();
                return false;
            }

            altitude = AttributeValueFormatter.FormatDouble(value: altitudeValue,
                context: ValueFormatContext.RoundTrip);
            if (altitude != _current.GPSAltitude)
            {
                altitudeRef = altitudeValue >= 0 ? "Above Sea Level" : "Below Sea Level";
            }
        }
        else
        {
            altitudeRef = "";
        }

        _current.FavouriteName = name;
        _current.Radius = (int)nud_FavouriteRadius.Value;
        _current.AutoAssign = ckb_FavouriteAutoAssign.Checked;
        _current.Snap = ckb_FavouriteSnap.Checked;
        _current.GPSAltitude = altitude;
        _current.GPSAltitudeRef = altitudeRef;
        _current.TimeZoneId = cbx_FavouriteTimeZone.Text.Trim();
        _current.CountryCode = cbx_CountryCode.Text.Trim();
        _current.Country = cbx_Country.Text.Trim();
        _current.State = tbx_State.Text.Trim();
        _current.City = tbx_City.Text.Trim();
        _current.Sublocation = tbx_Sublocation.Text.Trim();
        _current.Keywords = FavouriteKeywords.Split(stored: FavouriteKeywords.Join(
            keywords: lbx_FavouriteKeywords.Items.Cast<string>()));

        _listChanged = true;
        SetEditsPending(pending: false);

        // the name and the location may have moved it to another branch of the tree.
        RebuildTree(select: _current);
        return true;
    }

    #endregion

    #region Location, keywords and clearing

    /// <summary>
    ///     Fills country, state, city, sublocation and time zone from the web (the same GeoNames lookup, and the same
    ///     custom rules, the edit-file dialog uses) for the favourite's position.
    /// </summary>
    private void btn_FavouriteGetFromWeb_Click(object sender,
                                               EventArgs e)
    {
        if (!FavouriteMatcher.TryGetPosition(favourite: _current, latitude: out double latitude,
                longitude: out double longitude))
        {
            ThemeHelper.ShowMessageBox(
                message: MessageText(name: "mbx_FrmManageFavourites_WarningNoCoordinates",
                    fallback: "This favourite has no valid coordinates."),
                icon: MessageBoxIcon.Warning);
            return;
        }

        DataTable toponomy;
        HelperVariables.OperationAPIReturnedOKResponse = true;
        Cursor = Cursors.WaitCursor;
        try
        {
            toponomy = ReadExifData.DTFromAPIExifGetToponomyFromWebOrSQL(
                lat: latitude.ToString(provider: CultureInfo.InvariantCulture),
                lng: longitude.ToString(provider: CultureInfo.InvariantCulture),
                fileNameWithoutPath: tbx_FavouriteName.Text);
        }
        finally
        {
            Cursor = Cursors.Default;
        }

        if (toponomy == null || toponomy.Rows.Count == 0)
        {
            // a failed request has already told the user why; an empty answer has not.
            if (HelperVariables.OperationAPIReturnedOKResponse)
            {
                ThemeHelper.ShowMessageBox(
                    message: MessageText(name: "mbx_FrmManageFavourites_InfoWebNothingFound",
                        fallback: "The web service has nothing for this position."),
                    icon: MessageBoxIcon.Information);
            }

            return;
        }

        DataRow row = toponomy.Rows[index: 0];
        SetCountry(code: Cell(row: row, item: DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode),
            name: Cell(row: row, item: DefaultColumnNamesFromElementAttributesForFileEditing.Country));
        tbx_State.Text = Cell(row: row, item: DefaultColumnNamesFromElementAttributesForFileEditing.State);
        tbx_City.Text = Cell(row: row, item: DefaultColumnNamesFromElementAttributesForFileEditing.City);
        tbx_Sublocation.Text = Cell(row: row, item: DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation);

        string timeZone = Cell(row: row, item: DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId);
        if (timeZone.Length > 0)
        {
            cbx_FavouriteTimeZone.Text = timeZone;
        }

        SetEditsPending(pending: true);
    }

    private static string Cell(DataRow row,
                               DefaultColumnNamesFromElementAttributesForFileEditing item)
    {
        string columnName = ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
            key: ReadExifData.GetToponomyDataColumnName(itemName: item, useDefaultHardcodedEnglishValues: true)];

        return row.Table.Columns.Contains(name: columnName)
            ? $"{row[columnName: columnName]}".Trim()
            : "";
    }

    private void btn_FavouriteClearAll_Click(object sender,
                                             EventArgs e)
    {
        if (_current == null)
        {
            return;
        }

        _loading = true;
        try
        {
            nud_FavouriteRadius.Value = 0;
            ckb_FavouriteAutoAssign.Checked = false;
            ckb_FavouriteSnap.Checked = false;
            ckb_FavouriteSnap.Enabled = false;
            tbx_FavouriteAltitude.Text = "";
            cbx_FavouriteTimeZone.Text = "";
            SetCountry(code: "", name: "");
            tbx_State.Text = "";
            tbx_City.Text = "";
            tbx_Sublocation.Text = "";
            lbx_FavouriteKeywords.Items.Clear();
            tbx_FavouriteKeyword.Text = "";
        }
        finally
        {
            _loading = false;
        }

        SetEditsPending(pending: true);
    }

    private void btn_FavouriteAddKeyword_Click(object sender,
                                               EventArgs e)
    {
        AddKeyword();
    }

    private void tbx_FavouriteKeyword_KeyDown(object sender,
                                              KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            AddKeyword();
        }
    }

    private void AddKeyword()
    {
        string keyword = tbx_FavouriteKeyword.Text.Trim();
        if (keyword.Length == 0)
        {
            return;
        }

        if (!lbx_FavouriteKeywords.Items.Cast<string>()
                                  .Contains(value: keyword, comparer: StringComparer.OrdinalIgnoreCase))
        {
            _ = lbx_FavouriteKeywords.Items.Add(item: keyword);
            SetEditsPending(pending: true);
        }

        tbx_FavouriteKeyword.Text = "";
        tbx_FavouriteKeyword.Focus();
    }

    private void btn_FavouriteRemoveKeyword_Click(object sender,
                                                  EventArgs e)
    {
        RemoveSelectedKeyword();
    }

    private void lbx_FavouriteKeywords_KeyDown(object sender,
                                               KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete)
        {
            e.Handled = true;
            RemoveSelectedKeyword();
        }
    }

    private void RemoveSelectedKeyword()
    {
        int index = lbx_FavouriteKeywords.SelectedIndex;
        if (index < 0)
        {
            return;
        }

        lbx_FavouriteKeywords.Items.RemoveAt(index: index);
        if (lbx_FavouriteKeywords.Items.Count > 0)
        {
            lbx_FavouriteKeywords.SelectedIndex = Math.Min(val1: index, val2: lbx_FavouriteKeywords.Items.Count - 1);
        }

        SetEditsPending(pending: true);
    }

    #endregion
}
