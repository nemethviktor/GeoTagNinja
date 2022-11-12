using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace GeoTagNinja;

internal partial class HelperNonStatic
{
    #region Generic

    /// <summary>
    ///     Generic summary: these return each subcontrol for a given parent control, for example all buttons in a tabPage etc.
    /// </summary>

    // via https://stackoverflow.com/a/3426721/3968494
    internal IEnumerable<Control> GetAllControls(Control control,
                                                 Type type)
    {
        IEnumerable<Control> controls = control.Controls.Cast<Control>();

        return controls.SelectMany(selector: ctrl => GetAllControls(control: ctrl, type: type))
            .Concat(second: controls)
            .Where(predicate: c => c.GetType() == type);
    }

    internal IEnumerable<Control> GetAllControls(Control control)
    {
        IEnumerable<Control> controls = control.Controls.Cast<Control>();

        return controls.SelectMany(selector: ctrl => GetAllControls(control: ctrl))
            .Concat(second: controls);
    }

    internal IEnumerable<ToolStripItem> GetMenuItems(ToolStripItem item)
    {
        if (item is ToolStripMenuItem)
        {
            foreach (ToolStripItem tsi in (item as ToolStripMenuItem).DropDownItems)
            {
                if (tsi is ToolStripMenuItem)
                {
                    if ((tsi as ToolStripMenuItem).HasDropDownItems)
                    {
                        foreach (ToolStripItem subItem in GetMenuItems(item: tsi as ToolStripMenuItem))
                        {
                            yield return subItem;
                        }
                    }

                    yield return tsi as ToolStripMenuItem;
                }
                else if (tsi is ToolStripSeparator)
                {
                    yield return tsi as ToolStripSeparator;
                }
            }
        }
        else if (item is ToolStripSeparator)
        {
            yield return item as ToolStripSeparator;
        }
    }

    #endregion
}