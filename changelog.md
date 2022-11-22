# GeoTagNinja Changelog
v0.6.83XX [202211XX]
- Added some logic to locally store (for the length of the session) each file's data. This is so that if the actual image/xmp files don't change then there's no need to re-parse everything _again_. It's slow and pointless.
- Added functionality to "Get Data from Other File" in the Edit File section.
- Changed logic around the addition of sidecar XMP files and (possibly) overwriting the source image file.
-- Added some logic to the above to allow for "Original Files DateTime" to be reset to CreateDateTime by default for RAW images. (These can all be changed in Settings/File Specific)
- Bugfixes:
-- Error msg/image name not showing properly when file gone missing
-- The previous version introduced an error in Non-English regions when the user clicked on the map. This has been fixed.
-- Rewrote logic re: UK Cities/Regions being mixed up. Now only applies to London.

v0.6.8358 [20221119]
- Finally managed to get hold of ReSharper so the code has been refactored in totality. Famous last words but it shouldn't affect usage.
- Added a ".." to the main grid (listView) where applicable (parent folder).
- Added the capability to navigate to the top of the folder structure. (e.g. MyComputer and then list the drives.)
- Changed how the previews get created. This will hopefully result in faster preview-creation. The orientation-problem is still unsolved but it's likely to remain so for the time being.
- Bugfixes:
-- If a user's "Pictures" (or any other "Special") folder had been moved and renamed the would break because Windows treats special folders in odd ways. (e.g. if the Pics folder is called "Pics" Windows would still show "Pictures", which doesn't per se exist.)
-- If the user had chosen "Delete All GPS Data" and subsequently added GPS data the addition would not have gone through upon save. This is now fixed.
-- Rewrote the logic of (re)creating sidecar XMP files as the original logic would pull data from the RAW file (only), possibly overwriting Adobe-specific stuff that had been stored in an already-existing XMP.

v0.6.8350 [20221111]
- NEW FEATURE: (experimental for now) -> sync/import GPS Track Files.
-- Version bump as for new feature.
- Added the capability to resize the main elements. Their positions aren't saved for now.
- Added a button to the ToolStrip to "get all from the web" (toponomy & altitude for selected items)
- Bumped exifTool to v12.50
- Bugfixes 
-- Using keyboard/keys on the main grid (listView).
-- ToolStripButtons' ToolTips now work as intended.
-- Removing data when sidecar XMPs are enabled now actually removes data from the sidecar XMPs
-- Code updates re how integers and decimals are handled in various culture settings. (tested on HU and FR)
-- IDK (i don't know) - so far 39 files have been updated, that is almost everything. I should do proper commits, not one-huge-commit, it is bad practice.
- Various further fixes and internal enhancements
- Updated the logic of language file creation
- Code refactoring
- Updates to nuGet packages
- Special thanks to pbranly for very extensive testing.

v0.5.8334 [20221026]
- Bumped exifTool to v12.49
- Added Multi-Select capability to the map. (i.e. multiple pins now show properly)
- Partial French translations added in (thanks to pbranly)
- Update checks will now only happen once a week. No need to spam the world with API requests.

v0.5.8333 [20221025]
- Changed the logic relating to checking the newest version of exifTool online. The original was querying an API that hadn't been updated for months.

v0.5.8314 [20221006] + v0.5.8318 [20221010]
- Added a "don't ask again" button for the "loc-to-file" dialogbox. This is session specific (if you close/restart the app, it will come back again). [Updated this logic in the v0.5.8318 release]
- Minor code cleanup

v0.5.8304 [20220926]
- Added a new setting to control whether to default to lat/long 0/0 on the map if there is no geodata in the file clicked.

v0.5.8293 [20220915]
- Disabled the notification for "1 new file created" when saving RAW files that don't already have an XMP s/c file. This came up for each file separately and was a bit annoying.
- Changed max zoom on map to more "zoomier" (from lvl 13 to 19). FWIW I think the new one is the "zoomiest" the system allows.
- Bugfixes

v0.5.8270 [20220823]
- [WIP] Time shift groundwork.
- Added check for exifTool version change tracking
- Added check for GeoTagNinja version change tracking
- Added (better) commentary to the code
- Changed Settings/Edit Forms' behaviour not to block other windows.
- Bugfixes

v0.5.8251 [20220804]
- Added "Remove GeoData" to main form as well as Edit form
- Added this changelog.md file
- Added a readme to the ExtraFiles folder
- Added handling to messageBox texts being read from SQLite
 
v0.5.8248 [20220801]
- Initial release