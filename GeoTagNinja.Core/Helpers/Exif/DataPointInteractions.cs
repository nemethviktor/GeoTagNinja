using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeoTagNinja.Helpers.UI;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja.Helpers.Exif;

internal static class DataPointInteractions
{
    /// <summary>
    ///     When "keep keywords in step with the location" is off, tells the user that removing the location data leaves
    ///     the keywords alone - the place names and <c>geo:</c> keywords some tools put there stay in the file. Says
    ///     nothing when the setting is on (it removes them) or when none of the files has any keywords to leave behind.
    /// </summary>
    /// <param name="directoryElements">The files the removal is about to run on.</param>
    internal static void NotifyIfKeywordsWillRemain(IEnumerable<DirectoryElement> directoryElements)
    {
        if (HelperVariables.UserSettingSyncGeoKeywords)
        {
            return;
        }

        bool anyKeywords = directoryElements.Any(predicate: element =>
            element.Type == DirectoryElement.ElementType.File &&
            new[] { ElementAttribute.IPTCKeywords, ElementAttribute.XMLSubjects }.Any(predicate: attribute =>
                {
                    string text = element.GetAttributeValueAsString(attribute: attribute, notFoundValue: string.Empty)
                                         .Trim();
                    return text.Length > 0 &&
                           text != NullEquivalents.StringGeneric;
                }));

        if (anyKeywords)
        {
            _ = UiServices.ShowMessageBoxWithResult(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_Helper_InfoRemoveGeoDataKeywordsRemain",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                icon: UiMessageIcon.Information,
                buttons: UiMessageButtons.OK);
        }
    }

    /// <summary>
    ///     Queues up a command to remove existing geo-data. Depending on the sender this can be for one or many
    ///     files.
    /// </summary>
    /// <param name="senderName">At this point this can either be the main listview or the one from Edit (file) data</param>
    internal static async Task ExifRemoveLocationData(
        DirectoryElement dirElemFileToModify,
        DirectoryElement.AttributeVersion attributeVersion)
    {
        if (dirElemFileToModify.Type != DirectoryElement.ElementType.File)
        {
            return;
        }

        // GeoDataAttributes is a readonly and I don't want to modify it for the rest of the code.
        List<ElementAttribute> geoDataAttributes = Enum
                                                  .GetValues(
                                                       enumType: typeof(ElementAttribute))
                                                  .Cast<ElementAttribute>()
                                                  .Where(predicate: GetElementAttributesIsGeoData)
                                                  .ToList();

        geoDataAttributes.Add(item: ElementAttribute.RemoveAllGPS); //"gps*"

        if (ApplicationSettings.DataReadSQLiteSettings(
                dataTable: HelperVariables.DtHelperDataApplicationSettings,
                settingTabPage: "tpg_Application",
                settingId: "ckb_RemoveGeoDataRemovesTimeOffset") ==
            "true")
        {
            geoDataAttributes.Add(item: ElementAttribute.OffsetTime);
        }

        foreach (ElementAttribute toponomyDetail in geoDataAttributes)
        {
            dirElemFileToModify.SetAttributeValueAnyType(
                attribute: toponomyDetail,
                value: "",
                version: attributeVersion,
                isMarkedForDeletion: true);
        }

        dirElemFileToModify.SetAttributeValueAnyType(
            attribute: ElementAttribute.RemoveAllGPS,
            value: "",
            version: attributeVersion,
            isMarkedForDeletion: true);
    }
}
