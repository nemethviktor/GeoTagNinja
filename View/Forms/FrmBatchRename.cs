using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.FileSystem;
using GeoTagNinja.Helpers.Localisation;
using GeoTagNinja.Helpers.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     Batch Rename, modelled on Adobe Bridge's dialog of the same name: a name built from pieces (text, folder name,
///     sequence number, date, metadata, find-and-replace...), where the files go, presets and a live preview.
/// </summary>
/// <remarks>
///     Built entirely in code: the rows change their controls with the kind of piece, which a designer file would only
///     get in the way of. It only collects choices and shows what they would do (<see cref="BatchRenamePlanner" />);
///     the caller runs the plan (<see cref="BatchRenameExecutor" />). All text comes from <c>Strings.resx</c> through
///     <see cref="BatchRenameText" />, so it can be translated; the keys of the drop-downs are built from the names of
///     the enums behind them (<c>lbl_BatchRename_Kind_&lt;BatchRenameKind&gt;</c> and so on), which
///     <c>BatchRenameTextTests</c> checks.
/// </remarks>
internal sealed class FrmBatchRename : Form
{
    private const int RowWidth = 750;
    private const int RowHeight = 36;
    private const int SubstitutionRowHeight = 108;

    /// <summary>The kinds of piece, in the order the drop-down lists them.</summary>
    internal static readonly BatchRenameKind[] KindOrder =
    [
        BatchRenameKind.Text, BatchRenameKind.NewExtension, BatchRenameKind.CurrentFilename, BatchRenameKind.PreservedFilename,
        BatchRenameKind.SequenceNumber, BatchRenameKind.SequenceLetter, BatchRenameKind.DateTime, BatchRenameKind.Metadata,
        BatchRenameKind.FolderName, BatchRenameKind.StringSubstitution
    ];

    private static readonly BatchRenameCase[] CaseOrder = [BatchRenameCase.Original, BatchRenameCase.Lower, BatchRenameCase.Upper];

    private static readonly BatchRenameFilePart[] PartOrder =
    [
        BatchRenameFilePart.NameAndExtension, BatchRenameFilePart.Name, BatchRenameFilePart.Extension, BatchRenameFilePart.NumberSuffix
    ];

    /// <summary>The dates on offer, in the order the drop-down lists them.</summary>
    internal static readonly BatchRenameDateSource[] DateSourceOrder =
    [
        BatchRenameDateSource.DateTaken, BatchRenameDateSource.DateCreated, BatchRenameDateSource.FileModified,
        BatchRenameDateSource.FileCreated, BatchRenameDateSource.Today, BatchRenameDateSource.Yesterday
    ];

    private static readonly BatchRenameSubstituteSource[] SubstituteSourceOrder =
        [BatchRenameSubstituteSource.OriginalFilename, BatchRenameSubstituteSource.PreservedFilename];

    /// <summary>The most digits a sequence number can be padded to in the drop-down.</summary>
    internal const int MaxDigitsOffered = 6;

    private static readonly string[] DateFormats =
    [
        "YYYYMMDD", "YYYY-MM-DD", "YYMMDD", "MMDDYYYY", "DDMMYYYY", "YYYY", "MM", "DD",
        "hhmmss", "hh-mm-ss", "YYYYMMDD_hhmmss", "YYYY-MM-DD_hh-mm-ss"
    ];

    private static string T(string key,
                            params object[] args)
    {
        return BatchRenameText.Get(key: key, args: args);
    }

    private static string[] Captions<TEnum>(string prefix,
                                            IEnumerable<TEnum> values)
    {
        return values.Select(selector: value => T(key: $"{prefix}_{value}")).ToArray();
    }

    private readonly IReadOnlyList<BatchRenameSource> _sources;
    private readonly IReadOnlyList<(string key, string caption)> _fields;
    private readonly List<ComponentRow> _rows = [];

    private readonly ComboBox _cbxPreset = new();
    private readonly Label _lblPresetState = new();
    private readonly Button _btnSavePreset = new();
    private readonly Button _btnDeletePreset = new();
    private readonly RadioButton _rbtSame = new();
    private readonly RadioButton _rbtMove = new();
    private readonly RadioButton _rbtCopy = new();
    private readonly Button _btnBrowse = new();
    private readonly Label _lblFolder = new();
    private readonly Panel _pnlRows = new();
    private readonly CheckBox _ckbMac = new();
    private readonly CheckBox _ckbUnix = new();
    private readonly Label _lblCurrent = new();
    private readonly Label _lblNew = new();
    private readonly Label _lblCount = new();
    private readonly Button _btnRename = new();
    private readonly Button _btnPreview = new();
    private readonly Timer _refreshTimer = new() { Interval = 150 };

