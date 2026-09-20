using System;
using System.Collections.Generic;
using System.Globalization;

namespace GeoTagNinja.Helpers.Localisation;

/// <summary>
///     Static language data: the ISO 639-1 list offered in the settings dialog, the lookup that maps
///     per-form control names onto shared "generic" resource keys, and the small set of cultures whose
///     own formatting is preferred over the invariant one.
/// </summary>
internal static class LanguageLists
{
    // this stores the kvp for language tags and values (ie the label and whatnots + their human-readable counterparts).
    internal static Dictionary<string, string> LanguageStringsDict = [];

    internal static Dictionary<string, string> GetISO_639_1_Languages()
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
        {
            // TwoLetterISOLanguageName corresponds to ISO 639-1 (e.g., "en", "af", "am")
            string isoCode = ci.TwoLetterISOLanguageName;

            // Skip non-standard, invariant, or 3-letter fallback cultures
            if (string.IsNullOrEmpty(isoCode) || isoCode.Length != 2 || isoCode == "iv")
                continue;

            // ci.NativeName -> "аҧсуа бызшәа"
            // ci.EnglishName -> "Abkhaz"
            string nativeName = ci.NativeName;
            string englishName = ci.EnglishName;

            // Format: "NativeName [EnglishName]"
            string formattedName = $"{nativeName} [{englishName}]";

            // Avoid duplicates (e.g., specific regional variants mapping to the same neutral code)
            if (!result.ContainsKey(isoCode))
            {
                result.Add(isoCode, formattedName);
            }
        }

