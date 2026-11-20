<!--
  The text of the in-app Help (F1). One "## Title {#id}" section per topic; the left-hand list shows the titles.
  Plain Markdown (no tables, no raw HTML). Keep it in step with the app: see "Keeping the Help current" in development-notes.md.
  Ids used by code (the callers of HelpLauncher.Show): getting-started, main-window, tagging, edit-files, favourites, gpx, batch-rename, shortcuts, settings, troubleshooting.
-->

## Getting started {#getting-started}

GeoTagNinja reads and writes photo metadata through [ExifTool](https://exiftool.org/). If ExifTool can read or write a file, GeoTagNinja can too.

1. Type or browse to a folder in the box at the top of the window (`Backspace` or the up-folder button goes up one).
2. Select one or more photos. The map shows where they are (if they have a location).
3. Give them a location: drag the map marker, long-press a point on the map, search for a place in the map's search box, or type the coordinates under the map.
4. Press `Get from Web` to fill in country, state, city and altitude from GeoNames.
5. Save with `Ctrl+S` (or from the `File` menu). **Nothing is written to your files until you save**; edits are shown in the list as pending.

Before place names and altitudes work, open `Settings` and enter your GeoNames user name. Register for free at geonames.org, then activate the free web service on your account's `Manage Account` page.

Make backups of photos you care about. The author accepts no liability for damage.

Press `F1` at any time to open this Help. In `Edit File(s)`, `Manage Favourites`, `Batch Rename`, `Import/Export GPX` and `Settings` it opens at the matching topic.

## The main window {#main-window}

- **Folder box and file list**: the current folder's files. `Enter` opens a folder, `Backspace` goes up one. Columns can be chosen from the list's right-click menu and filtered from the column headers.
- **Folder tree** (`View` > `Folder tree`, `Ctrl+B`, or the × on its header): drives and folders at the left; click one to go there. It follows the folder box. The star in its header opens your **favourite folders**: pick one to jump to it, or add or remove the current folder (right-clicking a folder does the same). Not to be confused with the favourite locations.
- **Details and Thumbnails** (`View` menu): switch at any time. Thumbnails can be resized with `Ctrl` + mouse wheel, `Ctrl` + `+` / `-`, the slider under the list, or `View` > `Thumbnail Size`. There are no column headers in the thumbnail view, so use `View` > `Sort By` there; filtering by column is for `Details` only. Drag a rectangle over empty space to select several pictures.
- **Preview and metadata panel**: the selected photo's preview and its metadata.
- **Map**: shows the selected photos. Drag the marker, or long-press a point, to apply a location to the selected photos. The map's search box tries GeoNames, then Photon, then Nominatim (street addresses come from the last two; Nominatim only when you press `Enter`). If a photo has an image direction, a line shows it when exactly one such photo is selected.
- **Strip under the map**: the coordinates, what to do with them (`Show on Map`, `Save to File(s)`, `Save to Destination`, `Save Photo`, `Apply to Photos`) and `Favourites`.
- **Flat Mode** (`File` menu): loads every subfolder of the current folder at once.
- **Loading**: big folders load in the background, with progress on the status bar and the taskbar button. `Esc` cancels.
- **Dark Mode** and the choice of map layers are in `Settings`.

## Tagging photos with a location {#tagging}

Everything here applies to all the photos you have selected.

- **From the map**: drag the marker, long-press a point, or use the search box.
- **From coordinates**: type them into `Latitude` / `Longitude` under the map, or paste a `lat, lon` pair.
- **From the web**: `Get from Web` fills in country, country code, state, city, sublocation and altitude from GeoNames. It always uses the coordinates the photo has in the main list, not what you have typed into an edit form but not yet applied. Existing country details are not changed unless you press `Get from Web`.
- **From a GPX or KML track**: see *Importing and exporting tracks*.
- **From a favourite**: see *Favourites*.
- **Copy and paste between photos**: `Ctrl+Shift+C` copies the selected photo's location data, `Ctrl+Shift+V` pastes it onto the selected photos (also in the `File` and right-click menus).
- **Remove location data**: `Remove all location data` clears the location from the selected files. With the setting `Keep keywords in step with the location` on, the keywords for the old place are removed too.
- **Open the position elsewhere**: the right-click menu can open the coordinates in Google, Bing, OpenStreetMap and other maps.
- **Save**: `Ctrl+S`. Until then, edits are only staged.

Notes:

- Altitude values of `32K` or `-32K` from the web service are treated as "unknown" and blanked.
- RAW files: with `Process the original image` switched off for an extension, only the `.xmp` sidecar is written and the RAW file itself is left alone. Data in the XMP takes precedence over the RAW when they disagree. Some programs (Adobe Bridge) still read the RAW's coordinates; process the original in GeoTagNinja if that matters.
- If a file's data does not show, try moving it to a simple folder such as `C:\temp` with a plain file name. That is an ExifTool limitation.

## Edit File(s) {#edit-files}

Open it with the toolbar button, `File` > `Edit Files...` or `Ctrl+Enter`. It edits the selected photos.

- **Tabs**: `Location` (coordinates also as degrees-minutes-seconds, altitude, image direction with a compass drop-down, destination latitude and longitude, country, state, city, sublocation, time zone, `Get from Web`), the date tabs, `Categories/Keywords`, `Source/Description` and `Contact`.
- **The `...` buttons** beside the text fields offer `Insert from` and `Copy to` any other field, `Most Recently Used Items` (the last 20 values typed into that field) and `Forget a Remembered Item`.
- **Keywords** are a real list: each keyword is written as its own entry, to `IPTC:Keywords` and `XMP-dc:Subject` together. As you change location fields the old place names in the list are replaced by the new ones; you can still correct them before saving.
- **Templates**: `Load from Template...`, `Save as Template...` and `Load from Image...` move a chosen set of values between the photo, a saved template and another image. Dates and the time zone start unticked. Anything loaded is staged like a hand edit; you review and save as usual.
- **Pasting coordinates**: `Ctrl+V` in the form pastes a clipboard that holds only a coordinate pair such as `56.1234, 12.5678` (separated by a comma, or your Windows list separator).
- **Time shifts** are applied to each photo's own original date.
- **Time zone** starts blank: files hold an offset such as `+01:00`, not a zone, so GeoTagNinja cannot know which zone it was. Choose one yourself or use `Get from Web` (coordinates plus the date identify it).

## Favourites {#favourites}

Open `Manage Favourites` from `Settings` > `Favourites` or from the `Manage...` button under the map.

- The tree on the left groups favourites by country, state/province and city (`Group By` also offers country code, coordinates, or no grouping). The selected favourite's details are on the right.
- `Add from selected photo` makes a favourite from the photo selected in the main window. `Duplicate` and `Delete` act on the selected favourite.
- Edits are staged: `Accept` / `Discard` apply to the favourite you are on. Nothing is saved until `OK`; `Cancel` throws all changes away.
- **Radius, Auto Assign and Snap Images to Favourite Position**: when a photo that already has coordinates is read and lies inside the radius of an `Auto Assign` favourite (the nearest wins), that favourite's country, state, city and sublocation are staged on the photo, and with `Snap` its coordinates and altitude too. They are staged like any other edit, so you review and save as usual. Photos that already have pending edits are left alone.
- `Get from Web` fills country, state, city, sublocation and time zone from GeoNames for the favourite's position.
- Favourites exported by GeoSetter can be imported from the `File` menu.

## Importing and exporting tracks {#gpx}

Open `Import/Export GPX` from the `File` menu, or use the two toolbar buttons: `Import` opens the dialog on its `Import` tab, `Export` on its `Export` tab. Both need at least one file selected.

- **Import**: reads a GPX, KML or similar track file and matches your photos to it by time. The track can be overlaid on the map first, so you can decide whether it is the right one. Check the time zone and offset options: the camera's clock and the track's clock must agree.
- **Export**: writes the selected photos' positions to a `GPX` or `KML` track. The file extension you choose picks the format. Photos missing a coordinate or a timestamp (or an altitude, if `Include altitude` is ticked) are left out.

## Batch Rename {#batch-rename}

`File` > `Batch Rename...`, the file list's right-click menu or `Ctrl+Shift+R`. It renames, moves or copies many files at once.

- Build the new name from pieces (`Text`, `New Extension`, `Current Filename`, `Preserved Filename`, `Sequence Number`, `Sequence Letter`, `Date Time`, `Metadata`, `Folder Name`, `String Substitution`); `-` removes a piece and `+` adds one.
- Choose `Rename In Same Folder`, `Move to Other Folder` or `Copy to Other Folder`.
- The first file's new name is shown live; `Preview` lists every file. Files are numbered in the order they are listed.
- Save a setup as a preset.
- `Compatibility` always applies the Windows rules (illegal characters become `_`); `Mac` and `Unix` also drop a leading full stop.
- The batch cannot start while any file has a problem (a name already taken, two files getting the same name, a missing date, a path that is too long). On `Rename`, the disk is checked too (missing or locked files, no write access, not enough free space) before anything moves. If a move fails part-way, everything already done is put back and you are asked whether to carry on without that file or stop.
- XMP sidecars follow their photo.

## Keyboard shortcuts {#shortcuts}

**Anywhere**

- `F1`: this Help
- `Esc`: cancel the folder load or other running operation
- `Ctrl+B`: show or hide the folder tree

**File list and thumbnails**

- `Ctrl+A`: select all
- `Ctrl+S`: save the files with pending edits
- `Ctrl+Enter`: edit the selected files
- `Ctrl+Shift+C` / `Ctrl+Shift+V`: copy / paste location data
- `Ctrl+Shift+R`: Batch Rename
- `Enter`: open the selected folder
- `Backspace`: up one folder
- `F5`: refresh the folder
- `F2`: rename the selected file
- `Delete`: send the selected files to the Recycle Bin after asking (`Shift+Delete` deletes them permanently, also after asking)
- `Shift+F10` or the menu key: right-click menu

**Thumbnails only**

- Arrow keys, `Home`, `End`, `Page Up`, `Page Down`: move; with `Shift` they extend the selection, with `Ctrl` they move without selecting
- `Ctrl+Space`: toggle the current picture's selection
- `Ctrl` + mouse wheel, or `Ctrl` + `+` / `-`: thumbnail size

**Edit File(s)**

- `Ctrl+V`: paste a coordinate pair into the latitude and longitude boxes

## Settings {#settings}

Open `Settings` > `Settings`. A few worth knowing:

- **GeoNames user name** is needed for place names and altitude.
- **Process the original image** (per extension): write into RAW files as well as an XMP sidecar, or the sidecar only.
- **Keep keywords in step with the location**: on by default; see *Tagging photos with a location*.
- **Show video files (experimental)**: off by default. Switched on, MP4, MOV and similar clips are listed after you reopen the folder, with the location and date they carry (a phone clip's location, a GoPro's). Videos can be read but not saved yet: saving skips them and says so, and the other files in the same save are written as usual. For a picture of each video in the thumbnail view and the preview pane, Windows' own thumbnail is used; to cover every codec (iPhone and many Android clips are HEVC) give **Path to ffmpeg** the location of ffmpeg (the file, or the folder holding it). The path is shown in red if nothing is there; if it is not a working ffmpeg, the Windows thumbnail is used instead, and a video with no picture at all shows a play symbol.
- **Leave the modified date untouched** when saving, so the file keeps its date.
- **After an update, show what has changed**: on by default.
- Dark Mode, the map layers and the language. Until you choose a language, GeoTagNinja uses your Windows display language if it has been translated well enough, otherwise English.
- `Import` / `Export` back up your settings, custom rules, favourites, `Edit Data` templates and field history, and Batch Rename presets. Window layout is deliberately left out.

Custom rules let you change how place names are written for a country (for example, to shorten a long country name).

## Troubleshooting {#troubleshooting}

- **No place names or altitude**: check your GeoNames user name in `Settings` and that the free web service is activated on your GeoNames account.
- **The map is blank or the app complains about WebView2**: install the Microsoft Edge WebView2 runtime (needed mostly on Windows 7). Some antivirus programs that inspect embedded pages can crash the app on its first start; it should be fine afterwards.
- **A file's data does not show**: move it to a simple folder with a plain name (ExifTool limitation).
- **A file will not save**: another program may have it open. Close it and try again.
- **Long paths** (260+ characters) work where Windows allows them (`LongPathsEnabled`) and ExifTool 13 is installed; `Help` > `About` shows the ExifTool version in use.
- **Command line**: `GeoTagNinja.exe -f "C:\Pics"` starts in a folder; `--collection=<list.txt>` starts in Collection Mode (a text file with one file path per line, as used with Lightroom Classic's *Run Any Command* plug-in).
- **Still stuck?** `Help` > `Bug Report` and `Help` > `Feedback / Feature Request` open the GitHub issue forms. Please say which build and Windows version you have, what you did and what you expected.
