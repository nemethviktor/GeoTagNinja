using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static GeoTagNinja.Model.SourcesAndAttributes;

namespace GeoTagNinja;

internal static class AncillaryListsArrays
{
    // this stores the kvp for language tags and values (ie the label and whatnots + their human-readable counterparts).
    internal static Dictionary<string, string> LanguageStringsDict = new();

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

    internal static Dictionary<string, string> GetISO_639_1_Languages()
    {
        Dictionary<string, string> result = new()
        {
            { "aa", "Afaraf [Afar]" },
            { "ab", "аҧсуа бызшәа [Abkhaz]" },
            { "ae", "avesta [Avestan]" },
            { "af", "Afrikaans [Afrikaans]" },
            { "ak", "Akan [Akan]" },
            { "am", "አማርኛ [Amharic]" },
            { "an", "aragonés [Aragonese]" },
            { "ar", "اللغة العربية [Arabic]" },
            { "as", "অসমীয়া [Assamese]" },
            { "av", "авар мацӀ [Avaric]" },
            { "ay", "aymar aru [Aymara]" },
            { "az", "azərbaycan dili [Azerbaijani]" },
            { "ba", "башҡорт теле [Bashkir]" },
            { "be", "беларуская мова [Belarusian]" },
            { "bg", "български език [Bulgarian]" },
            { "bh", "भोजपुरी [Bihari]" },
            { "bi", "Bislama [Bislama]" },
            { "bm", "bamanankan [Bambara]" },
            { "bn", "বাংলা [Bengali]" },
            { "bo", "བོད་ཡིག [Tibetan]" },
            { "br", "brezhoneg [Breton]" },
            { "bs", "bosanski jezik [Bosnian]" },
            { "ca", "Català [Catalan]" },
            { "ce", "нохчийн мотт [Chechen]" },
            { "ch", "Chamoru [Chamorro]" },
            { "co", "corsu [Corsican]" },
            { "cr", "ᓀᐦᐃᔭᐍᐏᐣ [Cree]" },
            { "cs", "čeština [Czech]" },
            { "cu", "ѩзыкъ словѣньскъ [Old Church Slavonic]" },
            { "cv", "чӑваш чӗлхи [Chuvash]" },
            { "cy", "Cymraeg [Welsh]" },
            { "da", "dansk [Danish]" },
            { "de", "Deutsch [German]" },
            { "dv", "Dhivehi [Divehi]" },
            { "dz", "རྫོང་ཁ [Dzongkha]" },
            { "ee", "Eʋegbe [Ewe]" },
            { "el", "Ελληνικά [Greek]" },
            { "en", "English [English]" },
            { "eo", "Esperanto [Esperanto]" },
            { "es", "Español [Spanish]" },
            { "et", "eesti [Estonian]" },
            { "eu", "euskara [Basque]" },
            { "fa", "فارسی [Persian]" },
            { "ff", "Fulfulde [Fula]" },
            { "fi", "suomi [Finnish]" },
            { "fj", "Vakaviti [Fijian]" },
            { "fo", "føroyskt [Faroese]" },
            { "fr", "Français [French]" },
            { "fy", "Frysk [Western Frisian]" },
            { "ga", "Gaeilge [Irish]" },
            { "gd", "Gàidhlig [Scottish Gaelic]" },
            { "gl", "galego [Galician]" },
            { "gu", "ગુજરાતી [Gujarati]" },
            { "gv", "Gaelg [Manx]" },
            { "ha", "هَوُسَ [Hausa]" },
            { "he", "עברית [Hebrew]" },
            { "hi", "हिन्दी [Hindi]" },
            { "ho", "Hiri Motu [Hiri Motu]" },
            { "hr", "Hrvatski [Croatian]" },
            { "ht", "Kreyòl ayisyen [Haitian]" },
            { "hu", "magyar [Hungarian]" },
            { "hy", "Հայերեն [Armenian]" },
            { "hz", "Otjiherero [Herero]" },
            { "ia", "Interlingua [Interlingua]" },
            { "id", "Bahasa Indonesia [Indonesian]" },
            { "ie", "Interlingue [Interlingue]" },
            { "ig", "Asụsụ Igbo [Igbo]" },
            { "ii", "ꆈꌠ꒿ Nuosuhxop [Nuosu]" },
            { "ik", "Iñupiaq [Inupiaq]" },
            { "io", "Ido [Ido]" },
            { "is", "Íslenska [Icelandic]" },
            { "it", "Italiano [Italian]" },
            { "iu", "ᐃᓄᒃᑎᑐᑦ [Inuktitut]" },
            { "ja", "日本語 [Japanese]" },
            { "jv", "basa Jawa [Javanese]" },
            { "ka", "ქართული [Georgian]" },
            { "kg", "Kikongo [Kongo]" },
            { "ki", "Gĩkũyũ [Kikuyu]" },
            { "kj", "Kuanyama [Kwanyama]" },
            { "kk", "қазақ тілі [Kazakh]" },
            { "kl", "kalaallisut [Kalaallisut]" },
            { "km", "ខេមរភាសា [Khmer]" },
            { "kn", "ಕನ್ನಡ [Kannada]" },
            { "ko", "한국어 [Korean]" },
            { "kr", "Kanuri [Kanuri]" },
            { "ks", "कश्मीरी [Kashmiri]" },
            { "ku", "Kurdî [Kurdish]" },
            { "kv", "коми кыв [Komi]" },
            { "kw", "Kernewek [Cornish]" },
            { "ky", "Кыргызча [Kyrgyz]" },
            { "la", "latine [Latin]" },
            { "lb", "Lëtzebuergesch [Luxembourgish]" },
            { "lg", "Luganda [Ganda]" },
            { "li", "Limburgs [Limburgish]" },
            { "ln", "Lingála [Lingala]" },
            { "lo", "ພາສາ [Lao]" },
            { "lt", "lietuvių kalba [Lithuanian]" },
            { "lu", "Tshiluba [Luba-Katanga]" },
            { "lv", "latviešu valoda [Latvian]" },
            { "mg", "fiteny malagasy [Malagasy]" },
            { "mh", "Kajin M̧ajeļ [Marshallese]" },
            { "mi", "te reo Māori [Māori]" },
            { "mk", "македонски јазик [Macedonian]" },
            { "ml", "മലയാളം [Malayalam]" },
            { "mn", "Монгол хэл [Mongolian]" },
            { "mr", "मराठी [Marathi]" },
            { "ms", "Bahasa Malaysia [Malay]" },
            { "mt", "Malti [Maltese]" },
            { "my", "ဗမာစာ [Burmese]" },
            { "na", "Ekakairũ Naoero [Nauru]" },
            { "nb", "Norsk bokmål [Norwegian Bokmål]" },
            { "nd", "isiNdebele [Northern Ndebele]" },
            { "ne", "नेपाली [Nepali]" },
            { "ng", "Owambo [Ndonga]" },
            { "nl", "Nederlands [Dutch]" },
            { "nn", "Norsk nynorsk [Norwegian Nynorsk]" },
            { "no", "Norsk [Norwegian]" },
            { "nr", "isiNdebele [Southern Ndebele]" },
            { "nv", "Diné bizaad [Navajo]" },
            { "ny", "chiCheŵa [Chichewa]" },
            { "oc", "occitan [Occitan]" },
            { "oj", "ᐊᓂᔑᓈᐯᒧᐎᓐ [Ojibwe]" },
            { "om", "Afaan Oromoo [Oromo]" },
            { "or", "ଓଡ଼ିଆ [Oriya]" },
            { "os", "ирон æвзаг [Ossetian]" },
            { "pa", "ਪੰਜਾਬੀ [Panjabi]" },
            { "pi", "पाऴि [Pāli]" },
            { "pl", "Polski [Polish]" },
            { "ps", "پښتو [Pashto]" },
            { "pt", "Português [Portuguese]" },
            { "qu", "Runa Simi [Quechua]" },
            { "rm", "rumantsch grischun [Romansh]" },
            { "rn", "Ikirundi [Kirundi]" },
            { "ro", "Română [Romanian]" },
            { "ru", "Русский [Russian]" },
            { "rw", "Ikinyarwanda [Kinyarwanda]" },
            { "sa", "संस्कृतम् [Sanskrit]" },
            { "sc", "sardu [Sardinian]" },
            { "sd", "सिन्धी [Sindhi]" },
            { "se", "Davvisámegiella [Northern Sami]" },
            { "sg", "yângâ tî sängö [Sango]" },
            { "si", "සිංහල [Sinhala]" },
            { "sk", "slovenčina [Slovak]" },
            { "sl", "slovenščina [Slovenian]" },
            { "sn", "chiShona [Shona]" },
            { "so", "Soomaaliga [Somali]" },
            { "sq", "Shqip [Albanian]" },
            { "sr", "српски језик [Serbian]" },
            { "ss", "SiSwati [Swati]" },
            { "st", "Sesotho [Southern Sotho]" },
            { "su", "Basa Sunda [Sundanese]" },
            { "sv", "Svenska [Swedish]" },
            { "sw", "Kiswahili [Swahili]" },
            { "ta", "தமிழ் [Tamil]" },
            { "te", "తెలుగు [Telugu]" },
            { "tg", "тоҷикӣ [Tajik]" },
            { "th", "ไทย [Thai]" },
            { "ti", "ትግርኛ [Tigrinya]" },
            { "tk", "Türkmen [Turkmen]" },
            { "tl", "Wikang Tagalog [Tagalog]" },
            { "tn", "Setswana [Tswana]" },
            { "to", "faka Tonga [Tonga]" },
            { "tr", "Türkçe [Turkish]" },
            { "ts", "Xitsonga [Tsonga]" },
            { "tt", "татар теле [Tatar]" },
            { "tw", "Twi [Twi]" },
            { "ty", "Reo Tahiti [Tahitian]" },
            { "ug", "ئۇيغۇرچە‎ [Uyghur]" },
            { "uk", "Українська [Ukrainian]" },
            { "ur", "اردو [Urdu]" },
            { "uz", "Ўзбек [Uzbek]" },
            { "ve", "Tshivenḓa [Venda]" },
            { "vi", "Tiếng Việt [Vietnamese]" },
            { "vo", "Volapük [Volapük]" },
            { "wa", "walon [Walloon]" },
            { "wo", "Wollof [Wolof]" },
            { "xh", "isiXhosa [Xhosa]" },
            { "yi", "ייִדיש [Yiddish]" },
            { "yo", "Yorùbá [Yoruba]" },
            { "za", "Saɯ cueŋƅ [Zhuang]" },
            { "zh", "中文 [Chinese]" },
            { "zu", "isiZulu [Zulu]" }
        };
        return result;
    }

