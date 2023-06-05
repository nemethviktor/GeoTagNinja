﻿# GeoTagNinja Changelog

**Build 84XX [202304XX]**
- NEW & UPDATED:
  - Added a "Collections Mode", in particular to work with Jeffrey Frield's LightRoom Classic Plugin "Run Any Command". Details on how to use this are in the readme.
  - Rewrote the sorting logic to account for the data type of the column being sorted (i.e. date, number, text etc)
  - Added Country to Favourites
  - Bumped exifTool to v12.58
- BUGS & FIXES:
  - Fixed a bug where the TZ dropdown in Import GPX could cause a crash if the user was in a TZ that wasn't on the dropdown.
  - Paste-values logic minor revamp
  - Changed settings to default-write-into-image-file to false
  - Rewrote the File Edit Form load process. The existing one took way too long because it parsed data for a number of Objects that were entirely irrelevant.
  - Rewrote the Track Import logic not to overflow when too many files were involved. (see ticket 78).

**Build 8469 [20230310]**
- NEW & UPDATED:
  - Entirely rewrote the code to work off DirectoryElements rather than DataTables. This is techy so I won't get into it here, for anyone cares read the code or discussion #62
  - Added functionality to auto-navigate to a selected Favourite in the dropdown.
  - Added a "recall last Track Sync Shift" option.
  - Added logic for the pick-from-API-choices Form to accept doubleClick and (keyboard) Enter (key)
  - Added a button in the Paste Form for generic All/None
  - Added a button in the Paste Form for recalling the last Paste settings
  - Added an option to halt further API pulls if one failed
  - Changed certain numeric TextBoxes to NumericUpDowns.
  - Added logic to replace hard-coded City-definition logic with user choices.
  - Changed versioning logic. Will use simple build numbers from now on in the "public facing" element of the app. Windows & Visual Studio still require the ##.##.#### logic rather than just a build number but the About section won't show it..
- BUGS & FIXES:
  - Fixed a bug where NumericUpDown changes didn't trigger setting fonts to Bold
  - Fixed the map zooming way too far out when negative lat/long values are present
  - Changed the pick-from-API-choices Form to be 1-based-index rather than 0-based
  - Fixed messageBox captions always saying "Error" as output
  - Fixed a bug where one or two Settings were ignored on load
  - Fixed a variety of random bugs relating to time-shift copypastes not entirely behaving
  - Further fixed a variety of bugs that yet again I don't entirely remember

