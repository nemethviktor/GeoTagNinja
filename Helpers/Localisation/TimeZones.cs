using System.Collections.Generic;
using System.Linq;

namespace GeoTagNinja.Helpers.Localisation;

internal enum TimeShiftTypes
{
    TakenDate,
    CreateDate
}

/// <summary>
///     The hard-coded IANA time-zone list offered to the user, plus the helpers that pick an entry out of
///     it by UTC offset.
/// </summary>
/// <remarks>
///     The list is hard-coded rather than taken from <see cref="System.TimeZoneInfo" /> so that the names
///     shown to the user stay stable across Windows releases, and so that they match what is written into
///     the files.
/// </remarks>
internal static class TimeZones
{
    /// <summary>
    ///     Gets an array of time zone identifiers.
    /// </summary>
    /// <returns>
    ///     An array of strings where each string represents a time zone identifier.
    /// </returns>
    internal static List<string> GetTimeZones()
    {
        // note to self: 
        // source: https://en.wikipedia.org/w/index.php?title=List_of_tz_database_time_zones&oldid=1175477220
        // the way to update this is: copy the relevant table from wikipedia to Excel
        // 1) remove columns beyond DST
        // 2) paste over here as tab-separated (default).

        string tzData = """
                        Code	TimeZone	STD	DST
                        CI, BF, GH, GM, GN, IS, ML, MR, SH, SL, SN, TG	Africa/Abidjan	+00:00	+00:00
                        GH	Africa/Accra	+00:00	+00:00
                        ET	Africa/Addis_Ababa	+03:00	+03:00
                        DZ	Africa/Algiers	+01:00	+01:00
                        ER	Africa/Asmara	+03:00	+03:00
                        ER	Africa/Asmera	+03:00	+03:00
                        ML	Africa/Bamako	+00:00	+00:00
                        CF	Africa/Bangui	+01:00	+01:00
                        GM	Africa/Banjul	+00:00	+00:00
                        GW	Africa/Bissau	+00:00	+00:00
                        MW	Africa/Blantyre	+02:00	+02:00
                        CG	Africa/Brazzaville	+01:00	+01:00
                        BI	Africa/Bujumbura	+02:00	+02:00
                        EG	Africa/Cairo	+02:00	+03:00
                        MA	Africa/Casablanca	+01:00	+00:00
                        ES	Africa/Ceuta	+01:00	+02:00
                        GN	Africa/Conakry	+00:00	+00:00
                        SN	Africa/Dakar	+00:00	+00:00
                        TZ	Africa/Dar_es_Salaam	+03:00	+03:00
                        DJ	Africa/Djibouti	+03:00	+03:00
                        CM	Africa/Douala	+01:00	+01:00
                        EH	Africa/El_Aaiun	+01:00	+00:00
                        SL	Africa/Freetown	+00:00	+00:00
                        BW	Africa/Gaborone	+02:00	+02:00
                        ZW	Africa/Harare	+02:00	+02:00
                        ZA, LS, SZ	Africa/Johannesburg	+02:00	+02:00
                        SS	Africa/Juba	+02:00	+02:00
                        UG	Africa/Kampala	+03:00	+03:00
                        SD	Africa/Khartoum	+02:00	+02:00
                        RW	Africa/Kigali	+02:00	+02:00
                        CD	Africa/Kinshasa	+01:00	+01:00
                        NG, AO, BJ, CD, CF, CG, CM, GA, GQ, NE	Africa/Lagos	+01:00	+01:00
                        GA	Africa/Libreville	+01:00	+01:00
                        TG	Africa/Lome	+00:00	+00:00
                        AO	Africa/Luanda	+01:00	+01:00
                        CD	Africa/Lubumbashi	+02:00	+02:00
                        ZM	Africa/Lusaka	+02:00	+02:00
                        GQ	Africa/Malabo	+01:00	+01:00
                        MZ, BI, BW, CD, MW, RW, ZM, ZW	Africa/Maputo	+02:00	+02:00
                        LS	Africa/Maseru	+02:00	+02:00
                        SZ	Africa/Mbabane	+02:00	+02:00
                        SO	Africa/Mogadishu	+03:00	+03:00
                        LR	Africa/Monrovia	+00:00	+00:00
                        KE, DJ, ER, ET, KM, MG, SO, TZ, UG, YT	Africa/Nairobi	+03:00	+03:00
                        TD	Africa/Ndjamena	+01:00	+01:00
                        NE	Africa/Niamey	+01:00	+01:00
                        MR	Africa/Nouakchott	+00:00	+00:00
                        BF	Africa/Ouagadougou	+00:00	+00:00
                        BJ	Africa/Porto-Novo	+01:00	+01:00
                        ST	Africa/Sao_Tome	+00:00	+00:00
                        ML	Africa/Timbuktu	+00:00	+00:00
                        LY	Africa/Tripoli	+02:00	+02:00
                        TN	Africa/Tunis	+01:00	+01:00
                        NA	Africa/Windhoek	+02:00	+02:00
                        US	America/Adak	−10:00	−09:00
                        US	America/Anchorage	−09:00	−08:00
                        AI	America/Anguilla	−04:00	−04:00
                        AG	America/Antigua	−04:00	−04:00
                        BR	America/Araguaina	−03:00	−03:00
                        AR	America/Argentina/Buenos_Aires	−03:00	−03:00
                        AR	America/Argentina/Catamarca	−03:00	−03:00
                        AR	America/Argentina/ComodRivadavia	−03:00	−03:00
                        AR	America/Argentina/Cordoba	−03:00	−03:00
                        AR	America/Argentina/Jujuy	−03:00	−03:00
                        AR	America/Argentina/La_Rioja	−03:00	−03:00
                        AR	America/Argentina/Mendoza	−03:00	−03:00
                        AR	America/Argentina/Rio_Gallegos	−03:00	−03:00
                        AR	America/Argentina/Salta	−03:00	−03:00
                        AR	America/Argentina/San_Juan	−03:00	−03:00
                        AR	America/Argentina/San_Luis	−03:00	−03:00
                        AR	America/Argentina/Tucuman	−03:00	−03:00
                        AR	America/Argentina/Ushuaia	−03:00	−03:00
                        AW	America/Aruba	−04:00	−04:00
                        PY	America/Asuncion	−04:00	−03:00
                        CA	America/Atikokan	−05:00	−05:00
                        US	America/Atka	−10:00	−09:00
                        BR	America/Bahia	−03:00	−03:00
                        MX	America/Bahia_Banderas	−06:00	−06:00
                        BB	America/Barbados	−04:00	−04:00
                        BR	America/Belem	−03:00	−03:00
                        BZ	America/Belize	−06:00	−06:00
                        CA	America/Blanc-Sablon	−04:00	−04:00
                        BR	America/Boa_Vista	−04:00	−04:00
                        CO	America/Bogota	−05:00	−05:00
                        US	America/Boise	−07:00	−06:00
                        AR	America/Buenos_Aires	−03:00	−03:00
                        CA	America/Cambridge_Bay	−07:00	−06:00
                        BR	America/Campo_Grande	−04:00	−04:00
                        MX	America/Cancun	−05:00	−05:00
                        VE	America/Caracas	−04:00	−04:00
                        AR	America/Catamarca	−03:00	−03:00
                        GF	America/Cayenne	−03:00	−03:00
                        KY	America/Cayman	−05:00	−05:00
                        US	America/Chicago	−06:00	−05:00
                        MX	America/Chihuahua	−06:00	−06:00
                        MX	America/Ciudad_Juarez	−07:00	−06:00
                        CA	America/Coral_Harbour	−05:00	−05:00
                        AR	America/Cordoba	−03:00	−03:00
                        CR	America/Costa_Rica	−06:00	−06:00
                        CA	America/Creston	−07:00	−07:00
                        BR	America/Cuiaba	−04:00	−04:00
                        CW	America/Curacao	−04:00	−04:00
                        GL	America/Danmarkshavn	+00:00	+00:00
                        CA	America/Dawson	−07:00	−07:00
                        CA	America/Dawson_Creek	−07:00	−07:00
                        US	America/Denver	−07:00	−06:00
                        US	America/Detroit	−05:00	−04:00
                        DM	America/Dominica	−04:00	−04:00
                        CA	America/Edmonton	−07:00	−06:00
                        BR	America/Eirunepe	−05:00	−05:00
                        SV	America/El_Salvador	−06:00	−06:00
                        MX	America/Ensenada	−08:00	−07:00
                        CA	America/Fort_Nelson	−07:00	−07:00
                        US	America/Fort_Wayne	−05:00	−04:00
                        BR	America/Fortaleza	−03:00	−03:00
                        CA	America/Glace_Bay	−04:00	−03:00
                        GL	America/Godthab	−02:00	−02:00
                        CA	America/Goose_Bay	−04:00	−03:00
                        TC	America/Grand_Turk	−05:00	−04:00
                        GD	America/Grenada	−04:00	−04:00
                        GP	America/Guadeloupe	−04:00	−04:00
                        GT	America/Guatemala	−06:00	−06:00
                        EC	America/Guayaquil	−05:00	−05:00
                        GY	America/Guyana	−04:00	−04:00
                        CA	America/Halifax	−04:00	−03:00
                        CU	America/Havana	−05:00	−04:00
                        MX	America/Hermosillo	−07:00	−07:00
                        US	America/Indiana/Indianapolis	−05:00	−04:00
                        US	America/Indiana/Knox	−06:00	−05:00
                        US	America/Indiana/Marengo	−05:00	−04:00
                        US	America/Indiana/Petersburg	−05:00	−04:00
                        US	America/Indiana/Tell_City	−06:00	−05:00
                        US	America/Indiana/Vevay	−05:00	−04:00
                        US	America/Indiana/Vincennes	−05:00	−04:00
                        US	America/Indiana/Winamac	−05:00	−04:00
                        US	America/Indianapolis	−05:00	−04:00
                        CA	America/Inuvik	−07:00	−06:00
                        CA	America/Iqaluit	−05:00	−04:00
                        JM	America/Jamaica	−05:00	−05:00
                        AR	America/Jujuy	−03:00	−03:00
                        US	America/Juneau	−09:00	−08:00
                        US	America/Kentucky/Louisville	−05:00	−04:00
                        US	America/Kentucky/Monticello	−05:00	−04:00
                        US	America/Knox_IN	−06:00	−05:00
                        BQ	America/Kralendijk	−04:00	−04:00
                        BO	America/La_Paz	−04:00	−04:00
                        PE	America/Lima	−05:00	−05:00
                        US	America/Los_Angeles	−08:00	−07:00
                        US	America/Louisville	−05:00	−04:00
                        SX	America/Lower_Princes	−04:00	−04:00
                        BR	America/Maceio	−03:00	−03:00
                        NI	America/Managua	−06:00	−06:00
                        BR	America/Manaus	−04:00	−04:00
                        MF	America/Marigot	−04:00	−04:00
                        MQ	America/Martinique	−04:00	−04:00
                        MX	America/Matamoros	−06:00	−05:00
                        MX	America/Mazatlan	−07:00	−07:00
                        AR	America/Mendoza	−03:00	−03:00
                        US	America/Menominee	−06:00	−05:00
                        MX	America/Merida	−06:00	−06:00
                        US	America/Metlakatla	−09:00	−08:00
                        MX	America/Mexico_City	−06:00	−06:00
                        PM	America/Miquelon	−03:00	−02:00
                        CA	America/Moncton	−04:00	−03:00
                        MX	America/Monterrey	−06:00	−06:00
                        UY	America/Montevideo	−03:00	−03:00
                        CA	America/Montreal	−05:00	−04:00
                        MS	America/Montserrat	−04:00	−04:00
                        BS	America/Nassau	−05:00	−04:00
                        US	America/New_York	−05:00	−04:00
                        CA	America/Nipigon	−05:00	−04:00
                        US	America/Nome	−09:00	−08:00
                        BR	America/Noronha	−02:00	−02:00
                        US	America/North_Dakota/Beulah	−06:00	−05:00
                        US	America/North_Dakota/Center	−06:00	−05:00
                        US	America/North_Dakota/New_Salem	−06:00	−05:00
                        GL	America/Nuuk	−02:00	−02:00
                        MX	America/Ojinaga	−06:00	−05:00
                        PA, CA, KY	America/Panama	−05:00	−05:00
                        CA	America/Pangnirtung	−05:00	−04:00
                        SR	America/Paramaribo	−03:00	−03:00
                        US, CA	America/Phoenix	−07:00	−07:00
                        HT	America/Port-au-Prince	−05:00	−04:00
                        TT	America/Port_of_Spain	−04:00	−04:00
                        BR	America/Porto_Acre	−05:00	−05:00
                        BR	America/Porto_Velho	−04:00	−04:00
                        PR, AG, CA, AI, AW, BL, BQ, CW, DM, GD, GP, KN, LC, MF, MS, SX, TT, VC, VG, VI	America/Puerto_Rico	−04:00	−04:00
                        CL	America/Punta_Arenas	−03:00	−03:00
                        CA	America/Rainy_River	−06:00	−05:00
                        CA	America/Rankin_Inlet	−06:00	−05:00
                        BR	America/Recife	−03:00	−03:00
                        CA	America/Regina	−06:00	−06:00
                        CA	America/Resolute	−06:00	−05:00
                        BR	America/Rio_Branco	−05:00	−05:00
                        AR	America/Rosario	−03:00	−03:00
                        MX	America/Santa_Isabel	−08:00	−07:00
                        BR	America/Santarem	−03:00	−03:00
                        CL	America/Santiago	−04:00	−03:00
                        DO	America/Santo_Domingo	−04:00	−04:00
                        BR	America/Sao_Paulo	−03:00	−03:00
                        GL	America/Scoresbysund	−01:00	+00:00
                        US	America/Shiprock	−07:00	−06:00
                        US	America/Sitka	−09:00	−08:00
                        BL	America/St_Barthelemy	−04:00	−04:00
                        CA	America/St_Johns	−03:30	−02:30
                        KN	America/St_Kitts	−04:00	−04:00
                        LC	America/St_Lucia	−04:00	−04:00
                        VI	America/St_Thomas	−04:00	−04:00
                        VC	America/St_Vincent	−04:00	−04:00
                        CA	America/Swift_Current	−06:00	−06:00
                        HN	America/Tegucigalpa	−06:00	−06:00
                        GL	America/Thule	−04:00	−03:00
                        CA	America/Thunder_Bay	−05:00	−04:00
                        MX	America/Tijuana	−08:00	−07:00
                        CA, BS	America/Toronto	−05:00	−04:00
                        VG	America/Tortola	−04:00	−04:00
                        CA	America/Vancouver	−08:00	−07:00
                        VI	America/Virgin	−04:00	−04:00
                        CA	America/Whitehorse	−07:00	−07:00
                        CA	America/Winnipeg	−06:00	−05:00
                        US	America/Yakutat	−09:00	−08:00
                        CA	America/Yellowknife	−07:00	−06:00
                        AQ	Antarctica/Casey	+11:00	+11:00
                        AQ	Antarctica/Davis	+07:00	+07:00
                        AQ	Antarctica/DumontDUrville	+10:00	+10:00
                        AU	Antarctica/Macquarie	+10:00	+11:00
                        AQ	Antarctica/Mawson	+05:00	+05:00
                        AQ	Antarctica/McMurdo	+12:00	+13:00
                        AQ	Antarctica/Palmer	−03:00	−03:00
                        AQ	Antarctica/Rothera	−03:00	−03:00
                        AQ	Antarctica/South_Pole	+12:00	+13:00
                        AQ	Antarctica/Syowa	+03:00	+03:00
                        AQ	Antarctica/Troll	+00:00	+02:00
                        AQ	Antarctica/Vostok	+06:00	+06:00
                        SJ	Arctic/Longyearbyen	+01:00	+02:00
                        YE	Asia/Aden	+03:00	+03:00
                        KZ	Asia/Almaty	+06:00	+06:00
                        JO	Asia/Amman	+03:00	+03:00
                        RU	Asia/Anadyr	+12:00	+12:00
                        KZ	Asia/Aqtau	+05:00	+05:00
                        KZ	Asia/Aqtobe	+05:00	+05:00
                        TM	Asia/Ashgabat	+05:00	+05:00
                        TM	Asia/Ashkhabad	+05:00	+05:00
                        KZ	Asia/Atyrau	+05:00	+05:00
                        IQ	Asia/Baghdad	+03:00	+03:00
                        BH	Asia/Bahrain	+03:00	+03:00
                        AZ	Asia/Baku	+04:00	+04:00
                        TH, CX, KH, LA, VN	Asia/Bangkok	+07:00	+07:00
                        RU	Asia/Barnaul	+07:00	+07:00
                        LB	Asia/Beirut	+02:00	+03:00
                        KG	Asia/Bishkek	+06:00	+06:00
                        BN	Asia/Brunei	+08:00	+08:00
                        IN	Asia/Calcutta	+05:30	+05:30
                        RU	Asia/Chita	+09:00	+09:00
                        MN	Asia/Choibalsan	+08:00	+08:00
                        CN	Asia/Chongqing	+08:00	+08:00
                        CN	Asia/Chungking	+08:00	+08:00
                        LK	Asia/Colombo	+05:30	+05:30
                        BD	Asia/Dacca	+06:00	+06:00
                        SY	Asia/Damascus	+03:00	+03:00
                        BD	Asia/Dhaka	+06:00	+06:00
                        TL	Asia/Dili	+09:00	+09:00
                        AE, OM, RE, SC, TF	Asia/Dubai	+04:00	+04:00
                        TJ	Asia/Dushanbe	+05:00	+05:00
                        CY	Asia/Famagusta	+02:00	+03:00
                        PS	Asia/Gaza	+02:00	+03:00
                        CN	Asia/Harbin	+08:00	+08:00
                        PS	Asia/Hebron	+02:00	+03:00
                        VN	Asia/Ho_Chi_Minh	+07:00	+07:00
                        HK	Asia/Hong_Kong	+08:00	+08:00
                        MN	Asia/Hovd	+07:00	+07:00
                        RU	Asia/Irkutsk	+08:00	+08:00
                        TR	Asia/Istanbul	+03:00	+03:00
                        ID	Asia/Jakarta	+07:00	+07:00
                        ID	Asia/Jayapura	+09:00	+09:00
                        IL	Asia/Jerusalem	+02:00	+03:00
                        AF	Asia/Kabul	+04:30	+04:30
                        RU	Asia/Kamchatka	+12:00	+12:00
                        PK	Asia/Karachi	+05:00	+05:00
                        CN	Asia/Kashgar	+06:00	+06:00
                        NP	Asia/Kathmandu	+05:45	+05:45
                        NP	Asia/Katmandu	+05:45	+05:45
                        RU	Asia/Khandyga	+09:00	+09:00
                        IN	Asia/Kolkata	+05:30	+05:30
                        RU	Asia/Krasnoyarsk	+07:00	+07:00
                        MY	Asia/Kuala_Lumpur	+08:00	+08:00
                        MY, BN	Asia/Kuching	+08:00	+08:00
                        KW	Asia/Kuwait	+03:00	+03:00
                        MO	Asia/Macao	+08:00	+08:00
                        MO	Asia/Macau	+08:00	+08:00
                        RU	Asia/Magadan	+11:00	+11:00
                        ID	Asia/Makassar	+08:00	+08:00
                        PH	Asia/Manila	+08:00	+08:00
                        OM	Asia/Muscat	+04:00	+04:00
                        CY	Asia/Nicosia	+02:00	+03:00
                        RU	Asia/Novokuznetsk	+07:00	+07:00
                        RU	Asia/Novosibirsk	+07:00	+07:00
                        RU	Asia/Omsk	+06:00	+06:00
                        KZ	Asia/Oral	+05:00	+05:00
                        KH	Asia/Phnom_Penh	+07:00	+07:00
                        ID	Asia/Pontianak	+07:00	+07:00
                        KP	Asia/Pyongyang	+09:00	+09:00
                        QA, BH	Asia/Qatar	+03:00	+03:00
                        KZ	Asia/Qostanay	+06:00	+06:00
                        KZ	Asia/Qyzylorda	+05:00	+05:00
                        MM	Asia/Rangoon	+06:30	+06:30
                        SA, AQ, KW, YE	Asia/Riyadh	+03:00	+03:00
                        VN	Asia/Saigon	+07:00	+07:00
                        RU	Asia/Sakhalin	+11:00	+11:00
                        UZ	Asia/Samarkand	+05:00	+05:00
                        KR	Asia/Seoul	+09:00	+09:00
                        CN	Asia/Shanghai	+08:00	+08:00
                        SG, MY	Asia/Singapore	+08:00	+08:00
                        RU	Asia/Srednekolymsk	+11:00	+11:00
                        TW	Asia/Taipei	+08:00	+08:00
                        UZ	Asia/Tashkent	+05:00	+05:00
                        GE	Asia/Tbilisi	+04:00	+04:00
                        IR	Asia/Tehran	+03:30	+03:30
                        IL	Asia/Tel_Aviv	+02:00	+03:00
                        BT	Asia/Thimbu	+06:00	+06:00
                        BT	Asia/Thimphu	+06:00	+06:00
                        JP	Asia/Tokyo	+09:00	+09:00
                        RU	Asia/Tomsk	+07:00	+07:00
                        ID	Asia/Ujung_Pandang	+08:00	+08:00
                        MN	Asia/Ulaanbaatar	+08:00	+08:00
                        MN	Asia/Ulan_Bator	+08:00	+08:00
                        CN, AQ	Asia/Urumqi	+06:00	+06:00
                        RU	Asia/Ust-Nera	+10:00	+10:00
                        LA	Asia/Vientiane	+07:00	+07:00
                        RU	Asia/Vladivostok	+10:00	+10:00
                        RU	Asia/Yakutsk	+09:00	+09:00
                        MM, CC	Asia/Yangon	+06:30	+06:30
                        RU	Asia/Yekaterinburg	+05:00	+05:00
                        AM	Asia/Yerevan	+04:00	+04:00
                        PT	Atlantic/Azores	−01:00	+00:00
                        BM	Atlantic/Bermuda	−04:00	−03:00
                        ES	Atlantic/Canary	+00:00	+01:00
                        CV	Atlantic/Cape_Verde	−01:00	−01:00
                        FO	Atlantic/Faeroe	+00:00	+01:00
                        FO	Atlantic/Faroe	+00:00	+01:00
                        SJ	Atlantic/Jan_Mayen	+01:00	+02:00
                        PT	Atlantic/Madeira	+00:00	+01:00
                        IS	Atlantic/Reykjavik	+00:00	+00:00
                        GS	Atlantic/South_Georgia	−02:00	−02:00
                        SH	Atlantic/St_Helena	+00:00	+00:00
                        FK	Atlantic/Stanley	−03:00	−03:00
                        AU	Australia/ACT	+10:00	+11:00
                        AU	Australia/Adelaide	+09:30	+10:30
                        AU	Australia/Brisbane	+10:00	+10:00
                        AU	Australia/Broken_Hill	+09:30	+10:30
                        AU	Australia/Canberra	+10:00	+11:00
                        AU	Australia/Currie	+10:00	+11:00
                        AU	Australia/Darwin	+09:30	+09:30
                        AU	Australia/Eucla	+08:45	+08:45
                        AU	Australia/Hobart	+10:00	+11:00
                        AU	Australia/LHI	+10:30	+11:00
                        AU	Australia/Lindeman	+10:00	+10:00
                        AU	Australia/Lord_Howe	+10:30	+11:00
                        AU	Australia/Melbourne	+10:00	+11:00
                        AU	Australia/North	+09:30	+09:30
                        AU	Australia/NSW	+10:00	+11:00
                        AU	Australia/Perth	+08:00	+08:00
                        AU	Australia/Queensland	+10:00	+10:00
                        AU	Australia/South	+09:30	+10:30
                        AU	Australia/Sydney	+10:00	+11:00
                        AU	Australia/Tasmania	+10:00	+11:00
                        AU	Australia/Victoria	+10:00	+11:00
                        AU	Australia/West	+08:00	+08:00
                        AU	Australia/Yancowinna	+09:30	+10:30
                        BR	Brazil/Acre	−05:00	−05:00
                        BR	Brazil/DeNoronha	−02:00	−02:00
                        BR	Brazil/East	−03:00	−03:00
                        BR	Brazil/West	−04:00	−04:00
                        CA	Canada/Atlantic	−04:00	−03:00
                        CA	Canada/Central	−06:00	−05:00
                        CA	Canada/Eastern	−05:00	−04:00
                        CA	Canada/Mountain	−07:00	−06:00
                        CA	Canada/Newfoundland	−03:30	−02:30
                        CA	Canada/Pacific	−08:00	−07:00
                        CA	Canada/Saskatchewan	−06:00	−06:00
                        CA	Canada/Yukon	−07:00	−07:00
                        CL	Chile/Continental	−04:00	−03:00
                        CL	Chile/EasterIsland	−06:00	−05:00
                        CU	Cuba	−05:00	−04:00
                        EG	Egypt	+02:00	+03:00
                        IE	Eire	+01:00	+00:00
                        NL	Europe/Amsterdam	+01:00	+02:00
                        AD	Europe/Andorra	+01:00	+02:00
                        RU	Europe/Astrakhan	+04:00	+04:00
                        GR	Europe/Athens	+02:00	+03:00
                        GB	Europe/Belfast	+00:00	+01:00
                        RS, BA, HR, ME, MK, SI	Europe/Belgrade	+01:00	+02:00
                        DE, DK, NO, SE, SJ	Europe/Berlin	+01:00	+02:00
                        SK	Europe/Bratislava	+01:00	+02:00
                        BE, LU, NL	Europe/Brussels	+01:00	+02:00
                        RO	Europe/Bucharest	+02:00	+03:00
                        HU	Europe/Budapest	+01:00	+02:00
                        DE	Europe/Busingen	+01:00	+02:00
                        MD	Europe/Chisinau	+02:00	+03:00
                        DK	Europe/Copenhagen	+01:00	+02:00
                        IE	Europe/Dublin	+01:00	+00:00
                        GI	Europe/Gibraltar	+01:00	+02:00
                        GG	Europe/Guernsey	+00:00	+01:00
                        FI, AX	Europe/Helsinki	+02:00	+03:00
                        IM	Europe/Isle_of_Man	+00:00	+01:00
                        TR	Europe/Istanbul	+03:00	+03:00
                        JE	Europe/Jersey	+00:00	+01:00
                        RU	Europe/Kaliningrad	+02:00	+02:00
                        UA	Europe/Kiev	+02:00	+03:00
                        RU	Europe/Kirov	+03:00	+03:00
                        UA	Europe/Kyiv	+02:00	+03:00
                        PT	Europe/Lisbon	+00:00	+01:00
                        SI	Europe/Ljubljana	+01:00	+02:00
                        GB, GG, IM, JE	Europe/London	+00:00	+01:00
                        LU	Europe/Luxembourg	+01:00	+02:00
                        ES	Europe/Madrid	+01:00	+02:00
                        MT	Europe/Malta	+01:00	+02:00
                        AX	Europe/Mariehamn	+02:00	+03:00
                        BY	Europe/Minsk	+03:00	+03:00
                        MC	Europe/Monaco	+01:00	+02:00
                        RU	Europe/Moscow	+03:00	+03:00
                        CY	Europe/Nicosia	+02:00	+03:00
                        NO	Europe/Oslo	+01:00	+02:00
                        FR, MC	Europe/Paris	+01:00	+02:00
                        ME	Europe/Podgorica	+01:00	+02:00
                        CZ, SK	Europe/Prague	+01:00	+02:00
                        LV	Europe/Riga	+02:00	+03:00
                        IT, SM, VA	Europe/Rome	+01:00	+02:00
                        RU	Europe/Samara	+04:00	+04:00
                        SM	Europe/San_Marino	+01:00	+02:00
                        BA	Europe/Sarajevo	+01:00	+02:00
                        RU	Europe/Saratov	+04:00	+04:00
                        RU, UA	Europe/Simferopol	+03:00	+03:00
                        MK	Europe/Skopje	+01:00	+02:00
                        BG	Europe/Sofia	+02:00	+03:00
                        SE	Europe/Stockholm	+01:00	+02:00
                        EE	Europe/Tallinn	+02:00	+03:00
                        AL	Europe/Tirane	+01:00	+02:00
                        MD	Europe/Tiraspol	+02:00	+03:00
                        RU	Europe/Ulyanovsk	+04:00	+04:00
                        UA	Europe/Uzhgorod	+02:00	+03:00
                        LI	Europe/Vaduz	+01:00	+02:00
                        VA	Europe/Vatican	+01:00	+02:00
                        AT	Europe/Vienna	+01:00	+02:00
                        LT	Europe/Vilnius	+02:00	+03:00
                        RU	Europe/Volgograd	+03:00	+03:00
                        PL	Europe/Warsaw	+01:00	+02:00
                        HR	Europe/Zagreb	+01:00	+02:00
                        UA	Europe/Zaporozhye	+02:00	+03:00
                        CH, DE, LI	Europe/Zurich	+01:00	+02:00
                        GB	GB	+00:00	+01:00
                        GB	GB-Eire	+00:00	+01:00
                        HK	Hongkong	+08:00	+08:00
                        IS	Iceland	+00:00	+00:00
                        MG	Indian/Antananarivo	+03:00	+03:00
                        IO	Indian/Chagos	+06:00	+06:00
                        CX	Indian/Christmas	+07:00	+07:00
                        CC	Indian/Cocos	+06:30	+06:30
                        KM	Indian/Comoro	+03:00	+03:00
                        TF	Indian/Kerguelen	+05:00	+05:00
                        SC	Indian/Mahe	+04:00	+04:00
                        MV, TF	Indian/Maldives	+05:00	+05:00
                        MU	Indian/Mauritius	+04:00	+04:00
                        YT	Indian/Mayotte	+03:00	+03:00
                        RE	Indian/Reunion	+04:00	+04:00
                        IR	Iran	+03:30	+03:30
                        IL	Israel	+02:00	+03:00
                        JM	Jamaica	−05:00	−05:00
                        JP	Japan	+09:00	+09:00
                        MH	Kwajalein	+12:00	+12:00
                        LY	Libya	+02:00	+02:00
                        MX	Mexico/BajaNorte	−08:00	−07:00
                        MX	Mexico/BajaSur	−07:00	−07:00
                        MX	Mexico/General	−06:00	−06:00
                        US	Navajo	−07:00	−06:00
                        NZ	NZ	+12:00	+13:00
                        NZ	NZ-CHAT	+12:45	+13:45
                        WS	Pacific/Apia	+13:00	+13:00
                        NZ, AQ	Pacific/Auckland	+12:00	+13:00
                        PG	Pacific/Bougainville	+11:00	+11:00
                        NZ	Pacific/Chatham	+12:45	+13:45
                        FM	Pacific/Chuuk	+10:00	+10:00
                        CL	Pacific/Easter	−06:00	−05:00
                        VU	Pacific/Efate	+11:00	+11:00
                        KI	Pacific/Enderbury	+13:00	+13:00
                        TK	Pacific/Fakaofo	+13:00	+13:00
                        FJ	Pacific/Fiji	+12:00	+12:00
                        TV	Pacific/Funafuti	+12:00	+12:00
                        EC	Pacific/Galapagos	−06:00	−06:00
                        PF	Pacific/Gambier	−09:00	−09:00
                        SB, FM	Pacific/Guadalcanal	+11:00	+11:00
                        GU, MP	Pacific/Guam	+10:00	+10:00
                        US	Pacific/Honolulu	−10:00	−10:00
                        US	Pacific/Johnston	−10:00	−10:00
                        KI	Pacific/Kanton	+13:00	+13:00
                        KI	Pacific/Kiritimati	+14:00	+14:00
                        FM	Pacific/Kosrae	+11:00	+11:00
                        MH	Pacific/Kwajalein	+12:00	+12:00
                        MH	Pacific/Majuro	+12:00	+12:00
                        PF	Pacific/Marquesas	−09:30	−09:30
                        UM	Pacific/Midway	−11:00	−11:00
                        NR	Pacific/Nauru	+12:00	+12:00
                        NU	Pacific/Niue	−11:00	−11:00
                        NF	Pacific/Norfolk	+11:00	+12:00
                        NC	Pacific/Noumea	+11:00	+11:00
                        AS, UM	Pacific/Pago_Pago	−11:00	−11:00
                        PW	Pacific/Palau	+09:00	+09:00
                        PN	Pacific/Pitcairn	−08:00	−08:00
                        FM	Pacific/Pohnpei	+11:00	+11:00
                        FM	Pacific/Ponape	+11:00	+11:00
                        PG, AQ, FM	Pacific/Port_Moresby	+10:00	+10:00
                        CK	Pacific/Rarotonga	−10:00	−10:00
                        MP	Pacific/Saipan	+10:00	+10:00
                        AS	Pacific/Samoa	−11:00	−11:00
                        PF	Pacific/Tahiti	−10:00	−10:00
                        KI, MH, TV, UM, WF	Pacific/Tarawa	+12:00	+12:00
                        TO	Pacific/Tongatapu	+13:00	+13:00
                        FM	Pacific/Truk	+10:00	+10:00
                        UM	Pacific/Wake	+12:00	+12:00
                        WF	Pacific/Wallis	+12:00	+12:00
                        FM	Pacific/Yap	+10:00	+10:00
                        US	US/Alaska	−09:00	−08:00
                        US	US/Aleutian	−10:00	−09:00
                        US	US/Arizona	−07:00	−07:00
                        US	US/Central	−06:00	−05:00
                        US	US/East-Indiana	−05:00	−04:00
                        US	US/Eastern	−05:00	−04:00
                        US	US/Hawaii	−10:00	−10:00
                        US	US/Indiana-Starke	−06:00	−05:00
                        US	US/Michigan	−05:00	−04:00
                        US	US/Mountain	−07:00	−06:00
                        US	US/Pacific	−08:00	−07:00
                        AS	US/Samoa	−11:00	−11:00

                        """;

        return ExtractTzData(tzData: tzData);
    }

