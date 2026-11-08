using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.Exif;
using GeoTagNinja.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static GeoTagNinja.Helpers.UI.HelperControlAndMessageBoxHandling;
using static GeoTagNinja.Model.SourcesAndAttributes;
using static GeoTagNinja.View.Forms.FrmMainApp;
using Themer = WinFormsDarkThemerNinja.Themer;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     "Load from Template", "Load from Image" and "Save as Template" on the Edit Data form: whole sets of values
///     moved between a file, a saved template and another image.
/// </summary>
/// <remarks>
///     Loading stages the chosen values as ordinary pending edits of the file shown (the same stage the form's own
///     controls write to), then redraws the form from them, so they are reviewed and saved like anything typed by hand.
/// </remarks>
public partial class FrmEditFileData
{
    private static readonly Logger TemplateLog = LogManager.GetCurrentClassLogger();

    private Button btn_LoadFromTemplate;
    private Button btn_LoadFromImage;
    private Button btn_SaveAsTemplate;

    /// <summary>Adds the three buttons under the tabs.</summary>
    private void BuildTemplateButtons()
    {
        btn_LoadFromTemplate = NewTemplateButton(name: "btn_LoadFromTemplate", english: "Load from Template...",
            x: 455, onClick: LoadFromTemplate);
        btn_LoadFromImage = NewTemplateButton(name: "btn_LoadFromImage", english: "Load from Image...",
            x: 639, onClick: LoadFromImage);
        btn_SaveAsTemplate = NewTemplateButton(name: "btn_SaveAsTemplate", english: "Save as Template...",
            x: 823, onClick: SaveAsTemplate);
    }

    private Button NewTemplateButton(string name,
                                     string english,
                                     int x,
                                     Action onClick)
    {
        string text = ReturnControlText(controlName: name, fakeControlType: FakeControlTypes.Button);
        Button button = new()
        {
            Name = name,
            // a button name is looked up in the language files by the generic loader too, which is why every one has an
            // entry there; the fallback only matters for a language that lacks it
            Text = string.IsNullOrWhiteSpace(value: text)
                ? english
                : text,
            Location = new Point(x: x, y: btn_SetCurrentValues.Bottom + 6),
            Size = new Size(width: 176, height: 23),
            UseVisualStyleBackColor = true
        };
        button.Click += (_,
                         _) => onClick();
        Controls.Add(value: button);
        return button;
    }

    private static string TemplateText(string key,
                                       string fallback)
    {
        string text = ReturnControlText(controlName: key, fakeControlType: FakeControlTypes.MessageBox);
        return string.IsNullOrWhiteSpace(value: text)
            ? fallback
            : text;
    }

    /// <summary>Saves chosen values of the file shown as a named template.</summary>
    private void SaveAsTemplate()
    {
        DirectoryElement element = CurrentElement();
        if (element == null)
        {
            return;
        }

        Dictionary<ElementAttribute, string> values = EditTemplateGroups.Capture(element: element);

        using FrmEditTemplateDialog dialog = new(mode: FrmEditTemplateDialog.NameMode.Save,
            title: ReturnControlText(controlName: "btn_SaveAsTemplate", fakeControlType: FakeControlTypes.Button)
               .TrimEnd('.'),
            values: values);
        if (dialog.ShowDialog(owner: this) != DialogResult.OK)
        {
            return;
        }

        Dictionary<ElementAttribute, string> chosen = [];
        foreach (TemplateGroup group in dialog.TickedGroups)
        {
            foreach (ElementAttribute attribute in group.Attributes.Where(predicate: values.ContainsKey))
            {
                chosen[key: attribute] = values[key: attribute];
            }
        }

        if (!EditTemplates.Write(templateName: dialog.TemplateName,
                values: EditTemplateGroups.ToNamed(values: chosen)))
        {
            _ = Themer.ShowMessageBoxWithResult(
                message: TemplateText(key: "mbx_FrmEditFileData_ErrorTemplateNotSaved",
                    fallback: "The template could not be saved."),
                icon: MessageBoxIcon.Error,
                buttons: MessageBoxButtons.OK);
        }
    }

