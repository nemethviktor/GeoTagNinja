using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace GeoTagNinja.Helpers;

internal partial class HelperNonStatic
{
    #region Generic

    /// <summary>
    ///     Generic summary: these return each subcontrol for a given parent control, 
    ///     for example all buttons in a tabPage etc.
    ///     via https://stackoverflow.com/a/3426721/3968494
    /// </summary>
    internal IEnumerable<Control> GetAllControls(Control control,
                                                 Type type)
    {
        IEnumerable<Control> controls = control.Controls.Cast<Control>();

        return controls.SelectMany(selector: ctrl => GetAllControls(control: ctrl, type: type))
            .Concat(second: controls)
            .Where(predicate: c => c.GetType() == type);
    }

    /// <summary>
    /// Retrieves all child controls contained within the specified parent control, including controls nested at any
    /// depth.
    /// </summary>
    /// <remarks>This method recursively traverses the control hierarchy to gather all descendant controls.
    /// Ensure that the provided control is part of a valid control tree before calling this method.</remarks>
    /// <param name="control">The parent control from which to retrieve all descendant controls. This parameter cannot be null.</param>
    /// <returns>An enumerable collection of all child controls, including those nested within other controls.</returns>
    internal IEnumerable<Control> GetAllControls(Control control)
    {
        IEnumerable<Control> controls = control.Controls.Cast<Control>();

        return controls.SelectMany(selector: GetAllControls)
            .Concat(second: controls);
    }

    /// <summary>
    /// Retrieves a collection of menu items from the specified ToolStripItem, including nested ToolStripMenuItems and
    /// separators.
    /// </summary>
    /// <remarks>This method recursively retrieves all ToolStripMenuItems and ToolStripSeparators from the
    /// provided item. If the item has no dropdown items, only the item itself is returned.</remarks>
    /// <param name="item">The ToolStripItem from which to retrieve menu items. This can be a ToolStripMenuItem or a ToolStripSeparator.</param>
    /// <returns>An IEnumerable of ToolStripItem representing the menu items, including any nested items if the specified item is
    /// a ToolStripMenuItem.</returns>
    internal IEnumerable<ToolStripItem> GetMenuItems(ToolStripItem item)
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
                else if (tsi is ToolStripSeparator separator)
                {
                    yield return separator;
                }
            }
        }
        else if (item is ToolStripSeparator separator)
        {
            yield return separator;
        }
    }

    /// <summary>
    /// Centers the Form on the screen
    /// </summary>
    /// <param name="frm"></param>
    public void CenterForm(Form frm)
    {
        int multiplier = getScreenMultiplier(frm: frm);

        frm.SetBounds(x: ((Screen.GetBounds(ctl: frm).Width / 2) - (frm.Width / 2)) / multiplier,
            y: (Screen.GetBounds(ctl: frm).Height / 2) - (frm.Height / 2),
            width: frm.Width,
            height: frm.Height,
            specified: BoundsSpecified.Location);
        ;
    }

    private static int getScreenMultiplier(Form frm)
    {
        return Screen.GetBounds(ctl: frm).Width > 2000 ? 2 : 1;
    }

    #endregion
}