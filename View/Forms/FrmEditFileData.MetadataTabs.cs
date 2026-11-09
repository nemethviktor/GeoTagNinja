using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static GeoTagNinja.Helpers.UI.HelperControlAndMessageBoxHandling;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     The Keywords/Categories, Source/Description and Contact tabs of the Edit Data form.
/// </summary>
/// <remarks>
///     These tabs are built in code rather than in the designer: there are about thirty near-identical fields, and a
///     hand-edited <c>InitializeComponent</c> of that size is the thing the designer is most likely to mangle. The
///     controls follow the form's naming rule, <c>&lt;prefix&gt;_&lt;AttributeName&gt;</c>, so the generic load and
///     change handlers pick up the text fields without any extra code; the list fields (keywords and
///     supplemental categories) are handled by <see cref="ListEditor" />. The cost is that these tabs do not show in
///     the Visual Studio designer.
/// </remarks>
public partial class FrmEditFileData
{
    /// <summary>
    ///     Put in a control's <see cref="Control.Tag" /> to keep the generic caption and value loader away from it
    ///     (entry boxes and "..." buttons are not attributes and have no caption of their own).
    /// </summary>
    internal const string NoAttributeTag = "NoAttribute";

    private const int FieldLabelHeight = 16;
    private const int FieldControlHeight = 23;
    private const int MoreButtonWidth = 26;

    /// <summary>The list-valued fields, by attribute.</summary>
    private readonly Dictionary<ElementAttribute, ListEditor> _listEditors = [];

    /// <summary>Every field control on the new tabs, by attribute (the "..." menu reads and writes through this).</summary>
    private readonly Dictionary<ElementAttribute, Control> _metadataFieldControls = [];

    private TabPage tpg_Keywords;
    private TabPage tpg_SourceDescription;
    private TabPage tpg_Contact;

    /// <summary>One keyword-style field: an entry box to add to, and the list of what is already there.</summary>
    private sealed class ListEditor
    {
        internal ElementAttribute Attribute;
        internal ComboBox Entry;
        internal ListBox List;
    }

    private static string FieldCaption(string controlName,
                                       string fallback)
    {
        string text = ReturnControlText(controlName: controlName, fakeControlType: FakeControlTypes.Label);
        return string.IsNullOrWhiteSpace(value: text)
            ? fallback
            : text;
    }

