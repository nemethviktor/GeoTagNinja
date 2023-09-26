﻿using System;
using System.Windows.Forms;
using geoTagNinja;
using GeoTagNinja.View.DialogAndMessageBoxes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace GeoTagNinja.Helpers;

internal static class HelperAPIGeoNamesToponomyExtractor
{
    /// <summary>
    ///     Responsible for pulling the toponomy response for the API
    /// </summary>
    /// <param name="latitude">As on the tin.</param>
    /// <param name="longitude">As on the tin.</param>
    /// <returns>Structured toponomy response</returns>
    internal static GeoResponseToponomy API_ExifGetGeoDataFromWebToponomy(string latitude,
                                                                          string longitude,
                                                                          string radius)
    {
        if (HelperVariables.UserSettingGeoNamesUserName == null)
        {
            try
            {
                HelperVariables.UserSettingGeoNamesUserName = HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_UserName");
                HelperVariables.UserSettingGeoNamesPwd = HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_Pwd");
            }
            catch (Exception ex)
            {
                CustomMessageBox customMessageBox = new(
                    text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorCantReadDefaultSQLiteDB") +
                          ex.Message,
                    caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                        captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Error.ToString()),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Error);
                customMessageBox.ShowDialog();
            }
        }

        GeoResponseToponomy returnVal = new();
        RestClientOptions options = new(baseUrl: "http://api.geonames.org")
        {
            Authenticator = new HttpBasicAuthenticator(username: HelperVariables.UserSettingGeoNamesUserName, password: HelperVariables.UserSettingGeoNamesPwd)
        };
        RestClient client = new(options: options);

        string SOnlyShowFCodePPL = HelperVariables.UserSettingOnlyShowFCodePPL
            ? "&fcode=PPL"
            : "";

        RestRequest requestToponomy = new(resource: "findNearbyPlaceNameJSON?lat=" +
                                                    latitude +
                                                    "&lng=" +
                                                    longitude +
                                                    "&lang=" +
                                                    HelperVariables.APILanguageToUse +
                                                    SOnlyShowFCodePPL +
                                                    "&style=FULL" +
                                                    "&radius=" +
                                                    radius +
                                                    "&maxRows=" +
                                                    HelperVariables.ToponomyMaxRows);
        RestResponse responseToponomy = client.ExecuteGet(request: requestToponomy);
        // check API reponse is OK
        if (responseToponomy.Content != null && responseToponomy.Content.Contains(value: "the hourly limit of "))
        {
            HelperVariables.OperationAPIReturnedOKResponse = false;
            CustomMessageBox customMessageBox = new(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") +
                      responseToponomy.Content,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                    captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Warning.ToString()),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
            customMessageBox.ShowDialog();
        }
        else if (responseToponomy.StatusCode.ToString() == "OK")
        {
            HelperVariables.OperationAPIReturnedOKResponse = true;
            JObject data = (JObject)JsonConvert.DeserializeObject(value: responseToponomy.Content);
            GeoResponseToponomy geoResponseToponomy = GeoResponseToponomy.FromJson(Json: data.ToString());
            returnVal = geoResponseToponomy;
        }
        else
        {
            HelperVariables.OperationAPIReturnedOKResponse = false;
            CustomMessageBox customMessageBox = new(
                text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(
                          messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") +
                      responseToponomy.StatusCode,
                caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(
                    captionType: HelperControlAndMessageBoxHandling.MessageBoxCaption.Warning.ToString()),
                buttons: MessageBoxButtons.OK,
                icon: MessageBoxIcon.Warning);
            customMessageBox.ShowDialog();
        }

        return returnVal;
    }
}