**Build 8421 [20230121]**
- NEW & UPDATED:
  - Added a "Manage Favourites" element to the Settings area.
  - Added Custom Rules. This is experimental for now. Wiki [here](https://github.com/nemethviktor/GeoTagNinja/wiki/Settings-&-Custom-Rules).
  - Updated CountryCode ISO SQLite logic with CSV
  - Updated Altitude-pull (from Web) logic. At the same time removed the separate button(s) to do that as it's now part of the Toponomy pull.
  - Added an option for changing the API language. This won't affect countrynames though because they derive from the CountryCode so there might be little visibility of this change in practice.
  - Added an option for GPX import to ignore reverse geocoding.
  - Bumped exifTool to v12.55
  - Made the installer look a tad prettier. (I know, right?!)
  - Rewrote parts of the Settings Form and the underlying logic.
- BUGS & FIXES:
  - Fixed Paste not actually pasting Country
  - Fixed crash when API offline or Unauth'd
  - Fixed crash when User Folder not existing
  - Fixed a bug re: the API sending back invalid Altitude data. While I can't do much about that per se but now if there is existing data in the file then that will be retained rather than making it -32756m or some such.
  - Fixed a recursion bug while loading the Edit Form. Should run faster now.
  - Fixed a bug re: how DateTime formats are handled. 
  - Fixed a bug re: Time-Shift values not being properly recalled if user didn't explicitly refresh folder.
  - Fixed a bug in Settings where subject to certain preceding actions the wrong checkbox value would be saved.

**Build 8391 [20221222]**
- NEW & UPDATED:
  - Added Column Sort (kudos to Urmel, thank you!)
  - Added Column Show/Hide (kudos to Urmel, thank you!)
  - Added a Setting to return X rows of data from the API within Y miles radius (I think it's miles anyway, maybe KM)
  - Added a Setting to optionally replace empty Toponomy values with whatever the user wants
  - Updated Time-Shift to work properly w/ Copy Paste. 
  - Updated Read-in logic to process the whole folder in one go. Should run _a lot_ faster now.
  - Updated the Paste logic (preselection based on what has changed in the source file.)
  - Updated GPX import to only apply to selected files, not the whole folder
  - Updated logic so that map refreshes without markers when there are no files with coordinates selected (rather than leave markers on, which can be misleading)
  - Updated Translations. (thanks pbranly)
  - Updated City/Sublocation logic (thanks Clariden)
  - Bumped exifTool to v12.52
- BUGS & FIXES:
  - Eliminate MD5 checks. Basically the bloody thing takes longer than re-parsing the whole folder.
  - Removed the built-in webView2 installer because it was more of a pain in the backside than benefit. Updated the readme w/ instructions should this cause a problem.
  - Fixed logic to ensure if a value in the FrmEdit is replaced with a blank it actually does get queued up for writing.
  - Stuff I don't remember.

**Build 8370 [20221201]**
- NEW & UPDATED:
  - Added Modify TakenDate & CreateDate (it's in the Edit File section - and no the time-shift cannot be copy-pasted yet -> shift works in terms of modifying the literal DateTime at the moment not as in a proper copyable "shift" value.)
  - Added a hold/blocker on entering a folder until the folder loads. This is annoying but is needed to prevent users from starting operations before the files are processed.
  - Added some logic to locally store (for the length of the session) each file's data. This is so that if the actual image/xmp files don't change then there's no need to re-parse everything _again_. It's slow and pointless.
  - Added functionality to "Get Data from Other File" in the Edit File section.
  - Changed logic around the addition of sidecar XMP files and (possibly) overwriting the source image file.
  - Added this changelog.md file to the project outputs. I don't expect anyone to read it locally but at least now it's possible.
  - Bumped exifTool to v12.51
  - Added some logic to the above to allow for "Original Files DateTime" to be reset to CreateDateTime by default for RAW images. (These can all be changed in Settings/File Specific)
  - Rewrote the Excel macro that deals with exporting languages. It can now also import. (less relevant for the users but makes my life easier.)
- BUGS & FIXES:
  - Fixed Copy-Paste properly. Users can now pick what to paste. It's faster too now.
  - Error msg/image name not showing properly when file gone missing
  - The previous version introduced an error in Non-English regions when the user clicked on the map. This has been fixed.
  - Rewrote logic re: UK Cities/Regions being mixed up. Now only applies to London.
  - Thoroughly renamed all my rather ambigous "filePath" variables in the code to specify fileNameWithPath and fileNameWithoutPath. This isn't a material change but it makes things a bit more readable.
  - Further French lang updates. Thanks to pbranly once again.

**Build 8358 [20221119]**
- NEW & UPDATED:
  - Finally managed to get hold of ReSharper so the code has been refactored in totality. Famous last words but it shouldn't affect usage.
  - Added a ".." to the main grid (listView) where applicable (parent folder).
  - Added the capability to navigate to the top of the folder structure. (e.g. MyComputer and then list the drives.)
- BUGS & FIXES:
  - Changed how the previews get created. This will hopefully result in faster preview-creation. The orientation-problem is still unsolved but it's likely to remain so for the time being.
  - If a user's "Pictures" (or any other "Special") folder had been moved and renamed the would break because Windows treats special folders in odd ways. (e.g. if the Pics folder is called "Pics" Windows would still show "Pictures", which doesn't per se exist.)
  - If the user had chosen "Delete All GPS Data" and subsequently added GPS data the addition would not have gone through upon save. This is now fixed.
  - Rewrote the logic of (re)creating sidecar XMP files as the original logic would pull data from the RAW file (only), possibly overwriting Adobe-specific stuff that had been stored in an already-existing XMP.

**Build 8350 [20221111]**
- NEW & UPDATED:
  - Added sync/import GPS Track Files.
  - Added the capability to resize the main elements. Their positions aren't saved for now.
  - Added a button to the ToolStrip to "get all from the web" (toponomy & altitude for selected items)
  - Bumped exifTool to v12.50
  - Updated the logic of language file creation
  - Code refactoring
  - Updates to nuGet packages
  - Special thanks to pbranly for very extensive testing.
- BUGS & FIXES:
  - Using keyboard/keys on the main grid (listView).
  - ToolStripButtons' ToolTips now work as intended.
  - Removing data when sidecar XMPs are enabled now actually removes data from the sidecar XMPs
  - Code updates re how integers and decimals are handled in various culture settings. (tested on HU and FR)
  - IDK (i don't know) - so far 39 files have been updated, that is almost everything. I should do proper commits, not one-huge-commit, it is bad practice.

**Build 8334 [20221026]**
- NEW & UPDATED:
  - Bumped exifTool to v12.49
  - Added Multi-Select capability to the map. (i.e. multiple pins now show properly)
  - Partial French translations added in (thanks to pbranly)
  - Update checks will now only happen once a week. No need to spam the world with API requests.

**Build 8333 [20221025]**
- NEW & UPDATED:
  - Changed the logic relating to checking the newest version of exifTool online. The original was querying an API that hadn't been updated for months.

**Build 8314 [20221006] + Build 8318 [20221010]**
- NEW & UPDATED:
  - Added a "don't ask again" button for the "loc-to-file" dialogbox. This is session specific (if you close/restart the app, it will come back again). [Updated this logic in the Build 8318 release]
- BUGS & FIXES:
  - Minor code cleanup

**Build 8304 [20220926]**
- NEW & UPDATED:
  - Added a new setting to control whether to default to lat/long 0/0 on the map if there is no geodata in the file clicked.

**Build 8293 [20220915]**
- NEW & UPDATED:
  - Disabled the notification for "1 new file created" when saving RAW files that don't already have an XMP s/c file. This came up for each file separately and was a bit annoying.
  - Changed max zoom on map to more "zoomier" (from lvl 13 to 19). FWIW I think the new one is the "zoomiest" the system allows.
- BUGS & FIXES:
  - Various

**Build 8270 [20220823]**
- NEW & UPDATED:
  - Added check for exifTool version change tracking
  - Added check for GeoTagNinja version change tracking
  - Added (better) commentary to the code
  - Changed Settings/Edit Forms' behaviour not to block other windows.
- BUGS & FIXES:
  - Various

**Build 8251 [20220804]**
- NEW & UPDATED:
  - Added "Remove GeoData" to main form as well as Edit form
  - Added this changelog.md file
  - Added a readme to the ExtraFiles folder
  - Added handling to messageBox texts being read from SQLite
 
**Build 8248 [20220801]**
- NEW & UPDATED:
  - Initial release