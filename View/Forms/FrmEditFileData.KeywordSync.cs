using GeoTagNinja.Helpers;
using GeoTagNinja.Helpers.Geo;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.View.Forms;

/// <summary>
///     Keeps the keyword list on the Edit Data form in step with the location fields while the form is open, so
///     the swap of place-name keywords is visible (and can be corrected) before anything is saved.
/// </summary>
/// <remarks>
///     It works from the location the keywords were last matched to rather than from the file's original, so
///     several edits in a row, and keywords the user has added or removed by hand in between, are all respected.
///     It runs when a location field is left, not on every keystroke, because a half-typed name must never be
///     mistaken for a keyword. Saving does it once more for every file as a last catch for values set by the
///     map, the web lookups and templates.
/// </remarks>
public partial class FrmEditFileData
{
    private static readonly string[] s_locationControls =
    [
        "cbx_Country", "cbx_CountryCode", "tbx_State", "tbx_City", "tbx_Sublocation", "nud_GPSLatitude",
        "nud_GPSLongitude"
    ];

    /// <summary>The location each file's keywords were last matched to.</summary>
    private readonly Dictionary<DirectoryElement, GeoKeywordLocation> _keywordsMatchedTo = [];

    /// <summary>Syncs when the user leaves one of the location fields.</summary>
    private void HookKeywordSync()
    {
        if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
        {
            return;
        }

        foreach (string name in s_locationControls)
        {
            foreach (Control control in Controls.Find(key: name, searchAllChildren: true))
            {
                control.Leave += (_,
                                  _) => SyncKeywordsWithLocation(element: CurrentElement());
            }
        }
    }

    /// <summary>Forgets what was matched, for when the pending edits are saved or thrown away.</summary>
    private void ForgetKeywordSync()
    {
        _keywordsMatchedTo.Clear();
    }

    /// <summary>
    ///     Swaps the place-name keywords of the file for ones that match its location as it stands now, and
    ///     redraws the keyword list.
    /// </summary>
    private void SyncKeywordsWithLocation(DirectoryElement element)
    {
        if (element == null ||
            !HelperVariables.UserSettingSyncGeoKeywords)
        {
            return;
        }

        GeoKeywordLocation now = GeoKeywordSync.Snapshot(element: element, version: null);
        if (!_keywordsMatchedTo.TryGetValue(key: element, value: out GeoKeywordLocation before))
        {
            before = GeoKeywordSync.Snapshot(element: element, version: DirectoryElement.AttributeVersion.Original);
        }

        if (before.Equals(other: now))
        {
            return;
        }

        _keywordsMatchedTo[key: element] = now;

        TagList current = GeoKeywordSync.CurrentKeywords(element: element);
        TagList updated = GeoKeywordSync.Apply(keywords: current, before: before, after: now);
        if (updated.Equals(other: current))
        {
            return;
        }

        foreach (DirectoryElement.AttributeVersion version in new[]
                 {
                     DirectoryElement.AttributeVersion.Stage1EditFormIntraTabTransferQueue,
                     DirectoryElement.AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue
                 })
        {
            element.SetAttributeValue(attribute: ElementAttribute.IPTCKeywords,
                value: updated,
                version: version,
                isMarkedForDeletion: false);
        }

        if (element != CurrentElement())
        {
            return;
        }

        bool wasLoading = _frmEditFileDataNowLoadingFileData;
        _frmEditFileDataNowLoadingFileData = true;
        try
        {
            ShowListEditors(element: element);
        }
        finally
        {
            _frmEditFileDataNowLoadingFileData = wasLoading;
        }
    }
}
