using System.Windows.Forms;

namespace GeoTagNinja.Helpers.UI;

/// <summary>
///     Places forms on screen.
/// </summary>
/// <remarks>
///     The application is marked DPI-unaware (see <c>ApplicationHighDpiMode</c> in the project file), so the bounds
///     Windows reports are not always the bounds the form is actually drawn at. The halving below is a long-standing
///     empirical correction for that, not a considered scaling model; it is one of the things to revisit if the app is
///     ever made DPI-aware.
/// </remarks>
internal static class FormPositioning
{
    /// <summary>
    ///     Centres a form on the screen it currently sits on.
    /// </summary>
    /// <param name="frm">The form to centre.</param>
    internal static void CenterForm(Form frm)
    {
        int multiplier = GetScreenMultiplier(frm: frm);

        frm.SetBounds(x: ((Screen.GetBounds(ctl: frm).Width / 2) - (frm.Width / 2)) / multiplier,
            y: (Screen.GetBounds(ctl: frm).Height / 2) - (frm.Height / 2),
            width: frm.Width,
            height: frm.Height,
            specified: BoundsSpecified.Location);
    }

    /// <summary>
    ///     Compensates for the DPI-unaware coordinate space on wide displays. See the class remarks.
    /// </summary>
    private static int GetScreenMultiplier(Form frm)
    {
        return Screen.GetBounds(ctl: frm).Width > 2000
            ? 2
            : 1;
    }
}