    #endregion

    #region Columns

    internal static ElementAttribute[] GpxTagsToOverwrite()
    {
        ElementAttribute[] result =
        {
            ElementAttribute.GPSAltitude,
            ElementAttribute.GPSAltitudeRef,
            //ElementAttribute.GPSDateStamp,
            ElementAttribute.GPSImgDirection,
            ElementAttribute.GPSImgDirectionRef,
            ElementAttribute.GPSLatitude,
            ElementAttribute.GPSLatitudeRef,
            ElementAttribute.GPSLongitude,
            ElementAttribute.GPSLongitudeRef,
            ElementAttribute.GPSSpeed,
            ElementAttribute.GPSSpeedRef
            //ElementAttribute.GPSTimeStamp,
            //ElementAttribute.GPSTrack,
            //ElementAttribute.GPSTrackRef,
        };

        return result;
    }

    internal static ElementAttribute[] ToponomyReplaces()
    {
        ElementAttribute[] result =
        {
            ElementAttribute.City,
            ElementAttribute.State,
            ElementAttribute.Sub_location,
            ElementAttribute.GPSAltitude
        };
        return result;
    }

    internal static ElementAttribute[] GetFavouriteTags()
    {
        ElementAttribute[] result =
        {
            ElementAttribute.GPSAltitude,
            ElementAttribute.GPSAltitudeRef,
            ElementAttribute.GPSLatitude,
            ElementAttribute.GPSLatitudeRef,
            ElementAttribute.GPSLongitude,
            ElementAttribute.GPSLongitudeRef,
            ElementAttribute.Coordinates,
            ElementAttribute.City,
            ElementAttribute.CountryCode,
            ElementAttribute.Country,
            ElementAttribute.State,
            ElementAttribute.Sub_location
        };
        return result;
    }

