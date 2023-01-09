using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace GeoTagNinja;

internal static class AncillaryListsArrays
{
    internal static List<KeyValuePair<string, string>> CommonNamesKvp = new();
    internal static readonly string[] CityNameIsAdminName1Arr = { "LIE", "SMR", "MNE", "MKD", "MLT", "SVN" };
    internal static readonly string[] CityNameIsAdminName2Arr = { "ALA", "BRA", "COL", "CUB", "CYP", "DNK", "FRO", "GTM", "HND", "HRV", "ISL", "LUX", "LVA", "NIC", "NLD", "NOR", "PRI", "PRT", "ROU", "SWE" };
    internal static readonly string[] CityNameIsAdminName3Arr = { "AUT", "CHE", "CHL", "CZE", "EST", "ESP", "FIN", "GRC", "ITA", "PAN", "PER", "POL", "SRB", "SVK", "USA", "ZAF" };
    internal static readonly string[] CityNameIsAdminName4Arr = { "BEL", "DEU", "FRA", "GUF", "GLP", "MTQ" };

    #region Time zones

    internal static string[] GetTimeZones()
    {
        string[] result =
        {
            // via https://en.wikipedia.org/w/index.php?title=List_of_tz_database_time_zones&oldid=1119058681	
            "(+00:00/+00:00) # Africa/Abidjan",
            "(+00:00/+00:00) # Africa/Accra",
            "(+03:00/+03:00) # Africa/Addis_Ababa",
            "(+01:00/+01:00) # Africa/Algiers",
            "(+03:00/+03:00) # Africa/Asmera",
            "(+00:00/+00:00) # Africa/Bamako",
            "(+01:00/+01:00) # Africa/Bangui",
            "(+00:00/+00:00) # Africa/Banjul",
            "(+00:00/+00:00) # Africa/Bissau",
            "(+02:00/+02:00) # Africa/Blantyre",
            "(+01:00/+01:00) # Africa/Brazzaville",
            "(+02:00/+02:00) # Africa/Bujumbura",
            "(+02:00/+02:00) # Africa/Cairo",
            "(+01:00/+00:00) # Africa/Casablanca",
            "(+00:00/+00:00) # Africa/Conakry",
            "(+00:00/+00:00) # Africa/Dakar",
            "(+03:00/+03:00) # Africa/Dar_es_Salaam",
            "(+03:00/+03:00) # Africa/Djibouti",
            "(+01:00/+01:00) # Africa/Douala",
            "(+01:00/+00:00) # Africa/El_Aaiun",
            "(+00:00/+00:00) # Africa/Freetown",
            "(+02:00/+02:00) # Africa/Gaborone",
            "(+02:00/+02:00) # Africa/Harare",
            "(+02:00/+02:00) # Africa/Johannesburg",
            "(+02:00/+02:00) # Africa/Juba",
            "(+03:00/+03:00) # Africa/Kampala",
            "(+02:00/+02:00) # Africa/Khartoum",
            "(+02:00/+02:00) # Africa/Kigali",
            "(+01:00/+01:00) # Africa/Kinshasa",
            "(+01:00/+01:00) # Africa/Lagos",
            "(+01:00/+01:00) # Africa/Libreville",
            "(+00:00/+00:00) # Africa/Lome",
            "(+01:00/+01:00) # Africa/Luanda",
            "(+02:00/+02:00) # Africa/Lubumbashi",
            "(+02:00/+02:00) # Africa/Lusaka",
            "(+01:00/+01:00) # Africa/Malabo",
            "(+02:00/+02:00) # Africa/Maputo",
            "(+02:00/+02:00) # Africa/Maseru",
            "(+02:00/+02:00) # Africa/Mbabane",
            "(+03:00/+03:00) # Africa/Mogadishu",
            "(+00:00/+00:00) # Africa/Monrovia",
            "(+03:00/+03:00) # Africa/Nairobi",
            "(+01:00/+01:00) # Africa/Ndjamena",
            "(+01:00/+01:00) # Africa/Niamey",
            "(+00:00/+00:00) # Africa/Nouakchott",
            "(+00:00/+00:00) # Africa/Ouagadougou",
            "(+01:00/+01:00) # Africa/Porto-Novo",
            "(+00:00/+00:00) # Africa/Sao_Tome",
            "(+02:00/+02:00) # Africa/Tripoli",
            "(+01:00/+01:00) # Africa/Tunis",
            "(+02:00/+02:00) # Africa/Windhoek",
            "(-10:00/-09:00) # America/Adak",
            "(-09:00/-08:00) # America/Anchorage",
            "(-04:00/-04:00) # America/Anguilla",
            "(-04:00/-04:00) # America/Antigua",
            "(-03:00/-03:00) # America/Araguaina",
            "(-04:00/-04:00) # America/Aruba",
            "(-04:00/-03:00) # America/Asuncion",
            "(-03:00/-03:00) # America/Bahia",
            "(-04:00/-04:00) # America/Barbados",
            "(-06:00/-06:00) # America/Belize",
            "(-04:00/-04:00) # America/Blanc-Sablon",
            "(-05:00/-05:00) # America/Bogota",
            "(-03:00/-03:00) # America/Buenos_Aires",
            "(-05:00/-05:00) # America/Cancun",
            "(-04:00/-04:00) # America/Caracas",
            "(-03:00/-03:00) # America/Cayenne",
            "(-05:00/-05:00) # America/Cayman",
            "(-06:00/-05:00) # America/Chicago",
            "(-06:00/-06:00) # America/Chihuahua",
            "(-05:00/-05:00) # America/Coral_Harbour",
            "(-06:00/-06:00) # America/Costa_Rica",
            "(-04:00/-04:00) # America/Cuiaba",
            "(-04:00/-04:00) # America/Curacao",
            "(+00:00/+00:00) # America/Danmarkshavn",
            "(-07:00/-06:00) # America/Denver",
            "(-04:00/-04:00) # America/Dominica",
            "(-06:00/-06:00) # America/El_Salvador",
            "(-03:00/-02:00) # America/Godthab",
            "(-05:00/-04:00) # America/Grand_Turk",
            "(-04:00/-04:00) # America/Grenada",
            "(-04:00/-04:00) # America/Guadeloupe",
            "(-06:00/-06:00) # America/Guatemala",
            "(-05:00/-05:00) # America/Guayaquil",
            "(-04:00/-04:00) # America/Guyana",
            "(-04:00/-03:00) # America/Halifax",
            "(-05:00/-04:00) # America/Havana",
            "(-07:00/-07:00) # America/Hermosillo",
            "(-05:00/-04:00) # America/Indianapolis",
            "(-05:00/-05:00) # America/Jamaica",
            "(-04:00/-04:00) # America/Kralendijk",
            "(-04:00/-04:00) # America/La_Paz",
            "(-05:00/-05:00) # America/Lima",
            "(-08:00/-07:00) # America/Los_Angeles",
            "(-04:00/-04:00) # America/Lower_Princes",
            "(-06:00/-06:00) # America/Managua",
            "(-04:00/-04:00) # America/Marigot",
            "(-04:00/-04:00) # America/Martinique",
            "(-06:00/-05:00) # America/Matamoros",
            "(-06:00/-06:00) # America/Mexico_City",
            "(-03:00/-02:00) # America/Miquelon",
            "(-03:00/-03:00) # America/Montevideo",
            "(-04:00/-04:00) # America/Montserrat",
            "(-05:00/-04:00) # America/Nassau",
            "(-05:00/-04:00) # America/New_York",
            "(-02:00/-02:00) # America/Noronha",
            "(-06:00/-06:00) # America/Ojinaga",
            "(-05:00/-05:00) # America/Panama",
            "(-03:00/-03:00) # America/Paramaribo",
            "(-07:00/-07:00) # America/Phoenix",
            "(-05:00/-04:00) # America/Port-au-Prince",
            "(-04:00/-04:00) # America/Port_of_Spain",
            "(-04:00/-04:00) # America/Puerto_Rico",
            "(-03:00/-03:00) # America/Punta_Arenas",
            "(-06:00/-06:00) # America/Regina",
            "(-04:00/-03:00) # America/Santiago",
            "(-04:00/-04:00) # America/Santo_Domingo",
            "(-03:00/-03:00) # America/Sao_Paulo",
            "(-01:00/+00:00) # America/Scoresbysund",
            "(-04:00/-04:00) # America/St_Barthelemy",
            "(-03:30/-02:30) # America/St_Johns",
            "(-04:00/-04:00) # America/St_Kitts",
            "(-04:00/-04:00) # America/St_Lucia",
            "(-04:00/-04:00) # America/St_Thomas",
            "(-04:00/-04:00) # America/St_Vincent",
            "(-06:00/-06:00) # America/Tegucigalpa",
            "(-04:00/-03:00) # America/Thule",
            "(-08:00/-07:00) # America/Tijuana",
            "(-04:00/-04:00) # America/Tortola",
            "(-08:00/-07:00) # America/Vancouver",
            "(-07:00/-07:00) # America/Whitehorse",
            "(+11:00/+11:00) # Antarctica/Casey",
            "(+07:00/+07:00) # Antarctica/Davis",
            "(+10:00/+10:00) # Antarctica/DumontDUrville",
            "(+05:00/+05:00) # Antarctica/Mawson",
            "(+12:00/+13:00) # Antarctica/McMurdo",
            "(+03:00/+03:00) # Antarctica/Syowa",
            "(+06:00/+06:00) # Antarctica/Vostok",
            "(+01:00/+02:00) # Arctic/Longyearbyen",
            "(+03:00/+03:00) # Asia/Aden",
            "(+06:00/+06:00) # Asia/Almaty",
            "(+03:00/+03:00) # Asia/Amman",
            "(+05:00/+05:00) # Asia/Ashgabat",
            "(+03:00/+03:00) # Asia/Baghdad",
            "(+03:00/+03:00) # Asia/Bahrain",
            "(+04:00/+04:00) # Asia/Baku",
            "(+07:00/+07:00) # Asia/Bangkok",
            "(+07:00/+07:00) # Asia/Barnaul",
            "(+02:00/+03:00) # Asia/Beirut",
            "(+06:00/+06:00) # Asia/Bishkek",
            "(+08:00/+08:00) # Asia/Brunei",
            "(+05:30/+05:30) # Asia/Calcutta",
            "(+09:00/+09:00) # Asia/Chita",
            "(+05:30/+05:30) # Asia/Colombo",
            "(+03:00/+03:00) # Asia/Damascus",
            "(+06:00/+06:00) # Asia/Dhaka",
            "(+09:00/+09:00) # Asia/Dili",
            "(+04:00/+04:00) # Asia/Dubai",
            "(+05:00/+05:00) # Asia/Dushanbe",
            "(+02:00/+03:00) # Asia/Hebron",
            "(+08:00/+08:00) # Asia/Hong_Kong",
            "(+07:00/+07:00) # Asia/Hovd",
            "(+08:00/+08:00) # Asia/Irkutsk",
            "(+09:00/+09:00) # Asia/Jayapura",
            "(+02:00/+03:00) # Asia/Jerusalem",
            "(+04:30/+04:30) # Asia/Kabul",
            "(+12:00/+12:00) # Asia/Kamchatka",
            "(+05:00/+05:00) # Asia/Karachi",
            "(+05:45/+05:45) # Asia/Katmandu",
            "(+07:00/+07:00) # Asia/Krasnoyarsk",
            "(+03:00/+03:00) # Asia/Kuwait",
            "(+08:00/+08:00) # Asia/Macau",
            "(+11:00/+11:00) # Asia/Magadan",
            "(+08:00/+08:00) # Asia/Makassar",
            "(+08:00/+08:00) # Asia/Manila",
            "(+04:00/+04:00) # Asia/Muscat",
            "(+07:00/+07:00) # Asia/Novosibirsk",
            "(+06:00/+06:00) # Asia/Omsk",
            "(+07:00/+07:00) # Asia/Phnom_Penh",
            "(+09:00/+09:00) # Asia/Pyongyang",
            "(+03:00/+03:00) # Asia/Qatar",
            "(+05:00/+05:00) # Asia/Qyzylorda",
            "(+06:30/+06:30) # Asia/Rangoon",
            "(+03:00/+03:00) # Asia/Riyadh",
            "(+07:00/+07:00) # Asia/Saigon",
            "(+11:00/+11:00) # Asia/Sakhalin",
            "(+09:00/+09:00) # Asia/Seoul",
            "(+08:00/+08:00) # Asia/Shanghai",
            "(+08:00/+08:00) # Asia/Singapore",
            "(+11:00/+11:00) # Asia/Srednekolymsk",
            "(+08:00/+08:00) # Asia/Taipei",
            "(+05:00/+05:00) # Asia/Tashkent",
            "(+04:00/+04:00) # Asia/Tbilisi",
            "(+03:30/+03:30) # Asia/Tehran",
            "(+06:00/+06:00) # Asia/Thimphu",
            "(+09:00/+09:00) # Asia/Tokyo",
            "(+07:00/+07:00) # Asia/Tomsk",
            "(+08:00/+08:00) # Asia/Ulaanbaatar",
            "(+06:00/+06:00) # Asia/Urumqi",
            "(+07:00/+07:00) # Asia/Vientiane",
            "(+10:00/+10:00) # Asia/Vladivostok",
            "(+09:00/+09:00) # Asia/Yakutsk",
            "(+05:00/+05:00) # Asia/Yekaterinburg",
            "(+04:00/+04:00) # Asia/Yerevan",
            "(-01:00/+00:00) # Atlantic/Azores",
            "(-04:00/-03:00) # Atlantic/Bermuda",
            "(+00:00/+01:00) # Atlantic/Canary",
            "(-01:00/-01:00) # Atlantic/Cape_Verde",
            "(+00:00/+01:00) # Atlantic/Faeroe",
            "(+00:00/+00:00) # Atlantic/Reykjavik",
            "(-02:00/-02:00) # Atlantic/South_Georgia",
            "(+00:00/+00:00) # Atlantic/St_Helena",
            "(-03:00/-03:00) # Atlantic/Stanley",
            "(+09:30/+10:30) # Australia/Adelaide",
            "(+10:00/+10:00) # Australia/Brisbane",
            "(+09:30/+09:30) # Australia/Darwin",
            "(+08:45/+08:45) # Australia/Eucla",
            "(+10:00/+11:00) # Australia/Hobart",
            "(+10:30/+11:00) # Australia/Lord_Howe",
            "(+08:00/+08:00) # Australia/Perth",
            "(+10:00/+11:00) # Australia/Sydney",
            "(-06:00/-05:00) # CST6CDT",
            "(-05:00/-04:00) # EST5EDT",
            "(-01:00/-01:00) # Etc/GMT+1",
            "(-10:00/-10:00) # Etc/GMT+10",
            "(-11:00/-11:00) # Etc/GMT+11",
            "(-12:00/-12:00) # Etc/GMT+12",
            "(-02:00/-02:00) # Etc/GMT+2",
            "(-03:00/-03:00) # Etc/GMT+3",
            "(-04:00/-04:00) # Etc/GMT+4",
            "(-05:00/-05:00) # Etc/GMT+5",
            "(-06:00/-06:00) # Etc/GMT+6",
            "(-07:00/-07:00) # Etc/GMT+7",
            "(-08:00/-08:00) # Etc/GMT+8",
            "(-09:00/-09:00) # Etc/GMT+9",
            "(+01:00/+01:00) # Etc/GMT-1",
            "(+10:00/+10:00) # Etc/GMT-10",
            "(+11:00/+11:00) # Etc/GMT-11",
            "(+12:00/+12:00) # Etc/GMT-12",
            "(+13:00/+13:00) # Etc/GMT-13",
            "(+14:00/+14:00) # Etc/GMT-14",
            "(+02:00/+02:00) # Etc/GMT-2",
            "(+03:00/+03:00) # Etc/GMT-3",
            "(+04:00/+04:00) # Etc/GMT-4",
            "(+05:00/+05:00) # Etc/GMT-5",
            "(+06:00/+06:00) # Etc/GMT-6",
            "(+07:00/+07:00) # Etc/GMT-7",
            "(+08:00/+08:00) # Etc/GMT-8",
            "(+09:00/+09:00) # Etc/GMT-9",
            "(+00:00/+00:00) # Etc/UTC",
            "(+01:00/+02:00) # Europe/Amsterdam",
            "(+01:00/+02:00) # Europe/Andorra",
            "(+04:00/+04:00) # Europe/Astrakhan",
            "(+02:00/+03:00) # Europe/Athens",
            "(+01:00/+02:00) # Europe/Belgrade",
            "(+01:00/+02:00) # Europe/Berlin",
            "(+01:00/+02:00) # Europe/Bratislava",
            "(+01:00/+02:00) # Europe/Brussels",
            "(+02:00/+03:00) # Europe/Bucharest",
            "(+01:00/+02:00) # Europe/Budapest",
            "(+02:00/+03:00) # Europe/Chisinau",
            "(+01:00/+02:00) # Europe/Copenhagen",
            "(+01:00/+00:00) # Europe/Dublin",
            "(+01:00/+02:00) # Europe/Gibraltar",
            "(+00:00/+01:00) # Europe/Guernsey",
            "(+02:00/+03:00) # Europe/Helsinki",
            "(+00:00/+01:00) # Europe/Isle_of_Man",
            "(+03:00/+03:00) # Europe/Istanbul",
            "(+00:00/+01:00) # Europe/Jersey",
            "(+02:00/+02:00) # Europe/Kaliningrad",
            "(+02:00/+03:00) # Europe/Kiev",
            "(+01:00/+02:00) # Europe/Ljubljana",
            "(+00:00/+01:00) # Europe/London",
            "(+01:00/+02:00) # Europe/Luxembourg",
            "(+01:00/+02:00) # Europe/Malta",
            "(+02:00/+03:00) # Europe/Mariehamn",
            "(+03:00/+03:00) # Europe/Minsk",
            "(+01:00/+02:00) # Europe/Monaco",
            "(+03:00/+03:00) # Europe/Moscow",
            "(+01:00/+02:00) # Europe/Oslo",
            "(+01:00/+02:00) # Europe/Paris",
            "(+01:00/+02:00) # Europe/Podgorica",
            "(+01:00/+02:00) # Europe/Prague",
            "(+02:00/+03:00) # Europe/Riga",
            "(+01:00/+02:00) # Europe/Rome",
            "(+04:00/+04:00) # Europe/Samara",
            "(+01:00/+02:00) # Europe/San_Marino",
            "(+01:00/+02:00) # Europe/Sarajevo",
            "(+04:00/+04:00) # Europe/Saratov",
            "(+03:00/+03:00) # Europe/Simferopol",
            "(+01:00/+02:00) # Europe/Skopje",
            "(+02:00/+03:00) # Europe/Sofia",
            "(+01:00/+02:00) # Europe/Stockholm",
            "(+02:00/+03:00) # Europe/Tallinn",
            "(+01:00/+02:00) # Europe/Tirane",
            "(+01:00/+02:00) # Europe/Vaduz",
            "(+01:00/+02:00) # Europe/Vatican",
            "(+01:00/+02:00) # Europe/Vienna",
            "(+02:00/+03:00) # Europe/Vilnius",
            "(+03:00/+03:00) # Europe/Volgograd",
            "(+01:00/+02:00) # Europe/Warsaw",
            "(+01:00/+02:00) # Europe/Zagreb",
            "(+01:00/+02:00) # Europe/Zurich",
            "(+03:00/+03:00) # Indian/Antananarivo",
            "(+06:00/+06:00) # Indian/Chagos",
            "(+07:00/+07:00) # Indian/Christmas",
            "(+06:30/+06:30) # Indian/Cocos",
            "(+03:00/+03:00) # Indian/Comoro",
            "(+05:00/+05:00) # Indian/Kerguelen",
            "(+04:00/+04:00) # Indian/Mahe",
            "(+05:00/+05:00) # Indian/Maldives",
            "(+04:00/+04:00) # Indian/Mauritius",
            "(+03:00/+03:00) # Indian/Mayotte",
            "(+04:00/+04:00) # Indian/Reunion",
            "(-07:00/-06:00) # MST7MDT",
            "(+13:00/+13:00) # Pacific/Apia",
            "(+12:00/+13:00) # Pacific/Auckland",
            "(+11:00/+11:00) # Pacific/Bougainville",
            "(+12:45/+13:45) # Pacific/Chatham",
            "(-06:00/-05:00) # Pacific/Easter",
            "(+11:00/+11:00) # Pacific/Efate",
            "(+13:00/+13:00) # Pacific/Enderbury",
            "(+13:00/+13:00) # Pacific/Fakaofo",
            "(+12:00/+12:00) # Pacific/Fiji",
            "(+12:00/+12:00) # Pacific/Funafuti",
            "(-06:00/-06:00) # Pacific/Galapagos",
            "(-09:00/-09:00) # Pacific/Gambier",
            "(+11:00/+11:00) # Pacific/Guadalcanal",
            "(+10:00/+10:00) # Pacific/Guam",
            "(-10:00/-10:00) # Pacific/Honolulu",
            "(-10:00/-10:00) # Pacific/Johnston",
            "(+14:00/+14:00) # Pacific/Kiritimati",
            "(-09:30/-09:30) # Pacific/Marquesas",
            "(-11:00/-11:00) # Pacific/Midway",
            "(+12:00/+12:00) # Pacific/Nauru",
            "(-11:00/-11:00) # Pacific/Niue",
            "(+11:00/+12:00) # Pacific/Norfolk",
            "(+11:00/+11:00) # Pacific/Noumea",
            "(-11:00/-11:00) # Pacific/Pago_Pago",
            "(+09:00/+09:00) # Pacific/Palau",
            "(-08:00/-08:00) # Pacific/Pitcairn",
            "(+10:00/+10:00) # Pacific/Port_Moresby",
            "(-10:00/-10:00) # Pacific/Rarotonga",
            "(+10:00/+10:00) # Pacific/Saipan",
            "(-10:00/-10:00) # Pacific/Tahiti",
            "(+12:00/+12:00) # Pacific/Tarawa",
            "(+13:00/+13:00) # Pacific/Tongatapu",
            "(+10:00/+10:00) # Pacific/Truk",
            "(+12:00/+12:00) # Pacific/Wake",
            "(+12:00/+12:00) # Pacific/Wallis",
            "(-08:00/-07:00) # PST8PDT"
        };
        Array.Sort(array: result);
        return result;
    }