    /// <summary>
    ///     Extracts and sorts timezone data from a given string.
    /// </summary>
    /// <param name="tzData">The string containing timezone data, where each line represents a different timezone.</param>
    /// <remarks>
    ///     Just to add some human-made comments in here...what this does is that we take the tab-delimited table from
    ///     Wikipedia and convert it into a list. Since the table has a lot of unnecessary stuff I don't want to fish out each
    ///     time there's an update and so we only look at rows where the first column isn't blank, the STD column contains a
    ///     number.
    /// </remarks>
    /// <returns>A list of sorted timezone data, each item in the format "(STDCol/DSTCol) # TimeZoneCol".</returns>
    private static List<string> ExtractTzData(string tzData)
    {
        // zero-index
        int codeCol = 0;
        int timeZoneCol = 1;
        int stdCol = 2;
        int dstCol = 3;

        // wikipedia has some odd dashes.
        string[] lines = tzData.Replace(oldValue: "\u2212", newValue: "-")
                               .Split('\n');
        List<string[]> data = [];
        foreach (string line in lines)
        {
            string[] columns = line.Split('\t');
            if (!string.IsNullOrWhiteSpace(value: columns[codeCol]))
            {
                data.Add(item: columns);
            }
        }

        List<string> sortedData = data
                                 .Where(predicate: d =>
                                      d[stdCol].Length > 1 &&
                                      char.IsDigit(c: d[stdCol][index: 1]))
                                 //.GroupBy(keySelector: d =>
                                 //             new
                                 //             {
                                 //                 STD = d[stdCol], DST = d[dstCol]
                                 //             })
                                 //.Select(selector: g => g.First())
                                 .OrderBy(keySelector: d => d[stdCol])
                                 .Select(selector: d =>
                                      $"({d[stdCol]}/{d[dstCol]}) # {d[timeZoneCol]}")
                                 .ToList();
        return sortedData;
    }

    /// <summary>
    ///     Finds and returns the first line of timezone data that matches the input.
    /// </summary>
    /// <param name="offsetTimeToMatch">The string to match against the STD column of the timezone data.</param>
    /// <returns>
    ///     The first line of timezone data where the STD column value equals the input, or null if no matching line is
    ///     found.
    /// </returns>
    internal static string GetFirstMatchingTzData(string offsetTimeToMatch)
    {
        // Call the ExtractTzData method to get the sorted data
        List<string> sortedData = GetTimeZones();
        // Find the first line where the STD column value equals the input
        foreach (string line in sortedData)
        {
            // Split the line into columns
            string[] columns = line.Split('#');
            // Check if the STD column value equals the input
            if (columns[0]
               .Split('/')[0]
               .TrimStart('(')
               .Trim() ==
                offsetTimeToMatch)
            {
                // Return the matching line
                return line;
            }
        }

        // If no matching line was found, return null
        return null;
    }
}
