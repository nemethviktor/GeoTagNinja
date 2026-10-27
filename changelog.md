# GeoTagNinja Changelog

**Build 9xxx [202xxxxx]** (upcoming release)
- NEW & UPDATED:
	- `Thumbnail View` has moved from `Settings` to a new `View` menu (`View` > `Details` / `Thumbnails`) and can be switched at any time without restarting. The `Show Thumbnails` tick in `Settings` is gone; your previous choice carries over because it is stored under the same setting.
	- The thumbnail size can be changed on the fly, as in other image browsers: `Ctrl` + mouse wheel over the thumbnails, the slider at the bottom right of the file list, or `View` > `Thumbnail Size` (four presets, plus `Ctrl` `-` / `Ctrl` `+`). The size is remembered.
	- The thumbnail view is now a purpose-built control instead of the list's icon mode. It draws only the pictures on screen, so memory no longer grows with the number of files, folders of any size scroll smoothly, and thumbnails keep their proportions (no more black bars). You can sort it from `View` > `Sort By`, rename a file with `F2`, select several pictures by dragging a rectangle over empty space, and right-click (or `Shift`+`F10`) for the same actions menu as the list; filtering by column is still `Details` only.
	- The metadata (coordinates, dates, ...) of the rows you can see is now read first, then the next screenful, then the rest of the folder, instead of strictly top to bottom.
	- Thumbnails are remembered between sessions: an in-memory cache (512 MB cap) backed by a small database, `thumbnailcache.sqlite`, in the app's data folder (256 MB cap, entries unused for 90 days are dropped). Reopening a folder you have browsed before shows its thumbnails almost immediately. It is safe to delete that file at any time. It is flushed and closed when the app exits; if the app was killed or crashed, the next start checks it and rebuilds it from scratch if it was damaged.
	- Much faster icon-view thumbnails. They are now built in memory by `ThumbnailGenerator` instead of via temporary JPEG files: the JPEG already inside the file (EXIF thumbnail, or the RAW's embedded preview) is used where there is one, otherwise the image is decoded at reduced size, and a full decode is the last resort. They are also generated on a few background threads (`ThumbnailWorker`), so they no longer hold up the metadata read of the next file.
	- Much faster preview pane. The preview is now produced off the UI thread, so the window no longer freezes while a large JPEG is decoded, and a newer selection cancels an older one instead of queueing behind it. RAW previews are read in-process (a right-sized JPEG embedded in the file, via LibRaw: about 1600 px for the preview pane, about 600 px for thumbnails, instead of decoding the 36-megapixel one), HEIC/WebP via Magick, and recently shown ones are cached in memory (256 MB cap). ExifTool is only asked as a last resort, and then through the ExifTool process that is already running, rather than starting `cmd.exe` and a new `exiftool.exe` for every preview as before.
	- Icon-view thumbnails are generated for the rows you can see first (then the next screenful below and above), and they now start as soon as files are found instead of waiting for each file's metadata to be read, so a big folder shows its visible thumbnails straight away and re-prioritises as you scroll.
	- ExifTool requests now take turns on the shared process (a lock), so a preview request can't interleave with the metadata read.
	- Internal: the shared logger and the "no value" sentinels no longer live on the main form (each class has its own logger, like the model classes; the sentinels are in `Model/NullEquivalents`), so the helpers and model reach into the form a little less.
- BUGS & FIXES:
	- Opening a folder of RAW files no longer sits behind the parsing box for many seconds: every file was being read in full to checksum it before the list could continue. New files are no longer checksummed on the way in (they are read by ExifTool anyway); the checksums are filled in afterwards on background threads for the next rescan. The per-file element and sidecar lookups, which scaled with the square of the folder size, are now hash lookups.
	- The window froze for a second or two just as it appeared: the check for new ExifTool and GeoTagNinja versions made its web requests on the UI thread. They now run in the background. (In a Release build the check only runs about once a week; a Debug build does it every time.)
	- Loading a folder was slower than it needed to be because every file asked the shell for its list icon by touching the file; the icon is now looked up once per file type.
	- Opening a big folder (thousands of files) is much faster and no longer freezes the window: the list used to re-sort after every added row (3,000 files took about 20 s, now about 3 s), and each metadata update and the "files / with geodata" count scanned the whole list.
	- The window is no longer shown until the saved panel sizes have been applied and the grid has been drawn with the first folder, so the panels no longer jump into place and the grid no longer shows as an empty dark gap while it fills in.
	- When saving, an ExifTool argument was dropped if an earlier one merely contained it as text: `-IPTC:City=Foo` was lost if `-IPTC:City=Foobar` had been added before it, and a file path if a longer path containing it had. Only an exactly identical argument line is skipped now.
	- Starting the app in `Thumbnail View` left the keyboard focus on the menu bar, so the arrow keys did nothing until you clicked a picture. The thumbnails now take the focus once the window shows.
	- RAW previews and thumbnails could show portrait shots sideways: embedded RAW previews usually carry no orientation of their own, so the file's orientation is now applied.
	- Icon-view thumbnails of RAW files that needed a full decode showed the top-left corner of the picture instead of the whole picture.
	- Icon-view thumbnails were cached on disk by file name only, so two folders containing a file with the same name could show each other's thumbnail. They are now cached by full path, size and modified date, so an edited file gets a fresh thumbnail and same-named files in different folders can't clash.

**Build 9794 [20261025]**
- NEW & UPDATED:
	- Version bump to `1.19`. The installer project (`GeoTagNinja_Setup.vdproj`) was brought in line: its product version is `1.19.9794` and it has new product and package codes (same upgrade code, so it still replaces older installs).
	- Added Chinese translations via weblate
	- The GeoNames username in `Settings` is now masked like a password, with a `Show Username` checkbox to reveal it. (GeoNames only looks at the username.)
	- New users now start in dark or light mode to match Windows's "app mode" setting and, if dark, use the pale dark map (`Settings` > `Application` > `Use Dark Mode` and the map colour mode can still be changed afterwards). Existing users keep whatever they had.
	- Users upgrading from build 9778 or older get a one-time message about the map search change. The app now remembers the last build it ran (`Version.Build`) so future changes can be announced the same way; new users never see these. See `development-notes.md`.
	- The map's search box no longer needs an ArcGIS API key. It now tries GeoNames first (reusing your GeoNames username), then Photon (suggestions as you type), then Nominatim (on Enter only). The ArcGIS controls are hidden in `Settings` (not deleted, in case it returns) and a previously saved key is kept but unused.
	- Added a `Settings` > `Application` option, `Do Not Change File's Modified Date When Saving` (off by default), so adding a location to a photo no longer makes File Explorer show the date of the save instead of the date the photo was taken. It uses ExifTool's `-P`. It does not apply to a file type that has `Reset Image File DateTime to CreateDate` ticked, as that is an explicit request for a particular date.
	- The `Application` settings tab scrolls rather than the form growing when more options are added.
	- Updated `WinFormsDarkThemerNinja.Official` to 1.0.3. The package is now unsigned, so the setup project's assembly reference was updated to match (version and public key token). Nothing in the app needed changing: the `Themer` calls we use (`ApplyThemeToControl`, `ShowMessageBox`, `ShowMessageBoxWithResult`, `ThemeStyle`) are unchanged.
	- Docs: the contributor docs and the readme now cover the ThemeHelper, the test project and Release-build test gate, and the readme's feature list now covers features added since the original release.
- BUGS & FIXES:
	- Anything logged while the app was starting up (settings database, defaults, ...) was thrown away because logging was only configured afterwards, so startup problems left no trace in `logfile.txt`. Logging is now set up first.
	- A brand-new install failed at startup with SQLite `no such column: ...` (a country code such as `ZWE`) while seeding the default city-allocation rules: the values were spliced into the SQL as double-quoted literals, which newer SQLite builds treat as column names. They are now bound as parameters. Startup failures creating the database or writing the defaults are also logged now.
	- Closing the app with unsaved changes now asks Yes/No/Cancel *before* anything is shut down, and `Cancel` really keeps the app open. Previously the close handler was `async void`, so the window closed while the question or the save was still running, and `Cancel` did not stop it. The handler was also subscribed twice (designer and startup code); the duplicate is gone.
	- The VS Code `tasks.json` was stale: its Run task still looked in the old `bin\x64\Debug`, it had no Test task and it didn't use `.vscode/msbuild.ps1`. All four tasks (restore, build, run, test) now go through the script.
	- The installer project (`GeoTagNinja_Setup.vdproj`) now lives in the repo under `GeoTagNinja_Setup/` and is referenced from `GeoTagNinja.sln`, so it is version-controlled. Only the project file is tracked; the built `.msi` is git-ignored.
	- The repo no longer carries its own copy of `WebView2Loader.dll` (it was an old 1.0.3124 build). The loader now comes only from the `Microsoft.Web.WebView2` NuGet package, and a build step copies it next to the exe, so it follows package updates and the installer needs no manual refresh. The WebView2 *runtime* was never bundled: the app has always used the newest one installed on the machine.
	- The VS Code tasks and launch configuration were stale: F5 looked for the exe in the old in-repo `bin\x64\Debug`. They now use the `%LOCALAPPDATA%` build output, find MSBuild through `vswhere` (so VS 18 Insiders works) via `.vscode/msbuild.ps1`, restore with full-framework MSBuild, and gained a `Test GeoTagNinja` task.
	- The `x64` build configurations now write to the same `bin\Debug` / `bin\Release` folders as `Any CPU` (previously `bin\x64\...`), and the setup project's source paths were updated to match, so the installer packages the current build whichever platform is selected.
	- The one-off custom-rules rename in the settings database now binds its values as SQL parameters instead of double-quoted literals, which newer SQLite builds may reject.
	- Updating to `System.Data.SQLite` 2.x broke startup with `Unable to load DLL 'e_sqlite3'`: that version no longer bundles its native library. `SQLitePCLRaw.lib.e_sqlite3` is now referenced and its x64 `e_sqlite3.dll` copied next to the exe.
	- A failure during startup (e.g. SQLite failing to load) used to be swallowed, leaving an invisible, unkillable process. The app now shows an error message, logs it and exits.
	- With `Do Not Download Altitude` ticked, pulling toponomy data in a comma-decimal locale (e.g. `de-DE`) multiplied the existing altitude by 100 (`135,41` became `13541`). The fallback altitude was rendered with the current culture and then read back invariantly; it is now invariant end to end, and the Edit dialog passes the numeric control's value instead of its culture-formatted text.
	- A string attribute that was blanked could be written into the file as a literal `-` instead of deleting the tag; the save now treats the display placeholder as a blank.
	- Build output (`bin\` and `obj\`) now goes to `%LOCALAPPDATA%\build\GeoTagNinja\<project>\` via a new `Directory.Build.props`, so rebuilding no longer churns the synced repository folder. The setup project's two hardcoded native-DLL paths were updated to match.
	- Docs: moved `docs/` to the repository root, and split the non-layout notes out of `project-layout.md` into `development-notes.md`.

**Build 9778 [20261009]**
- NEW & UPDATED:
	- Added option ref #220 to disable downloading of Altitude data from the API. The default is to download Altitude data.
	- Unleased Claude on the code. Cost me $100. I await bug reports.
	- Added some Tests
- BUGS & FIXES:
	- Fix #215 `GPSDOP`/`GPSHPositioningError` not being processed.
	- Fix #221 OneDrive/Google Drive folders and files not being properly processed at all.
	- Fix #222 OneDrive/Google Drive folders and files not being properly processed when set to online-only.
	- Fixed an issue whereby importing data would fail due to nonexisting database
	- Fix #217 Multimonitor right-click crashing the app
	- Fix #224 `Retain Zoom` setting not respected + Fix map redrawing/reloading fully on every click even when the newly selected point is already in view
	- Fixed untranslated strings showing up at startup
	- Fixed Destination path arrows missing on the in-app map (WebView2 couldn't resolve the relative script path for the polyline decorator plugin)
	- Fixed another ExifTool-update-related issue. Apparently the actual download source has been moved to SourceForge.

**Build 9654 [20260607]**
- NEW & UPDATED:
	- Add column filtering #205
	- _Finally_ got around to bringing in the streaing data logic. No more lengthy "please wait" box! (It's still there, but for a lot less time.)
	- As a direct outcome of the above now we have rename (`F2`) and delete (`del`) capabilities #206
	- Version bump to `1.18` - well overdue.
	- Merged PR #211 - opt-in to keep map visible without refreshing.
	- Also ref #212 rewrote the ExifTool wrapper/parser to only care about the tags we need. This speeds up parsing tremendously.
- BUGS & FIXES:
	- Patch fractional text values not working in GPX import (#207)
	- `Remove All Geodata` should remove `GPSDOP` and `GPSHPositioningError`
	- Fix #212 image rotation not being respected for jpgs

**Build 9600 [20260414]**
- NEW & UPDATED:
 	- Add option for user to set whether to remember map zoom level within a session ref #199
- BUGS & FIXES:
 	- Fix #197 column name mismatches in internal datatables
	- Removed the ability to restart the app from within the app as it was causing continued headaches.
	- Hopefully fix issues with OSM Referrers. #200, #201, #202

**Build 9519 [20260123] // Build 9520 [20260124]**
- NEW & UPDATED:
	- Added `Use Last Used Folder` (for startup purposes) #191
	- Added `Carto` as map layer option ref #192 partial solution for "always English" map
	- Added a TOC to the readme. (via [this tool](https://www.docstomarkdown.pro/markdown-table-of-contents-generator-free/))
	- There's now also a Spanish translation stub via Weblate but it's empty.
- BUGS & FIXES:
	- Revamped theming internally
	- Fixed GeoNames API warnings not being relayed to the user
	- Build 9520 fixes a theming error introduced in Build 9519 - Ref #196

**Build 9458 [20251123]**
- NEW & UPDATED:
	- Added some context menus, `Select All/None/Invert`, `CopyPaste` etc
	- Added a `Donate` dropdown in case anybody feels like...
	- Added a `Save Selected` item to the `File` menu
- BUGS & FIXES:
	- Fixed Auto-Updater possibly misbehaving

**Build 9456 [20251121]**
- NEW & UPDATED:
	- Slightly changed `Dark Mode` colours to match the real ones.
	- Added German translations (via Weblate).
	- There's now also a Ukranian translation stub on Weblate but it's empty.
	- Added a new layer to the map (`Streets ESRI`) - I've had problems with OSM lately; possibly a Leaflet issue but this is a reasonable fallback.
	- My free Resharper licence has run out and they are no longer renewing small projects like mine so I have switched to Visual Studio 2026 and applied auto-reformatting of files. Rather in one go than bit-by-bit but I think it's for the better.
	- Added GPS Dilution Of Precision (`GPSDOP`) and GPS Horizontal Positioning Error calc (`GPSHPositioningError`) as new columns #187
- BUGS & FIXES:
	- Fixed #186 Favourites being saved with wrong Latitude info.
	- Fixed map attribution not showing.
	- Fixed packages.config being totally wrong. No clue what had happened there. 

**Build 9387 [20250912]** 
- NEW & UPDATED:
	- Capability to save certain defaults on the Import GPX Form #174
	- App version Bump 1.17 - should have done that w/ the last public release.
	- Added Polish Translations (via Weblate).
	- Added some basic feedback to the completion of the GPX Export. (aka `Files exported successfully`)
	- Added a Splash Screen. Bit of a gimmick but on slower computers it may be of some use.
- BUGS & FIXES:
	- Fixed app restart not working...hopefully (ref #175)
	- Fixed that the Installer forgot to add a relevant DLL file and so Magick wasn't working.
	- Fixed GPX Export not working properly when the desination path contained spaces. #179

**Build 9300 [20250618]** 
- NEW & UPDATED:
	- Added optional Satellite view layer to the map
	- Thumbnail view #164. Rudimentary and needs testing. Enable/Disable within `Settings` (requires app restart).
	- There is now a Russian translation stub (via Weblate) but it's empty. I've changed the code not to show empty languages. 
- BUGS & FIXES:
	- Added some logic to ignore certain particular warnings cropping up when saving files #159
	- Fix Custom Rules not working properly with the `TargetPointName` attribute
	- Fix API language selection not respected

**Build 9196 [20250306]** 
- NEW & UPDATED:
	- Added a `CancellationToken` to the Parsing of `DirectoryElements` (aka you can now cancel loading a folder.) #120
- BUGS & FIXES:
	- Changed the Translation/resources logic into a single file. I realised that having 10 different files would yield 20+ PRs if anyone does an update and it's annoying.
	- Fixed half of the translations not actually working because...not even sure why. My bad nonetheless.
	- Fixed the time element of the `TakenDate`/`CreateDate` not being read properly after modify #147
	- Fixed HEIC previews misbehaving #152
	- Fixed the `No access to API` error cropping up multiple times #153
	- Fixed Import (settings) not working due to localisation issues
	- Fixed DateTime copypaste not working and  not being handled properly on some Cultures [eg en-AU] #155

**Build 9102 [20241202]** 
- NEW & UPDATED:
	- I've moved the whole logic of languages being held in CSV files to RESx files.
	- Added ExifTool version number to the info within the `About` Form/Box.
	- Added the `GPSDOP` tag's handling into the GPX Import logic. This requires ExifTool 13.04 or newer. Ref ticket #121.
	- Added a Flat Mode ref Ticket #123 - this is a little experimental atm but should work.
	- Added the build number to the `Text` of `FrmMain`. I know this is a bit gimmicky but I like it.
	- App version Bump 1.16
- BUGS & FIXES:
	- I've moved the startup methods to be async/awaited. Hopefully this will hide random occurrences of stuff like `tmi_Help`. 
	- Also revamped how column orders are handled internally.
	- _Hopefully_ fixed the f....ing SQL Interop issues that occasionally crop up post-installation. It's a bug in VS Installer/Project config and is mighty annoying because it doesn't come up much in debug.
	- Updated the icon naming on the Desktop. For some obscure reason I had it coded with an extraneous space.

**Build 9054 [20241015]** 
- NEW & UPDATED:
	- Rewrote the SQL handling logic to use DataTables and Transactions rather than write ~~everyone~~ everything [_I swear guv I was sober..._] on a one-by-one basis. Jolly achievement for someone that claims to be a SQL dev rather than a C# dev but alas. 
	- When using copy-paste the Coordinates will now be default-enabled for paste. It's logical that people would want to paste this piece of data if available.
- BUGS & FIXES:
	- Application should no longer crash when encountering an invalid piece of Exif data #118
	- Added some logic to enable returning to `C:\` if the user enters a 'fake' folder name.
	- `Restart Now` (within Settings) should hopefully work as expected #119
	- Kinda fixed the `Do you want to save the write-Q?` box cropping up twice upon app-close.

**Build 8973 [20240726]** 
- NEW & UPDATED:
	- Added an option to Overlay GPX Coordinate Tracks onto the map. This can be found within the Import GPX Tracks Form, at the bottom. Experimental. #107
	- Panel sizes on the `FrmMainApp` should should now be stored/restored across sessions. #109
	- Added GPX Track Export functionality #114
	- Added the parsing of `GPSDateTime` tags as a read-only item.
	- Updated the ExifTool-update process to handle the new multi-file package. A mighty annoying move tbh but alas. Anyone experiencing an extra 3 sec delay upon exit, don't complain to me, script now has to extract and move 500 files instead of one.
	- Added some logic to check if a file had already been process within the session (ie you are reloading a folder or navigating back into a folder you've already visited). If so then the file won't be processed again unless changed.
		- This somewhat reintroduces the logic eliminated in `build 8391` but less awkwardly. The tradeoff with this is that the app needs to take a checksum of each and every file each time the user does a refresh. This is computer/performance dependent but I tested on 3GB data of 170 files and...
			- 1st time "enter folder" took around 9 seconds
			- Consecutive "enter same folder/refresh" took around 3 seconds. 
			- That on a Ryzen 7 5850 + PCIE3 SSD
	- App Version Bump: 1.15

- BUGS & FIXES:
	- Changed the way auto-updater works. It was taking `PublishedDate` as a basis for Build number, which wasn't entirely correct. #110

**Build 8897 [20240511]** 
- NEW & UPDATED:
	- Dark Mode should now apply to GPX Import Report
	- Custom MessageBoxes should now respect hotkeys in certain cases
	- Added PNG and WEBP file support
	- Added an auto-updater. It's a bit tricky to test but I think it works. Report bugs pls.
	- Updated some of the underlying packages; removed x86 references to SQLite 

- BUGS & FIXES:
	- Patched GPX Import Report re #105

**Build 8834 [20240309]**
- NEW & UPDATED:
	- Paste coordinate-pair string from clipboard into Edit Form (see readme about this or the tooltip in the Edit Form) #100
	- Added a button to the Edit Form to move to the next image when there's a multi-selection (Apply & Move to Next) [also #100]
	- Bumped ExifTool to v12.77
	- Rewrote the logic of updating for ExifTool, from now onwards, it should be automatic.

- BUGS & FIXES:
	- Added an extra warning if the file-save fails for what's likely a UTF path error #98
	- Added a check to ensure that files have the necessary data before attempting to create FOV re #99
	- Patched two vaguely related issues regarding SQLITE REPLACE and LastLat/Lng read on startup when not using EN-xx culture.
	- While at it also patched a cleanup issue with the SQLite database.

**Build 8768/8822 [20240103] (released 20240216)**
- NEW & UPDATED:
	- Long-pressing a point on the map will now offer the user to save the map location to the image so that there's no need for extra click #91
	- Added functionality as per #92 to drag the map marker and allow the user to add the new value to the selected image(s).
	- Added toponomymName to the options within Custom City Allocation Rules
	- Changed how file names with `[]` chars are handled for saving.
	- Bumped ExifTool to v12.72
- BUGS & FIXES:
	- Fixed an issue re #93 Open Coords in API misbehaving. 
	- Fixed Custom Rules panel breaking because the object type was wrong due to an earlier logic-consolidation.
	- Fixed Copy-Paste empty Direction value showing as 0 on the map.
	- Fixed Copy-Paste empty Destination value breaking the the map when selecting multiple files.
	- Run a spell checker on the readme....

**Build 8680 [20231007]** 
- NEW & UPDATED:
	- App Version Bump: 1.14
	- Implemented an Export/Import functionality for Settings and Custom Rules
	- Bumped ExifTool to v12.67
	- Streamlined a lot of code relating to the reusability of on-the-fly Form generation
	- Same goes for generic reusability, messageboxes, enums, buttons, whatnots
	- Various further Dark Mode changes/improvements
	- Added the capability to Restart the app immediately if the user wants so in the Settings
	- Added support for previewing HEIC images
- BUGS & FIXES:
	- Fixed Country DropDown not displaying anything if the existing data wasn't on the predefined list. It now takes value from CountryCode
	- Fixed Date Shift misbehaving 
	- Fixed newly-saved data not being written back into the DE-logic

**Build 8661 [20230918]** 
- NEW & UPDATED:
	- App Version Bump: 1.13
	- `GPSDestinationLat/Long` ...
		- is now being displayed in certain cases (when there is a series of images subject to a set of conditions. See the readme on how this works.),
		- can be modified via a new button below the map,
	- has been added to the list/function of `remove all geodata`
	- Added a new tab for the Exif info - this lists the values as they are held by GTN internally.
	- Added 2 new buttons for filing feature requests and bugs (both require a GitHub acct.)
	- Added Dark Mode support. Refer to the readme but it's very ugly and there's little I can do about that as WinForms doesn't support the rich capabilities Windows 10/11 can offer for the time being.
	- Added various options for `Dark Mode Map`
- BUGS & FIXES:
	- Fixed an error when data in MyComputer wasn't properly recorded and this caused a crash
	- Reverted a change introduced earlier re: webView2 initialisation after it randomly failed on one of my VMs

**Build 8646 [20230903]** 
- NEW & UPDATED:
	- App Version Bump: 1.10
	- Graduated the app to version 1 [technically 1.10 because it's been edging up on the minor versions ever since initial release] - yayy. It now has all the features I initially wanted.
		- Added some support for reading and displaying ImgDirection/Ref.
		- Added a TaskbarProgressBarState indicator
		- Update the update logic -> there's now a Setting that enables the user to update to pre-release [dev] releases or master-only. Master releases will no longer be marked as pre-release on github.
	- Bumped ExifTool to v12.65
	- Changed the internal logic as to how the ListView coordinates get put on the map. Trying to fully move away from reading data from the ListView rather than from the DirectoryElement
	- Changed internal logic re: (not) defaulting values to strings in certain cases
- BUGS & FIXES:
	- Fixed a bug where in Finland (English) culture [possibly others too] the app would crash because decimals were being converted in the wrong way.
	- Fixed some logic relating to certain Composite Tags not being written properly
	- Then fixed an issue caused by this where XMP tags weren't written out to XMP files properly. 
	- Also fixed a duplication issue in the outgoing args file while at it
	- Fixed a bug where Paste would also attempt to affect folders

**Build 8610 [20230729]** [dev branch release]
- NEW & UPDATED:
	- Added an option to use Imperial instead of Metric. In practice this still saves data in Metric but shows as Imperial. #82
	- Bumped ExifTool to v12.64
- BUGS & FIXES:
	- Fixed a bug where the original file is set not to be overwritten but XMP is and ultimately the DE didn't get cleread properly, resulting in what appeared to be an unresponsive non-save.
	- Fixed a bug pasting string values across files causing a crash
	- Updated the Kyiv TZ name (from Europe/Kiev to Europe/Kyiv) - this appears to have caused some problems before
	- Fixed a bug where timezone was read incorrectly from the Edit Form
	- Fixed a bug where RAW and XMP files contradict each other for Lat/Long Ref values and the wrong one is taken
	- Fixed a bug where opening the Edit Form via the File menu would cause crash
	- Fixed a logical flaw where, after pressing OK in the Edit File Form the code would parse the whole folder again for any outstanding data
	- Amended DE Parse logic for (lack of) dates values - re #86

**Build 8556 [20230605]**
- NEW & UPDATED:
	- App Version Bump: 0.9 
	- Added a `Collections Mode`, in particular to work with Jeffrey Frield's LightRoom Classic Plugin `Run Any Command`. Details on how to use this are in the readme.
	- Rewrote the sorting logic to account for the data type of the column being sorted (i.e. date, number, text etc)
	- Added Country to Favourites
	- Bumped ExifTool to v12.62
- BUGS & FIXES:
	- Fixed a bug where the TZ dropdown in Import GPX could cause a crash if the user was in a TZ that wasn't on the dropdown.
	- Paste-values logic minor revamp
	- Changed settings to default-write-into-image-file to false
	- Rewrote the File Edit Form load process. The existing one took way too long because it parsed data for a number of Objects that were entirely irrelevant.
	- Rewrote the Track Import logic not to overflow when too many files were involved. (see ticket 78).

**Build 8469 [20230310]**
- NEW & UPDATED:
	- App Version Bump: 0.8
	- Entirely rewrote the code to work off DirectoryElements rather than DataTables. This is techy so I won't get into it here, for anyone cares read the code or discussion #62
	- Added functionality to auto-navigate to a selected Favourite in the dropdown.
	- Added a `recall last Track Sync Shift` option.
	- Added logic for the pick-from-API-choices Form to accept doubleClick and (keyboard) Enter (key)
	- Added a button in the Paste Form for generic All/None
	- Added a button in the Paste Form for recalling the last Paste settings
	- Added an option to halt further API pulls if one failed
	- Changed certain numeric TextBoxes to NumericUpDowns.
	- Added logic to replace hard-coded City-definition logic with user choices.
	- Changed versioning logic. Will use simple build numbers from now on in the public facing element of the app. Windows & Visual Studio still require the ##.##.#### logic rather than just a build number but the About section won't show it..
- BUGS & FIXES:
	- Fixed a bug where NumericUpDown changes didn't trigger setting fonts to Bold
	- Fixed the map zooming way too far out when negative lat/long values are present
	- Changed the pick-from-API-choices Form to be 1-based-index rather than 0-based
	- Fixed MessageBox captions always saying `Error` as output
	- Fixed a bug where one or two Settings were ignored on load
	- Fixed a variety of random bugs relating to time-shift copypastes not entirely behaving
	- Further fixed a variety of bugs that yet again I don't entirely remember

**Build 8421 [20230121]**
- NEW & UPDATED:
	- Added a `Manage Favourites` element to the Settings area.
	- Added Custom Rules. This is experimental for now. Wiki [here](https://github.com/nemethviktor/GeoTagNinja/wiki/Settings-&-Custom-Rules).
	- Updated CountryCode ISO SQLite logic with CSV
	- Updated Altitude-pull (from Web) logic. At the same time removed the separate button(s) to do that as it's now part of the Toponomy pull.
	- Added an option for changing the API language. This won't affect countrynames though because they derive from the CountryCode so there might be little visibility of this change in practice.
	- Added an option for GPX import to ignore reverse geocoding.
	- Bumped ExifTool to v12.55
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
	- App Version Bump: 0.7
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
	- Bumped ExifTool to v12.52
- BUGS & FIXES:
	- Eliminate MD5 checks. Basically the bloody thing takes longer than re-parsing the whole folder.
	- Removed the built-in webView2 installer because it was more of a pain in the backside than benefit. Updated the readme w/ instructions should this cause a problem.
	- Fixed logic to ensure if a value in the FrmEdit is replaced with a blank it actually does get queued up for writing.
	- Stuff I don't remember.

**Build 8370 [20221201]**
- NEW & UPDATED:
	- Added Modify TakenDate & CreateDate (it's in the Edit File section - and no the time-shift cannot be copy-pasted yet -> shift works in terms of modifying the literal DateTime at the moment not as in a proper copyable `shift` value.)
	- Added a hold/blocker on entering a folder until the folder loads. This is annoying but is needed to prevent users from starting operations before the files are processed.
	- Added some logic to locally store (for the length of the session) each file's data. This is so that if the actual image/xmp files don't change then there's no need to re-parse everything _again_. It's slow and pointless.
	- Added functionality to `Get Data from Other File` in the Edit File section.
	- Changed logic around the addition of sidecar XMP files and (possibly) overwriting the source image file.
	- Added this changeLog.md file to the project outputs. I don't expect anyone to read it locally but at least now it's possible.
	- Bumped ExifTool to v12.51
	- Added some logic to the above to allow for `Original Files DateTime` to be reset to CreateDateTime by default for RAW images. (These can all be changed in Settings/File Specific)
	- Rewrote the Excel macro that deals with exporting languages. It can now also import. (less relevant for the users but makes my life easier.)
- BUGS & FIXES:
	- Fixed Copy-Paste properly. Users can now pick what to paste. It's faster too now.
	- Error msg/image name not showing properly when file gone missing
	- The previous version introduced an error in Non-English regions when the user clicked on the map. This has been fixed.
	- Rewrote logic re: UK Cities/Regions being mixed up. Now only applies to London.
	- Thoroughly renamed all my rather ambigous `filePath` variables in the code to specify `fileNameWithPath` and `fileNameWithoutPath`. This isn't a material change but it makes things a bit more readable.
	- Further French lang updates. Thanks to pbranly once again.

**Build 8358 [20221119]**
- NEW & UPDATED:
	- App Version Bump: 0.6
	- Finally managed to get hold of ReSharper so the code has been refactored in totality. Famous last words but it shouldn't affect usage.
	- Added a `..` to the main grid (listView) where applicable (parent folder).
	- Added the capability to navigate to the top of the folder structure. (e.g. MyComputer and then list the drives.)
- BUGS & FIXES:
	- Changed how the previews get created. This will hopefully result in faster preview-creation. The orientation-problem is still unsolved but it's likely to remain so for the time being.
	- If a user's "Pictures" (or any other "Special") folder had been moved and renamed the would break because Windows treats special folders in odd ways. (e.g. if the Pics folder is called "Pics" Windows would still show "Pictures", which doesn't per se exist.)
	- If the user had chosen `Delete All GPS Data` and subsequently added GPS data the addition would not have gone through upon save. This is now fixed.
	- Rewrote the logic of (re)creating sidecar XMP files as the original logic would pull data from the RAW file (only), possibly overwriting Adobe-specific stuff that had been stored in an already-existing XMP.

**Build 8350 [20221111]**
- NEW & UPDATED:
	- Added sync/import GPS Track Files.
	- Added the capability to resize the main elements. Their positions aren't saved for now.
	- Added a button to the ToolStrip to _get all from the web_ (toponomy & altitude for selected items)
	- Bumped ExifTool to v12.50
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
	- Bumped ExifTool to v12.49
	- Added Multi-Select capability to the map. (i.e. multiple pins now show properly)
	- Partial French translations added in (thanks to pbranly)
	- Update checks will now only happen once a week. No need to spam the world with API requests.

**Build 8333 [20221025]**
- NEW & UPDATED:
	- Changed the logic relating to checking the newest version of ExifTool online. The original was querying an API that hadn't been updated for months.

**Build 8314 [20221006] + Build 8318 [20221010]**
- NEW & UPDATED:
	- Added a `Don't ask again` button for the `loc-to-file` dialogbox. This is session specific (if you close/restart the app, it will come back again). [Updated this logic in the Build 8318 release]
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
	- Added check for ExifTool version change tracking
	- Added check for GeoTagNinja version change tracking
	- Added (better) commentary to the code
	- Changed Settings/Edit Forms' behaviour not to block other windows.
- BUGS & FIXES:
	- Various

**Build 8251 [20220804]**
- NEW & UPDATED:
	- Added `Remove GeoData` to main form as well as Edit form
	- Added this changeLog.md file
	- Added a readme to the ExtraFiles folder
	- Added handling to MessageBox texts being read from SQLite
 
**Build 8248 [20220801]**
- NEW & UPDATED:
	- Initial release
	- App Version Bump: 0.5