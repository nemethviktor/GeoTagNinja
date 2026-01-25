using ExCSS;
using GeoTagNinja.Model;
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

namespace GeoTagNinja.Helpers;

internal static class HelperExifReadExifData
{


    /// <summary>
    /// Provides the default mapping of English item names to their corresponding database column headers.
    /// </summary>
    /// <remarks>This dictionary is used to translate standardized item names into database column headers,
    /// enabling consistent data retrieval and manipulation. The mapping is initialized with a predefined set of item
    /// names and their associated column header strings.</remarks>
    internal static Dictionary<string, string> DefaultEnglishNamesToColumnHeaders = new()
    {
        { nameof(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Distance), "clh_Distance" },
        { nameof(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode), "clh_CountryCode" },
        { nameof(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Country), "clh_Country" },
        { nameof(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.City), "clh_City" },
        { nameof(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.State), "clh_State" },
        { nameof(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation), "clh_Sublocation" },
        { nameof(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.GPSAltitude), "clh_GPSAltitude" },
        { nameof(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId), "clh_timezoneId" },
    };

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

        bool includePredeterminedCountries = HelperDataApplicationSettings.DataReadCheckBoxSettingTrueOrFalse(
            dataTable: HelperVariables.DtHelperDataApplicationSettings,
            settingTabPage: "tpg_CustomRules",
            settingId: "ckb_IncludePredeterminedCountries"
        );

        bool stopProcessingRules = HelperDataApplicationSettings.DataReadCheckBoxSettingTrueOrFalse(
            dataTable: HelperVariables.DtHelperDataApplicationSettings,
            settingTabPage: "tpg_CustomRules",
            settingId: "ckb_StopProcessingRules"
        );

        #region Actual value allocation block

        // As per https://github.com/nemethviktor/GeoTagNinja/issues/38#issuecomment-1356844255 (see below comment a few lines down)