        return result;
    }

    /// <summary>
    ///     This is part of a messy and obsolete code logic whereby I was using separate buttons and lables and whatnots for
    ///     ultimately identical purposes (e.g. "OK" or "None" etc). These have now been merged more-or-less and this
    ///     dictionary provides a translation vehicle between the old and new because I haven't actually changed everything I'd
    ///     hazard.
    ///     The other part of the logic is that there is some code that's dependent on the name of the object, for example the
    ///     code dealing with Taken Date vs Create Date (amongst others) needs to know what button was pressed but the
    ///     user-facing translation is identical.
    /// </summary>
    /// <param name="controlName"></param>
    /// <returns></returns>
    internal static string GetGenericControlName(string controlName)
    {
        Dictionary<string, string> lookupDictionary = new()
        {
            { "btn_AllData_All", "Generic_All" },
            { "btn_AllData_None", "Generic_None" },
            { "btn_Cancel", "Generic_Cancel" },
            { "btn_Close", "Generic_Close" },
            { "btn_Dates_All", "Generic_All" },
            { "btn_Dates_None", "Generic_None" },
            { "btn_Delete", "Generic_Delete" },
            { "btn_ExportSettings", "Generic_Export" },
            { "btn_getAllFromWeb_Altitude", "Generic_ForAllImages" },
            { "btn_getAllFromWeb_Toponomy", "Generic_ForAllImages" },
            { "btn_getFromWeb_Altitude", "Generic_ForThisImage" },
            { "btn_getFromWeb_Toponomy", "Generic_ForThisImage" },
            { "btn_GPSData_All", "Generic_All" },
            { "btn_GPSData_None", "Generic_None" },
            { "btn_ImportSettings", "Generic_Import" },
            { "btn_InsertCreateDate", "Generic_Insert" },
            { "btn_InsertTakenDate", "Generic_Insert" },
            { "btn_LocationData_All", "Generic_All" },
            { "btn_LocationData_None", "Generic_None" },
            { "btn_No", "Generic_No" },
            { "btn_OK", "Generic_OK" },
            { "btn_PleaseWait", "Generic_PleaseWait" },
            { "btn_Rename", "Generic_Rename" },
            { "btn_Save", "Generic_Save" },
            { "btn_SaveDefaults", "Generic_SaveDefaults" },
            { "btn_Yes", "Generic_Yes" },
            { "ckb_City", "Generic_City" },
            { "ckb_Country", "Generic_Country" },
            { "ckb_CountryCode", "Generic_CountryCode" },
            { "ckb_CreateDate", "Generic_CreateDate" },
            { "ckb_GPSAltitude", "Generic_GPSAltitude" },
            { "ckb_GPSDestLatitude", "Generic_GPSDestLatitude" },
            { "ckb_GPSDestLongitude", "Generic_GPSDestLongitude" },
            { "ckb_GPSImgDirection", "Generic_GPSImgDirection" },
            { "ckb_GPSLatitude", "Generic_GPSLatitude" },
            { "ckb_GPSLongitude", "Generic_GPSLongitude" },
            { "ckb_GPSSpeed", "Generic_GPSSpeed" },
            { "ckb_QuestionDontAskAgain", "Generic_QuestionDontAskAgain" },
            { "ckb_ShowPassword_ARCGIS_APIKey", "Generic_ShowPassword" },
            { "ckb_ShowPassword_GeoNames", "Generic_ShowPassword" },
            { "ckb_State", "Generic_State" },
            { "ckb_Sublocation", "Generic_Sublocation" },
            { "ckb_TakenDate", "Generic_TakenDate" },
            { "ckb_UseDST", "Generic_UseDST" },
            { "clh_City", "Generic_City" },
            { "clh_Coordinates", "Generic_Coordinates" },
            { "clh_Country", "Generic_Country" },
            { "clh_CountryCode", "Generic_CountryCode" },
            { "clh_CreateDate", "Generic_CreateDate" },
            { "clh_DestCoordinates", "Generic_DestCoordinates" },
            { "clh_ExposureTime", "Generic_ExposureTime" },
            { "clh_FileModifyDate", "Generic_FileModifyDate" },
            { "clh_FileName", "Generic_FileName" },
            { "clh_Fnumber", "Generic_Fnumber" },
            { "clh_FocalLength", "Generic_FocalLength" },
            { "clh_FocalLengthIn35mmFormat", "Generic_FocalLengthIn35mmFormat" },
            { "clh_GPSAltitude", "Generic_GPSAltitude" },
            { "clh_GPSAltitudeRef", "Generic_GPSAltitudeRef" },
            { "clh_GPSDateTime", "Generic_GPSDateTime" },
            { "clh_GPSDestLatitude", "Generic_GPSDestLatitude" },
            { "clh_GPSDestLatitudeRef", "Generic_GPSDestLatitudeRef" },
            { "clh_GPSDestLongitude", "Generic_GPSDestLongitude" },
            { "clh_GPSDestLongitudeRef", "Generic_GPSDestLongitudeRef" },
            { "clh_GPSImgDirection", "Generic_GPSImgDirection" },
            { "clh_GPSImgDirectionRef", "Generic_GPSImgDirectionRef" },
            { "clh_GPSLatitude", "Generic_GPSLatitude" },
            { "clh_GPSLatitudeRef", "Generic_GPSLatitudeRef" },
            { "clh_GPSLongitude", "Generic_GPSLongitude" },
            { "clh_GPSLongitudeRef", "Generic_GPSLongitudeRef" },
            { "clh_GPSSpeed", "Generic_GPSSpeed" },
            { "clh_GPSSpeedRef", "Generic_GPSSpeedRef" },
            { "clh_GUID", "Generic_GUID" },
            { "clh_IPTCKeywords", "Generic_IPTCKeywords" },
            { "clh_ISO", "Generic_ISO" },
            { "clh_LensSpec", "Generic_LensSpec" },
            { "clh_Make", "Generic_Make" },
            { "clh_Model", "Generic_Model" },
            { "clh_OffsetTime", "Generic_OffsetTime" },
            { "clh_Rating", "Generic_Rating" },
            { "clh_State", "Generic_State" },
            { "clh_Sublocation", "Generic_Sublocation" },
            { "clh_TakenDate", "Generic_TakenDate" },
            { "clh_XMLSubjects", "Generic_XMLSubjects" },
            { "clh_GPSDOP", "Generic_GPSDOP" },
            { "clh_GPSHPositioningError", "Generic_GPSHPositioningError" },
            { "clh_Distance", "Generic_Distance" },
            { "clh_timezoneId", "Generic_TimeZoneID" },
            { "lbl_City", "Generic_City" },
            { "lbl_Country", "Generic_Country" },
            { "lbl_CountryCode", "Generic_CountryCode" },
            { "lbl_CreateDateDaysShift", "Generic_Days" },
            { "lbl_CreateDateHoursShift", "Generic_Hours" },
            { "lbl_CreateDateMinutesShift", "Generic_Minutes" },
            { "lbl_CreateDateSecondsShift", "Generic_Seconds" },
            { "lbl_Decimal", "Generic_Decimal" },
            { "lbl_Favourites", "Generic_Favourites" },
            { "lbl_Feet", "Generic_Feet" },
            { "lbl_Feet_Abbr", "Generic_Feet_Abbr" },
            { "lbl_GPSAltitude", "Generic_GPSAltitude" },
            { "lbl_GPSAltitudeRef", "Generic_GPSAltitudeRef" },
            { "lbl_GPSDestLatitude", "Generic_GPSDestLatitude" },
            { "lbl_GPSDestLatitudeRef", "Generic_GPSDestLatitudeRef" },
            { "lbl_GPSDestLongitude", "Generic_GPSDestLongitude" },
            { "lbl_GPSDestLongitudeRef", "Generic_GPSDestLongitudeRef" },
            { "lbl_GPSImgDirection", "Generic_GPSImgDirection" },
            { "lbl_GPSImgDirectionRef", "Generic_GPSImgDirectionRef" },
            { "lbl_GPSLatitude", "Generic_GPSLatitude" },
            { "lbl_GPSLatitudeRef", "Generic_GPSLatitudeRef" },
            { "lbl_GPSLongitude", "Generic_GPSLongitude" },
            { "lbl_GPSLongitudeRef", "Generic_GPSLongitudeRef" },
            { "lbl_GPSSpeed", "Generic_GPSSpeed" },
            { "lbl_GPSSpeedRef", "Generic_GPSSpeedRef" },
            { "lbl_ImportExportGpxDays", "Generic_Days" },
            { "lbl_ImportExportGpxHours", "Generic_Hours" },
            { "lbl_ImportExportGpxMinutes", "Generic_Minutes" },
            { "lbl_ImportExportGpxSeconds", "Generic_Seconds" },
            { "lbl_lat", "Generic_GPSLatitude" },
            { "lbl_lng", "Generic_GPSLongitude" },
            { "lbl_Metres", "Generic_Metres" },
            { "lbl_Metres_Abbr", "Generic_Metres_Abbr" },
            { "lbl_Miles", "Generic_Miles" },
            { "lbl_State", "Generic_State" },
            { "lbl_Sublocation", "Generic_Sublocation" },
            { "lbl_TakenDateDaysShift", "Generic_Days" },
            { "lbl_TakenDateHoursShift", "Generic_Hours" },
            { "lbl_TakenDateMinutesShift", "Generic_Minutes" },
            { "lbl_TakenDateSecondsShift", "Generic_Seconds" },
            { "lbl_ImportGPXDays", "Generic_Days" },
            { "lbl_ImportGPXHours", "Generic_Hours" },
            { "lbl_ImportGPXMinutes", "Generic_Minutes" },
            { "lbl_ImportGPXSeconds", "Generic_Seconds" }
        };

        bool isInDictionary = lookupDictionary.TryGetValue(key: controlName, value: out string retValue);
        return isInDictionary ? retValue : HelperVariables.ControlItemNameNotGeneric;
    }

}