    #endregion

    #region Languages

    internal static List<KeyValuePair<string, string>> GetISO_639_1_Languages()
    {
        List<KeyValuePair<string, string>> result = new()
        {
            new KeyValuePair<string, string>(key: "aa", value: "Afaraf [Afar]"),
            new KeyValuePair<string, string>(key: "ab", value: "аҧсуа бызшәа [Abkhaz]"),
            new KeyValuePair<string, string>(key: "ae", value: "avesta [Avestan]"),
            new KeyValuePair<string, string>(key: "af", value: "Afrikaans [Afrikaans]"),
            new KeyValuePair<string, string>(key: "ak", value: "Akan [Akan]"),
            new KeyValuePair<string, string>(key: "am", value: "አማርኛ [Amharic]"),
            new KeyValuePair<string, string>(key: "an", value: "aragonés [Aragonese]"),
            new KeyValuePair<string, string>(key: "ar", value: "اللغة العربية [Arabic]"),
            new KeyValuePair<string, string>(key: "as", value: "অসমীয়া [Assamese]"),
            new KeyValuePair<string, string>(key: "av", value: "авар мацӀ [Avaric]"),
            new KeyValuePair<string, string>(key: "ay", value: "aymar aru [Aymara]"),
            new KeyValuePair<string, string>(key: "az", value: "azərbaycan dili [Azerbaijani]"),
            new KeyValuePair<string, string>(key: "ba", value: "башҡорт теле [Bashkir]"),
            new KeyValuePair<string, string>(key: "be", value: "беларуская мова [Belarusian]"),
            new KeyValuePair<string, string>(key: "bg", value: "български език [Bulgarian]"),
            new KeyValuePair<string, string>(key: "bh", value: "भोजपुरी [Bihari]"),
            new KeyValuePair<string, string>(key: "bi", value: "Bislama [Bislama]"),
            new KeyValuePair<string, string>(key: "bm", value: "bamanankan [Bambara]"),
            new KeyValuePair<string, string>(key: "bn", value: "বাংলা [Bengali]"),
            new KeyValuePair<string, string>(key: "bo", value: "བོད་ཡིག [Tibetan]"),
            new KeyValuePair<string, string>(key: "br", value: "brezhoneg [Breton]"),
            new KeyValuePair<string, string>(key: "bs", value: "bosanski jezik [Bosnian]"),
            new KeyValuePair<string, string>(key: "ca", value: "Català [Catalan]"),
            new KeyValuePair<string, string>(key: "ce", value: "нохчийн мотт [Chechen]"),
            new KeyValuePair<string, string>(key: "ch", value: "Chamoru [Chamorro]"),
            new KeyValuePair<string, string>(key: "co", value: "corsu [Corsican]"),
            new KeyValuePair<string, string>(key: "cr", value: "ᓀᐦᐃᔭᐍᐏᐣ [Cree]"),
            new KeyValuePair<string, string>(key: "cs", value: "čeština [Czech]"),
            new KeyValuePair<string, string>(key: "cu", value: "ѩзыкъ словѣньскъ [Old Church Slavonic]"),
            new KeyValuePair<string, string>(key: "cv", value: "чӑваш чӗлхи [Chuvash]"),
            new KeyValuePair<string, string>(key: "cy", value: "Cymraeg [Welsh]"),
            new KeyValuePair<string, string>(key: "da", value: "dansk [Danish]"),
            new KeyValuePair<string, string>(key: "de", value: "Deutsch [German]"),
            new KeyValuePair<string, string>(key: "dv", value: "Dhivehi [Divehi]"),
            new KeyValuePair<string, string>(key: "dz", value: "རྫོང་ཁ [Dzongkha]"),
            new KeyValuePair<string, string>(key: "ee", value: "Eʋegbe [Ewe]"),
            new KeyValuePair<string, string>(key: "el", value: "Ελληνικά [Greek]"),
            new KeyValuePair<string, string>(key: "en", value: "English [English]"),
            new KeyValuePair<string, string>(key: "eo", value: "Esperanto [Esperanto]"),
            new KeyValuePair<string, string>(key: "es", value: "Español [Spanish]"),
            new KeyValuePair<string, string>(key: "et", value: "eesti [Estonian]"),
            new KeyValuePair<string, string>(key: "eu", value: "euskara [Basque]"),
            new KeyValuePair<string, string>(key: "fa", value: "فارسی [Persian]"),
            new KeyValuePair<string, string>(key: "ff", value: "Fulfulde [Fula]"),
            new KeyValuePair<string, string>(key: "fi", value: "suomi [Finnish]"),
            new KeyValuePair<string, string>(key: "fj", value: "Vakaviti [Fijian]"),
            new KeyValuePair<string, string>(key: "fo", value: "føroyskt [Faroese]"),
            new KeyValuePair<string, string>(key: "fr", value: "Français [French]"),
            new KeyValuePair<string, string>(key: "fy", value: "Frysk [Western Frisian]"),
            new KeyValuePair<string, string>(key: "ga", value: "Gaeilge [Irish]"),
            new KeyValuePair<string, string>(key: "gd", value: "Gàidhlig [Scottish Gaelic]"),
            new KeyValuePair<string, string>(key: "gl", value: "galego [Galician]"),
            new KeyValuePair<string, string>(key: "gu", value: "ગુજરાતી [Gujarati]"),
            new KeyValuePair<string, string>(key: "gv", value: "Gaelg [Manx]"),
            new KeyValuePair<string, string>(key: "ha", value: "هَوُسَ [Hausa]"),
            new KeyValuePair<string, string>(key: "he", value: "עברית [Hebrew]"),
            new KeyValuePair<string, string>(key: "hi", value: "हिन्दी [Hindi]"),
            new KeyValuePair<string, string>(key: "ho", value: "Hiri Motu [Hiri Motu]"),
            new KeyValuePair<string, string>(key: "hr", value: "Hrvatski [Croatian]"),
            new KeyValuePair<string, string>(key: "ht", value: "Kreyòl ayisyen [Haitian]"),
            new KeyValuePair<string, string>(key: "hu", value: "magyar [Hungarian]"),
            new KeyValuePair<string, string>(key: "hy", value: "Հայերեն [Armenian]"),
            new KeyValuePair<string, string>(key: "hz", value: "Otjiherero [Herero]"),
            new KeyValuePair<string, string>(key: "ia", value: "Interlingua [Interlingua]"),
            new KeyValuePair<string, string>(key: "id", value: "Bahasa Indonesia [Indonesian]"),
            new KeyValuePair<string, string>(key: "ie", value: "Interlingue [Interlingue]"),
            new KeyValuePair<string, string>(key: "ig", value: "Asụsụ Igbo [Igbo]"),
            new KeyValuePair<string, string>(key: "ii", value: "ꆈꌠ꒿ Nuosuhxop [Nuosu]"),
            new KeyValuePair<string, string>(key: "ik", value: "Iñupiaq [Inupiaq]"),
            new KeyValuePair<string, string>(key: "io", value: "Ido [Ido]"),
            new KeyValuePair<string, string>(key: "is", value: "Íslenska [Icelandic]"),
            new KeyValuePair<string, string>(key: "it", value: "Italiano [Italian]"),
            new KeyValuePair<string, string>(key: "iu", value: "ᐃᓄᒃᑎᑐᑦ [Inuktitut]"),
            new KeyValuePair<string, string>(key: "ja", value: "日本語 [Japanese]"),
            new KeyValuePair<string, string>(key: "jv", value: "basa Jawa [Javanese]"),
            new KeyValuePair<string, string>(key: "ka", value: "ქართული [Georgian]"),
            new KeyValuePair<string, string>(key: "kg", value: "Kikongo [Kongo]"),
            new KeyValuePair<string, string>(key: "ki", value: "Gĩkũyũ [Kikuyu]"),
            new KeyValuePair<string, string>(key: "kj", value: "Kuanyama [Kwanyama]"),
            new KeyValuePair<string, string>(key: "kk", value: "қазақ тілі [Kazakh]"),
            new KeyValuePair<string, string>(key: "kl", value: "kalaallisut [Kalaallisut]"),
            new KeyValuePair<string, string>(key: "km", value: "ខេមរភាសា [Khmer]"),
            new KeyValuePair<string, string>(key: "kn", value: "ಕನ್ನಡ [Kannada]"),
            new KeyValuePair<string, string>(key: "ko", value: "한국어 [Korean]"),
            new KeyValuePair<string, string>(key: "kr", value: "Kanuri [Kanuri]"),
            new KeyValuePair<string, string>(key: "ks", value: "कश्मीरी [Kashmiri]"),
            new KeyValuePair<string, string>(key: "ku", value: "Kurdî [Kurdish]"),
            new KeyValuePair<string, string>(key: "kv", value: "коми кыв [Komi]"),
            new KeyValuePair<string, string>(key: "kw", value: "Kernewek [Cornish]"),
            new KeyValuePair<string, string>(key: "ky", value: "Кыргызча [Kyrgyz]"),
            new KeyValuePair<string, string>(key: "la", value: "latine [Latin]"),
            new KeyValuePair<string, string>(key: "lb", value: "Lëtzebuergesch [Luxembourgish]"),
            new KeyValuePair<string, string>(key: "lg", value: "Luganda [Ganda]"),
            new KeyValuePair<string, string>(key: "li", value: "Limburgs [Limburgish]"),
            new KeyValuePair<string, string>(key: "ln", value: "Lingála [Lingala]"),
            new KeyValuePair<string, string>(key: "lo", value: "ພາສາ [Lao]"),
            new KeyValuePair<string, string>(key: "lt", value: "lietuvių kalba [Lithuanian]"),
            new KeyValuePair<string, string>(key: "lu", value: "Tshiluba [Luba-Katanga]"),
            new KeyValuePair<string, string>(key: "lv", value: "latviešu valoda [Latvian]"),
            new KeyValuePair<string, string>(key: "mg", value: "fiteny malagasy [Malagasy]"),
            new KeyValuePair<string, string>(key: "mh", value: "Kajin M̧ajeļ [Marshallese]"),
            new KeyValuePair<string, string>(key: "mi", value: "te reo Māori [Māori]"),
            new KeyValuePair<string, string>(key: "mk", value: "македонски јазик [Macedonian]"),
            new KeyValuePair<string, string>(key: "ml", value: "മലയാളം [Malayalam]"),
            new KeyValuePair<string, string>(key: "mn", value: "Монгол хэл [Mongolian]"),
            new KeyValuePair<string, string>(key: "mr", value: "मराठी [Marathi]"),
            new KeyValuePair<string, string>(key: "ms", value: "Bahasa Malaysia [Malay]"),
            new KeyValuePair<string, string>(key: "mt", value: "Malti [Maltese]"),
            new KeyValuePair<string, string>(key: "my", value: "ဗမာစာ [Burmese]"),
            new KeyValuePair<string, string>(key: "na", value: "Ekakairũ Naoero [Nauru]"),
            new KeyValuePair<string, string>(key: "nb", value: "Norsk bokmål [Norwegian Bokmål]"),
            new KeyValuePair<string, string>(key: "nd", value: "isiNdebele [Northern Ndebele]"),
            new KeyValuePair<string, string>(key: "ne", value: "नेपाली [Nepali]"),
            new KeyValuePair<string, string>(key: "ng", value: "Owambo [Ndonga]"),
            new KeyValuePair<string, string>(key: "nl", value: "Nederlands [Dutch]"),
            new KeyValuePair<string, string>(key: "nn", value: "Norsk nynorsk [Norwegian Nynorsk]"),
            new KeyValuePair<string, string>(key: "no", value: "Norsk [Norwegian]"),
            new KeyValuePair<string, string>(key: "nr", value: "isiNdebele [Southern Ndebele]"),
            new KeyValuePair<string, string>(key: "nv", value: "Diné bizaad [Navajo]"),
            new KeyValuePair<string, string>(key: "ny", value: "chiCheŵa [Chichewa]"),
            new KeyValuePair<string, string>(key: "oc", value: "occitan [Occitan]"),
            new KeyValuePair<string, string>(key: "oj", value: "ᐊᓂᔑᓈᐯᒧᐎᓐ [Ojibwe]"),
            new KeyValuePair<string, string>(key: "om", value: "Afaan Oromoo [Oromo]"),
            new KeyValuePair<string, string>(key: "or", value: "ଓଡ଼ିଆ [Oriya]"),
            new KeyValuePair<string, string>(key: "os", value: "ирон æвзаг [Ossetian]"),
            new KeyValuePair<string, string>(key: "pa", value: "ਪੰਜਾਬੀ [Panjabi]"),
            new KeyValuePair<string, string>(key: "pi", value: "पाऴि [Pāli]"),
            new KeyValuePair<string, string>(key: "pl", value: "Polski [Polish]"),
            new KeyValuePair<string, string>(key: "ps", value: "پښتو [Pashto]"),
            new KeyValuePair<string, string>(key: "pt", value: "Português [Portuguese]"),
            new KeyValuePair<string, string>(key: "qu", value: "Runa Simi [Quechua]"),
            new KeyValuePair<string, string>(key: "rm", value: "rumantsch grischun [Romansh]"),
            new KeyValuePair<string, string>(key: "rn", value: "Ikirundi [Kirundi]"),
            new KeyValuePair<string, string>(key: "ro", value: "Română [Romanian]"),
            new KeyValuePair<string, string>(key: "ru", value: "Русский [Russian]"),
            new KeyValuePair<string, string>(key: "rw", value: "Ikinyarwanda [Kinyarwanda]"),
            new KeyValuePair<string, string>(key: "sa", value: "संस्कृतम् [Sanskrit]"),
            new KeyValuePair<string, string>(key: "sc", value: "sardu [Sardinian]"),
            new KeyValuePair<string, string>(key: "sd", value: "सिन्धी [Sindhi]"),
            new KeyValuePair<string, string>(key: "se", value: "Davvisámegiella [Northern Sami]"),
            new KeyValuePair<string, string>(key: "sg", value: "yângâ tî sängö [Sango]"),
            new KeyValuePair<string, string>(key: "si", value: "සිංහල [Sinhala]"),
            new KeyValuePair<string, string>(key: "sk", value: "slovenčina [Slovak]"),
            new KeyValuePair<string, string>(key: "sl", value: "slovenščina [Slovenian]"),
            new KeyValuePair<string, string>(key: "sn", value: "chiShona [Shona]"),
            new KeyValuePair<string, string>(key: "so", value: "Soomaaliga [Somali]"),
            new KeyValuePair<string, string>(key: "sq", value: "Shqip [Albanian]"),
            new KeyValuePair<string, string>(key: "sr", value: "српски језик [Serbian]"),
            new KeyValuePair<string, string>(key: "ss", value: "SiSwati [Swati]"),
            new KeyValuePair<string, string>(key: "st", value: "Sesotho [Southern Sotho]"),
            new KeyValuePair<string, string>(key: "su", value: "Basa Sunda [Sundanese]"),
            new KeyValuePair<string, string>(key: "sv", value: "Svenska [Swedish]"),
            new KeyValuePair<string, string>(key: "sw", value: "Kiswahili [Swahili]"),
            new KeyValuePair<string, string>(key: "ta", value: "தமிழ் [Tamil]"),
            new KeyValuePair<string, string>(key: "te", value: "తెలుగు [Telugu]"),
            new KeyValuePair<string, string>(key: "tg", value: "тоҷикӣ [Tajik]"),
            new KeyValuePair<string, string>(key: "th", value: "ไทย [Thai]"),
            new KeyValuePair<string, string>(key: "ti", value: "ትግርኛ [Tigrinya]"),
            new KeyValuePair<string, string>(key: "tk", value: "Türkmen [Turkmen]"),
            new KeyValuePair<string, string>(key: "tl", value: "Wikang Tagalog [Tagalog]"),
            new KeyValuePair<string, string>(key: "tn", value: "Setswana [Tswana]"),
            new KeyValuePair<string, string>(key: "to", value: "faka Tonga [Tonga]"),
            new KeyValuePair<string, string>(key: "tr", value: "Türkçe [Turkish]"),
            new KeyValuePair<string, string>(key: "ts", value: "Xitsonga [Tsonga]"),
            new KeyValuePair<string, string>(key: "tt", value: "татар теле [Tatar]"),
            new KeyValuePair<string, string>(key: "tw", value: "Twi [Twi]"),
            new KeyValuePair<string, string>(key: "ty", value: "Reo Tahiti [Tahitian]"),
            new KeyValuePair<string, string>(key: "ug", value: "ئۇيغۇرچە‎ [Uyghur]"),
            new KeyValuePair<string, string>(key: "uk", value: "Українська [Ukrainian]"),
            new KeyValuePair<string, string>(key: "ur", value: "اردو [Urdu]"),
            new KeyValuePair<string, string>(key: "uz", value: "Ўзбек [Uzbek]"),
            new KeyValuePair<string, string>(key: "ve", value: "Tshivenḓa [Venda]"),
            new KeyValuePair<string, string>(key: "vi", value: "Tiếng Việt [Vietnamese]"),
            new KeyValuePair<string, string>(key: "vo", value: "Volapük [Volapük]"),
            new KeyValuePair<string, string>(key: "wa", value: "walon [Walloon]"),
            new KeyValuePair<string, string>(key: "wo", value: "Wollof [Wolof]"),
            new KeyValuePair<string, string>(key: "xh", value: "isiXhosa [Xhosa]"),
            new KeyValuePair<string, string>(key: "yi", value: "ייִדיש [Yiddish]"),
            new KeyValuePair<string, string>(key: "yo", value: "Yorùbá [Yoruba]"),
            new KeyValuePair<string, string>(key: "za", value: "Saɯ cueŋƅ [Zhuang]"),
            new KeyValuePair<string, string>(key: "zh", value: "中文 [Chinese]"),
            new KeyValuePair<string, string>(key: "zu", value: "isiZulu [Zulu]")
        };
        return result;
    }

