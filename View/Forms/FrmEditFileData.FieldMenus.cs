using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     The "..." menu next to the free-text fields of the Edit Data form: Insert from, Copy to, Copy Selection to
///     Categories/Keywords and the most recently used values of the field.
/// </summary>
public partial class FrmEditFileData
{
    /// <summary>
    ///     The fields the Insert from and Copy to submenus offer, in the order the old Edit Data dialog listed them.
    ///     The English text is the fallback for a language with no caption.
    /// </summary>
    private static readonly (ElementAttribute Attribute, string English)[] s_menuFields =
    [
        (ElementAttribute.State, "State/Province"),
        (ElementAttribute.City, "City"),
        (ElementAttribute.Sublocation, "Sublocation"),
        (ElementAttribute.Caption, "Caption"),
        (ElementAttribute.SpecialInstructions, "Special Instructions"),
        (ElementAttribute.ObjectName, "Object Name"),
        (ElementAttribute.Headline, "Headline"),
        (ElementAttribute.Credit, "Credit"),
        (ElementAttribute.Copyright, "Copyright"),
        (ElementAttribute.Artist, "Artist"),
        (ElementAttribute.CaptionWriter, "Caption Writer"),
        (ElementAttribute.Source, "Source"),
        (ElementAttribute.SourceURL, "URL"),
        (ElementAttribute.Category, "Category"),
        (ElementAttribute.SupplementalCategories, "Supplemental Categories"),
        (ElementAttribute.IPTCKeywords, "Keywords"),
        (ElementAttribute.Byline, "Contact Byline"),
        (ElementAttribute.BylineTitle, "Contact Byline Title"),
        (ElementAttribute.CreatorAddress, "Contact Address"),
        (ElementAttribute.CreatorCity, "Contact City"),
        (ElementAttribute.CreatorPostalCode, "Contact PostalCode"),
        (ElementAttribute.CreatorRegion, "Contact State/Province"),
        (ElementAttribute.CreatorCountry, "Contact Country"),
        (ElementAttribute.CreatorPhone, "Contact Phone"),
        (ElementAttribute.CreatorEmail, "Contact E-Mail"),
        (ElementAttribute.CreatorURL, "Contact URL")
    ];

    /// <summary>The fields whose drop-down is filled from, and whose values are remembered in, <see cref="FieldHistory" />.</summary>
    private readonly HashSet<ElementAttribute> _historyFields = [];

    /// <summary>
    ///     Gives a field of the Location tab the same "..." button as the new tabs. The field is narrowed to make room
    ///     (the designer layout has no gap beside it); this is done at run time so the designer file stays untouched.
    /// </summary>
    private void AddMoreButtonToExistingField(ElementAttribute attribute,
                                              string controlName)
    {
        Control field = Controls.Find(key: controlName, searchAllChildren: true)
                                .FirstOrDefault();
        if (field?.Parent == null)
        {
            return;
        }

        field.Width -= MoreButtonWidth + 2;
        Button button = NewMoreButton(attribute: attribute, x: field.Right + 2, y: field.Top - 1);
        field.Parent.Controls.Add(value: button);
        _metadataFieldControls[key: attribute] = field;
    }

    private string CaptionOfMenuField(ElementAttribute attribute,
                                      string english)
    {
        string name = GetElementAttributesName(attributeToFind: attribute);
        string caption = FieldCaption(controlName: $"lbl_{name}", fallback: english);
        return english.StartsWith(value: "Contact ", comparisonType: StringComparison.Ordinal) &&
               !caption.StartsWith(value: "Contact ", comparisonType: StringComparison.Ordinal)
            ? $"{FieldCaption(controlName: "lbl_ContactPrefix", fallback: "Contact")} {caption}"
            : caption;
    }

    /// <summary>The value a field shows now, as text. A list is its items joined by commas; blank is empty.</summary>
    private string FieldValueAsText(ElementAttribute attribute)
    {
        if (!_metadataFieldControls.TryGetValue(key: attribute, value: out Control control))
        {
            return string.Empty;
        }

        string text = control is ListBox list
            ? string.Join(separator: ", ", values: ListOf(list: list))
            : control.Text;

        return text == NullEquivalents.StringGeneric
            ? string.Empty
            : text.Trim();
    }

    /// <summary>
    ///     Puts text into a field as if the user had typed it, which stages it as a pending edit. A list field gains the
    ///     comma- or semicolon-separated items it did not have.
    /// </summary>
    private void SetFieldValue(ElementAttribute attribute,
                               string text)
    {
        if (!_metadataFieldControls.TryGetValue(key: attribute, value: out Control control))
        {
            return;
        }

        if (_listEditors.TryGetValue(key: attribute, value: out ListEditor editor))
        {
            TagList updated = ListOf(list: editor.List);
            foreach (string item in text.Split(separator: [',', ';', '\r', '\n'],
                         options: StringSplitOptions.RemoveEmptyEntries))
            {
                updated = updated.With(item: item);
            }

            SetListItems(editor: editor, items: updated);
            StageListEdit(editor: editor);
            return;
        }

        bool multiline = control is TextBox { Multiline: true };
        control.Text = multiline
            ? text
            : text.Replace(oldValue: "\r\n", newValue: " ")
                  .Replace(oldValue: "\n", newValue: " ");
    }

    /// <summary>The text currently selected in a field, if any.</summary>
    private string SelectedTextOf(ElementAttribute attribute)
    {
        return _metadataFieldControls.TryGetValue(key: attribute, value: out Control control)
            ? control switch
            {
                TextBoxBase textBox => textBox.SelectedText,
                ComboBox combo => combo.SelectedText,
                ListBox list => string.Join(separator: ", ",
                    values: list.SelectedItems.Cast<object>()
                                .Select(selector: item => item.ToString())),
                _ => string.Empty
            }
            : string.Empty;
    }

