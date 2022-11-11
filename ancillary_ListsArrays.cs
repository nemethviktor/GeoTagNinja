using System;

namespace GeoTagNinja
{
    internal class ancillary_ListsArrays
    {
        #region Extensions
        internal static string[] allCompatibleExtensions()
        {
            string[] result =
            {
                "arq	Sony Alpha Pixel-Shift RAW (TIFF-based)"
                ,"arw	Sony Alpha RAW (TIFF-based)"
                ,"cr2	Canon RAW 2 (TIFF-based) (CR2 spec)"
                ,"cr3	Canon RAW 3 (QuickTime-based) (CR3 spec)"
                ,"dcp	DNG Camera Profile (DNG-like)"
                ,"dng	Digital Negative (TIFF-based)"
                ,"erf	Epson RAW Format (TIFF-based)"
                ,"exv	Exiv2 metadata file (JPEG-based)"
                ,"fff	Hasselblad Flexible File Format (TIFF-based)"
                ,"gpr	GoPro RAW (DNG-based)"
                ,"hdp	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)"
                ,"heic	High Efficiency Image Format (QuickTime-based)"
                ,"heif	High Efficiency Image Format (QuickTime-based)"
                ,"hif	High Efficiency Image Format (QuickTime-based)"
                ,"iiq	Phase One Intelligent Image Quality RAW (TIFF-based)"
                ,"insp	Insta360 Picture (JPEG-based)"
                ,"jp2	JPEG 2000 image [Compound/Extended]"
                ,"jpe	Joint Photographic Experts Group image"
                ,"jpeg	Joint Photographic Experts Group image"
                ,"jpf	JPEG 2000 image [Compound/Extended]"
                ,"jpg	Joint Photographic Experts Group image"
                ,"jpm	JPEG 2000 image [Compound/Extended]"
                ,"jpx	JPEG 2000 image [Compound/Extended]"
                ,"jxl	JPEG XL (codestream and ISO BMFF)"
                ,"jxr	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)"
                ,"mef	Mamiya (RAW) Electronic Format (TIFF-based)"
                ,"mie	Meta Information Encapsulation (MIE specification)"
                ,"mos	Creo Leaf Mosaic (TIFF-based)"
                ,"mpo	Extended Multi-Picture format (JPEG with MPF extensions)"
                ,"mrw	Minolta RAW"
                ,"nef	Nikon (RAW) Electronic Format (TIFF-based)"
                ,"nrw	Nikon RAW (2) (TIFF-based)"
                ,"orf	Olympus RAW Format (TIFF-based)"
                ,"ori	Olympus RAW Format (TIFF-based)"
                ,"pef	Pentax (RAW) Electronic Format (TIFF-based)"
                ,"raf	FujiFilm RAW Format"
                ,"raw	Kyocera Contax N Digital RAW"
                ,"rw2	Panasonic RAW 2 (TIFF-based)"
                ,"rwl	Leica RAW (TIFF-based)"
                ,"sr2	Sony RAW 2 (TIFF-based)"
                ,"srw	Samsung RAW format (TIFF-based)"
                ,"thm	Thumbnail image (JPEG)"
                ,"tif	QuickTime Image File"
                ,"tiff	Tagged Image File Format"
                ,"wdp 	Windows HD Photo / Media Photo / JPEG XR (TIFF-based)"
                ,"x3f	Sigma/Foveon RAW"
            };
            return result;
        }
        internal static string[] gpxExtensions()
        {
            string[] result =
            {
                "gpx", // GPX
                "nmea", // NMEA (RMC, GGA, GLL and GSA sentences)
                "kml", // KML
                "igc", // IGC (glider format)
                "xml", "tcx" , // Garmin XML and TCX
                "log", // Magellan eXplorist PMGNTRK + // Honeywell PTNTHPR + // Bramor gEO log
                "txt", // Winplus Beacon .TXT
                "json", // Google Takeout .JSON
                "csv" // GPS/IMU .CSV + // DJI .CSV + // ExifTool .CSV file
            };

            return result;
        }