    #endregion

    #region Columns

    internal static string[] GpxTagsToOverwrite()
    {
        string[] result =
        {
            "GPSAltitude",
            "GPSAltitudeRef",
            "GPSDateStamp",
            "GPSImgDirection",
            "GPSImgDirectionRef",
            "GPSLatitude",
            "GPSLatitudeRef",
            "GPSLongitude",
            "GPSLongitudeRef",
            "GPSSpeed",
            "GPSSpeedRef",
            "GPSTimeStamp",
            "GPSTrack",
            "GPSTrackRef"
        };

        return result;
    }

    internal static string[] ToponomyReplaces()
    {
        string[] result =
            { "City", "State", "Sub_location", "GPSAltitude" };
        return result;
    }

    internal static string[] GetFavouriteTags()
    {
        string[] result =
        {
            "GPSAltitude",
            "GPSAltitudeRef",
            "GPSLatitude",
            "GPSLatitudeRef",
            "GPSLongitude",
            "GPSLongitudeRef",
            "Coordinates",
            "City",
            "CountryCode",
            "Country",
            "State",
            "Sub_location"
        };
        return result;
    }

    internal static string[] CustomRulesDataTargets()
    {
        string[] result =
        {
            "State",
            "City",
            "Sub_location"
        };

        return result;
        ;
    }

