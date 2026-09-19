using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     Walks WinForms control and menu trees.
/// </summary>
/// <remarks>
///     Used mainly by the localisation pass, which needs to visit every control on a form to replace its text, and by
///     the settings dialog, which collects controls by type in order to read and write them in bulk.
/// </remarks>
internal static class ControlTraversal
{
    /// <summary>
    ///     Returns every descendant of the given control that is of the given type.
    /// </summary>
    /// <remarks>via https://stackoverflow.com/a/3426721/3968494</remarks>
    /// <param name="control">The root of the subtree to search.</param>
    /// <param name="type">The exact control type to match. Subclasses are not matched.</param>
    internal static IEnumerable<Control> GetAllControls(Control control,
                                                        Type type)
    {
        IEnumerable<Control> controls = control.Controls.Cast<Control>();

        return controls.SelectMany(selector: ctrl => GetAllControls(control: ctrl, type: type))
                       .Concat(second: controls)
                       .Where(predicate: c => c.GetType() == type);
    }

    /// <summary>
    ///     Returns every descendant of the given control, at any depth.
    /// </summary>
    /// <param name="control">The root of the subtree to search. Must not be null.</param>
    internal static IEnumerable<Control> GetAllControls(Control control)
    {
        IEnumerable<Control> controls = control.Controls.Cast<Control>();

        return controls.SelectMany(selector: GetAllControls)
                       .Concat(second: controls);
    }

    /// <summary>
    ///     Flattens a menu item into itself and all of its nested items and separators.
    /// </summary>
    /// <param name="item">The item to flatten. Items without a dropdown yield only themselves.</param>
    internal static IEnumerable<ToolStripItem> GetMenuItems(ToolStripItem item)
    {
        if (item is ToolStripMenuItem menuItem)
        {
            foreach (ToolStripItem tsi in menuItem.DropDownItems)
            {
                if (tsi is ToolStripMenuItem stripMenuItem)
                {
                    if (stripMenuItem.HasDropDownItems)
                    {
                        foreach (ToolStripItem subItem in GetMenuItems(item: stripMenuItem))
                        {
                            yield return subItem;
                        }
                    }

                    yield return stripMenuItem;
                }
                else if (tsi is ToolStripSeparator nestedSeparator)
                {
                    yield return nestedSeparator;
                }
            }
        }
        else if (item is ToolStripSeparator separator)
        {
            yield return separator;
        }
    }
}