    /// <summary>Puts chosen values of a saved template onto the file shown.</summary>
    private void LoadFromTemplate()
    {
        if (CurrentElement() == null)
        {
            return;
        }

        using FrmEditTemplateDialog dialog = new(mode: FrmEditTemplateDialog.NameMode.Load,
            title: ReturnControlText(controlName: "btn_LoadFromTemplate", fakeControlType: FakeControlTypes.Button)
               .TrimEnd('.'),
            values: new Dictionary<ElementAttribute, string>());
        if (dialog.ShowDialog(owner: this) == DialogResult.OK)
        {
            ApplyValues(values: dialog.Values, groups: dialog.TickedGroups);
        }
    }

    /// <summary>Reads another image and puts chosen values of it onto the file shown.</summary>
    private void LoadFromImage()
    {
        if (CurrentElement() == null)
        {
            return;
        }

        using OpenFileDialog open = new()
        {
            Title = ReturnControlText(controlName: "btn_LoadFromImage", fakeControlType: FakeControlTypes.Button)
               .TrimEnd('.'),
            CheckFileExists = true,
            Multiselect = false
        };
        if (open.ShowDialog(owner: this) != DialogResult.OK)
        {
            return;
        }

        Dictionary<ElementAttribute, string> values;
        try
        {
            values = ReadValuesFromImage(path: open.FileName);
        }
        catch (Exception ex)
        {
            TemplateLog.Error(exception: ex, message: $"Could not read '{open.FileName}'");
            values = [];
        }

        if (values.Count == 0)
        {
            _ = Themer.ShowMessageBoxWithResult(
                message: TemplateText(key: "mbx_FrmEditFileData_InfoImageHasNothingToLoad",
                    fallback: "That file has nothing this form can load."),
                icon: MessageBoxIcon.Information,
                buttons: MessageBoxButtons.OK);
            return;
        }

        using FrmEditTemplateDialog dialog = new(mode: FrmEditTemplateDialog.NameMode.None,
            title: Path.GetFileName(path: open.FileName),
            values: values);
        if (dialog.ShowDialog(owner: this) == DialogResult.OK)
        {
            ApplyValues(values: values, groups: dialog.TickedGroups);
        }
    }

    /// <summary>
    ///     Reads a file through the app's own ExifTool process and the same parser the folder read uses, so another
    ///     image is understood exactly as it would be if it were in the list.
    /// </summary>
    private static Dictionary<ElementAttribute, string> ReadValuesFromImage(string path)
    {
        List<KeyValuePair<string, string>> properties = [];
        ExifTool exifTool = DirectoryElements.ExifTool;
        if (exifTool != null)
        {
            exifTool.GetProperties(filename: path, propertiesRead: properties);
        }
        else
        {
            using ExifTool temporary = new();
            temporary.GetProperties(filename: path, propertiesRead: properties);
        }

        Dictionary<string, string> tags = properties.GroupBy(keySelector: pair => pair.Key)
                                                    .ToDictionary(keySelector: group => group.Key,
                                                        elementSelector: group => group.First()
                                                                                       .Value);
        return EditTemplateGroups.Capture(parsed: ExifTagSetParser.ParseTagSet(dictTagsIn: tags));
    }

    /// <summary>Stages the ticked groups' values on the file shown and redraws the form from them.</summary>
    private void ApplyValues(IReadOnlyDictionary<ElementAttribute, string> values,
                             IEnumerable<TemplateGroup> groups)
    {
        DirectoryElement element = CurrentElement();
        if (element == null)
        {
            return;
        }

        foreach (ElementAttribute attribute in groups.SelectMany(selector: group => group.Attributes)
                                                     .Where(predicate: values.ContainsKey))
        {
            element.SetAttributeValueAnyType(attribute: attribute,
                value: values[key: attribute],
                version: DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                isMarkedForDeletion: false,
                context: ValueFormatContext.RoundTrip);
        }

        SyncKeywordsWithLocation(element: element);
        ShowDEDataInRelevantControls(dirElemFileToModify: element);
    }
}