    internal static string[] CustomRulesDataSources(bool isOutcome = false)
    {
        string[] result =
        {
            "AdminName1",
            "AdminName2",
            "AdminName3",
            "AdminName4",
            "ToponymName"
        };
        if (isOutcome)
        {
            Array.Resize(array: ref result, newSize: result.Length + 2);
            result[result.Length - 2] = "Null (empty)";
            result[result.Length - 1] = "Custom";
        }

        return result;
        ;
    }

    internal static string[] CustomRulesDataConditions()
    {
        string[] result =
        {
            "Is",
            "Contains",
            "StartsWith",
            "EndsWith"
        };
        return result;
    }

    #endregion

    #region Countries & Country Codes

    internal static string[] GetCountries()
    {
        List<string> retList = new();

        retList.Add(item: "");
        foreach (DataRow dataRow in FrmMainApp.DtIsoCountryCodeMapping.Rows)
        {
            retList.Add(item: dataRow[columnName: "Country"]
                            .ToString());
        }

        return retList.ToArray();
    }

    internal static string[] GetCountryCodes()
    {
        List<string> retList = new();
        retList.Add(item: "");
        foreach (DataRow dataRow in FrmMainApp.DtIsoCountryCodeMapping.Rows)
        {
            retList.Add(item: dataRow[columnName: "ISO_3166_1A3"]
                            .ToString());
        }

        return retList.ToArray();
    }