    /// <summary>Builds and shows the "..." menu of a field under its button.</summary>
    private void ShowFieldMenu(ElementAttribute attribute,
                               Control anchor)
    {
        string current = FieldValueAsText(attribute: attribute);
        ContextMenuStrip menu = new();

        ToolStripMenuItem insertFrom = new(text: FieldCaption(controlName: "tmi_FieldMenu_InsertFrom",
            fallback: "Insert from"));
        ToolStripMenuItem copyTo = new(text: FieldCaption(controlName: "tmi_FieldMenu_CopyTo", fallback: "Copy to"));

        foreach ((ElementAttribute other, string english) in s_menuFields)
        {
            if (other == attribute)
            {
                continue;
            }

            string caption = CaptionOfMenuField(attribute: other, english: english);
            string otherValue = FieldValueAsText(attribute: other);

            ToolStripMenuItem insertItem = new(text: caption)
            {
                // nothing to insert from a field that is empty or that this form does not have
                Enabled = otherValue.Length > 0
            };
            insertItem.Click += (_,
                                 _) => SetFieldValue(attribute: attribute, text: otherValue);
            _ = insertFrom.DropDownItems.Add(value: insertItem);

            ToolStripMenuItem copyItem = new(text: caption)
            {
                Enabled = current.Length > 0 &&
                          _metadataFieldControls.ContainsKey(key: other)
            };
            copyItem.Click += (_,
                               _) => SetFieldValue(attribute: other, text: current);
            _ = copyTo.DropDownItems.Add(value: copyItem);
        }

        _ = menu.Items.Add(value: insertFrom);
        _ = menu.Items.Add(value: new ToolStripSeparator());
        _ = menu.Items.Add(value: copyTo);

        string selection = SelectedTextOf(attribute: attribute);
        _ = menu.Items.Add(value: SelectionItem(target: ElementAttribute.SupplementalCategories,
            captionKey: "tmi_FieldMenu_CopySelectionToCategories",
            english: "Copy Selection to Categories",
            selection: selection));
        _ = menu.Items.Add(value: SelectionItem(target: ElementAttribute.IPTCKeywords,
            captionKey: "tmi_FieldMenu_CopySelectionToKeywords",
            english: "Copy Selection to Keywords",
            selection: selection));

        _ = menu.Items.Add(value: new ToolStripSeparator());

        ToolStripMenuItem recent = new(text: FieldCaption(controlName: "tmi_FieldMenu_MostRecentlyUsed",
            fallback: "Most Recently Used Items"));
        foreach (string value in FieldHistory.Read(
                     attributeName: GetElementAttributesName(attributeToFind: attribute)))
        {
            string captured = value;
            ToolStripMenuItem item = new(text: captured.Length > 60
                ? captured.Substring(startIndex: 0, length: 57) + "..."
                : captured);
            item.Click += (_,
                           _) => SetFieldValue(attribute: attribute, text: captured);
            _ = recent.DropDownItems.Add(value: item);
        }

        recent.Enabled = recent.DropDownItems.Count > 0;
        _ = menu.Items.Add(value: recent);

        menu.ShowImageMargin = false;
        ThemeHelper.StyleMenu(menu: menu);
        menu.Show(control: anchor, position: new Point(x: 0, y: anchor.Height));
    }

    private ToolStripMenuItem SelectionItem(ElementAttribute target,
                                            string captionKey,
                                            string english,
                                            string selection)
    {
        ToolStripMenuItem item = new(text: FieldCaption(controlName: captionKey, fallback: english))
        {
            Enabled = !string.IsNullOrWhiteSpace(value: selection) &&
                      _metadataFieldControls.ContainsKey(key: target)
        };
        item.Click += (_,
                       _) => SetFieldValue(attribute: target, text: selection);
        return item;
    }

    /// <summary>Fills the drop-downs of the free-text fields with what was typed there before.</summary>
    private void FillFieldHistory()
    {
        foreach (ElementAttribute attribute in _historyFields)
        {
            ComboBox combo = null;
            if (_listEditors.TryGetValue(key: attribute, value: out ListEditor editor))
            {
                combo = editor.Entry;
            }
            else if (_metadataFieldControls.TryGetValue(key: attribute, value: out Control field))
            {
                combo = field as ComboBox;
            }

            if (combo == null)
            {
                continue;
            }

            combo.Items.Clear();
            combo.Items.AddRange(items: FieldHistory.Read(attributeName: GetElementAttributesName(attributeToFind: attribute))
                                                    .Cast<object>()
                                                    .ToArray());
        }
    }

    /// <summary>
    ///     Remembers what was entered into the free-text fields of a file about to be written, so it can be offered again.
    ///     Call it before the pending edits are moved on.
    /// </summary>
    private void RememberFieldHistory(DirectoryElement element)
    {
        foreach (ElementAttribute attribute in _historyFields)
        {
            if (!element.HasSpecificAttributeWithVersion(attribute: attribute,
                    version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue) ||
                element.IsMarkedForDeletion(attribute: attribute,
                    version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue))
            {
                continue;
            }

            string text = element.GetAttributeValueAsString(attribute: attribute,
                version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                notFoundValue: string.Empty,
                context: ValueFormatContext.Display);

            // a list's items go in one by one; a multi-line text is kept whole
            IEnumerable<string> values = GetElementAttributesType(attributeToFind: attribute) == typeof(TagList)
                ? TagList.Parse(text: text)
                : [text];

            FieldHistory.Remember(attributeName: GetElementAttributesName(attributeToFind: attribute),
                values: values);
        }
    }
}