    /// <summary>Creates the three tabs and every field on them.</summary>
    private void BuildMetadataTabs()
    {
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
        {
            return;
        }

        tpg_Keywords = NewTabPage(name: "tpg_Keywords", english: "Categories/Keywords");
        tpg_SourceDescription = NewTabPage(name: "tpg_SourceDescription", english: "Source/Description");
        tpg_Contact = NewTabPage(name: "tpg_Contact", english: "Contact");
        WidenTabHeaders();

        // Categories / Keywords: categories on the left, keywords on the right.
        AddTextField(page: tpg_Keywords, attribute: ElementAttribute.Category, english: "Category",
            x: 8, y: 8, width: 240, height: 0);
        AddListField(page: tpg_Keywords, attribute: ElementAttribute.SupplementalCategories,
            english: "Supplemental Categories", x: 8, y: 54, width: 240, listHeight: 370);
        AddListField(page: tpg_Keywords, attribute: ElementAttribute.IPTCKeywords, english: "Keywords",
            x: 285, y: 8, width: 240, listHeight: 416);

        // Source / Description
        AddSectionLabel(page: tpg_SourceDescription, name: "lbl_SectionSource", english: "Source", y: 4);
        AddTextField(page: tpg_SourceDescription, attribute: ElementAttribute.Artist, english: "Artist",
            x: 8, y: 22, width: 500, height: 0);
        AddTextField(page: tpg_SourceDescription, attribute: ElementAttribute.CaptionWriter, english: "Caption Writer",
            x: 8, y: 66, width: 230, height: 0);
        AddTextField(page: tpg_SourceDescription, attribute: ElementAttribute.Source, english: "Source",
            x: 278, y: 66, width: 230, height: 0);
        AddTextField(page: tpg_SourceDescription, attribute: ElementAttribute.Credit, english: "Credit",
            x: 8, y: 110, width: 230, height: 0);
        AddTextField(page: tpg_SourceDescription, attribute: ElementAttribute.SourceURL, english: "URL",
            x: 278, y: 110, width: 230, height: 0);
        AddTextField(page: tpg_SourceDescription, attribute: ElementAttribute.Copyright, english: "Copyright",
            x: 8, y: 154, width: 500, height: 46);

        AddSectionLabel(page: tpg_SourceDescription, name: "lbl_SectionDescription", english: "Description", y: 228);
        AddTextField(page: tpg_SourceDescription, attribute: ElementAttribute.ObjectName, english: "Object Name",
            x: 8, y: 246, width: 500, height: 0);
        AddTextField(page: tpg_SourceDescription, attribute: ElementAttribute.Headline, english: "Headline",
            x: 8, y: 290, width: 500, height: 0);
        AddTextField(page: tpg_SourceDescription, attribute: ElementAttribute.Caption, english: "Caption",
            x: 8, y: 334, width: 500, height: 62);
        AddTextField(page: tpg_SourceDescription, attribute: ElementAttribute.SpecialInstructions,
            english: "Special Instructions", x: 8, y: 414, width: 500, height: 46);

        // Contact
        AddTextField(page: tpg_Contact, attribute: ElementAttribute.Byline, english: "Byline",
            x: 8, y: 8, width: 240, height: 0);
        AddTextField(page: tpg_Contact, attribute: ElementAttribute.BylineTitle, english: "Byline Title",
            x: 8, y: 54, width: 240, height: 0);
        AddTextField(page: tpg_Contact, attribute: ElementAttribute.CreatorAddress, english: "Address",
            x: 8, y: 100, width: 240, height: 72);
        AddTextField(page: tpg_Contact, attribute: ElementAttribute.CreatorCity, english: "City",
            x: 8, y: 192, width: 240, height: 0);
        AddTextField(page: tpg_Contact, attribute: ElementAttribute.CreatorPostalCode, english: "Postal Code",
            x: 8, y: 238, width: 240, height: 0);
        AddTextField(page: tpg_Contact, attribute: ElementAttribute.CreatorRegion, english: "State/Province",
            x: 8, y: 284, width: 240, height: 0);
        AddTextField(page: tpg_Contact, attribute: ElementAttribute.CreatorCountry, english: "Country",
            x: 8, y: 330, width: 240, height: 0);
        AddTextField(page: tpg_Contact, attribute: ElementAttribute.CreatorPhone, english: "Phone",
            x: 285, y: 8, width: 240, height: 0);
        AddTextField(page: tpg_Contact, attribute: ElementAttribute.CreatorEmail, english: "E-Mail",
            x: 285, y: 54, width: 240, height: 0);
        AddTextField(page: tpg_Contact, attribute: ElementAttribute.CreatorURL, english: "URL",
            x: 285, y: 100, width: 240, height: 0);

        // The Location tab's own free-text fields get the same menu.
        AddMoreButtonToExistingField(attribute: ElementAttribute.Country, controlName: "cbx_Country");
        AddMoreButtonToExistingField(attribute: ElementAttribute.State, controlName: "tbx_State");
        AddMoreButtonToExistingField(attribute: ElementAttribute.City, controlName: "tbx_City");
        AddMoreButtonToExistingField(attribute: ElementAttribute.Sublocation, controlName: "tbx_Sublocation");

        BuildLocationExtras();
        BuildTemplateButtons();
    }

    /// <summary>
    ///     In dark mode the tabs are owner-drawn at one fixed size, which clips a long caption such as
    ///     "Categories/Keywords" onto a second line. Make every header as wide as the longest caption needs.
    ///     (Light mode sizes each tab to its own text and ignores the width.)
    /// </summary>
    private void WidenTabHeaders()
    {
        int widest = tcr_EditData.TabPages.Cast<TabPage>()
                                 .Select(selector: page => TextRenderer.MeasureText(text: page.Text,
                                     font: tcr_EditData.Font)
                                                                       .Width)
                                 .Max();
        // icon, its gap and the padding either side
        tcr_EditData.ItemSize = new System.Drawing.Size(width: widest + 40, height: tcr_EditData.ItemSize.Height);
    }