        // read from SQL
        if (lstToponomySessionData.Count > 0)
        {
            bool isPredeterminedCountry = false;
            // CountryCode etc does not need changing to hardcoded english or "clh_" style here.
            CountryCode = $"{lstToponomySessionData[index: 0][columnName: "CountryCode"]}";
            Country = HelperDataLanguageTZ.DataReadDTCountryCodesNames(
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

            drSQLToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Distance, true)]] = Distance;
            drSQLToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode, true)]] = CountryCode;
            drSQLToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Country, true)]] = Country;
            drSQLToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.City, true)]] = City;
            drSQLToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.State, true)]] = State;
            drSQLToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation, true)]] = Sublocation;
            drSQLToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.GPSAltitude, true)]] = Altitude;
            drSQLToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId, true)]] = timezoneId;

            dtSQLToponomyData.Rows.Add(row: drSQLToponomyRow);
        }
        // read from API
        else if (HelperVariables.OperationAPIReturnedOKResponse)
        {
            bool isPredeterminedCountry = false;

            readJsonToponomy = HelperAPIGeoNamesToponomyExtractor.API_ExifGetGeoDataFromWebToponomy(
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
                        readJsonToponomy = HelperAPIGeoNamesToponomyExtractor.API_ExifGetGeoDataFromWebToponomy(
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
                    foreach (string column in (List<string>)["lat", "lng", "AdminName1", "AdminName2", "AdminName3", "AdminName4", "ToponymName", "CountryCode", "GPSAltitude", "timezoneId"])
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
                            CountryCode = HelperDataLanguageTZ.DataReadDTCountryCodesNames(
                                queryWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A2,
                                inputVal: APICountryCode,
                                returnWhat: LanguageMappingQueryOrReturnWhat.ISO_3166_1A3
                            );
                            Country = HelperDataLanguageTZ.DataReadDTCountryCodesNames(
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

                        drAPIToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Distance, true)]] = Distance;
                        drAPIToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode, true)]] = CountryCode;
                        drAPIToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Country, true)]] = Country;
                        drAPIToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.City, true)]] = City;
                        drAPIToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.State, true)]] = State;
                        drAPIToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation, true)]] = Sublocation;
                        drAPIToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.GPSAltitude, true)]] = Altitude;
                        drAPIToponomyRow[columnName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId, true)]] = timezoneId;

                        dtSQLToponomyData.Rows.Add(row: drAPIToponomyRow);

                        // write back the new stuff to sql

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

                        HelperDataOtherDataRelated.UpdateAddToDataTableTopopnomy(
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
                        FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];
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
                            _ = HelperExifReadGetImagePreviews.GenericCreateImagePreview(
                                directoryElement: directoryElement,
                                initiator: HelperExifReadGetImagePreviews.Initiator.FrmMainAppAPIDataSelection
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
                        HelperDataOtherDataRelated.UpdateAddToDataTableTopopnomy(
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
                    HelperDataOtherDataRelated.UpdateAddToDataTableTopopnomy(
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
                GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Distance, true),
                DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Distance, true)]
            },
            {
                GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode, true),
                DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.CountryCode, true)]
            },
            {
                GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Country, true),
                DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Country, true)]
            },
            {
                GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.City, true),
                DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.City, true)]
            },
            {
                GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.State, true),
                DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.State, true)]
            },
            {
                GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation, true),
                DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.Sublocation, true)]
            },
            {
                GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.GPSAltitude, true),
                DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.GPSAltitude, true)]
            },
            {
                GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId, true),
                DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing.timezoneId, true)]
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
    internal static string GetToponomyDataColumnName(HelperGenericAncillaryListsArrays.DefaultColumnNamesFromElementAttributesForFileEditing itemName, bool useDefaultHardcodedEnglishValues)
    {
        return useDefaultHardcodedEnglishValues
                            ? $"{itemName}"
                            : HelperControlAndMessageBoxHandling.ReturnControlText(
                                fakeControlType: HelperControlAndMessageBoxHandling.FakeControlTypes.ColumnHeader,
                                controlName: DefaultEnglishNamesToColumnHeaders[GetToponomyDataColumnName(itemName, true)]);
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
        if (HelperGenericAncillaryListsArrays.ToponomyReplaces()
                                             .Contains(value: settingId) &&
            HelperVariables.ToponomyReplace &&
            settingValue.Length == 0)
        {
            retStr = HelperVariables.ToponomyReplaceWithWhat;
        }

        return retStr;
    }

    /// <summary>
    ///     This translates between plain English and exiftool tags. For example if the tag we are looking for is "Model" (of
    ///     the camera)..
    ///     ... this will get all the possible tag names where model-info can sit and extract those from the data. E.g. if
    ///     we're looking for Model ...
    ///     this will get both EXIF:Model and and XMP:Model - as it does a cartesian join on the objectNames table.
    /// </summary>
    /// <param name="dtFileExif">Raw exiftool outout of all tags (datatable with rows such as "exif:GPSLatitude" and their values</param>
    /// <param name="dataPoint">Plain English datapoint we're after - e.g. "GPSLatitude"</param>
    /// <returns>Value of that datapoint if exists (e.g "Canon EOS 30D") - unwrangled, raw.</returns>
    private static string ExifGetRawDataPointFromExif(DataTable dtFileExif,
                                                      string dataPoint)
    {
        FrmMainApp.Log.Trace(message: $"Starting - dataPoint:{dataPoint}");
        string tryDataValue = FrmMainApp.NullStringEquivalentGeneric;
        ElementAttribute attribute = GetElementAttributesElementAttribute(dataPoint);
        List<string> orderedTags = GetElementAttributesIn(attribute);

        if (orderedTags.Any() && dtFileExif.Rows.Count > 0)
        {
            foreach (string tagWanted in orderedTags)
            {
                DataRow filteredRows = dtFileExif.Select(filterExpression: $"attribute = '{tagWanted}'")
                                                 .FirstOrDefault();
                if (filteredRows != null)
                {
                    tryDataValue = filteredRows[columnIndex: 1]?.ToString();
                    if (!string.IsNullOrEmpty(value: tryDataValue))
                    {
                        FrmMainApp.Log.Trace(message: $"dataPoint:{dataPoint} -> {tagWanted}: {tryDataValue}");
                        break;
                    }
                }
            }
        }

        FrmMainApp.Log.Debug(message: $"Done - dataPoint:{dataPoint}");
        return tryDataValue;
    }

    /// <summary>
    ///     Wrangles data from raw exiftool output to presentable and standardised data.
    ///     This only gets called via the API calls (e.g. ReadTrackFile) [plus possibly recursively, from within here], 
    ///     ...not the "normal" exif extraction.
    /// </summary>
    /// <param name="dtFileExif">Raw values tag from exiftool</param>
    /// <param name="dataPoint">Name of the exiftag we want the data for</param>
    /// <returns>Standardised exif tag output</returns>
    internal static string ExifGetStandardisedDataPointFromExifAsString(DataTable dtFileExif,
                                                                string dataPoint)
    {
        string tryDataValue = FrmMainApp.NullStringEquivalentGeneric;
        string tmpOutLatLongVal = "";

        FrmMainApp.Log.Trace(message: $"Starting - dataPoint:{dataPoint}");
        try
        {
            tryDataValue = ExifGetRawDataPointFromExif(dtFileExif: dtFileExif, dataPoint: dataPoint);
            // Not logging this bcs it gets called inside and is basically redunant here.
            // FrmMainApp.Log.Trace(message: "dataPoint:" + dataPoint + " - ExifGetRawDataPointFromExif: " + tryDataValue);
        }
        catch (Exception ex)
        {
            FrmMainApp.Log.Error(message: $"datapoint:{dataPoint} - Error: {ex.Message}");
        }

        switch (dataPoint)
        {
            case "GPSLatitude" or "GPSLongitude":
                if (tryDataValue != FrmMainApp.NullStringEquivalentGeneric)
                {
                    string tmpLatLongRefVal;
                    // we want N instead of North etc.
                    // Get the Ref Tag for the corresponding data point and thereof the first character
                    // (Should be N of North, etc.)
                    // If this character is not contained in the data point value, add it before it
                    // Finally ensure that dec sep. is "."
                    try
                    {
                        tmpLatLongRefVal =
                            ExifGetRawDataPointFromExif(
                                    dtFileExif: dtFileExif, dataPoint: $"{dataPoint}Ref")
                               .Substring(startIndex: 0, length: 1);
                    }
                    catch
                    {
                        tmpLatLongRefVal = FrmMainApp.NullStringEquivalentGeneric;
                    }

                    if (!tryDataValue.Contains(value: tmpLatLongRefVal) &&
                        tmpLatLongRefVal != FrmMainApp.NullStringEquivalentGeneric)
                    {
                        tryDataValue = tmpLatLongRefVal + tryDataValue;
                    }

                    tmpOutLatLongVal = HelperExifDataPointInteractions
                                      .AdjustLatLongNegative(point: tryDataValue)
                                      .ToString()
                                      .Replace(oldChar: ',', newChar: '.');
                }

                tryDataValue = tmpOutLatLongVal;
                break;
            case "Coordinates" or "DestCoordinates":
                string isDest = dataPoint.Contains(value: "Dest")
                    ? "Dest"
                    : "";
                // this is entirely the duplicate of the above
                // check there is lat/long
                string tmpLatVal =
                    ExifGetRawDataPointFromExif(dtFileExif: dtFileExif,
                            dataPoint: $"GPS{isDest}Latitude")
                       .Replace(oldChar: ',', newChar: '.');
                string tmpLongVal = ExifGetRawDataPointFromExif(
                dtFileExif: dtFileExif, dataPoint: $"GPS{isDest}Longitude")
           .Replace(oldChar: ',', newChar: '.');
                if (tmpLatVal == "")
                {
                    tmpLatVal = FrmMainApp.NullStringEquivalentGeneric;
                }

                if (tmpLongVal == "")
                {
                    tmpLongVal = FrmMainApp.NullStringEquivalentGeneric;
                }

                string tmpLatRefVal;
                string tmpLongRefVal;
                if (ExifGetRawDataPointFromExif(dtFileExif: dtFileExif,
                            dataPoint: $"GPS{isDest}LatitudeRef")
                       .Length >
                    0 &&
                    ExifGetRawDataPointFromExif(dtFileExif: dtFileExif,
                            dataPoint: $"GPS{isDest}LongitudeRef")
                       .Length >
                    0)
                {
                    tmpLatRefVal = ExifGetRawDataPointFromExif(
                                       dtFileExif: dtFileExif,
                                       dataPoint: $"GPS{isDest}LatitudeRef")
                                  .Substring(startIndex: 0, length: 1)
                                  .Replace(oldChar: ',', newChar: '.');
                    tmpLongRefVal = ExifGetRawDataPointFromExif(
                                        dtFileExif: dtFileExif,
                                        dataPoint: $"GPS{isDest}LongitudeRef")
                                   .Substring(startIndex: 0, length: 1)
                                   .Replace(oldChar: ',', newChar: '.');
                }

                // this shouldn't really happen but ET v12.49 extracts trackfile data in the wrong format so...
                else if ((tmpLatVal.Contains(value: 'N') ||
                          tmpLatVal.Contains(value: 'S')) &&
                         (tmpLongVal.Contains(value: 'E') ||
                          tmpLongVal.Contains(value: 'W')))
                {
                    tmpLatRefVal = tmpLatVal.Contains(value: 'N')
                        ? "N"
                        : "S";

                    tmpLongRefVal = tmpLongVal.Contains(value: 'E')
                        ? "E"
                        : "W";
                }
                else
                {
                    tmpLatRefVal = FrmMainApp.NullStringEquivalentGeneric;
                    tmpLongRefVal = FrmMainApp.NullStringEquivalentGeneric;
                }

                // check there is one bit of data for both components
                if (tmpLatVal != FrmMainApp.NullStringEquivalentGeneric &&
                    tmpLongVal != FrmMainApp.NullStringEquivalentGeneric)
                {
                    // stick Ref at the end of LatLong
                    if (!tmpLatVal.Contains(value: tmpLatRefVal))
                    {
                        tmpLatVal += tmpLatRefVal;
                    }

                    if (!tmpLongVal.Contains(value: tmpLongRefVal))
                    {
                        tmpLongVal += tmpLongRefVal;
                    }

                    tmpLatVal = HelperExifDataPointInteractions
                               .AdjustLatLongNegative(point: tmpLatVal)
                               .ToString()
                               .Replace(oldChar: ',', newChar: '.');
                    tmpLongVal = HelperExifDataPointInteractions
                                .AdjustLatLongNegative(point: tmpLongVal)
                                .ToString()
                                .Replace(oldChar: ',', newChar: '.');
                    tryDataValue = $"{tmpLatVal};{tmpLongVal}";
                }
                else
                {
                    tryDataValue = FrmMainApp.NullStringEquivalentGeneric;
                }

                break;
            case "GPSAltitude":
                if (tryDataValue.Contains(value: "m"))
                {
                    tryDataValue = tryDataValue.Split('m')[0]
                                               .Trim()
                                               .Replace(oldChar: ',', newChar: '.');
                }

                tryDataValue = ConvertFractionalToString(tryDataValue: tryDataValue);

                break;
            case "GPSAltitudeRef":
                tryDataValue = tryDataValue.ToLower()
                                .Contains(value: "below") ||
                    tryDataValue.Contains(value: "1")
                    ? "Below Sea Level"
                    : "Above Sea Level";

                break;
            case "GPSDOP":
                // i'm a little unsure about this because it's supposed to be a numeric value but
                // in the first attempt i've seen stuff like "43/10" rather than "4.3"

                tryDataValue = ConvertFractionalToString(tryDataValue: tryDataValue);
                break;
            case "GPSHPositioningError":
                // if there is a value we take it, if not we try to get it from GPSDOP*3
                if (tryDataValue == FrmMainApp.NullStringEquivalentGeneric)
                {
                    string tmpDop = ExifGetStandardisedDataPointFromExifAsString(dtFileExif: dtFileExif, dataPoint: $"GPSDOP");
                    tryDataValue = !string.IsNullOrEmpty(value: tmpDop) && double.TryParse(tmpDop, out double dopAsDouble)
                        ? (dopAsDouble * 3)
                                      .ToString(provider: CultureInfo.InvariantCulture)
                        : FrmMainApp.NullStringEquivalentGeneric;
                }
                break;
            case "ExposureTime":
                tryDataValue = tryDataValue.Replace(oldValue: "sec", newValue: "")
                                           .Trim();
                break;
            case "Fnumber" or "FocalLength" or "FocalLengthIn35mmFormat" or "ISO":
                if (tryDataValue != FrmMainApp.NullStringEquivalentGeneric)
                {
                    if (dataPoint == "FocalLengthIn35mmFormat")
                    {
                        // at least with a Canon 40D this returns stuff like: "51.0 mm (35 mm equivalent: 81.7 mm)" so i think it's safe to assume that 
                        // this might need a bit of debugging and community feeback. or someone with decent regex knowledge
                        if (tryDataValue.Contains(value: ':'))
                        {
                            tryDataValue = Regex
                                          .Replace(input: tryDataValue,
                                               pattern: @"[^\d:.]", replacement: "")
                                          .Split(':')
                                          .Last();
                        }
                        else
                        {
                            // this is untested. soz. feedback welcome.
                            tryDataValue = Regex.Replace(
                                input: tryDataValue, pattern: @"[^\d:.]",
                                replacement: "");
                        }
                    }
                    else
                    {
                        tryDataValue = tryDataValue.Replace(oldValue: "mm", newValue: "")
                                                   .Replace(oldValue: "f/", newValue: "")
                                                   .Replace(oldValue: "f", newValue: "")
                                                   .Replace(oldValue: "[", newValue: "")
                                                   .Replace(oldValue: "]", newValue: "")
                                                   .Trim();
                    }

                    if (tryDataValue.Contains(value: "/"))
                    {
                        tryDataValue = Math
                                      .Round(
                                           value: double.Parse(
                                                      s: tryDataValue.Split('/')[0],
                                                      style: NumberStyles.Any,
                                                      provider: CultureInfo
                                                         .InvariantCulture) /
                                                  double.Parse(
                                                      s: tryDataValue.Split('/')[1],
                                                      style: NumberStyles.Any,
                                                      provider: CultureInfo
                                                         .InvariantCulture),
                                           digits: 1)
                                      .ToString();
                    }
                }

                break;
            case /*"FileModifyDate" or */"TakenDate" or "CreateDate":
                {
                    tryDataValue =
                        DateTime.TryParse(s: tryDataValue, result: out _)
                            ? HelperGenericTypeOperations.ConvertStringToDateTimeBackToString(
                                dateTimeToConvert: tryDataValue)
                            : FrmMainApp.NullStringEquivalentGeneric;

                    break;
                }
        }

        FrmMainApp.Log.Trace(message: $"Done - dataPoint:{dataPoint}: {tryDataValue}");
        string returnVal = tryDataValue;
        return returnVal;
    }

    /// <summary>
    ///     Takes a string and attempts to convert it to a number-looking-string if it's actually a fractional. (ie "43/10" to
    ///     "4.3")
    /// </summary>
    /// <param name="tryDataValue"></param>
    /// <returns></returns>
    private static string ConvertFractionalToString(string tryDataValue)
    {
        if (tryDataValue.Contains(value: "/"))
        {
            if (tryDataValue.Contains(value: ",") ||
                tryDataValue.Contains(value: "."))
            {
                tryDataValue = tryDataValue.Split('/')[0]
                                           .Trim()
                                           .Replace(oldChar: ',', newChar: '.');
            }
            else // attempt to convert it to decimal
            {
                try
                {
                    bool parseBool = double.TryParse(
                        s: tryDataValue.Split('/')[0], style: NumberStyles.Any,
                        provider: CultureInfo.InvariantCulture,
                        result: out double numerator);
                    parseBool = double.TryParse(
                        s: tryDataValue.Split('/')[1], style: NumberStyles.Any,
                        provider: CultureInfo.InvariantCulture,
                        result: out double denominator);
                    tryDataValue = Math
                                  .Round(value: numerator / denominator,
                                       digits: 2)
                                  .ToString(
                                       provider: CultureInfo.InvariantCulture);
                }
                catch
                {
                    tryDataValue = "0.0";
                }
            }
        }

        return tryDataValue;
    }
}