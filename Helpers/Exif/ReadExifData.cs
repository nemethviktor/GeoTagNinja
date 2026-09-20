using ExCSS;
using GeoTagNinja.Helpers.API;
using GeoTagNinja.Helpers.Data;
using GeoTagNinja.Helpers.UI;
using GeoTagNinja.Model;
using GeoTagNinja.View.Forms;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WinFormsDarkThemerNinja;
using static GeoTagNinja.Model.SourcesAndAttributes;
using Point = System.Drawing.Point;

namespace GeoTagNinja.Helpers.Exif;

# region Toponomy

internal static class ReadExifData
{
    /// <summary>
    ///     Performs a search in the local SQLite database for cached toponomy info and if finds it, returns that, else queries
    ///     the API
    /// </summary>
    /// <param name="lat">latitude to be queried</param>
    /// <param name="lng">longitude to be queried</param>
    /// <param name="fileNameWithoutPath">Name of the file</param>
    /// 
    /// <returns>
    ///     See summary. Returns the toponomy info either from SQLite if available or the API in DataTable for further
    ///     processing
    /// </returns>
    internal static DataTable DTFromAPIExifGetToponomyFromWebOrSQL(
        string lat,
        string lng,
        string fileNameWithoutPath = "")
    {
        DataTable dtSQLToponomyData = new();
        AddColumnsToToponomyDataTable(dtSQLToponomyData);

        EnumerableRowCollection<DataRow> drDataTableData =
            from DataRow dataRow in FrmMainApp.DTToponomySessionData.AsEnumerable()
            where dataRow.Field<string>(columnName: "lat") == lat && dataRow.Field<string>(columnName: "lng") == lng
            select dataRow;

        List<DataRow> lstToponomySessionData = drDataTableData.ToList();

        GeoResponseToponomy readJsonToponomy;

        string? Distance = "";
        string? CountryCode = "";
        string? Country = "";
        string? City = "";
        string? State = "";
        string? Sublocation = "";
        string? Altitude = "0";
        string? timezoneId = "";

        bool includePredeterminedCountries = ApplicationSettings.DataReadCheckBoxSettingTrueOrFalse(
            dataTable: HelperVariables.DtHelperDataApplicationSettings,
            settingTabPage: "tpg_CustomRules",
            settingId: "ckb_IncludePredeterminedCountries"
, defaultValue: false);

        bool stopProcessingRules = ApplicationSettings.DataReadCheckBoxSettingTrueOrFalse(
            dataTable: HelperVariables.DtHelperDataApplicationSettings,
            settingTabPage: "tpg_CustomRules",
            settingId: "ckb_StopProcessingRules"
, defaultValue: false);

        #region Actual value allocation block

        // As per https://github.com/nemethviktor/GeoTagNinja/issues/38#issuecomment-1356844255 (see below comment a few lines down)

        // read from SQL
        if (lstToponomySessionData.Count > 0)
        {
            bool isPredeterminedCountry = false;
            // CountryCode etc does not need changing to hardcoded english or "clh_" style here.
            CountryCode = $"{lstToponomySessionData[index: 0][columnName: "CountryCode"]}";
            Country = LanguageTZ.DataReadDTCountryCodesNames(
                    queryWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A3,
                    inputVal: CountryCode,
                    returnWhat: LanguageMappingQueryOrReturnWhat.Country)
                ;

            Altitude = $"{lstToponomySessionData[index: 0][columnName: "GPSAltitude"]}";

            timezoneId = $"{lstToponomySessionData[index: 0][columnName: "timezoneId"]}";

            string? AdminName1InSQL = $"{lstToponomySessionData[index: 0][columnName: "AdminName1"]}";
            string? AdminName2InSQL = $"{lstToponomySessionData[index: 0][columnName: "AdminName2"]}";
            string? AdminName3InSQL = $"{lstToponomySessionData[index: 0][columnName: "AdminName3"]}";
            string? AdminName4InSQL = $"{lstToponomySessionData[index: 0][columnName: "AdminName4"]}";
            string? ToponymNameInSQL = $"{lstToponomySessionData[index: 0][columnName: "ToponymName"]}";

            // In a country where you know, which admin level the cities belong to (see arrays), use the adminNameX as city name.
            // If the toponymName doesn't match the adminNameX, use the toponymName as sublocation name. toponymNames ...
            // ... for populated places may be city names or names of some populated entity below city level, but they're never used for something above city level.
            // In a country where city names are not assigned to a specific admin level, I'd use the toponymName as the city name and leave the sublocation name blank.

            if (HelperVariables.LstCityNameIsAdminName1.Contains(item: CountryCode) ||
                HelperVariables.LstCityNameIsAdminName2.Contains(item: CountryCode) ||
                HelperVariables.LstCityNameIsAdminName3.Contains(item: CountryCode) ||
                HelperVariables.LstCityNameIsAdminName4.Contains(item: CountryCode)
               )
            {
                isPredeterminedCountry = true;

                Sublocation = ToponymNameInSQL;

                if (HelperVariables.LstCityNameIsAdminName1.Contains(item: CountryCode))
                {
                    City = AdminName1InSQL;
                    State = "";
                }
                else if (HelperVariables.LstCityNameIsAdminName2.Contains(item: CountryCode))
                {
                    City = AdminName2InSQL;
                }
                else if (HelperVariables.LstCityNameIsAdminName3.Contains(item: CountryCode))
                {
                    City = AdminName3InSQL;
                }
                else if (HelperVariables.LstCityNameIsAdminName4.Contains(item: CountryCode))
                {
                    City = AdminName4InSQL;
                }

                if (City == Sublocation)
                {
                    Sublocation = "";
                }

                if (!HelperVariables.LstCityNameIsAdminName1.Contains(item: CountryCode))
                {
                    State = AdminName1InSQL;
                }
            }

            if (!isPredeterminedCountry || includePredeterminedCountries)
            {
                bool customRuleChangedState = false;
                bool customRuleChangedCity = false;
                bool customRuleChangedSublocation = false;

                EnumerableRowCollection<DataRow> drCustomRulesData =
                    from DataRow dataRow in HelperVariables.DtCustomRules.AsEnumerable()
                    where dataRow.Field<string>(columnName: "CountryCode") == CountryCode
                    select dataRow;

                if (drCustomRulesData.Any())
                {
                    foreach (DataRow dataRow in drCustomRulesData)
                    {
                        string DataPointName = $"{dataRow[columnName: "DataPointName"]}";

                        string DataPointConditionType = $"{dataRow[columnName: "DataPointConditionType"]}";

                        string DataPointValueInSQL = null;

                        switch (DataPointName)
                        {
                            case "AdminName1":
                                DataPointValueInSQL = AdminName1InSQL;
                                break;
                            case "AdminName2":
                                DataPointValueInSQL = AdminName2InSQL;
                                break;
                            case "AdminName3":
                                DataPointValueInSQL = AdminName3InSQL;
                                break;
                            case "AdminName4":
                                DataPointValueInSQL = AdminName4InSQL;
                                break;
                            case "ToponymName":
                                DataPointValueInSQL = ToponymNameInSQL;
                                break;
                        }

                        // don't bother if null
                        if (!string.IsNullOrEmpty(value: DataPointValueInSQL))
                        {
                            string? DataPointConditionValue = $"{dataRow[columnName: "DataPointConditionValue"]}";
                            string? DataPointValueInSQLLC = DataPointValueInSQL.ToLower();
                            string? DataPointConditionValueLC = DataPointConditionValue.ToLower();
                            bool comparisonIsTrue = false;
                            switch (DataPointConditionType)
                            {
                                case "Is":
                                    if (DataPointValueInSQLLC == DataPointConditionValueLC)
                                    {
                                        comparisonIsTrue = true;
                                    }

                                    break;
                                case "Contains":
                                    if (DataPointValueInSQLLC.Contains(value: DataPointConditionValueLC))
                                    {
                                        comparisonIsTrue = true;
                                    }

                                    break;
                                case "StartsWith":
                                    if (DataPointValueInSQLLC.StartsWith(value: DataPointConditionValueLC))
                                    {
                                        comparisonIsTrue = true;
                                    }

                                    break;
                                case "EndsWith":
                                    if (DataPointValueInSQLLC.EndsWith(value: DataPointConditionValueLC))
                                    {
                                        comparisonIsTrue = true;
                                    }

                                    break;
                            }

                            if (comparisonIsTrue && ((stopProcessingRules && !customRuleChangedSublocation) ||
                                                     !stopProcessingRules))
                            {
                                string? TargetPointName = $"{dataRow[columnName: "TargetPointName"]}";
                                string? TargetPointOutcome = $"{dataRow[columnName: "TargetPointOutcome"]}";
                                string? TargetPointOutcomeCustom = $"{dataRow[columnName: "TargetPointOutcomeCustom"]}";

                                switch (TargetPointName)
                                {
                                    case "State":
                                        switch (TargetPointOutcome)
                                        {
                                            case "AdminName1":
                                                State = AdminName1InSQL;
                                                break;
                                            case "AdminName2":
                                                State = AdminName2InSQL;
                                                break;
                                            case "AdminName3":
                                                State = AdminName3InSQL;
                                                break;
                                            case "AdminName4":
                                                State = AdminName4InSQL;
                                                break;
                                            case "ToponymName":
                                                State = ToponymNameInSQL;
                                                break;
                                            case "Null (empty)":
                                                State = "";
                                                break;
                                            case "Custom":
                                                State = TargetPointOutcomeCustom;
                                                break;
                                        }

                                        customRuleChangedState = true;
                                        break;
                                    case "City":
                                        switch (TargetPointOutcome)
                                        {
                                            case "AdminName1":
                                                City = AdminName1InSQL;
                                                break;
                                            case "AdminName2":
                                                City = AdminName2InSQL;
                                                break;
                                            case "AdminName3":
                                                City = AdminName3InSQL;
                                                break;
                                            case "AdminName4":
                                                City = AdminName4InSQL;
                                                break;
                                            case "ToponymName":
                                                City = ToponymNameInSQL;
                                                break;
                                            case "Null (empty)":
                                                City = "";
                                                break;
                                            case "Custom":
                                                City = TargetPointOutcomeCustom;
                                                break;
                                        }

                                        customRuleChangedCity = true;
                                        break;
                                    case "Sublocation":
                                        switch (TargetPointOutcome)
                                        {
                                            //todo dontprocessmorerules
                                            case "AdminName1":
                                                Sublocation = AdminName1InSQL;
                                                break;
                                            case "AdminName2":
                                                Sublocation = AdminName2InSQL;
                                                break;
                                            case "AdminName3":
                                                Sublocation = AdminName3InSQL;
                                                break;
                                            case "AdminName4":
                                                Sublocation = AdminName4InSQL;
                                                break;
                                            case "ToponymName":
                                                Sublocation = ToponymNameInSQL;
                                                break;
                                            case "Null (empty)":
                                                Sublocation = "";
                                                break;
                                            case "Custom":
                                                Sublocation = TargetPointOutcomeCustom;
                                                break;
                                        }

                                        customRuleChangedSublocation = true;
                                        break;
                                }
                            }
                        }
                    }
                }

                if (!customRuleChangedState)
                {
                    State = AdminName1InSQL;
                }

                if (!customRuleChangedCity)
                {
                    City = ToponymNameInSQL;
                }

                if (!customRuleChangedSublocation)
                {
                    Sublocation = "";
                }
            }

        #endregion
            DataRow drSQLToponomyRow = dtSQLToponomyData.NewRow();

            drSQLToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Distance, true)]] = Distance;
            drSQLToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode, true)]] = CountryCode;
            drSQLToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Country, true)]] = Country;
            drSQLToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.City, true)]] = City;
            drSQLToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.State, true)]] = State;
            drSQLToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation, true)]] = Sublocation;
            drSQLToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.GPSAltitude, true)]] = Altitude;
            drSQLToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId, true)]] = timezoneId;

            dtSQLToponomyData.Rows.Add(row: drSQLToponomyRow);
        }
        // read from API
        else if (HelperVariables.OperationAPIReturnedOKResponse)
        {
            bool isPredeterminedCountry = false;

            readJsonToponomy = GeoNamesToponomyExtractor.API_ExifGetGeoDataFromWebToponomy(
                latitude: lat,
                longitude: lng,
                radius: HelperVariables.ToponomyRadiusValue
            );

            string ctrlNameForLocalError = "mbx_HelperStaticExifNoAPI";
            // if that returns nothing then try again with something bigger.
            try
            {
                if (readJsonToponomy.Geonames != null)
                {
                    if (readJsonToponomy.Geonames.Length == 0)
                    {
                        readJsonToponomy = GeoNamesToponomyExtractor.API_ExifGetGeoDataFromWebToponomy(
                            latitude: lat,
                            longitude: lng,
                            radius: "300"
                        );
                    }
                }
                else
                {
                    if (!HelperVariables.errorsAlreadyShownHashSet.Contains(item: ctrlNameForLocalError))
                    {
                        Themer.ShowMessageBox(
                            message: HelperControlAndMessageBoxHandling.ReturnControlText(
                                controlName: ctrlNameForLocalError,
                                fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                            icon: MessageBoxIcon.Error,
                            buttons: MessageBoxButtons.OK);

                        _ = HelperVariables.errorsAlreadyShownHashSet.Add(item: ctrlNameForLocalError);
                    }
                }
            }
            catch
            {
                if (!HelperVariables.errorsAlreadyShownHashSet.Contains(item: ctrlNameForLocalError))
                {
                    Themer.ShowMessageBox(
                        message: HelperControlAndMessageBoxHandling.ReturnControlText(
                            controlName: ctrlNameForLocalError,
                            fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.MessageBox),
                        icon: MessageBoxIcon.Error,
                        buttons: MessageBoxButtons.OK);

                    _ = HelperVariables.errorsAlreadyShownHashSet.Add(item: ctrlNameForLocalError);
                }
            }

            // ignore if unauthorised or some such
            if (readJsonToponomy.Geonames != null)
            {
                if (readJsonToponomy.Geonames.Length > 0)
                {
                    // this is to pseudo-replicate the dataTable table but for SQL, which has a different logic. (of course it does.)
                    DataTable dtWriteToSQLite = new();
                    dtWriteToSQLite.Clear();
                    foreach (string column in (List<string>)
                    [
                        "lat",
                        "lng",
                        "AdminName1",
                        "AdminName2",
                        "AdminName3",
                        "AdminName4",
                        "ToponymName",
                        "CountryCode",
                        "GPSAltitude",
                        "timezoneId"
                    ])
                    {
                        _ = dtWriteToSQLite.Columns.Add(columnName: column);
                    }

                    for (int index = 0; index < readJsonToponomy.Geonames.Length; index++)
                    {
                        DataRow drAPIToponomyRow = dtSQLToponomyData.NewRow();
                        DataRow drWriteToSQLiteRow = dtWriteToSQLite.NewRow();

                        string APICountryCode = readJsonToponomy.Geonames[index]
                                                                .CountryCode;
                        if (APICountryCode.Length == 2)
                        {
                            CountryCode = LanguageTZ.DataReadDTCountryCodesNames(
                                queryWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A2,
                                inputVal: APICountryCode,
                                returnWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A3
                            );
                            Country = LanguageTZ.DataReadDTCountryCodesNames(
                                queryWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A2,
                                inputVal: APICountryCode,
                                returnWhat: LanguageMappingQueryOrReturnWhat.Country
                            );
                        }

                        _ = double.TryParse(s: readJsonToponomy.Geonames[index]
                                                                   .Srtm3.ToString(), result: out double tmpAlt);
                        try
                        {
                            // can return 32768 or -32768 in some cases. this is the API's "fault" (not that of the code.)
                            if (Math.Abs(value: tmpAlt) > 32000.0)
                            {
                                if (!string.IsNullOrEmpty(value: HelperVariables.CurrentAltitudeAsString))
                                {
                                    _ = double.TryParse(s: HelperVariables.CurrentAltitudeAsString, result: out tmpAlt);
                                }
                                else
                                {
                                    tmpAlt = 0.0;
                                }
                            }
                        }
                        catch
                        {
                            tmpAlt = 0.0;
                        }

                        Altitude = $"{tmpAlt}";

                        // this is already String.
                        timezoneId = readJsonToponomy.Geonames[index]
                                                     .Timezone.TimeZoneId;

                        Distance = readJsonToponomy.Geonames[index]
                                                   .Distance;

                        string? AdminName1InAPI = readJsonToponomy.Geonames[index].AdminName1;
                        string? AdminName2InAPI = readJsonToponomy.Geonames[index].AdminName2;
                        string? AdminName3InAPI = readJsonToponomy.Geonames[index].AdminName3;
                        string? AdminName4InAPI = readJsonToponomy.Geonames[index].AdminName4;
                        string? ToponymNameInAPI = readJsonToponomy.Geonames[index].ToponymName;

                        // Comments are copied from above.
                        // In a country where you know, which admin level the cities belong to (see arrays), use the adminNameX as city name.
                        // If the toponymName doesn't match the adminNameX, use the toponymName as sublocation name. toponymNames ...
                        // ... for populated places may be city names or names of some populated entity below city level, but they're never used for something above city level.
                        // In a country where city names are not assigned to a specific admin level, I'd use the toponymName as the city name and leave the sublocation name blank.

                        if (HelperVariables.LstCityNameIsAdminName1.Contains(item: CountryCode) ||
                            HelperVariables.LstCityNameIsAdminName2.Contains(item: CountryCode) ||
                            HelperVariables.LstCityNameIsAdminName3.Contains(item: CountryCode) ||
                            HelperVariables.LstCityNameIsAdminName4.Contains(item: CountryCode)
                           )
                        {
                            isPredeterminedCountry = true;

                            Sublocation = readJsonToponomy.Geonames[index]
                                                          .ToponymName;
                            if (HelperVariables.LstCityNameIsAdminName1.Contains(item: CountryCode))
                            {
                                City = AdminName1InAPI;
                                State = "";
                            }
                            else if (HelperVariables.LstCityNameIsAdminName2.Contains(item: CountryCode))
                            {
                                City = AdminName2InAPI;
                            }
                            else if (HelperVariables.LstCityNameIsAdminName3.Contains(item: CountryCode))
                            {
                                City = AdminName3InAPI;
                            }
                            else if (HelperVariables.LstCityNameIsAdminName4.Contains(item: CountryCode))
                            {
                                City = AdminName4InAPI;
                            }

                            if (City == Sublocation)
                            {
                                Sublocation = "";
                            }

                            if (!HelperVariables.LstCityNameIsAdminName1.Contains(item: CountryCode))
                            {
                                State = AdminName1InAPI;
                            }
                        }

                        if (!isPredeterminedCountry || includePredeterminedCountries)
                        {
                            bool customRuleChangedState = false;
                            bool customRuleChangedCity = false;
                            bool customRuleChangedSublocation = false;

                            EnumerableRowCollection<DataRow> drCustomRulesData =
                                from DataRow dataRow in HelperVariables.DtCustomRules.AsEnumerable()
                                where dataRow.Field<string>(columnName: "CountryCode") == CountryCode
                                select dataRow;

                            if (drCustomRulesData.Any())
                            {
                                foreach (DataRow dataRow in drCustomRulesData)
                                {
                                    string? DataPointName = $"{dataRow[columnName: "DataPointName"]}";

                                    string? DataPointConditionType = $"{dataRow[columnName: "DataPointConditionType"]}";

                                    string? DataPointValueInAPI = null;
                                    switch (DataPointName)
                                    {
                                        case "AdminName1":
                                            DataPointValueInAPI = AdminName1InAPI;
                                            break;
                                        case "AdminName2":
                                            DataPointValueInAPI = AdminName2InAPI;
                                            break;
                                        case "AdminName3":
                                            DataPointValueInAPI = AdminName3InAPI;
                                            break;
                                        case "AdminName4":
                                            DataPointValueInAPI = AdminName4InAPI;
                                            break;
                                        case "ToponymName":
                                            DataPointValueInAPI = ToponymNameInAPI;
                                            break;
                                    }

                                    // don't bother if null
                                    if (!string.IsNullOrEmpty(value: DataPointValueInAPI))
                                    {
                                        string? DataPointConditionValue = $"{dataRow[columnName: "DataPointConditionValue"]}";
                                        string? DataPointValueInAPILC = DataPointValueInAPI?.ToLower();
                                        string? DataPointConditionValueLC = DataPointConditionValue.ToLower();
                                        bool comparisonIsTrue = false;
                                        switch (DataPointConditionType)
                                        {
                                            case "Is":
                                                if (DataPointValueInAPILC == DataPointConditionValueLC)
                                                {
                                                    comparisonIsTrue = true;
                                                }

                                                break;
                                            case "Contains":
                                                if (DataPointValueInAPILC.Contains(value: DataPointConditionValueLC))
                                                {
                                                    comparisonIsTrue = true;
                                                }

                                                break;
                                            case "StartsWith":
                                                if (DataPointValueInAPILC.StartsWith(value: DataPointConditionValueLC))
                                                {
                                                    comparisonIsTrue = true;
                                                }

                                                break;
                                            case "EndsWith":
                                                if (DataPointValueInAPILC.EndsWith(value: DataPointConditionValueLC))
                                                {
                                                    comparisonIsTrue = true;
                                                }

                                                break;
                                        }

                                        if (comparisonIsTrue &&
                                            ((stopProcessingRules && !customRuleChangedSublocation) ||
                                             !stopProcessingRules))
                                        {
                                            string? TargetPointName = $"{dataRow[columnName: "TargetPointName"]}";
                                            string? TargetPointOutcome = $"{dataRow[columnName: "TargetPointOutcome"]}";
                                            string? TargetPointOutcomeCustom = $"{dataRow[columnName: "TargetPointOutcomeCustom"]}";

                                            switch (TargetPointName)
                                            {
                                                case "State":
                                                    switch (TargetPointOutcome)
                                                    {
                                                        case "AdminName1":
                                                            State = AdminName1InAPI;
                                                            break;
                                                        case "AdminName2":
                                                            State = AdminName2InAPI;
                                                            break;
                                                        case "AdminName3":
                                                            State = AdminName3InAPI;
                                                            break;
                                                        case "AdminName4":
                                                            State = AdminName4InAPI;
                                                            break;
                                                        case "ToponymName":
                                                            State = ToponymNameInAPI;
                                                            break;
                                                        case "Null (empty)":
                                                            State = "";
                                                            break;
                                                        case "Custom":
                                                            State = TargetPointOutcomeCustom;
                                                            break;
                                                    }

                                                    customRuleChangedState = true;
                                                    break;
                                                case "City":
                                                    switch (TargetPointOutcome)
                                                    {
                                                        case "AdminName1":
                                                            City = AdminName1InAPI;
                                                            break;
                                                        case "AdminName2":
                                                            City = AdminName2InAPI;
                                                            break;
                                                        case "AdminName3":
                                                            City = AdminName3InAPI;
                                                            break;
                                                        case "AdminName4":
                                                            City = AdminName4InAPI;
                                                            break;
                                                        case "ToponymName":
                                                            City = ToponymNameInAPI;
                                                            break;
                                                        case "Null (empty)":
                                                            City = "";
                                                            break;
                                                        case "Custom":
                                                            City = TargetPointOutcomeCustom;
                                                            break;
                                                    }

                                                    customRuleChangedCity = true;
                                                    break;
                                                case "Sublocation":
                                                    switch (TargetPointOutcome)
                                                    {
                                                        case "AdminName1":
                                                            Sublocation = AdminName1InAPI;
                                                            break;
                                                        case "AdminName2":
                                                            Sublocation = AdminName2InAPI;
                                                            break;
                                                        case "AdminName3":
                                                            Sublocation = AdminName3InAPI;
                                                            break;
                                                        case "AdminName4":
                                                            Sublocation = AdminName4InAPI;
                                                            break;
                                                        case "ToponymName":
                                                            Sublocation = ToponymNameInAPI;
                                                            break;
                                                        case "Null (empty)":
                                                            Sublocation = "";
                                                            break;
                                                        case "Custom":
                                                            Sublocation = TargetPointOutcomeCustom;
                                                            break;
                                                    }

                                                    customRuleChangedSublocation = true;
                                                    break;
                                            }
                                        }
                                    }
                                }
                            }

                            if (!customRuleChangedState)
                            {
                                State = AdminName1InAPI;
                            }

                            if (!customRuleChangedCity)
                            {
                                City = ToponymNameInAPI;
                            }

                            if (!customRuleChangedSublocation)
                            {
                                Sublocation = "";
                            }
                        }

                        // add to return-table to offer to user

                        drAPIToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                            GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Distance, true)]] = Distance;
                        drAPIToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                            GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode, true)]] = CountryCode;
                        drAPIToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                            GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Country, true)]] = Country;
                        drAPIToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                            GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.City, true)]] = City;
                        drAPIToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                            GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.State, true)]] = State;
                        drAPIToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                            GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation, true)]] = Sublocation;
                        drAPIToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                            GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.GPSAltitude, true)]] = Altitude;
                        drAPIToponomyRow[columnName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                            GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId, true)]] = timezoneId;

                        dtSQLToponomyData.Rows.Add(row: drAPIToponomyRow);

                        // write back the new stuff to SQL

                        drWriteToSQLiteRow[columnName: "lat"] = lat;
                        drWriteToSQLiteRow[columnName: "lng"] = lng;
                        drWriteToSQLiteRow[columnName: "AdminName1"] = readJsonToponomy.Geonames[index]
                           .AdminName1;
                        drWriteToSQLiteRow[columnName: "AdminName2"] = readJsonToponomy.Geonames[index]
                           .AdminName2;
                        drWriteToSQLiteRow[columnName: "AdminName3"] = readJsonToponomy.Geonames[index]
                           .AdminName3;
                        drWriteToSQLiteRow[columnName: "AdminName4"] = readJsonToponomy.Geonames[index]
                           .AdminName4;
                        drWriteToSQLiteRow[columnName: "ToponymName"] = readJsonToponomy.Geonames[index]
                           .ToponymName;
                        drWriteToSQLiteRow[columnName: "CountryCode"] = CountryCode;
                        drWriteToSQLiteRow[columnName: "GPSAltitude"] = Altitude;
                        drWriteToSQLiteRow[columnName: "timezoneId"] = timezoneId;

                        dtWriteToSQLite.Rows.Add(row: drWriteToSQLiteRow);
                    }

                    if (dtSQLToponomyData.Rows.Count == 1)
                    {
                        // not adding anything to dataTable because it has 1 row, and that's the one that will be returned.

                        SessionDataTables.UpdateAddToDataTableTopopnomy(
                            lat: $"{dtWriteToSQLite.Rows[index: 0][columnName: "lat"]}",
                            lng: $"{dtWriteToSQLite.Rows[index: 0][columnName: "lng"]}",
                            adminName1: $"{dtWriteToSQLite.Rows[index: 0][columnName: "AdminName1"]}",
                            adminName2: $"{dtWriteToSQLite.Rows[index: 0][columnName: "AdminName2"]}",
                            adminName3: $"{dtWriteToSQLite.Rows[index: 0][columnName: "AdminName3"]}",
                            adminName4: $"{dtWriteToSQLite.Rows[index: 0][columnName: "AdminName4"]}",
                            toponymName: $"{dtWriteToSQLite.Rows[index: 0][columnName: "ToponymName"]}",
                            countryCode: $"{dtWriteToSQLite.Rows[index: 0][columnName: "CountryCode"]}",
                            altitude: $"{dtWriteToSQLite.Rows[index: 0][columnName: "GPSAltitude"]}",
                            timezoneId: $"{dtWriteToSQLite.Rows[index: 0][columnName: "timezoneId"]}"
                        );
                    }
                    else
                    {
                        FrmMainApp frmMainAppInstance = FrmMainApp.Instance;
                        // scroll to the file in question and show the image of it...makes life a lot easier
                        if (!string.IsNullOrEmpty(value: fileNameWithoutPath))
                        {
                            string fileNameWithPath =
                                Path.Combine(path1: FrmMainApp.FolderName, path2: fileNameWithoutPath);
                            ListViewItem lvi =
                                frmMainAppInstance.lvw_FileList.FindItemWithText(text: fileNameWithoutPath);
                            frmMainAppInstance.lvw_FileList.FocusedItem = lvi;
                            DirectoryElement directoryElement = lvi.Tag as DirectoryElement;
                            frmMainAppInstance.lvw_FileList.EnsureVisible(index: lvi.Index);
                            _ = ReadGetImagePreviews.GenericCreateImagePreview(
                                directoryElement: directoryElement,
                                initiator: ReadGetImagePreviews.Initiator.FrmMainAppAPIDataSelection
                            );
                            Application.DoEvents();
                        }

                        int useDr = showDataFromAPIPicker(dtIn: dtSQLToponomyData);
                        dtSQLToponomyData = dtSQLToponomyData.AsEnumerable()
                                           .Where(predicate: (row,
                                                              index) => index == useDr)
                                           .CopyToDataTable();

                        dtWriteToSQLite = dtWriteToSQLite.AsEnumerable()
                                                         .Where(predicate: (row,
                                                                            index) => index == useDr)
                                                         .CopyToDataTable();

                        // [0] because we just killed off the other rows above.
                        SessionDataTables.UpdateAddToDataTableTopopnomy(
                            lat: $"{dtWriteToSQLite.Rows[index: 0][columnName: "lat"]}",
                            lng: $"{dtWriteToSQLite.Rows[index: 0][columnName: "lng"]}",
                            adminName1: $"{dtWriteToSQLite.Rows[index: 0][columnName: "AdminName1"]}",
                            adminName2: $"{dtWriteToSQLite.Rows[index: 0][columnName: "AdminName2"]}",
                            adminName3: $"{dtWriteToSQLite.Rows[index: 0][columnName: "AdminName3"]}",
                            adminName4: $"{dtWriteToSQLite.Rows[index: 0][columnName: "AdminName4"]}",
                            toponymName: $"{dtWriteToSQLite.Rows[index: 0][columnName: "ToponymName"]}",
                            countryCode: $"{dtWriteToSQLite.Rows[index: 0][columnName: "CountryCode"]}",
                            altitude: $"{dtWriteToSQLite.Rows[index: 0][columnName: "GPSAltitude"]}",
                            timezoneId: $"{dtWriteToSQLite.Rows[index: 0][columnName: "timezoneId"]}"
                        );

                        int showDataFromAPIPicker(DataTable dtIn)
                        {
                            Form FrmPickDataFromAPIBox = new()
                            {
                                Text = HelperControlAndMessageBoxHandling.ReturnControlText(
                                    controlName: "FrmPickDataFromAPIBox",
                                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.Form),
                                MinimizeBox = false,
                                MaximizeBox = false,
                                ShowIcon = false,
                                ShowInTaskbar = false,
                                StartPosition = FormStartPosition.CenterScreen
                            };

                            FlowLayoutPanel panel = new();

                            ListView lvwDataChoices = new()
                            {
                                Size = new Size(width: 800, height: 200),
                                View = System.Windows.Forms.View.Details,
                                MultiSelect = false,
                                FullRowSelect = true
                            };

                            _ = lvwDataChoices.Columns.Add(text: "Index");

                            foreach (DataColumn dc in dtIn.Columns)
                            {
                                _ = lvwDataChoices.Columns.Add(text: dc.ColumnName, width: -2);
                            }

                            lvwDataChoices.MouseDoubleClick += (sender,
                                                                args) =>
                            {
                                ListViewHitTestInfo info = lvwDataChoices.HitTest(x: args.X, y: args.Y);
                                ListViewItem item = info.Item;

                                if (item != null)
                                {
                                    FrmPickDataFromAPIBox.Close();
                                }
                            };

                            foreach (DataRow drItem in dtSQLToponomyData.Rows)
                            {
                                // make it not-zero based.
                                ListViewItem lvi = new(text: (dtSQLToponomyData.Rows.IndexOf(row: drItem) +
                                                              1)
                                   .ToString());
                                foreach (DataColumn dc in dtIn.Columns)
                                {
                                    string dataToAdd = $"{drItem[column: dc]}";

                                    _ = lvi.SubItems.Add(text: dataToAdd);
                                }

                                _ = lvwDataChoices.Items.Add(value: lvi);
                            }

                            lvwDataChoices.Items[index: 0]
                                          .Selected = true;
                            lvwDataChoices.Select();

                            lvwDataChoices.KeyUp += (sender,
                                                     args) =>
                            {
                                if (args.KeyCode == Keys.Enter)
                                {
                                    if (lvwDataChoices.SelectedItems.Count == 1)
                                    {
                                        FrmPickDataFromAPIBox.Close();
                                    }
                                }
                            };

                            panel.Controls.Add(value: lvwDataChoices);
                            panel.SetFlowBreak(control: lvwDataChoices, value: true);

                            Button btn_Generic_OK = new()
                            {
                                Text = HelperControlAndMessageBoxHandling.ReturnControlText(
                                    controlName: "Generic_OK",
                                    fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.Button)
                            };
                            btn_Generic_OK.Click += (sender,
                                                     e) =>
                            {
                                FrmPickDataFromAPIBox.Close();
                            };
                            btn_Generic_OK.Location = new Point(x: 10, y: lvwDataChoices.Bottom + 15);
                            btn_Generic_OK.AutoSize = true;
                            panel.Controls.Add(value: btn_Generic_OK);

                            panel.Padding = new Padding(all: 5);
                            panel.AutoSize = true;

                            FrmPickDataFromAPIBox.Controls.Add(value: panel);
                            FrmPickDataFromAPIBox.MinimumSize = new Size(width: lvwDataChoices.Width + 40,
                                height: btn_Generic_OK.Bottom + 20);

                            _ = FrmPickDataFromAPIBox.ShowDialog();

                            try
                            {
                                return lvwDataChoices.SelectedItems[index: 0]
                                                     .Index;
                            }
                            catch
                            {
                                return 0;
                            }
                        }
                    }
                }
                else if (HelperVariables.OperationAPIReturnedOKResponse)
                {
                    // write back empty
                    SessionDataTables.UpdateAddToDataTableTopopnomy(
                        lat: lat,
                        lng: lng,
                        adminName1: "",
                        adminName2: "",
                        adminName3: "",
                        adminName4: "",
                        toponymName: "",
                        countryCode: "",
                        altitude: "",
                        timezoneId: ""
                    );
                }
            }
        }

        return dtSQLToponomyData;
    }

    /// <summary>
    /// Adds the required columns for toponomy data to the specified <see cref="DataTable"/> instance.
    /// </summary>
    /// <remarks>This method retrieves the necessary column names for toponomy data and appends them to the
    /// provided <see cref="DataTable"/>. If the table already contains columns with the same names, duplicate columns
    /// may be created. It is recommended to ensure the table's schema is appropriate before invoking this
    /// method.</remarks>
    /// <param name="dataTable">The <see cref="DataTable"/> to which the toponomy columns will be added. This parameter must be initialized and
    /// not null before calling the method.</param>
    private static void AddColumnsToToponomyDataTable(DataTable dataTable)
    {
        Dictionary<string, string> columnsToAddList = GetToponomyDataTableColumnNames(dataTable);

        foreach (KeyValuePair<string, string> s in columnsToAddList)
        {
            _ = dataTable.Columns.Add(columnName: s.Value);
        }
    }

    /// <summary>
    /// Creates a dictionary that maps toponomy data column names to their corresponding database column headers, and
    /// clears the specified DataTable prior to population.
    /// </summary>
    /// <remarks>This method ensures that the DataTable is cleared before constructing the mapping dictionary.
    /// The returned dictionary uses predefined mappings to maintain consistency with the expected database
    /// schema.</remarks>
    /// <param name="dtReturn">The DataTable to be cleared and prepared for toponomy data column name mappings. Must not be null.</param>
    /// <returns>A dictionary where each key is a toponomy data column name and each value is the corresponding database column
    /// header.</returns>
    private static Dictionary<string, string> GetToponomyDataTableColumnNames(DataTable dtReturn)
    {
        dtReturn.Clear();
        // what we want here is {"Distance", "clh_Distance"} etc.
        // but it's a little more foolproof hopefully
        Dictionary<string, string> columnsToAddList = new()
        {
            {
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Distance, true),
                ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                    GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Distance, true)]
            },
            {
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode, true),
                ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                    GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode, true)]
            },
            {
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Country, true),
                ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                    GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Country, true)]
            },
            {
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.City, true),
                ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                    GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.City, true)]
            },
            {
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.State, true),
                ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                    GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.State, true)]
            },
            {
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation, true),
                ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                    GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation, true)]
            },
            {
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.GPSAltitude, true),
                ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                    GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.GPSAltitude, true)]
            },
            {
                GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId, true),
                ToponomyColumns.DefaultEnglishNamesToColumnHeaders[
                    GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId, true)]
            },
        };
        return columnsToAddList;
    }

    /// <summary>
    /// Retrieves the column name associated with the specified item name, optionally using default hardcoded English
    /// values.
    /// </summary>
    /// <remarks>The result of this method depends on the value of useDefaultHardcodedEnglishValues. When set
    /// to true, the method returns the item name directly as a string, which may be useful for scenarios where
    /// localization is not required.</remarks>
    /// <param name="itemName">The item name for which to obtain the corresponding column name.</param>
    /// <param name="useDefaultHardcodedEnglishValues">true to return the default hardcoded English value for the column name; false to retrieve the column name from
    /// the localized mapping.</param>
    /// <returns>A string containing the column name associated with the specified item name. If useDefaultHardcodedEnglishValues
    /// is true, the item name is returned as a string; otherwise, the localized column name is returned.</returns>
    internal static string GetToponomyDataColumnName(DefaultColumnNamesFromElementAttributesForFileEditing itemName, bool useDefaultHardcodedEnglishValues)
    {
        return useDefaultHardcodedEnglishValues
                            ? $"{itemName}"
                            : HelperControlAndMessageBoxHandling.ReturnControlText(
                                fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.ColumnHeader,
                                controlName: ToponomyColumns.DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(itemName, true)]);
    }

    /// <summary>
    ///     Checks and replaces blank toponomy values as required
    /// </summary>
    /// <param name="settingId"></param>
    /// <param name="settingValue"></param>
    /// <returns></returns>
    internal static string ReplaceBlankToponomy(ElementAttribute settingId,
                                                string settingValue)
    {
        string retStr = settingValue;
        if (CustomRulesMetadata.ToponomyReplaces()
                                             .Contains(value: settingId) &&
            HelperVariables.ToponomyReplace &&
            settingValue.Length == 0)
        {
            retStr = HelperVariables.ToponomyReplaceWithWhat;
        }

        return retStr;
    }

#endregion
}