using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using WinFormsDarkThemerNinja;

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
                HelperVariables.UserSettingGeoNamesUserName = HelperDataApplicationSettings.DataReadSQLiteSettings(dataTable: HelperVariables.DtHelperDataApplicationSettings, settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_UserName");
                HelperVariables.UserSettingGeoNamesPwd = HelperDataApplicationSettings.DataReadSQLiteSettings(dataTable: HelperVariables.DtHelperDataApplicationSettings, settingTabPage: "tpg_Application", settingId: "tbx_GeoNames_Pwd");
            }
            catch (Exception)
            {
                Themer.ShowMessageBox(
                    message: HelperControlAndMessageBoxHandling.ReturnControlText(
                        controlName: "mbx_Helper_ErrorCantReadDefaultSQLiteDB",
                        fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                    icon: MessageBoxIcon.Error,
                    buttons: MessageBoxButtons.OK);
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

        RestRequest requestToponomy = new(resource:
            $"findNearbyPlaceNameJSON?lat={latitude}&lng={longitude}&lang={HelperVariables.APILanguageToUse}{SOnlyShowFCodePPL}&style=FULL&radius={radius}&maxRows={HelperVariables.ToponyMaxRowsChoiceOfferCount}");
        RestResponse responseToponomy = client.ExecuteGet(request: requestToponomy);
        // check API reponse is OK and isn't telling us that we're out of our allocated limit
        if (responseToponomy.Content != null & !
            responseToponomy.Content.Contains(value: "the hourly limit of "))
        {
            HelperVariables.OperationAPIReturnedOKResponse = true;
            JObject data = (JObject)JsonConvert.DeserializeObject(value: responseToponomy.Content);
            GeoResponseToponomy geoResponseToponomy = GeoResponseToponomy.FromJson(Json: data.ToString());
            returnVal = geoResponseToponomy;
        }
        else
        {
            string apiMessage = string.Empty;
            try
            {
                // Parse the string into a JObject
                JObject apiContentDetails = JObject.Parse(responseToponomy.Content);

                // Access the nested "message" field
                apiMessage = apiContentDetails["status"]["message"].ToString();
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
            HelperVariables.OperationAPIReturnedOKResponse = false;
            Themer.ShowMessageBox(
                message: HelperControlAndMessageBoxHandling.ReturnControlText(
                    controlName: "mbx_Helper_WarningGeoNamesAPIResponse",
                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox)
                    + Environment.NewLine
                    + apiMessage,
                icon: MessageBoxIcon.Warning,
                buttons: MessageBoxButtons.OK);
        }

        return returnVal;
    }
}