        #endregion
        #region Time zones
        internal static string[] getTimeZones()
        {
            string[] result =
            { 
                // via https://en.wikipedia.org/w/index.php?title=List_of_tz_database_time_zones&oldid=1119058681
                "(+00:00/+00:00) # Africa/Abidjan # [GMT/]",
                "(+00:00/+00:00) # Africa/Accra # [GMT/]",
                "(+03:00/+03:00) # Africa/Addis_Ababa # [EAT/]",
                "(+01:00/+01:00) # Africa/Algiers # [CET/]",
                "(+03:00/+03:00) # Africa/Asmera # [EAT/]",
                "(+00:00/+00:00) # Africa/Bamako # [GMT/]",
                "(+01:00/+01:00) # Africa/Bangui # [WAT/]",
                "(+00:00/+00:00) # Africa/Banjul # [GMT/]",
                "(+00:00/+00:00) # Africa/Bissau # [GMT/]",
                "(+02:00/+02:00) # Africa/Blantyre # [CAT/]",
                "(+01:00/+01:00) # Africa/Brazzaville # [WAT/]",
                "(+02:00/+02:00) # Africa/Bujumbura # [CAT/]",
                "(+02:00/+02:00) # Africa/Cairo # [EET/]",
                "(+01:00/+00:00) # Africa/Casablanca # [1/0]",
                "(+00:00/+00:00) # Africa/Conakry # [GMT/]",
                "(+00:00/+00:00) # Africa/Dakar # [GMT/]",
                "(+03:00/+03:00) # Africa/Dar_es_Salaam # [EAT/]",
                "(+03:00/+03:00) # Africa/Djibouti # [EAT/]",
                "(+01:00/+01:00) # Africa/Douala # [WAT/]",
                "(+01:00/+00:00) # Africa/El_Aaiun # [1/0]",
                "(+00:00/+00:00) # Africa/Freetown # [GMT/]",
                "(+02:00/+02:00) # Africa/Gaborone # [CAT/]",
                "(+02:00/+02:00) # Africa/Harare # [CAT/]",
                "(+02:00/+02:00) # Africa/Johannesburg # [SAST/]",
                "(+02:00/+02:00) # Africa/Juba # [CAT/]",
                "(+03:00/+03:00) # Africa/Kampala # [EAT/]",
                "(+02:00/+02:00) # Africa/Khartoum # [CAT/]",
                "(+02:00/+02:00) # Africa/Kigali # [CAT/]",
                "(+01:00/+01:00) # Africa/Kinshasa # [WAT/]",
                "(+01:00/+01:00) # Africa/Lagos # [WAT/]",
                "(+01:00/+01:00) # Africa/Libreville # [WAT/]",
                "(+00:00/+00:00) # Africa/Lome # [GMT/]",
                "(+01:00/+01:00) # Africa/Luanda # [WAT/]",
                "(+02:00/+02:00) # Africa/Lubumbashi # [CAT/]",
                "(+02:00/+02:00) # Africa/Lusaka # [CAT/]",
                "(+01:00/+01:00) # Africa/Malabo # [WAT/]",
                "(+02:00/+02:00) # Africa/Maputo # [CAT/]",
                "(+02:00/+02:00) # Africa/Maseru # [SAST/]",
                "(+02:00/+02:00) # Africa/Mbabane # [SAST/]",
                "(+03:00/+03:00) # Africa/Mogadishu # [EAT/]",
                "(+00:00/+00:00) # Africa/Monrovia # [GMT/]",
                "(+03:00/+03:00) # Africa/Nairobi # [EAT/]",
                "(+01:00/+01:00) # Africa/Ndjamena # [WAT/]",
                "(+01:00/+01:00) # Africa/Niamey # [WAT/]",
                "(+00:00/+00:00) # Africa/Nouakchott # [GMT/]",
                "(+00:00/+00:00) # Africa/Ouagadougou # [GMT/]",
                "(+01:00/+01:00) # Africa/Porto-Novo # [WAT/]",
                "(+00:00/+00:00) # Africa/Sao_Tome # [GMT/]",
                "(+02:00/+02:00) # Africa/Tripoli # [EET/]",
                "(+01:00/+01:00) # Africa/Tunis # [CET/]",
                "(+02:00/+02:00) # Africa/Windhoek # [CAT/]",
                "(-10:00/-09:00) # America/Adak # [HST/HDT]",
                "(-09:00/-08:00) # America/Anchorage # [AKST/AKDT]",
                "(-04:00/-04:00) # America/Anguilla # [AST/]",
                "(-04:00/-04:00) # America/Antigua # [AST/]",
                "(-03:00/-03:00) # America/Araguaina # [-3/]",
                "(-04:00/-04:00) # America/Aruba # [AST/]",
                "(-04:00/-03:00) # America/Asuncion # [-4/-3]",
                "(-03:00/-03:00) # America/Bahia # [-3/]",
                "(-04:00/-04:00) # America/Barbados # [AST/]",
                "(-06:00/-06:00) # America/Belize # [CST/]",
                "(-04:00/-04:00) # America/Blanc-Sablon # [AST/]",
                "(-05:00/-05:00) # America/Bogota # [-5/]",
                "(-03:00/-03:00) # America/Buenos_Aires # [-3/]",
                "(-05:00/-05:00) # America/Cancun # [EST/]",
                "(-04:00/-04:00) # America/Caracas # [-4/]",
                "(-03:00/-03:00) # America/Cayenne # [-3/]",
                "(-05:00/-05:00) # America/Cayman # [EST/]",
                "(-06:00/-05:00) # America/Chicago # [CST/CDT]",
                "(-06:00/-06:00) # America/Chihuahua # [CST/]",
                "(-05:00/-05:00) # America/Coral_Harbour # [EST/]",
                "(-06:00/-06:00) # America/Costa_Rica # [CST/]",
                "(-04:00/-04:00) # America/Cuiaba # [-4/]",
                "(-04:00/-04:00) # America/Curacao # [AST/]",
                "(+00:00/+00:00) # America/Danmarkshavn # [GMT/]",
                "(-07:00/-06:00) # America/Denver # [MST/MDT]",
                "(-04:00/-04:00) # America/Dominica # [AST/]",
                "(-06:00/-06:00) # America/El_Salvador # [CST/]",
                "(-03:00/-02:00) # America/Godthab # [-3/-2]",
                "(-05:00/-04:00) # America/Grand_Turk # [EST/EDT]",
                "(-04:00/-04:00) # America/Grenada # [AST/]",
                "(-04:00/-04:00) # America/Guadeloupe # [AST/]",
                "(-06:00/-06:00) # America/Guatemala # [CST/]",
                "(-05:00/-05:00) # America/Guayaquil # [-5/]",
                "(-04:00/-04:00) # America/Guyana # [-4/]",
                "(-04:00/-03:00) # America/Halifax # [AST/ADT]",
                "(-05:00/-04:00) # America/Havana # [CST/CDT]",
                "(-07:00/-07:00) # America/Hermosillo # [MST/]",
                "(-05:00/-04:00) # America/Indianapolis # [EST/EDT]",
                "(-05:00/-05:00) # America/Jamaica # [EST/]",
                "(-04:00/-04:00) # America/Kralendijk # [AST/]",
                "(-04:00/-04:00) # America/La_Paz # [-4/]",
                "(-05:00/-05:00) # America/Lima # [-5/]",
                "(-08:00/-07:00) # America/Los_Angeles # [PST/PDT]",
                "(-04:00/-04:00) # America/Lower_Princes # [AST/]",
                "(-06:00/-06:00) # America/Managua # [CST/]",
                "(-04:00/-04:00) # America/Marigot # [AST/]",
                "(-04:00/-04:00) # America/Martinique # [AST/]",
                "(-06:00/-05:00) # America/Matamoros # [CST/CDT]",
                "(-06:00/-06:00) # America/Mexico_City # [CST/]",
                "(-03:00/-02:00) # America/Miquelon # [-3/-2]",
                "(-03:00/-03:00) # America/Montevideo # [-3/]",
                "(-04:00/-04:00) # America/Montserrat # [AST/]",
                "(-05:00/-04:00) # America/Nassau # [EST/EDT]",
                "(-05:00/-04:00) # America/New_York # [EST/EDT]",
                "(-02:00/-02:00) # America/Noronha # [-2/]",
                "(-06:00/-06:00) # America/Ojinaga # [CST/]",
                "(-05:00/-05:00) # America/Panama # [EST/]",
                "(-03:00/-03:00) # America/Paramaribo # [-3/]",
                "(-07:00/-07:00) # America/Phoenix # [MST/]",
                "(-05:00/-04:00) # America/Port-au-Prince # [EST/EDT]",
                "(-04:00/-04:00) # America/Port_of_Spain # [AST/]",
                "(-04:00/-04:00) # America/Puerto_Rico # [AST/]",
                "(-03:00/-03:00) # America/Punta_Arenas # [-3/]",
                "(-06:00/-06:00) # America/Regina # [CST/]",
                "(-04:00/-03:00) # America/Santiago # [-4/-3]",
                "(-04:00/-04:00) # America/Santo_Domingo # [AST/]",
                "(-03:00/-03:00) # America/Sao_Paulo # [-3/]",
                "(-01:00/+00:00) # America/Scoresbysund # [-1/0]",
                "(-04:00/-04:00) # America/St_Barthelemy # [AST/]",
                "(-03:30/-02:30) # America/St_Johns # [NST/NDT]",
                "(-04:00/-04:00) # America/St_Kitts # [AST/]",
                "(-04:00/-04:00) # America/St_Lucia # [AST/]",
                "(-04:00/-04:00) # America/St_Thomas # [AST/]",
                "(-04:00/-04:00) # America/St_Vincent # [AST/]",
                "(-06:00/-06:00) # America/Tegucigalpa # [CST/]",
                "(-04:00/-03:00) # America/Thule # [AST/ADT]",
                "(-08:00/-07:00) # America/Tijuana # [PST/PDT]",
                "(-04:00/-04:00) # America/Tortola # [AST/]",
                "(-08:00/-07:00) # America/Vancouver # [PST/PDT]",
                "(-07:00/-07:00) # America/Whitehorse # [MST/]",
                "(+11:00/+11:00) # Antarctica/Casey # [11/]",
                "(+07:00/+07:00) # Antarctica/Davis # [7/]",
                "(+10:00/+10:00) # Antarctica/DumontDUrville # [10/]",
                "(+05:00/+05:00) # Antarctica/Mawson # [5/]",
                "(+12:00/+13:00) # Antarctica/McMurdo # [NZST/NZDT]",
                "(+03:00/+03:00) # Antarctica/Syowa # [3/]",
                "(+06:00/+06:00) # Antarctica/Vostok # [6/]",
                "(+01:00/+02:00) # Arctic/Longyearbyen # [CET/CEST]",
                "(+03:00/+03:00) # Asia/Aden # [3/]",
                "(+06:00/+06:00) # Asia/Almaty # [6/]",
                "(+03:00/+03:00) # Asia/Amman # [3/]",
                "(+05:00/+05:00) # Asia/Ashgabat # [5/]",
                "(+03:00/+03:00) # Asia/Baghdad # [3/]",
                "(+03:00/+03:00) # Asia/Bahrain # [3/]",
                "(+04:00/+04:00) # Asia/Baku # [4/]",
                "(+07:00/+07:00) # Asia/Bangkok # [7/]",
                "(+07:00/+07:00) # Asia/Barnaul # [7/]",
                "(+02:00/+03:00) # Asia/Beirut # [EET/EEST]",
                "(+06:00/+06:00) # Asia/Bishkek # [6/]",
                "(+08:00/+08:00) # Asia/Brunei # [8/]",
                "(+05:30/+05:30) # Asia/Calcutta # [IST/]",
                "(+09:00/+09:00) # Asia/Chita # [9/]",
                "(+05:30/+05:30) # Asia/Colombo # [530/]",
                "(+03:00/+03:00) # Asia/Damascus # [3/]",
                "(+06:00/+06:00) # Asia/Dhaka # [6/]",
                "(+09:00/+09:00) # Asia/Dili # [9/]",
                "(+04:00/+04:00) # Asia/Dubai # [4/]",
                "(+05:00/+05:00) # Asia/Dushanbe # [5/]",
                "(+02:00/+03:00) # Asia/Hebron # [EET/EEST]",
                "(+08:00/+08:00) # Asia/Hong_Kong # [HKT/]",
                "(+07:00/+07:00) # Asia/Hovd # [7/]",
                "(+08:00/+08:00) # Asia/Irkutsk # [8/]",
                "(+09:00/+09:00) # Asia/Jayapura # [WIT/]",
                "(+02:00/+03:00) # Asia/Jerusalem # [IST/IDT]",
                "(+04:30/+04:30) # Asia/Kabul # [430/]",
                "(+12:00/+12:00) # Asia/Kamchatka # [12/]",
                "(+05:00/+05:00) # Asia/Karachi # [PKT/]",
                "(+05:45/+05:45) # Asia/Katmandu # [545/]",
                "(+07:00/+07:00) # Asia/Krasnoyarsk # [7/]",
                "(+03:00/+03:00) # Asia/Kuwait # [3/]",
                "(+08:00/+08:00) # Asia/Macau # [CST/]",
                "(+11:00/+11:00) # Asia/Magadan # [11/]",
                "(+08:00/+08:00) # Asia/Makassar # [WITA/]",
                "(+08:00/+08:00) # Asia/Manila # [PST/]",
                "(+04:00/+04:00) # Asia/Muscat # [4/]",
                "(+07:00/+07:00) # Asia/Novosibirsk # [7/]",
                "(+06:00/+06:00) # Asia/Omsk # [6/]",
                "(+07:00/+07:00) # Asia/Phnom_Penh # [7/]",
                "(+09:00/+09:00) # Asia/Pyongyang # [KST/]",
                "(+03:00/+03:00) # Asia/Qatar # [3/]",
                "(+05:00/+05:00) # Asia/Qyzylorda # [5/]",
                "(+06:30/+06:30) # Asia/Rangoon # [630/]",
                "(+03:00/+03:00) # Asia/Riyadh # [3/]",
                "(+07:00/+07:00) # Asia/Saigon # [7/]",
                "(+11:00/+11:00) # Asia/Sakhalin # [11/]",
                "(+09:00/+09:00) # Asia/Seoul # [KST/]",
                "(+08:00/+08:00) # Asia/Shanghai # [CST/]",
                "(+08:00/+08:00) # Asia/Singapore # [8/]",
                "(+11:00/+11:00) # Asia/Srednekolymsk # [11/]",
                "(+08:00/+08:00) # Asia/Taipei # [CST/]",
                "(+05:00/+05:00) # Asia/Tashkent # [5/]",
                "(+04:00/+04:00) # Asia/Tbilisi # [4/]",
                "(+03:30/+03:30) # Asia/Tehran # [330/]",
                "(+06:00/+06:00) # Asia/Thimphu # [6/]",
                "(+09:00/+09:00) # Asia/Tokyo # [JST/]",
                "(+07:00/+07:00) # Asia/Tomsk # [7/]",
                "(+08:00/+08:00) # Asia/Ulaanbaatar # [8/]",
                "(+06:00/+06:00) # Asia/Urumqi # [6/]",
                "(+07:00/+07:00) # Asia/Vientiane # [7/]",
                "(+10:00/+10:00) # Asia/Vladivostok # [10/]",
                "(+09:00/+09:00) # Asia/Yakutsk # [9/]",
                "(+05:00/+05:00) # Asia/Yekaterinburg # [5/]",
                "(+04:00/+04:00) # Asia/Yerevan # [4/]",
                "(-01:00/+00:00) # Atlantic/Azores # [-1/0]",
                "(-04:00/-03:00) # Atlantic/Bermuda # [AST/ADT]",
                "(+00:00/+01:00) # Atlantic/Canary # [WET/WEST]",
                "(-01:00/-01:00) # Atlantic/Cape_Verde # [-1/]",
                "(+00:00/+01:00) # Atlantic/Faeroe # [WET/WEST]",
                "(+00:00/+00:00) # Atlantic/Reykjavik # [GMT/]",
                "(-02:00/-02:00) # Atlantic/South_Georgia # [-2/]",
                "(+00:00/+00:00) # Atlantic/St_Helena # [GMT/]",
                "(-03:00/-03:00) # Atlantic/Stanley # [-3/]",
                "(+09:30/+10:30) # Australia/Adelaide # [ACST/ACDT]",
                "(+10:00/+10:00) # Australia/Brisbane # [AEST/]",
                "(+09:30/+09:30) # Australia/Darwin # [ACST/]",
                "(+08:45/+08:45) # Australia/Eucla # [845/]",
                "(+10:00/+11:00) # Australia/Hobart # [AEST/AEDT]",
                "(+10:30/+11:00) # Australia/Lord_Howe # [1030/11]",
                "(+08:00/+08:00) # Australia/Perth # [AWST/]",
                "(+10:00/+11:00) # Australia/Sydney # [AEST/AEDT]",
                "(-06:00/-05:00) # CST6CDT # [CST/CDT]",
                "(-05:00/-04:00) # EST5EDT # [EST/EDT]",
                "(-01:00/-01:00) # Etc/GMT+1 # [-1/]",
                "(-10:00/-10:00) # Etc/GMT+10 # [-10/]",
                "(-11:00/-11:00) # Etc/GMT+11 # [-11/]",
                "(-12:00/-12:00) # Etc/GMT+12 # [-12/]",
                "(-02:00/-02:00) # Etc/GMT+2 # [-2/]",
                "(-03:00/-03:00) # Etc/GMT+3 # [-3/]",
                "(-04:00/-04:00) # Etc/GMT+4 # [-4/]",
                "(-05:00/-05:00) # Etc/GMT+5 # [-5/]",
                "(-06:00/-06:00) # Etc/GMT+6 # [-6/]",
                "(-07:00/-07:00) # Etc/GMT+7 # [-7/]",
                "(-08:00/-08:00) # Etc/GMT+8 # [-8/]",
                "(-09:00/-09:00) # Etc/GMT+9 # [-9/]",
                "(+01:00/+01:00) # Etc/GMT-1 # [1/]",
                "(+10:00/+10:00) # Etc/GMT-10 # [10/]",
                "(+11:00/+11:00) # Etc/GMT-11 # [11/]",
                "(+12:00/+12:00) # Etc/GMT-12 # [12/]",
                "(+13:00/+13:00) # Etc/GMT-13 # [13/]",
                "(+14:00/+14:00) # Etc/GMT-14 # [14/]",
                "(+02:00/+02:00) # Etc/GMT-2 # [2/]",
                "(+03:00/+03:00) # Etc/GMT-3 # [3/]",
                "(+04:00/+04:00) # Etc/GMT-4 # [4/]",
                "(+05:00/+05:00) # Etc/GMT-5 # [5/]",
                "(+06:00/+06:00) # Etc/GMT-6 # [6/]",
                "(+07:00/+07:00) # Etc/GMT-7 # [7/]",
                "(+08:00/+08:00) # Etc/GMT-8 # [8/]",
                "(+09:00/+09:00) # Etc/GMT-9 # [9/]",
                "(+00:00/+00:00) # Etc/UTC # [UTC/]",
                "(+01:00/+02:00) # Europe/Amsterdam # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Andorra # [CET/CEST]",
                "(+04:00/+04:00) # Europe/Astrakhan # [4/]",
                "(+02:00/+03:00) # Europe/Athens # [EET/EEST]",
                "(+01:00/+02:00) # Europe/Belgrade # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Berlin # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Bratislava # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Brussels # [CET/CEST]",
                "(+02:00/+03:00) # Europe/Bucharest # [EET/EEST]",
                "(+01:00/+02:00) # Europe/Budapest # [CET/CEST]",
                "(+02:00/+03:00) # Europe/Chisinau # [EET/EEST]",
                "(+01:00/+02:00) # Europe/Copenhagen # [CET/CEST]",
                "(+01:00/+00:00) # Europe/Dublin # [IST/GMT]",
                "(+01:00/+02:00) # Europe/Gibraltar # [CET/CEST]",
                "(+00:00/+01:00) # Europe/Guernsey # [GMT/BST]",
                "(+02:00/+03:00) # Europe/Helsinki # [EET/EEST]",
                "(+00:00/+01:00) # Europe/Isle_of_Man # [GMT/BST]",
                "(+03:00/+03:00) # Europe/Istanbul # [3/]",
                "(+00:00/+01:00) # Europe/Jersey # [GMT/BST]",
                "(+02:00/+02:00) # Europe/Kaliningrad # [EET/]",
                "(+02:00/+03:00) # Europe/Kiev # [EET/EEST]",
                "(+01:00/+02:00) # Europe/Ljubljana # [CET/CEST]",
                "(+00:00/+01:00) # Europe/London # [GMT/BST]",
                "(+01:00/+02:00) # Europe/Luxembourg # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Malta # [CET/CEST]",
                "(+02:00/+03:00) # Europe/Mariehamn # [EET/EEST]",
                "(+03:00/+03:00) # Europe/Minsk # [3/]",
                "(+01:00/+02:00) # Europe/Monaco # [CET/CEST]",
                "(+03:00/+03:00) # Europe/Moscow # [MSK/]",
                "(+01:00/+02:00) # Europe/Oslo # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Paris # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Podgorica # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Prague # [CET/CEST]",
                "(+02:00/+03:00) # Europe/Riga # [EET/EEST]",
                "(+01:00/+02:00) # Europe/Rome # [CET/CEST]",
                "(+04:00/+04:00) # Europe/Samara # [4/]",
                "(+01:00/+02:00) # Europe/San_Marino # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Sarajevo # [CET/CEST]",
                "(+04:00/+04:00) # Europe/Saratov # [4/]",
                "(+03:00/+03:00) # Europe/Simferopol # [MSK/]",
                "(+01:00/+02:00) # Europe/Skopje # [CET/CEST]",
                "(+02:00/+03:00) # Europe/Sofia # [EET/EEST]",
                "(+01:00/+02:00) # Europe/Stockholm # [CET/CEST]",
                "(+02:00/+03:00) # Europe/Tallinn # [EET/EEST]",
                "(+01:00/+02:00) # Europe/Tirane # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Vaduz # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Vatican # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Vienna # [CET/CEST]",
                "(+02:00/+03:00) # Europe/Vilnius # [EET/EEST]",
                "(+03:00/+03:00) # Europe/Volgograd # [3/]",
                "(+01:00/+02:00) # Europe/Warsaw # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Zagreb # [CET/CEST]",
                "(+01:00/+02:00) # Europe/Zurich # [CET/CEST]",
                "(+03:00/+03:00) # Indian/Antananarivo # [EAT/]",
                "(+06:00/+06:00) # Indian/Chagos # [6/]",
                "(+07:00/+07:00) # Indian/Christmas # [7/]",
                "(+06:30/+06:30) # Indian/Cocos # [630/]",
                "(+03:00/+03:00) # Indian/Comoro # [EAT/]",
                "(+05:00/+05:00) # Indian/Kerguelen # [5/]",
                "(+04:00/+04:00) # Indian/Mahe # [4/]",
                "(+05:00/+05:00) # Indian/Maldives # [5/]",
                "(+04:00/+04:00) # Indian/Mauritius # [4/]",
                "(+03:00/+03:00) # Indian/Mayotte # [EAT/]",
                "(+04:00/+04:00) # Indian/Reunion # [4/]",
                "(-07:00/-06:00) # MST7MDT # [MST/MDT]",
                "(+13:00/+13:00) # Pacific/Apia # [13/]",
                "(+12:00/+13:00) # Pacific/Auckland # [NZST/NZDT]",
                "(+11:00/+11:00) # Pacific/Bougainville # [11/]",
                "(+12:45/+13:45) # Pacific/Chatham # [1245/1345]",
                "(-06:00/-05:00) # Pacific/Easter # [-6/-5]",
                "(+11:00/+11:00) # Pacific/Efate # [11/]",
                "(+13:00/+13:00) # Pacific/Enderbury # [13/]",
                "(+13:00/+13:00) # Pacific/Fakaofo # [13/]",
                "(+12:00/+12:00) # Pacific/Fiji # [12/]",
                "(+12:00/+12:00) # Pacific/Funafuti # [12/]",
                "(-06:00/-06:00) # Pacific/Galapagos # [-6/]",
                "(-09:00/-09:00) # Pacific/Gambier # [-9/]",
                "(+11:00/+11:00) # Pacific/Guadalcanal # [11/]",
                "(+10:00/+10:00) # Pacific/Guam # [ChST/]",
                "(-10:00/-10:00) # Pacific/Honolulu # [HST/]",
                "(-10:00/-10:00) # Pacific/Johnston # [HST/]",
                "(+14:00/+14:00) # Pacific/Kiritimati # [14/]",
                "(-09:30/-09:30) # Pacific/Marquesas # [-930/]",
                "(-11:00/-11:00) # Pacific/Midway # [SST/]",
                "(+12:00/+12:00) # Pacific/Nauru # [12/]",
                "(-11:00/-11:00) # Pacific/Niue # [-11/]",
                "(+11:00/+12:00) # Pacific/Norfolk # [11/12]",
                "(+11:00/+11:00) # Pacific/Noumea # [11/]",
                "(-11:00/-11:00) # Pacific/Pago_Pago # [SST/]",
                "(+09:00/+09:00) # Pacific/Palau # [9/]",
                "(-08:00/-08:00) # Pacific/Pitcairn # [-8/]",
                "(+10:00/+10:00) # Pacific/Port_Moresby # [10/]",
                "(-10:00/-10:00) # Pacific/Rarotonga # [-10/]",
                "(+10:00/+10:00) # Pacific/Saipan # [ChST/]",
                "(-10:00/-10:00) # Pacific/Tahiti # [-10/]",
                "(+12:00/+12:00) # Pacific/Tarawa # [12/]",
                "(+13:00/+13:00) # Pacific/Tongatapu # [13/]",
                "(+10:00/+10:00) # Pacific/Truk # [10/]",
                "(+12:00/+12:00) # Pacific/Wake # [12/]",
                "(+12:00/+12:00) # Pacific/Wallis # [12/]",
                "(-08:00/-07:00) # PST8PDT # [PST/PDT]",

            };
            Array.Sort(result);
            return result;
        }
        #endregion
        #region Columns
        internal static string[] gpxTagsToOverwrite()
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
        #endregion
    }
}
