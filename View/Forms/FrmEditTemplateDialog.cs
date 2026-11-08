using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.Exif;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static GeoTagNinja.Helpers.UI.HelperControlAndMessageBoxHandling;
using static GeoTagNinja.Model.SourcesAndAttributes;
using Themer = WinFormsDarkThemerNinja.Themer;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     The small dialog behind the Edit Data form's "Save as Template", "Load from Template" and "Load from Image":
///     optionally a template name, and a list of what the template or image holds with a tick against each item to
///     use.
/// </summary>
/// <remarks>
///     Built entirely in code (there is nothing here worth a designer file). It only collects choices; saving and
///     applying are done by the caller.
/// </remarks>
internal sealed class FrmEditTemplateDialog : Form
{
    /// <summary>What the dialog asks for besides the ticks.</summary>
    internal enum NameMode
    {
        /// <summary>No name: the values come from an image.</summary>
        None,

        /// <summary>A name to save under, typed or picked from the existing templates.</summary>
        Save,

        /// <summary>An existing template to load, picked from a list.</summary>
        Load
    }

    private sealed class Item
    {
        internal TemplateGroup Group;
        internal string Text;

        public override string ToString()
        {
            return Text;
        }
    }

    private readonly NameMode _mode;
    private readonly ComboBox _cbxName = new();
    private readonly CheckedListBox _clbItems = new();
    private readonly Button _btnOk = new();
    private readonly Button _btnDelete = new();
    private IReadOnlyDictionary<ElementAttribute, string> _values;

    /// <summary>The name chosen or typed (<see cref="NameMode.None" />: empty).</summary>
    internal string TemplateName => _cbxName.Text.Trim();

    /// <summary>The values the dialog is showing: those of the picked template, or the ones it was given.</summary>
    internal IReadOnlyDictionary<ElementAttribute, string> Values => _values;

    /// <summary>The items that are ticked.</summary>
    internal List<TemplateGroup> TickedGroups => _clbItems.CheckedItems.Cast<Item>()
                                                          .Select(selector: item => item.Group)
                                                          .ToList();

    private static string Caption(string key,
                                  string fallback)
    {
        string text = ReturnControlText(controlName: key, fakeControlType: FakeControlTypes.Label);
        return string.IsNullOrWhiteSpace(value: text)
            ? fallback
            : text;
    }

    /// <param name="mode">What to ask for besides the ticks.</param>
    /// <param name="title">The window title.</param>
    /// <param name="values">
    ///     The values on offer. In <see cref="NameMode.Load" /> this is replaced by the picked template's.
    /// </param>
    internal FrmEditTemplateDialog(NameMode mode,
                                   string title,
                                   IReadOnlyDictionary<ElementAttribute, string> values)
    {
        _mode = mode;
        _values = values;

        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(width: 460, height: 470);

        int top = 12;
        if (mode != NameMode.None)
        {
            Label label = new()
            {
                Text = Caption(key: "lbl_TemplateName", fallback: "Template:"),
                Location = new Point(x: 12, y: top),
                AutoSize = true
            };
            Controls.Add(value: label);

            _cbxName.Location = new Point(x: 12, y: top + 18);
            _cbxName.Width = mode == NameMode.Load
                ? 350
                : 436;
            _cbxName.DropDownStyle = mode == NameMode.Load
                ? ComboBoxStyle.DropDownList
                : ComboBoxStyle.DropDown;
            foreach (string name in EditTemplates.Names())
            {
                _ = _cbxName.Items.Add(item: name);
            }

            _cbxName.SelectedIndexChanged += (_,
                                              _) => OnTemplateChosen();
            Controls.Add(value: _cbxName);

            if (mode == NameMode.Load)
            {
                _btnDelete.Text = Caption(key: "btn_TemplateDelete", fallback: "Delete");
                _btnDelete.Location = new Point(x: 368, y: top + 17);
                _btnDelete.Size = new Size(width: 80, height: 25);
                _btnDelete.Click += (_,
                                     _) => DeleteChosenTemplate();
                Controls.Add(value: _btnDelete);
            }

            top += 52;
        }

        Label include = new()
        {
            Text = Caption(key: "lbl_TemplateInclude", fallback: "Use these:"),
            Location = new Point(x: 12, y: top),
            AutoSize = true
        };
        Controls.Add(value: include);

        _clbItems.Location = new Point(x: 12, y: top + 18);
        _clbItems.Size = new Size(width: 436, height: 470 - top - 18 - 56);
        _clbItems.CheckOnClick = true;
        Controls.Add(value: _clbItems);

        _btnOk.Text = ReturnControlText(controlName: "btn_Generic_OK", fakeControlType: FakeControlTypes.Button);
        _btnOk.DialogResult = DialogResult.OK;
        _btnOk.Location = new Point(x: 270, y: 470 - 40);
        _btnOk.Size = new Size(width: 84, height: 25);
        _btnOk.Click += (_,
                         e) => OnOkClicked(e: e);
        Controls.Add(value: _btnOk);

        Button cancel = new()
        {
            Text = ReturnControlText(controlName: "btn_Generic_Cancel", fakeControlType: FakeControlTypes.Button),
            DialogResult = DialogResult.Cancel,
            Location = new Point(x: 364, y: 470 - 40),
            Size = new Size(width: 84, height: 25)
        };
        Controls.Add(value: cancel);

        AcceptButton = _btnOk;
        CancelButton = cancel;

        if (mode == NameMode.Load)
        {
            if (_cbxName.Items.Count > 0)
            {
                _cbxName.SelectedIndex = 0;
            }
            else
            {
                _btnOk.Enabled = false;
                _btnDelete.Enabled = false;
            }
        }
        else
        {
            FillItems();
        }

        Shown += (_,
                  _) => ThemeHelper.ApplyTo(control: this);
    }

