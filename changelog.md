# GeoTagNinja Changelog
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