    internal static ElementAttribute[] CustomRulesDataTargets()
    {
        ElementAttribute[] result =
        {
            ElementAttribute.State,
            ElementAttribute.City,
            ElementAttribute.Sub_location
        };

        return result;
        ;
    }

    internal static string[] CustomCityLogicDataSources()
    {
        string[] result =
        {
            "AdminName1",
            "AdminName2",
            "AdminName3",
            "AdminName4",
            "Undefined"
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
    ///     List of extensions that take an XMP sidecar
    /// </summary>
    internal static string[] FileExtensionsThatUseXMP()
    {
        List<string> retList = new();
        foreach (string extension in AllCompatibleExtensions())
        {
            if (extension.ToLower()
                    .Contains(value: "raw") ||
                extension.ToLower()
                    .Contains(value: "tiff"))
            {
                retList.Add(item: extension.Split('\t')
                                .FirstOrDefault());
            }
        }

        return retList.ToArray();
    }


    /// <summary>
    ///     Extracts only the file name extensions from the list of
    ///     AllCompatibleExtensions. The returned array is a copy and
    ///     can be used freely.
    /// </summary>
    /// <returns>An array of file extensions supported</returns>
    internal static string[] AllCompatibleExtensionsExt()
    {
        string[] allowedExtensions = AllCompatibleExtensions();

        // List contains the extension at then beginning and
        // after white space more description --> loop
        // to get only the extensions
        for (int i = 0; i < allowedExtensions.Length; i++)
        {
            allowedExtensions[i] = allowedExtensions[i]
                .Split('\t')
                .FirstOrDefault();
        }

        return allowedExtensions;
    }


    /// <summary>
    ///     List of supported side car file extensions.
    ///     The extension must be in lower case due to its use in comparisons!
    ///     Dictionary Extension -> Description
    /// </summary>
    internal static IDictionary<string, string> SideCarExtensions()
    {
        IDictionary<string, string> result = new Dictionary<string, string>
        {
            { "xmp", "XMP SideCar File" }
        };
        return result;
    }


    /// <summary>
    ///     Returns an array of extensions (string) of compatible side car files.
    ///     The returned array is a copy and can be used freely.
    /// </summary>
    internal static string[] GetSideCarExtensionsArray()
    {
        return SideCarExtensions()
            .Keys.ToArray();
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