    private TabPage NewTabPage(string name,
                               string english)
    {
        TabPage page = new()
        {
            Name = name,
            Text = FieldCaption(controlName: name, fallback: english),
            UseVisualStyleBackColor = true,
            AutoScroll = true
        };
        tcr_EditData.TabPages.Add(value: page);
        return page;
    }

    private static void AddSectionLabel(TabPage page,
                                        string name,
                                        string english,
                                        int y)
    {
        Label label = new()
        {
            Name = name,
            Text = FieldCaption(controlName: name, fallback: english),
            Location = new Point(x: 8, y: y),
            AutoSize = true,
            Font = new Font(prototype: page.Font, newStyle: FontStyle.Bold)
        };
        page.Controls.Add(value: label);
    }

    /// <summary>
    ///     A caption, an editable combo box (single line) or a text box (when <paramref name="height" /> is more than a
    ///     line), and a "..." button. The control is named for the attribute so the generic loader handles it.
    /// </summary>
    private void AddTextField(TabPage page,
                              ElementAttribute attribute,
                              string english,
                              int x,
                              int y,
                              int width,
                              int height)
    {
        string name = GetElementAttributesName(attributeToFind: attribute);

        Label label = NewFieldLabel(name: $"lbl_{name}", english: english, x: x, y: y);

        Control field;
        if (height > 0)
        {
            field = new TextBox
            {
                Name = $"tbx_{name}",
                Multiline = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(x: x, y: y + FieldLabelHeight),
                Size = new Size(width: width, height: height)
            };
        }
        else
        {
            field = new ComboBox
            {
                Name = $"cbx_{name}",
                DropDownStyle = ComboBoxStyle.DropDown,
                Location = new Point(x: x, y: y + FieldLabelHeight),
                Size = new Size(width: width, height: FieldControlHeight)
            };
        }

        field.TextChanged += tbx_cbx_dtp_nud_Any_TextChanged;
        _metadataFieldControls[key: attribute] = field;
        _ = _historyFields.Add(item: attribute);

        page.Controls.Add(value: label);
        page.Controls.Add(value: field);
        page.Controls.Add(value: NewMoreButton(attribute: attribute,
            x: x + width + 4,
            y: y + FieldLabelHeight));
    }