    private string _destinationFolder = string.Empty;
    private string _loadedPreset;
    private bool _loading;
    private List<BatchRenameEntry> _plan = [];

    /// <summary>What the user chose (valid after OK).</summary>
    internal BatchRenameSettings Result { get; private set; }

    /// <summary>What the choices do to each file (valid after OK; no entry has a problem).</summary>
    internal List<BatchRenameEntry> ResultPlan => _plan;

    /// <param name="sources">The files to rename, in the order they are listed (that is the order they are numbered in).</param>
    /// <param name="fields">The metadata the Metadata piece can pick: attribute name and caption.</param>
    internal FrmBatchRename(IReadOnlyList<BatchRenameSource> sources,
                            IReadOnlyList<(string key, string caption)> fields)
    {
        _sources = sources;
        _fields = fields;

        Text = T(key: "frm_BatchRename");
        AutoScaleDimensions = new SizeF(width: 96F, height: 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(width: 1000, height: 700);

        BuildPresetGroup();
        BuildDestinationGroup();
        BuildNamesGroup();
        BuildOptionsGroup();
        BuildPreviewGroup();
        BuildButtons();

        _refreshTimer.Tick += (_,
                               _) =>
        {
            _refreshTimer.Stop();
            Refresh_();
        };

        AddRow(component: new BatchRenameComponent { Kind = BatchRenameKind.CurrentFilename }, after: null);
        Refresh_();

        // not the (empty) preset box, which would open with its blank selection highlighted
        ActiveControl = _btnPreview;

        Shown += (_,
                  _) => ThemeHelper.ApplyTo(control: this);
        FormClosed += (_,
                       _) => _refreshTimer.Dispose();
    }

    #region Building the form

    private static GroupBox Group(string text,
                                  int top,
                                  int height)
    {
        return new GroupBox
        {
            Text = text,
            Location = new Point(x: 12, y: top),
            Size = new Size(width: 790, height: height)
        };
    }

    private void BuildPresetGroup()
    {
        GroupBox group = Group(text: T(key: "gbx_BatchRename_Presets"), top: 8, height: 62);
        group.Controls.Add(value: new Label { Text = T(key: "lbl_BatchRename_Preset"), AutoSize = true, Location = new Point(x: 14, y: 28) });

        _cbxPreset.DropDownStyle = ComboBoxStyle.DropDownList;
        _cbxPreset.Location = new Point(x: 90, y: 24);
        _cbxPreset.Width = 380;
        _cbxPreset.SelectedIndexChanged += (_,
                                            _) => LoadSelectedPreset();
        group.Controls.Add(value: _cbxPreset);

        _lblPresetState.AutoSize = true;
        _lblPresetState.Location = new Point(x: 480, y: 28);
        group.Controls.Add(value: _lblPresetState);

        _btnSavePreset.Text = T(key: "btn_BatchRename_Save");
        _btnSavePreset.Location = new Point(x: 600, y: 22);
        _btnSavePreset.Size = new Size(width: 84, height: 27);
        _btnSavePreset.Click += (_,
                                 _) => SavePreset();
        group.Controls.Add(value: _btnSavePreset);

        _btnDeletePreset.Text = T(key: "btn_BatchRename_Delete");
        _btnDeletePreset.Location = new Point(x: 690, y: 22);
        _btnDeletePreset.Size = new Size(width: 84, height: 27);
        _btnDeletePreset.Click += (_,
                                   _) => DeleteSelectedPreset();
        group.Controls.Add(value: _btnDeletePreset);

        FillPresetList(select: null);
        Controls.Add(value: group);
    }

    private void BuildDestinationGroup()
    {
        GroupBox group = Group(text: T(key: "gbx_BatchRename_Destination"), top: 78, height: 134);
        _rbtSame.Text = T(key: "rbt_BatchRename_SameFolder");
        _rbtMove.Text = T(key: "rbt_BatchRename_MoveToFolder");
        _rbtCopy.Text = T(key: "rbt_BatchRename_CopyToFolder");
        _rbtSame.Checked = true;
        int top = 22;
        foreach (RadioButton radio in new[] { _rbtSame, _rbtMove, _rbtCopy })
        {
            radio.AutoSize = true;
            radio.Location = new Point(x: 14, y: top);
            radio.CheckedChanged += (_,
                                     _) =>
            {
                UpdateDestinationControls();
                OnChanged();
            };
            group.Controls.Add(value: radio);
            top += 26;
        }

        _btnBrowse.Text = T(key: "btn_BatchRename_Browse");
        _btnBrowse.Location = new Point(x: 38, y: top + 2);
        _btnBrowse.Size = new Size(width: 90, height: 27);
        _btnBrowse.Click += (_,
                             _) => BrowseForFolder();
        group.Controls.Add(value: _btnBrowse);

        _lblFolder.AutoSize = false;
        _lblFolder.Location = new Point(x: 136, y: top + 7);
        _lblFolder.Size = new Size(width: 640, height: 20);
        _lblFolder.AutoEllipsis = true;
        group.Controls.Add(value: _lblFolder);

        UpdateDestinationControls();
        Controls.Add(value: group);
    }

    private void BuildNamesGroup()
    {
        GroupBox group = Group(text: T(key: "gbx_BatchRename_Names"), top: 220, height: 270);
        _pnlRows.Location = new Point(x: 12, y: 22);
        _pnlRows.Size = new Size(width: 766, height: 240);
        _pnlRows.AutoScroll = true;
        group.Controls.Add(value: _pnlRows);
        Controls.Add(value: group);
    }

    private void BuildOptionsGroup()
    {
        GroupBox group = Group(text: T(key: "gbx_BatchRename_Options"), top: 498, height: 58);
        group.Controls.Add(value: new Label { Text = T(key: "lbl_BatchRename_Compatibility"), AutoSize = true, Location = new Point(x: 14, y: 26) });

        // Windows rules always apply: ExifTool and this app run on Windows, so the box is ticked and fixed
        group.Controls.Add(value: new CheckBox
        {
            Text = T(key: "ckb_BatchRename_Windows"),
            Checked = true,
            Enabled = false,
            AutoSize = true,
            Location = new Point(x: 130, y: 24)
        });

        _ckbMac.Text = T(key: "ckb_BatchRename_Mac");
        _ckbMac.AutoSize = true;
        _ckbMac.Location = new Point(x: 250, y: 24);
        _ckbMac.CheckedChanged += (_,
                                   _) => OnChanged();
        group.Controls.Add(value: _ckbMac);

        _ckbUnix.Text = T(key: "ckb_BatchRename_Unix");
        _ckbUnix.AutoSize = true;
        _ckbUnix.Location = new Point(x: 340, y: 24);
        _ckbUnix.CheckedChanged += (_,
                                    _) => OnChanged();
        group.Controls.Add(value: _ckbUnix);
        Controls.Add(value: group);
    }

    private void BuildPreviewGroup()
    {
        GroupBox group = Group(text: T(key: "gbx_BatchRename_Preview"), top: 564, height: 126);
        int top = 22;
        foreach (Label label in new[] { _lblCurrent, _lblNew, _lblCount })
        {
            label.AutoSize = false;
            label.AutoEllipsis = true;
            label.Location = new Point(x: 14, y: top);
            label.Size = new Size(width: 762, height: 24);
            group.Controls.Add(value: label);
            top += 30;
        }

        Controls.Add(value: group);
    }

    private void BuildButtons()
    {
        _btnRename.Text = T(key: "btn_BatchRename_Rename");
        _btnRename.Location = new Point(x: 816, y: 20);
        _btnRename.Size = new Size(width: 172, height: 32);
        _btnRename.Click += (_,
                             _) => OnRenameClicked();
        Controls.Add(value: _btnRename);

        Button cancel = new()
        {
            Text = HelperControlAndMessageBoxHandling.ReturnControlText(controlName: "btn_Generic_Cancel",
                fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.Button),
            DialogResult = DialogResult.Cancel,
            Location = new Point(x: 816, y: 60),
            Size = new Size(width: 172, height: 32)
        };
        Controls.Add(value: cancel);

        _btnPreview.Text = T(key: "btn_BatchRename_PreviewAll");
        _btnPreview.Location = new Point(x: 816, y: 100);
        _btnPreview.Size = new Size(width: 172, height: 32);
        _btnPreview.Click += (_,
                              _) => ShowAllNames();
        Controls.Add(value: _btnPreview);

        AcceptButton = _btnRename;
        CancelButton = cancel;
    }

    #endregion

    #region Rows

    private void AddRow(BatchRenameComponent component,
                        ComponentRow after)
    {
        ComponentRow row = new(component: component, fields: _fields);
        row.Changed += (_,
                        _) => OnChanged();
        row.PlusClicked += (sender,
                            _) => AddRow(component: new BatchRenameComponent { Kind = BatchRenameKind.Text },
            after: (ComponentRow)sender);
        row.MinusClicked += (sender,
                             _) => RemoveRow(row: (ComponentRow)sender);
        row.SizeChanged += (_,
                            _) => LayoutRows();

        int index = after == null
            ? _rows.Count
            : _rows.IndexOf(item: after) + 1;
        _rows.Insert(index: index, item: row);
        _pnlRows.Controls.Add(value: row);
        LayoutRows();
        ThemeHelper.ApplyTo(control: row);
        OnChanged();
    }

    private void RemoveRow(ComponentRow row)
    {
        if (_rows.Count <= 1)
        {
            return;
        }

        _ = _rows.Remove(item: row);
        _pnlRows.Controls.Remove(value: row);
        row.Dispose();
        LayoutRows();
        OnChanged();
    }

    private void LayoutRows()
    {
        int top = 4;
        foreach (ComponentRow row in _rows)
        {
            row.Location = new Point(x: 0, y: top);
            row.CanRemove = _rows.Count > 1;
            top += row.Height + 2;
        }
    }

    #endregion

    #region Destination

    private void UpdateDestinationControls()
    {
        bool other = !_rbtSame.Checked;
        _btnBrowse.Enabled = other;
        _lblFolder.Text = other
            ? _destinationFolder
            : string.Empty;
    }

    private void BrowseForFolder()
    {
        using FolderBrowserDialog dialog = new();
        if (_destinationFolder.Length > 0 &&
            Directory.Exists(path: _destinationFolder))
        {
            dialog.SelectedPath = _destinationFolder;
        }

        if (dialog.ShowDialog(owner: this) == DialogResult.OK)
        {
            _destinationFolder = dialog.SelectedPath;
            UpdateDestinationControls();
            OnChanged();
        }
    }

    #endregion

    #region Presets

    private void FillPresetList(string select)
    {
        _loading = true;
        _cbxPreset.Items.Clear();
        foreach (string name in BatchRenamePresets.Names())
        {
            _ = _cbxPreset.Items.Add(item: name);
        }

        _cbxPreset.SelectedItem = select;
        _loading = false;
        _btnDeletePreset.Enabled = _cbxPreset.SelectedItem != null;
    }

    private void LoadSelectedPreset()
    {
        if (_loading ||
            _cbxPreset.SelectedItem is not string name)
        {
            return;
        }

        BatchRenameSettings settings = BatchRenamePresets.Read(presetName: name);
        if (settings == null ||
            settings.Components.Count == 0)
        {
            ThemeHelper.ShowMessageBox(message: T(key: "mbx_BatchRename_PresetUnreadable", name), caption: T(key: "frm_BatchRename"),
                buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
            return;
        }

        _loading = true;
        foreach (ComponentRow row in _rows)
        {
            _pnlRows.Controls.Remove(value: row);
            row.Dispose();
        }

        _rows.Clear();
        foreach (BatchRenameComponent component in settings.Components)
        {
            AddRow(component: component, after: null);
        }

        _destinationFolder = settings.DestinationFolder ?? string.Empty;
        _rbtSame.Checked = settings.Destination == BatchRenameDestination.SameFolder;
        _rbtMove.Checked = settings.Destination == BatchRenameDestination.MoveToFolder;
        _rbtCopy.Checked = settings.Destination == BatchRenameDestination.CopyToFolder;
        _ckbMac.Checked = settings.MacCompatible;
        _ckbUnix.Checked = settings.UnixCompatible;
        UpdateDestinationControls();
        _loadedPreset = name;
        _loading = false;
        _btnDeletePreset.Enabled = true;
        OnChanged();
        _lblPresetState.Text = string.Empty;
    }

    private void SavePreset()
    {
        string name = Microsoft.VisualBasic.Interaction.InputBox(Prompt: T(key: "mbx_BatchRename_PresetNamePrompt"),
            Title: T(key: "frm_BatchRename"), DefaultResponse: _loadedPreset ?? string.Empty).Trim();
        if (name.Length == 0)
        {
            return;
        }

        if (_cbxPreset.Items.Cast<string>().Contains(value: name, comparer: StringComparer.OrdinalIgnoreCase) &&
            ThemeHelper.ShowMessageBoxWithResult(message: T(key: "mbx_BatchRename_PresetReplace", name),
                caption: T(key: "frm_BatchRename"), buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        if (!BatchRenamePresets.Write(presetName: name, settings: Collect()))
        {
            ThemeHelper.ShowMessageBox(message: T(key: "mbx_BatchRename_PresetSaveFailed"), caption: T(key: "frm_BatchRename"),
                buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
            return;
        }

        _loadedPreset = name;
        FillPresetList(select: name);
        _lblPresetState.Text = string.Empty;
    }

    private void DeleteSelectedPreset()
    {
        if (_cbxPreset.SelectedItem is not string name ||
            ThemeHelper.ShowMessageBoxWithResult(message: T(key: "mbx_BatchRename_PresetDelete", name), caption: T(key: "frm_BatchRename"),
                buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        BatchRenamePresets.Delete(presetName: name);
        _loadedPreset = null;
        FillPresetList(select: null);
        _lblPresetState.Text = string.Empty;
    }

    #endregion

    #region Preview and running

    private BatchRenameSettings Collect()
    {
        return new BatchRenameSettings
        {
            Components = _rows.Select(selector: row => row.Component).ToList(),
            Destination = _rbtMove.Checked
                ? BatchRenameDestination.MoveToFolder
                : _rbtCopy.Checked
                    ? BatchRenameDestination.CopyToFolder
                    : BatchRenameDestination.SameFolder,
            DestinationFolder = _destinationFolder,
            MacCompatible = _ckbMac.Checked,
            UnixCompatible = _ckbUnix.Checked
        };
    }

    /// <summary>Something changed: work out the preview shortly (typing should not recheck every file per key).</summary>
    private void OnChanged()
    {
        if (_loading)
        {
            return;
        }

        if (_loadedPreset != null)
        {
            _lblPresetState.Text = T(key: "lbl_BatchRename_Modified");
        }

        _refreshTimer.Stop();
        _refreshTimer.Start();
    }

    private void Refresh_()
    {
        _plan = BatchRenamePlanner.Plan(sources: _sources, settings: Collect(), pathExists: File.Exists, now: DateTime.Now, maxPathLength: LongPathSupport.MaxPathLength);

        BatchRenameEntry first = _plan.Count > 0
            ? _plan[index: 0]
            : null;
        _lblCurrent.Text = first == null
            ? string.Empty
            : T(key: "lbl_BatchRename_CurrentFilename") + "  " + Path.GetFileName(path: first.Source.FullPath);
        _lblNew.Text = first == null
            ? string.Empty
            : T(key: "lbl_BatchRename_NewFilename") + "  " + first.NewFileName + (first.Problem == null
                ? string.Empty
                : "   (" + first.Problem + ")");

        int problems = _plan.Count(predicate: entry => entry.Problem != null);
        int changes = _plan.Count(predicate: entry => !entry.Unchanged);
        _btnRename.Enabled = problems == 0 && changes > 0;
        ShowCount();
    }

    /// <summary>The line under the preview: how many files, or how many have a problem.</summary>
    private void ShowCount()
    {
        int problems = _plan.Count(predicate: entry => entry.Problem != null);
        _lblCount.Text = problems > 0
            ? T(key: "mbx_BatchRename_SomeBlocked", problems, _plan.Count)
            : _plan.Count == 1
                ? T(key: "mbx_BatchRename_OneFile")
                : T(key: "mbx_BatchRename_ManyFiles", _plan.Count);
    }

    private void ShowAllNames()
    {
        Refresh_();
        using Form form = new()
        {
            Text = T(key: "frm_BatchRename_Preview"),
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(width: 900, height: 520),
            ShowInTaskbar = false,
            MinimizeBox = false
        };
        ListView list = new()
        {
            Dock = DockStyle.Fill,
            View = System.Windows.Forms.View.Details,
            FullRowSelect = true,
            HideSelection = false
        };
        _ = list.Columns.Add(text: T(key: "lbl_BatchRename_CurrentFilename"), width: 300);
        _ = list.Columns.Add(text: T(key: "lbl_BatchRename_NewFilename"), width: 300);
        _ = list.Columns.Add(text: T(key: "lbl_BatchRename_Note"), width: 280);
        foreach (BatchRenameEntry entry in _plan)
        {
            _ = list.Items.Add(value: new ListViewItem(items:
            [
                Path.GetFileName(path: entry.Source.FullPath),
                entry.NewFileName,
                entry.Problem ?? (entry.Unchanged
                    ? T(key: "lbl_BatchRename_Unchanged")
                    : string.Empty)
            ]));
        }

        form.Controls.Add(value: list);
        form.Shown += (_,
                       _) => ThemeHelper.ApplyTo(control: form);
        _ = form.ShowDialog(owner: this);
    }

    private void OnRenameClicked()
    {
        _refreshTimer.Stop();
        Refresh_();
        if (!_btnRename.Enabled)
        {
            return;
        }

        // what only the disk can tell (a file gone or in use, a folder that can't be written to, too little room): stop
        // before anything moves. Rename stays clickable so it can be tried again once the cause is dealt with.
        BatchRenameDestination destination = Collect().Destination;
        List<string> batchProblems = BatchRenamePreflight.Check(entries: _plan, destination: destination);
        List<BatchRenameEntry> blocked = _plan.Where(predicate: entry => entry.Problem != null).ToList();
        if (blocked.Count > 0 ||
            batchProblems.Count > 0)
        {
            ShowCount();
            List<string> lines = blocked.Take(count: 8)
                                        .Select(selector: entry => $"{Path.GetFileName(path: entry.Source.FullPath)}: {entry.Problem}")
                                        .ToList();
            if (blocked.Count > lines.Count)
            {
                lines.Add(item: T(key: "mbx_BatchRename_MoreProblems", blocked.Count - lines.Count));
            }

            lines.AddRange(collection: batchProblems);
            ThemeHelper.ShowMessageBox(
                message: T(key: "mbx_BatchRename_Preflight", string.Join(separator: Environment.NewLine, values: lines)),
                caption: T(key: "frm_BatchRename"), buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
            return;
        }

        Result = Collect();
        DialogResult = DialogResult.OK;
    }

    #endregion

    /// <summary>One line of the "New Filenames" list: the kind of piece, and the controls that kind needs.</summary>
    private sealed class ComponentRow : Panel
    {
        private readonly IReadOnlyList<(string key, string caption)> _fields;
        private readonly ComboBox _cbxKind = new();
        private readonly Button _btnMinus = new();
        private readonly Button _btnPlus = new();
        private readonly List<Control> _slots = [];
        private bool _building;

        /// <summary>What the row currently says.</summary>
        internal BatchRenameComponent Component { get; private set; }

        /// <summary>A row can only be removed while there is another.</summary>
        internal bool CanRemove
        {
            set => _btnMinus.Enabled = value;
        }

        internal event EventHandler Changed;
        internal event EventHandler PlusClicked;
        internal event EventHandler MinusClicked;

        internal ComponentRow(BatchRenameComponent component,
                              IReadOnlyList<(string key, string caption)> fields)
        {
            Component = component;
            _fields = fields;
            Width = RowWidth;

            _cbxKind.DropDownStyle = ComboBoxStyle.DropDownList;
            _cbxKind.Location = new Point(x: 0, y: 2);
            _cbxKind.Width = 220;
            _cbxKind.Items.AddRange(items: Captions(prefix: "lbl_BatchRename_Kind", values: KindOrder).Cast<object>().ToArray());
            _cbxKind.SelectedIndex = Math.Max(val1: 0, val2: Array.IndexOf(array: KindOrder, value: component.Kind));
            _cbxKind.SelectedIndexChanged += (_,
                                              _) =>
            {
                if (_building)
                {
                    return;
                }

                Component = NewComponent(kind: KindOrder[_cbxKind.SelectedIndex]);
                Rebuild();
                Changed?.Invoke(sender: this, e: EventArgs.Empty);
            };
            Controls.Add(value: _cbxKind);

            _btnMinus.Text = "-";
            _btnMinus.Size = new Size(width: 30, height: 27);
            _btnMinus.Location = new Point(x: RowWidth - 80, y: 1);
            _btnMinus.Click += (_,
                                _) => MinusClicked?.Invoke(sender: this, e: EventArgs.Empty);
            Controls.Add(value: _btnMinus);

            _btnPlus.Text = "+";
            _btnPlus.Size = new Size(width: 30, height: 27);
            _btnPlus.Location = new Point(x: RowWidth - 44, y: 1);
            _btnPlus.Click += (_,
                               _) => PlusClicked?.Invoke(sender: this, e: EventArgs.Empty);
            Controls.Add(value: _btnPlus);

            Rebuild();
        }

        private BatchRenameComponent NewComponent(BatchRenameKind kind)
        {
            return kind switch
            {
                BatchRenameKind.DateTime => new BatchRenameComponent { Kind = kind, Text = "YYYYMMDD", DateSource = BatchRenameDateSource.DateTaken },
                BatchRenameKind.Metadata => new BatchRenameComponent { Kind = kind, Text = _fields.Count > 0 ? _fields[index: 0].key : string.Empty },
                BatchRenameKind.CurrentFilename or BatchRenameKind.PreservedFilename => new BatchRenameComponent { Kind = kind, Part = BatchRenameFilePart.Name },
                _ => new BatchRenameComponent { Kind = kind }
            };
        }

        /// <summary>Replaces the controls after the kind box with the ones the current kind needs.</summary>
        private void Rebuild()
        {
            _building = true;
            foreach (Control slot in _slots)
            {
                Controls.Remove(value: slot);
                slot.Dispose();
            }

            _slots.Clear();
            Height = RowHeight;
            BatchRenameComponent c = Component;
            const int x1 = 230;
            const int x2 = 450;

            switch (c.Kind)
            {
                case BatchRenameKind.Text:
                    AddText(x: x1, width: 220, value: c.Text, set: v => c.Text = v);
                    break;

                case BatchRenameKind.NewExtension:
                case BatchRenameKind.FolderName:
                    AddCaseBox(x: x1);
                    break;

                case BatchRenameKind.CurrentFilename:
                case BatchRenameKind.PreservedFilename:
                    AddCombo(x: x1, width: 210, captions: Captions(prefix: "lbl_BatchRename_Part", values: PartOrder),
                        selected: Array.IndexOf(array: PartOrder, value: c.Part),
                        set: i => c.Part = PartOrder[i]);
                    AddCaseBox(x: x2);
                    break;

                case BatchRenameKind.SequenceNumber:
                    NumericUpDown start = new()
                    {
                        Location = new Point(x: x1, y: 2),
                        Width = 210,
                        Minimum = 0,
                        Maximum = 999999,
                        Value = Math.Max(val1: 0, val2: Math.Min(val1: 999999, val2: c.Start))
                    };
                    start.ValueChanged += (_,
                                           _) =>
                    {
                        c.Start = (int)start.Value;
                        Changed?.Invoke(sender: this, e: EventArgs.Empty);
                    };
                    AddSlot(control: start);
                    AddCombo(x: x2, width: 210,
                        captions: Enumerable.Range(start: 1, count: MaxDigitsOffered).Select(selector: n => T(key: $"lbl_BatchRename_Digits_{n}")).ToArray(),
                        selected: Math.Max(val1: 0, val2: Math.Min(val1: MaxDigitsOffered - 1, val2: c.Digits - 1)),
                        set: i => c.Digits = i + 1);
                    break;

                case BatchRenameKind.SequenceLetter:
                    AddCombo(x: x1, width: 210,
                        captions: [T(key: "lbl_BatchRename_Case_Lower"), T(key: "lbl_BatchRename_Case_Upper")],
                        selected: c.Case == BatchRenameCase.Upper
                            ? 1
                            : 0,
                        set: i => c.Case = i == 1
                            ? BatchRenameCase.Upper
                            : BatchRenameCase.Lower);
                    break;

                case BatchRenameKind.DateTime:
                    AddCombo(x: x1, width: 210, captions: Captions(prefix: "lbl_BatchRename_DateSource", values: DateSourceOrder),
                        selected: Math.Max(val1: 0, val2: Array.IndexOf(array: DateSourceOrder, value: c.DateSource)),
                        set: i => c.DateSource = DateSourceOrder[i]);
                    ComboBox format = new()
                    {
                        DropDownStyle = ComboBoxStyle.DropDown,
                        Location = new Point(x: x2, y: 2),
                        Width = 210,
                        Text = c.Text
                    };
                    format.Items.AddRange(items: DateFormats.Cast<object>().ToArray());
                    format.TextChanged += (_,
                                           _) =>
                    {
                        c.Text = format.Text;
                        Changed?.Invoke(sender: this, e: EventArgs.Empty);
                    };
                    AddSlot(control: format);
                    break;

                case BatchRenameKind.Metadata:
                    AddCombo(x: x1, width: 210, captions: _fields.Select(selector: f => f.caption).ToArray(),
                        selected: Math.Max(val1: 0, val2: _fields.ToList().FindIndex(match: f => f.key == c.Text)),
                        set: i => c.Text = _fields[index: i].key);
                    if (_fields.Count > 0 &&
                        !_fields.Any(predicate: f => f.key == c.Text))
                    {
                        c.Text = _fields[index: 0].key;
                    }

                    break;

                case BatchRenameKind.StringSubstitution:
                    AddCombo(x: x1, width: 210, captions: Captions(prefix: "lbl_BatchRename_SubstituteSource", values: SubstituteSourceOrder),
                        selected: Array.IndexOf(array: SubstituteSourceOrder, value: c.SubstituteSource),
                        set: i => c.SubstituteSource = SubstituteSourceOrder[i]);
                    AddLabel(text: T(key: "lbl_BatchRename_Find"), x: 2, y: 42);
                    AddText(x: 90, width: 240, value: c.Find, set: v => c.Find = v, y: 38);
                    AddLabel(text: T(key: "lbl_BatchRename_ReplaceWith"), x: 345, y: 42);
                    AddText(x: 470, width: 250, value: c.Replace, set: v => c.Replace = v, y: 38);
                    AddCheck(text: T(key: "ckb_BatchRename_IgnoreCase"), x: 2, y: 74, value: c.IgnoreCase, set: v => c.IgnoreCase = v);
                    AddCheck(text: T(key: "ckb_BatchRename_ReplaceAll"), x: 200, y: 74, value: c.ReplaceAll, set: v => c.ReplaceAll = v);
                    AddCheck(text: T(key: "ckb_BatchRename_UseRegex"), x: 380, y: 74, value: c.UseRegex, set: v => c.UseRegex = v);
                    Height = SubstitutionRowHeight;
                    break;
            }

            _building = false;
            ThemeHelper.ApplyTo(control: this);
        }

        private void AddSlot(Control control)
        {
            _slots.Add(item: control);
            Controls.Add(value: control);
        }

        private void AddLabel(string text,
                              int x,
                              int y)
        {
            AddSlot(control: new Label { Text = text, AutoSize = true, Location = new Point(x: x, y: y) });
        }

        private void AddText(int x,
                             int width,
                             string value,
                             Action<string> set,
                             int y = 2)
        {
            TextBox box = new() { Location = new Point(x: x, y: y), Width = width, Text = value ?? string.Empty };
            box.TextChanged += (_,
                                _) =>
            {
                set(obj: box.Text);
                Changed?.Invoke(sender: this, e: EventArgs.Empty);
            };
            AddSlot(control: box);
        }

        private void AddCheck(string text,
                              int x,
                              int y,
                              bool value,
                              Action<bool> set)
        {
            CheckBox box = new() { Text = text, AutoSize = true, Location = new Point(x: x, y: y), Checked = value };
            box.CheckedChanged += (_,
                                   _) =>
            {
                set(obj: box.Checked);
                Changed?.Invoke(sender: this, e: EventArgs.Empty);
            };
            AddSlot(control: box);
        }

        private void AddCombo(int x,
                              int width,
                              string[] captions,
                              int selected,
                              Action<int> set)
        {
            ComboBox box = new() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(x: x, y: 2), Width = width };
            box.Items.AddRange(items: captions.Cast<object>().ToArray());
            box.SelectedIndex = captions.Length == 0
                ? -1
                : Math.Max(val1: 0, val2: Math.Min(val1: captions.Length - 1, val2: selected));
            box.SelectedIndexChanged += (_,
                                         _) =>
            {
                if (_building ||
                    box.SelectedIndex < 0)
                {
                    return;
                }

                set(obj: box.SelectedIndex);
                Changed?.Invoke(sender: this, e: EventArgs.Empty);
            };
            AddSlot(control: box);
        }

        private void AddCaseBox(int x)
        {
            BatchRenameComponent c = Component;
            AddCombo(x: x, width: 210, captions: Captions(prefix: "lbl_BatchRename_Case", values: CaseOrder),
                selected: Math.Max(val1: 0, val2: Array.IndexOf(array: CaseOrder, value: c.Case)),
                set: i => c.Case = CaseOrder[i]);
        }
    }
}