    #endregion

    #region Extensions

    /// <summary>
    ///     this one basically handles what extensions we work with.
    ///     the actual list is used for file-specific Settings as well as the general running of the app
    ///     leave the \t in!
    /// </summary>
    internal static string[] AllCompatibleExtensions()
    {
        string[] result =
        {
            "arq	Sony Alpha Pixel-Shift RAW (TIFF-based)",
            "arw	Sony Alpha RAW (TIFF-based)",
            "cr2	Canon RAW 2 (TIFF-based) (CR2 spec)",
            "cr3	Canon RAW 3 (QuickTime-based) (CR3 spec)",
            "dcp	DNG Camera Profile (DNG-like)",
            "dng	Digital Negative (TIFF-based)",
            "erf	Epson RAW Format (TIFF-based)",
            "exv	Exiv2 metadata file (JPEG-based)",
            "fff	Hasselblad Flexible File Format (TIFF-based)",
            "gpr	GoPro RAW (DNG-based)",
            "hdp	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)",
            "heic	High Efficiency Image Format (QuickTime-based)",
            "heif	High Efficiency Image Format (QuickTime-based)",
            "hif	High Efficiency Image Format (QuickTime-based)",
            "iiq	Phase One Intelligent Image Quality RAW (TIFF-based)",
            "insp	Insta360 Picture (JPEG-based)",
            "jp2	JPEG 2000 image [Compound/Extended]",
            "jpe	Joint Photographic Experts Group image",
            "jpeg	Joint Photographic Experts Group image",
            "jpf	JPEG 2000 image [Compound/Extended]",
            "jpg	Joint Photographic Experts Group image",
            "jpm	JPEG 2000 image [Compound/Extended]",
            "jpx	JPEG 2000 image [Compound/Extended]",
            "jxl	JPEG XL (codestream and ISO BMFF)",
            "jxr	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)",
            "mef	Mamiya (RAW) Electronic Format (TIFF-based)",
            "mie	Meta Information Encapsulation (MIE specification)",
            "mos	Creo Leaf Mosaic (TIFF-based)",
            "mpo	Extended Multi-Picture format (JPEG with MPF extensions)",
            "mrw	Minolta RAW",
            "nef	Nikon (RAW) Electronic Format (TIFF-based)",
            "nrw	Nikon RAW (2) (TIFF-based)",
            "orf	Olympus RAW Format (TIFF-based)",
            "ori	Olympus RAW Format (TIFF-based)",
            "pef	Pentax (RAW) Electronic Format (TIFF-based)",
            "raf	FujiFilm RAW Format",
            "raw	Kyocera Contax N Digital RAW",
            "rw2	Panasonic RAW 2 (TIFF-based)",
            "rwl	Leica RAW (TIFF-based)",
            "sr2	Sony RAW 2 (TIFF-based)",
            "srw	Samsung RAW format (TIFF-based)",
            "thm	Thumbnail image (JPEG)",
            "tif	QuickTime Image File",
            "tiff	Tagged Image File Format",
            "wdp 	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)",
            "x3f	Sigma/Foveon RAW"
        };
        return result;
    }