    private void FillItems()
    {
        _clbItems.Items.Clear();
        foreach (TemplateGroup group in EditTemplateGroups.GroupsWithValues(values: _values))
        {
            string caption = Caption(key: group.Key, fallback: group.English);
            _ = _clbItems.Items.Add(
                item: new Item
                {
                    Group = group,
                    Text = $"{caption}: {EditTemplateGroups.Preview(group: group, values: _values)}"
                },
                isChecked: group.CheckedByDefault);
        }

        _btnOk.Enabled = _clbItems.Items.Count > 0 || _mode == NameMode.Save;
    }

    private void OnTemplateChosen()
    {
        if (_mode != NameMode.Load)
        {
            return;
        }

        _values = EditTemplateGroups.FromNamed(named: EditTemplates.Read(templateName: _cbxName.Text));
        FillItems();
    }

    private void DeleteChosenTemplate()
    {
        string name = _cbxName.Text;
        if (string.IsNullOrEmpty(value: name))
        {
            return;
        }

        string question = ReturnControlText(controlName: "mbx_FrmEditTemplateDialog_QuestionDeleteTemplate",
            fakeControlType: FakeControlTypes.MessageBox);
        if (string.IsNullOrWhiteSpace(value: question))
        {
            question = "Delete the template '{0}'?";
        }

        if (Themer.ShowMessageBoxWithResult(message: string.Format(format: question, arg0: name),
                icon: MessageBoxIcon.Question,
                buttons: MessageBoxButtons.YesNo) != DialogResult.Yes)
        {
            return;
        }

        EditTemplates.Delete(templateName: name);
        _cbxName.Items.Remove(value: name);
        if (_cbxName.Items.Count > 0)
        {
            _cbxName.SelectedIndex = 0;
        }
        else
        {
            _cbxName.Text = string.Empty;
            _values = new Dictionary<ElementAttribute, string>();
            FillItems();
            _btnOk.Enabled = false;
            _btnDelete.Enabled = false;
        }
    }

    private void OnOkClicked(EventArgs e)
    {
        if (_mode == NameMode.Save)
        {
            if (TemplateName.Length == 0 ||
                _clbItems.CheckedItems.Count == 0)
            {
                DialogResult = DialogResult.None;
                return;
            }

            if (_cbxName.Items.Cast<string>()
                         .Contains(value: TemplateName, comparer: StringComparer.OrdinalIgnoreCase))
            {
                string question = ReturnControlText(controlName: "mbx_FrmEditTemplateDialog_QuestionOverwriteTemplate",
                    fakeControlType: FakeControlTypes.MessageBox);
                if (string.IsNullOrWhiteSpace(value: question))
                {
                    question = "A template called '{0}' already exists. Replace it?";
                }

                if (Themer.ShowMessageBoxWithResult(message: string.Format(format: question, arg0: TemplateName),
                        icon: MessageBoxIcon.Question,
                        buttons: MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    DialogResult = DialogResult.None;
                }
            }
        }
        else if (_clbItems.CheckedItems.Count == 0)
        {
            DialogResult = DialogResult.None;
        }
    }
}
