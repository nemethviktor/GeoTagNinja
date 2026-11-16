using GeoTagNinja.Helpers.Help;
using System;
using System.Windows.Forms;

namespace GeoTagNinja.View.Forms;

/// <summary>The Help menu's <c>Help</c> entry and its <c>F1</c> shortcut.</summary>
public partial class FrmMainApp
{
    /// <summary>
    ///     Adds <c>Help</c> (F1) at the top of the Help menu. Created in code so the designer files stay untouched; called
    ///     once while the form is set up. The other forms get F1 from <see cref="FrmHelp.Attach" />.
    /// </summary>
    private void AddHelpMenu()
    {
        ToolStripMenuItem helpItem = NewMenuItem(controlName: "tmi_Help_Help");
        helpItem.ShortcutKeys = Keys.F1;
        helpItem.Click += (_,
                           _) => FrmHelp.Show(owner: this, topicId: HelpContent.DefaultTopicId);
        tmi_Help.DropDownItems.Insert(index: 0, value: helpItem);
    }
}