    /// <summary>
    /// Extracts only the file name extensions from the list of
    /// AllCompatibleExtensions. The returned array is a copy and
    /// can be used freely.
    /// </summary>
    /// <returns>An array of file extensions supported</returns>
    internal static string[] AllCompatibleExtensionsExt()
    {
        string[] allowedExtensions = AllCompatibleExtensions();

        // List contains the extension at then beginning and
        // after white space more description --> loop
        // to get only the extensions
        for (int i = 0; i < allowedExtensions.Length; i++)
            allowedExtensions[i] = allowedExtensions[i].Split('\t').FirstOrDefault();
        return allowedExtensions;
    }


    internal static string[] GpxExtensions()
    {
        string[] result =
        {
            "gpx", // GPX	
            "nmea", // NMEA (RMC, GGA, GLL and GSA sentences)	
            "kml", // KML	
            "igc", // IGC (glider format)	
            "xml", "tcx", // Garmin XML and TCX	
            "log", // Magellan eXplorist PMGNTRK + // Honeywell PTNTHPR + // Bramor gEO log	
            "txt", // Winplus Beacon .TXT	
            "json", // Google Takeout .JSON	
            "csv" // GPS/IMU .CSV + // DJI .CSV + // ExifTool .CSV file	
        };

        return result;
    }

    #endregion
}