using GeoTagNinja.Helpers.Localisation;
using NLog;

namespace GeoTagNinja.Helpers.UI;

internal static class HelperControlAndMessageBoxHandling
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public enum MessageBoxCaption
    {
        None,
        Information,
        Error,
        Exclamation,
        Question,
        Warning,
        Stop,
        Hand,
        Asterisk
    }

    /// <summary>
    ///     This is probably the worst idea ever but I'm creating fake control types for allocating them to the text-reader
    ///     e.g. ColumnHeader isn't a real control type but I use them.
    /// </summary>
    public enum FakeControlTypes
    {
        Button,
        CheckBox,
        ColumnHeader,
        Form,
        GroupBox,
        MessageBox,
        MessageBoxCaption,
        Generic,
        Label,
        OpenFileDialog,
        PictureBox,
        RadioButton,
        TabPage,
        ToolStripButton,
        ToolStripMenuItem,
        ToolTip,
        Undefined
    }

    /// <summary>
    ///     Gets the localised Text for the Control.
    /// </summary>
    /// <param name="controlName">The name of the Control as it appears in the Designer</param>
    /// <param name="fakeControlType">One of the <see cref="FakeControlTypes" /> (e.g.FakeControlTypes.Button =&gt; "Button")</param>
    /// <returns></returns>
    internal static string ReturnControlText(string controlName, FakeControlTypes fakeControlType)
    {
        Log.Trace(message: $"Starting - control: {controlName}");

        if (fakeControlType == FakeControlTypes.Form)
        {
            controlName = $"frm_{controlName}";
        }

        return ResourceManager.GetResourceValue(controlName: controlName, location: "Strings");
    }
}