    /// <summary>A caption, an entry box that adds on Enter, and the list below it (Delete removes the selection).</summary>
    private void AddListField(TabPage page,
                              ElementAttribute attribute,
                              string english,
                              int x,
                              int y,
                              int width,
                              int listHeight)
    {
        string name = GetElementAttributesName(attributeToFind: attribute);

        Label label = NewFieldLabel(name: $"lbl_{name}", english: english, x: x, y: y);

        ComboBox entry = new()
        {
            Name = $"cbx_{name}Entry",
            Tag = NoAttributeTag,
            DropDownStyle = ComboBoxStyle.DropDown,
            Location = new Point(x: x, y: y + FieldLabelHeight),
            Size = new Size(width: width, height: FieldControlHeight)
        };

        ListBox list = new()
        {
            Name = $"lbx_{name}",
            Tag = NoAttributeTag,
            SelectionMode = SelectionMode.MultiExtended,
            IntegralHeight = false,
            Location = new Point(x: x, y: y + FieldLabelHeight + FieldControlHeight + 4),
            Size = new Size(width: width, height: listHeight - FieldLabelHeight - FieldControlHeight - 4)
        };

        ListEditor editor = new() { Attribute = attribute, Entry = entry, List = list };
        _listEditors[key: attribute] = editor;
        _metadataFieldControls[key: attribute] = list;
        _ = _historyFields.Add(item: attribute);

        // Enter in the entry box must add the item, not press OK.
        entry.Enter += any_nud_Enter;
        entry.Leave += any_nud_Leave;
        entry.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                AddEntryToList(editor: editor);
            }
        };

        list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete)
            {
                e.Handled = true;
                RemoveSelectedFromList(editor: editor);
            }
        };

        page.Controls.Add(value: label);
        page.Controls.Add(value: entry);
        page.Controls.Add(value: list);
        page.Controls.Add(value: NewMoreButton(attribute: attribute, x: x + width + 4, y: y + FieldLabelHeight));
    }

    private static Label NewFieldLabel(string name,
                                       string english,
                                       int x,
                                       int y)
    {
        return new Label
        {
            Name = name,
            Text = FieldCaption(controlName: name, fallback: english),
            Location = new Point(x: x, y: y),
            AutoSize = true
        };
    }

    private Button NewMoreButton(ElementAttribute attribute,
                                 int x,
                                 int y)
    {
        Button button = new()
        {
            Name = $"btn_More_{GetElementAttributesName(attributeToFind: attribute)}",
            Tag = NoAttributeTag,
            Text = "...",
            Location = new Point(x: x, y: y),
            Size = new Size(width: MoreButtonWidth, height: FieldControlHeight),
            UseVisualStyleBackColor = true
        };
        button.Click += (_,
                         _) => ShowFieldMenu(attribute: attribute, anchor: button);
        return button;
    }

    #region List fields

    /// <summary>The element whose data the form is showing, or null when none is selected.</summary>
    private DirectoryElement CurrentElement()
    {
        return lvw_FileListEditImages.SelectedItems.Count > 0
            ? lvw_FileListEditImages.SelectedItems[index: 0]
                                    .Tag as DirectoryElement
            : null;
    }

    private static TagList ListOf(ListBox list)
    {
        return TagList.From(items: list.Items.Cast<object>()
                                       .Select(selector: item => item.ToString()));
    }

    private void AddEntryToList(ListEditor editor)
    {
        TagList current = ListOf(list: editor.List);
        TagList updated = current.With(item: editor.Entry.Text);
        editor.Entry.Text = string.Empty;
        if (!updated.Equals(other: current))
        {
            SetListItems(editor: editor, items: updated);
            StageListEdit(editor: editor);
        }
    }

    private void RemoveSelectedFromList(ListEditor editor)
    {
        TagList updated = ListOf(list: editor.List);
        foreach (object selected in editor.List.SelectedItems.Cast<object>())
        {
            updated = updated.Without(item: selected.ToString());
        }

        SetListItems(editor: editor, items: updated);
        StageListEdit(editor: editor);
    }

    private static void SetListItems(ListEditor editor,
                                     TagList items)
    {
        editor.List.BeginUpdate();
        editor.List.Items.Clear();
        foreach (string item in items)
        {
            _ = editor.List.Items.Add(item: item);
        }

        editor.List.EndUpdate();
    }

    /// <summary>Records the list as it is now as a pending edit of the current file.</summary>
    private void StageListEdit(ListEditor editor)
    {
        if (_frmEditFileDataNowLoadingFileData)
        {
            return;
        }

        DirectoryElement element = CurrentElement();
        if (element == null)
        {
            return;
        }

        TagList list = ListOf(list: editor.List);
        element.SetAttributeValue(attribute: editor.Attribute,
            value: list,
            version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
            isMarkedForDeletion: false);
        element.SetAttributeValue(attribute: editor.Attribute,
            value: list,
            version: DirectoryElement.AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue,
            isMarkedForDeletion: false);
        editor.List.Font = new Font(prototype: editor.List.Font, newStyle: FontStyle.Bold);
    }

    /// <summary>Shows the file's lists. Called by the loader while <c>_frmEditFileDataNowLoadingFileData</c> is set.</summary>
    private void ShowListEditors(DirectoryElement element)
    {
        foreach (ListEditor editor in _listEditors.Values)
        {
            DirectoryElement.AttributeVersion version = GetDEAttributeMaxAttributeVersion(
                directoryElement: element,
                attribute: editor.Attribute);

            TagList items = element.HasSpecificAttributeWithVersion(attribute: editor.Attribute, version: version)
                ? TagList.Parse(text: element.GetAttributeValueAsString(attribute: editor.Attribute,
                    version: version,
                    notFoundValue: null,
                    context: ValueFormatContext.Display))
                : TagList.Empty;

            SetListItems(editor: editor, items: items);
            editor.Entry.Text = string.Empty;
            editor.List.Font = new Font(prototype: editor.List.Font,
                newStyle: version != DirectoryElement.AttributeVersion.Original && !items.IsEmpty
                    ? FontStyle.Bold
                    : FontStyle.Regular);

            if (!items.IsEmpty)
            {
                element.SetAttributeValue(attribute: editor.Attribute,
                    value: items,
                    version: DirectoryElement.AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue,
                    isMarkedForDeletion: false);
            }
        }
    }

    #endregion
}
