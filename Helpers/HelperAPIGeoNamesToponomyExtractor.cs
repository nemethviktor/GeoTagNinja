using System;
using System.Windows.Forms;
using geoTagNinja;
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
        if (HelperVariables.SGeoNamesUserName == null)
        {
            try
            {
                HelperVariables.SGeoNamesUserName = HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_UserName");
                HelperVariables.SGeoNamesPwd = HelperDataApplicationSettings.DataReadSQLiteSettings(tableName: "settings", settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_Pwd");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_ErrorCantReadDefaultSQLiteDB") +
                          ex.Message,
                    caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Error"),
                    buttons: MessageBoxButtons.OK,
                    icon: MessageBoxIcon.Error);
            }
        }

        GeoResponseToponomy returnVal = new();
        RestClient client = new(baseUrl: "http://api.geonames.org/")
        {
            Authenticator = new HttpBasicAuthenticator(username: HelperVariables.SGeoNamesUserName, password: HelperVariables.SGeoNamesPwd)
        };

        string SOnlyShowFCodePPL = HelperVariables.SOnlyShowFCodePPL
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
            HelperVariables.SApiOkay = false;
            MessageBox.Show(text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") +
                                  responseToponomy.Content,
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }
        else if (responseToponomy.StatusCode.ToString() == "OK")
        {
            HelperVariables.SApiOkay = true;
            JObject data = (JObject)JsonConvert.DeserializeObject(value: responseToponomy.Content);
            GeoResponseToponomy geoResponseToponomy = GeoResponseToponomy.FromJson(Json: data.ToString());
            returnVal = geoResponseToponomy;
        }
        else
        {
            HelperVariables.SApiOkay = false;
            MessageBox.Show(text: HelperControlAndMessageBoxHandling.GenericGetMessageBoxText(messageBoxName: "mbx_Helper_WarningGeoNamesAPIResponse") +
                                  responseToponomy.StatusCode,
                            caption: HelperControlAndMessageBoxHandling.GenericGetMessageBoxCaption(captionType: "Warning"),
                            buttons: MessageBoxButtons.OK,
                            icon: MessageBoxIcon.Warning);
        }

        return returnVal;
    }
}