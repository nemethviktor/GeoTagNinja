# Development notes

How-tos and known loose ends. For where files live, see [project-layout.md](project-layout.md).

## Adding a setting to the Application tab

A checkbox on `FrmSettings`' `gbx_AppSettings` needs four things, and the form never needs to be taught
about it in code:

1. The control in `FrmSettings.Designer.cs` and `FrmSettings.resx`, wired to `Any_ckb_CheckStateChanged`
   (that is what bolds it and queues it for saving).
2. A caption in `Resources/Languages/Strings.resx` under the control's name. The form's own `.resx`
   text is only a placeholder.
3. A `UserSetting...` static in `HelperVariables`, mapped to the control name in
   `AppStartup.AppStartupApplyDefaults` (`boolSettingsWithFalseDefaults` or `...TrueDefaults`).
   A checkbox that should default to ticked must also be listed in `_checkBoxesWithTrueDefaultValue`
   in `FrmSettings`.
4. Whatever reads the static.

`tpg_Application` has `AutoScroll` on, so when `gbx_AppSettings` outgrows the tab the page scrolls and
the form does not need to get bigger. Grow `gbx_AppSettings` by the height of the new row and shift the
controls below it down; keep the group box's bottom within the tab to avoid a scrollbar until it is
actually needed.

Note: `ckb_SyncGeoKeywords` ("Keep keywords in step with the location") is the exception to point 1: it is added in code by
`FrmSettings.AddSyncGeoKeywordsCheckBox` (under the last checkbox of the left column, growing the group box if needed) before the
form reads its controls by name, so it needs only points 2 to 4. Its logic is `Helpers/Geo/GeoKeywordSync`, called from
`WriteSaveToFile` just before the write queue is built.

## Adding a table to the settings database

A table that holds something the user made must be importable and exportable, or moving to another machine loses it. Four things:

1. The `CREATE TABLE IF NOT EXISTS` statement in `DatabaseSchema` and the name in `AllTableNames`.
2. A member in `SettingsImportExportOptions`, a row in `SettingsImportExportTables`, and a `ckb_ImportExport_<Member>` caption in `Strings.resx`. The import/export dialog lists the enum, so it needs nothing else.
3. `SettingsImportExportCoverageTests` fails if a table has no option, an option has no table or no caption. Only `appLayout` (window positions, specific to one machine) is exempt.
4. Import replaces a table with the one in the file and copies only the columns both have (`RecreateTableEmpty` uses the canonical `CREATE`), so a column added later needs to be in that statement too (see below).

## Batch Rename

`Helpers/FileSystem/BatchRename*.cs`, `View/Forms/FrmBatchRename.cs` (code-only; the rows swap their controls with the kind of piece), `View/FileList/FileListView.BatchRename.cs` (builds the sources from the selected rows, runs the plan, updates the list) and `Helpers/Data/BatchRenamePresets.cs` (presets: one row per name, the settings as JSON).

- **Text is translatable.** Every caption, message and reason a file can't be renamed is a `Strings.resx` entry, read with `Helpers/Localisation/BatchRenameText.Get(key, args...)` (not `ReturnControlText`: that returns nothing when no language is set, as in the tests, and changes the thread's UI culture). Languages that haven't translated a key fall back to English; a key missing altogether shows as itself. Keys: `frm_`/`gbx_`/`lbl_`/`btn_`/`rbt_`/`ckb_BatchRename_...` for controls, `mbx_BatchRename_...` for messages (`{0}`, `{1}` are filled in), `mbx_BatchRename_Problem_...` for the reasons. The drop-down captions are built from enum names (`lbl_BatchRename_Kind_<BatchRenameKind>`, `_Case_`, `_Part_`, `_DateSource_`, `_SubstituteSource_`, `_Digits_<n>`, `_Field_<attribute name>`), so a new enum member or metadata field needs its entry; `BatchRenameTextTests` fails if it is missing. Log lines stay English in code.
- **The planner is pure** (`BatchRenamePlanner.Plan`; the disk is reached only through a `pathExists` callback) and tested under every culture: sequence digits, dates (`FormatDate` is hand-written, Bridge's `YYYY YY MM DD hh mm ss`, so Thai years or Arabic digits can't reach a file name) and letter case (invariant, so a Turkish `i` stays an `I`) must not depend on the user's culture. A problem on any entry blocks the batch.
- **Problems are caught in two places.** `BatchRenamePlanner` finds what the names alone show; `BatchRenamePreflight` (run when Rename is clicked, before anything moves) finds what the disk shows: source gone, locked (opened with `FileShare.None`; cloud-only placeholders are not probed, as opening one downloads it), in use by the app's own save (`FileLocking`), an unwritable folder (a hidden probe file, one per folder) and too little free space (copies and cross-drive moves). Both just set `BatchRenameEntry.Problem`, and any problem blocks the start. These are estimates, so a step can still fail later.
- **The executor is all-or-nothing, and says who failed.** Every move is logged and undone in exact reverse on failure (undoing in another order can find a name still occupied). The failure comes back as `BatchRenameResult.FailedEntry`; `FileListView.ShowBatchRenameDialog` then asks whether to carry on without it. `BatchRenameExecutor.ExcludeWithDependents` marks it `Unchanged` along with every file that was to take a name it keeps (a chain a→b→c→d with d failing drops all three), and the batch is run again, so a retry is still all-or-nothing for what remains. Chains, swaps and case-only renames (Windows sees `a.jpg` and `A.jpg` as one file) go through temporary names (`*.gtn-rename`).
- **XMP sidecars** are `name.xmp` next to `name.ext`, the convention `DirectoryElementCollection` reads (ExifTool's own examples use the same). A sidecar can be shared: `x.nef` and `x.jpg` both use `x.xmp`. If a file that is not being renamed still uses it (`BatchRenameSource.SidecarShared`) it is copied, not moved, so nobody loses theirs.
- **Old file names that live inside XMP**, which Batch Rename does not touch (checked against ExifTool's tag tables and Adobe forum threads; the xmpMM structures `DerivedFrom` and `History` were *not* verified):
  - `XMP-xmpMM:PreservedFileName`: the name a file had before Bridge or a Lightroom export renamed it. This is what Bridge's `Preserve current filename in XMP Metadata` writes and its `Preserved Filename` piece reads; here it is read (`ElementAttribute.PreservedFileName`) but not yet written. Writing it should go through the normal staged-edit and write queue rather than a second ExifTool call.
  - `XMP-crs:RawFileName`: "the name of the raw file the settings apply to", in Camera Raw and Lightroom sidecars. A sidecar renamed with its raw file still names the old file inside; whether Adobe's tools mind is not known.
  - `XMP-photoshop:SidecarForExtension`: the extension of the file a sidecar belongs to (used when a sidecar must not attach to a same-named file of another type). Extensions are never changed by Batch Rename (only their case), so it stays right.

## Adding a column to a settings-database table

`DatabaseSchema` is the one place a table's shape is written, but `CREATE TABLE IF NOT EXISTS` leaves an existing table alone, so a
new column needs two things: the column in the `CREATE` statement (for new databases) and an `ALTER TABLE ... ADD COLUMN` for ones an
older build made. The Favourites table is the example: `DatabaseSchema.FavouritesAddedColumns` lists the later columns and
`EnsureFavouritesColumnsExist` adds the missing ones when the database is opened (a settings import recreates the table in its canonical shape and copies only the columns the file has). Give the column a `DEFAULT` (or allow NULL)
so existing rows stay valid, and read it tolerantly (`Favourites.ReadText` / `ReadInt` accept a missing column or NULL), because a
settings file exported by an older build has no such column either.
## Telling upgrading users about a change

`Helpers/Startup/UpgradeNotices.cs` shows a one-off message to users who upgrade from an older build. The app stores the build it last ran (just `Version.Build`, e.g. `9778`, as `generic` / `lastRunBuild` in the settings table) and compares it with `UpgradeNotices.Registry` at startup.

To announce something, add an entry to `Registry` with the build to compare against and the English text. The text is deliberately in code, not `Strings.resx`: a build can go public before translators catch up.

- `showIfLastRunBuildAtMost` is the last build released *without* the change, so it must be lower than the build that ships the notice. A user whose stored build is `<=` it sees the message.
- It shows once: right after the box is dismissed the stored build is raised to the current one. Several due notices share one box.
- New users never see one. A fresh install is recognised by an empty `settings` table, which is why `UpgradeNotices.EvaluateAtStartup` has to run before the default settings are written (`AppStartup.AppStartupWriteDefaultSettings`).
- Users from before the tracking existed have no stored build and count as `LastBuildBeforeTracking` (9778).
- The stored build is never lowered, so running an older build doesn't make a notice reappear.

`Helpers/Startup/LegacyWindowsNotice.cs` is a separate, build-independent one-off: a Windows 7 (6.1) warning that support ends in 2027, shown once and remembered as `generic` / `win7EndOfSupportNoticeShown`. Delete it when Windows 7 support is dropped.

## Showing the changelog (before and after an update)

`changelog.md` is shown to the user in two places, both through `Helpers/Update/ChangelogNotes.cs` and the code-only dialog `View/Forms/FrmChangelog.cs` (an HTML view in WebView2, scripts off, links opened in the browser; plain text if WebView2 can't start).

- **Before an update**: `AutoUpdaterCallbacks.OnCheckForUpdate` asks `ChangelogNotes.AskToUpdate`, which fetches `changelog.md` from the **release tag** (`https://raw.githubusercontent.com/nemethviktor/GeoTagNinja/b{build}/changelog.md`) and shows the entries newer than the running build, with Update now / Later. If the notes can't be had (offline, no such tag, nothing in range) the old plain Yes/No box is used. The tag is the guard against showing anything from an upcoming release: it is a snapshot taken at release time.
- **After an update**: `UpgradeNotices.ShowPendingNotices` calls `ChangelogNotes.ShowAfterUpdate` with the build last run and the current one, and shows the entries in between from the `changelog.md` installed next to the exe (tag fetch as a fallback). The dialog has a "don't show this after updates" tick, and `Settings` has the same switch (`UserSettingShowChangelogAfterUpdate` / `ckb_ShowChangelogAfterUpdate`, on by default). The stored build is only raised after this has run, so it is shown once.
- **What counts as released** is decided in `ChangelogParser` purely by build number: an entry is shown only if its build is a real number, greater than the build the user has and no greater than the newest release. The upcoming entry's heading must therefore stay non-numeric (`**Build 9xxx [202xxxxx]**`); when you release, replace `9xxx` with the real build. A heading naming two builds counts as the higher one.
- **Pre-releases**: for a user with `Check for pre-releases` on (`UserSettingUpdatePreReleaseGTN`), the changelog at a pre-release's tag (and the one installed with a pre-release build) still has the unnumbered `9xxx` entry on top, and that entry *is* the pre-release. `ChangelogNotes.UpcomingAs` therefore passes the build being offered or run as `upcomingAsBuild`, and the entry is shown as `Build N (pre-release)`. Everyone else gets 0 and never sees it. (Known gap: the update check asks GitHub's `releases/latest`, which by definition never returns a pre-release, so that setting cannot currently *offer* one; the changelog follows the setting regardless.)
- **Trying the dialogs without a release**: there is no switch in the app for this. A throwaway harness that loads the built exe by reflection, feeds it the real `changelog.md` and takes a screenshot is easy to rebuild: run it under Windows PowerShell in STA (`powershell.exe -STA`), make it DPI-aware, and call `ChangelogParser.Between` and the `FrmChangelog` constructor (both internal) through reflection.
- Each entry is cut out of the file by its `**Build N [date]**` line (that is not a Markdown heading, so it can't be left to a parser) and its body is rendered with **Markdig** (CommonMark; `DisableHtml`, so raw HTML in the text is shown as text; links other than http(s) are neutralised). Markdig is a package reference, so it is there if something else in the app ever needs Markdown. The dialog captions are English in code, not `Strings.resx` (the settings tick's caption is in `Strings.resx` as usual).
- Checkboxes added to `FrmSettings` in code rather than in the designer (`AddCodeCreatedCheckBox`) are the shortcut for a new tick: `HelperVariables` static, the `AppStartup` true/false-default map, the caption in `Strings.resx`, and the name in `_checkBoxesWithTrueDefaultValue`.

## Adding a field to the Edit File form's metadata tabs

The Keywords/Categories, Source/Description and Contact tabs (and the extras on the Location tab) are built in code in
`View/Forms/FrmEditFileData.MetadataTabs.cs` / `.LocationExtras.cs`, not in the designer, so they do not show in the Visual
Studio designer. This was deliberate: thirty near-identical fields are easier to keep right as a table of calls than as a
generated `InitializeComponent`. To add a text field:

1. Add the attribute: an `ElementAttribute` value *below* the "items without OrderID" line (no file-list column), and its
   mapping in `Model/SourcesAndAttributes.cs` (in tags in order of preference, out tags to write). A string needs nothing
   else on the read or write side; a multi-value tag uses `typeof(TagList)`.
2. Add one `AddTextField(...)` (or `AddListField(...)`) call in `BuildMetadataTabs`. The control is named
   `<prefix>_<AttributeName>`, which is all the generic load/change handlers need.
3. Add the caption `lbl_<AttributeName>` to `Strings.resx` (the loader blanks a label that has no entry). A `Tag` of
   `FrmEditFileData.NoAttributeTag` keeps a control away from that loader.
4. To offer it in the `...` menus add it to `s_menuFields` in `.FieldMenus.cs`; for templates and "Load from Image" add a
   `TemplateGroup` in `Helpers/Exif/EditTemplateGroups.cs`.

Things that bite: ExifTool run with `-struct` prints the creator contact fields as one `XMP:CreatorContactInfo` structure,
not the flattened `XMP:CreatorCity` tags (`ExifStructTags` expands it); and a list tag must be written as repeated
`-Tag=item` (an empty `-Tag=` followed by `-Tag+=item` keeps the old items).

## Still outstanding

- **`Helpers/HelperVariables.cs`** is the last loose file at the `Helpers/` root, and deliberately so:
  it is a bag of ~50 mutable statics mixing user settings, per-session scratch state and well-known
  paths, referenced from 49 files. Splitting it into settings / session state / paths is worthwhile
  but is a behavioural change, not a move, so it was left for its own pass.
- **`FrmMainApp` still doubles as a service locator, less than it did.** The shared logger and the `Null*`
  sentinels no longer live on it (each class has its own logger; the sentinels are `Model/NullEquivalents`).
  `.DirectoryElements`, `.Instance`, `.AppLanguage`, `.FolderName` and `.FlatMode` are still reached from `Model/` and
  `Helpers/`, so those layers carry a `using GeoTagNinja.View.Forms;`. They are real application state, not constants, so
  moving them means deciding where the folder collection and the UI-independent session state live. That is a design
  change rather than a move, and was left for its own pass.
- **Thumbnails and previews: how they fit together, and what is left.**
  - *View.* `View/Controls/ThumbnailView` is a purpose-built `ScrollableControl` that paints the items of the (hidden, still
    alive) `FileListView`. The list stays the owner of items, order (sorting, filtering) and selection, so the map, preview,
    shortcuts and edit flows are unchanged: the view writes the user's clicks and keys back as selection on the list and raises
    `UserSelectionChanged` / `ItemActivated`, which `FrmMainApp.ViewMenu.cs` routes to the existing handlers
    (`lvw_HandleSelectionChange`, `lvw_FileList_KeyDown`, `OpenListItem`). Geometry and key navigation are pure and tested
    (`ThumbnailLayout`). It follows the list by polling `FileListView.ItemsVersion` (shape: add/remove/sort) and
    `ContentVersion` (text/colour) on a 100 ms timer; anything indexing the list must use the live `Items.Count`, not its
    cached count, because a folder reload clears the list between two ticks (an exception escaping `OnPaint` makes WinForms
    paint a red cross over the control for good, so cell drawing is also wrapped).
  - *Pictures.* Nothing is held per file. `ThumbnailProvider.Ensure` makes the picture (`ThumbnailGenerator`: embedded
    thumbnail/preview first, scaled decode next, full decode last; aspect ratio kept, no letterbox) and puts it in
    `ThumbnailCache` as a small JPEG at one of two stored sizes (128 or 256, chosen from the on-screen size), so resizing doesn't
    regenerate; the view scales to fit. Tiers: in-memory (`FileKeyedCache`, path + mtime + length + size), then the persistent
    `ThumbnailStore`. The view paints from the in-memory tier only (never the database or disk) via a small LRU of decoded
    bitmaps, and asks its own `ThumbnailWorker` (visible cells first) for anything missing. `PreviewCache` (previews of slow
    formats) is in-memory only.
  - *Store.* `ThumbnailStore` is a SQLite file of its own (WAL, batched writes), closed by `PerformAppClosingProcedure`
    (and `ProcessExit` as a net). A `thumbnailcache.sqlite.running` marker exists while open: finding it at start means a dirty
    exit, so the file is integrity-checked and rebuilt if damaged (any open/check failure also rebuilds). SQLite was kept after
    considering loose files (slow on cloud-synced folders), a hand-rolled pack file (needs our own compaction/recovery) and an
    embedded key-value store (new dependency, no real gain).
  - *Metadata order.* `DirectoryElementCollection.PrioritiseHydration` (fed by `FrmMainApp.StartVisibleRowPrioritisation`)
    makes the ExifTool read serve the visible rows first; both it and the thumbnail worker use `PriorityWorkList`.
  - *Preview pane.* Decodes off the UI thread, cancels superseded requests, RAW via `RawPreview` (LibRaw; the embedded JPEG that
    best fits, `RawContext.RawData` read by reflection as it is protected), and falls back to the running ExifTool
    (`ExifTool.TryGetEmbeddedPreview`: `-json -b`, binary as base64 over the text pipe). The wrapper's common `Filter` (which
    flattens newlines in text values) deliberately skips values starting with the JPEG marker, because it otherwise rewrites
    0x0A/0x0D bytes inside the image and corrupts it.
  - *Interaction.* Sorting (`View > Sort By`, via `FileListView.SortBy`, the same sort the column headers drive), rename (`F2`
    opens an editor over the caption and calls `FileListView.TryRenameItem`, the code the details list's editor uses too), the
    right-click / Shift+F10 menu (`FileListView.ShowActionsMenu`, the list's standard actions without the per-column filter),
    and rubber-band selection (`ThumbnailLayout.IndexesIn`, pure and tested) are all on the view itself. When testing by
    keyboard, remember the view must have the focus (it takes it once the window is shown).
  - *Still to do.* Per-column filtering is Details only; the JPEG/PNG/TIFF preview is still a full-resolution decode (fine now it is off the UI thread, but a
    scaled decode would be quicker for very large files); the metadata read is still strictly sequential within the priority order.
- **`HelperControlAndMessageBoxHandling`** kept its name while moving to `Helpers/UI/`. Renaming it
  touches 253 references and is pure churn on top of an already large diff; worth doing on its own.

## Moving a setting into a menu

The View menu is the first setting to leave the Settings dialog; more can follow the same way. A setting is a row in the
`settings` table whose id is the name of the control that edited it (`ckb_…` check box, `nud_…` number, `rbt_…` radio button, …)
under that control's tab page; import/export copies the whole table, `AppStartup.AppStartupApplyDefaults` maps each id to a
`HelperVariables` field, and `DataWriteSQLiteSettings` writes the row and refreshes the in-memory tables. So:

1. **Keep the id.** If it stays the same, the stored row keeps working and existing users keep their value: no migration.
   (Renaming an id *does* need a one-off migration of the stored row, run once, keyed on the build number that
   `UpgradeNotices` already remembers.)
2. Remove the control from `FrmSettings`: the designer lines, the `.resx` entries (and move the neighbours up to close the gap),
   any mention in the OK handler (restart warnings) and its tooltip handler. Leave the mapping in `AppStartup` and any default in
   `DatabaseAndStartup` alone; they are keyed by id, not by control. (The now-unused caption strings can stay: the language files
   are managed in Weblate.)
3. Add the menu entry in `FrmMainApp.ViewMenu.cs`: `NewMenuItem("tmi_…")` gives a localised, dark-mode-aware item (add its caption to
   `Strings.resx`, in British English); for a plain yes/no setting `AddSettingToggle(...)` does the tick, the store and the call to
   apply it; anything else stores with `MenuSettings.WriteBool/WriteInt/Write`. Mark the active item with `SetMenuMark`, not
   `Checked`: the stock check box is painted light blue in dark mode and as a tiny glyph in an oversized box on high-DPI
   displays, and an image larger than the text makes every row of that drop-down taller than the other menus' rows (hence the
   small, font-sized, unscaled tick). Menu items carry no colours of their own: after adding a menu, run
   `ThemeHelper.ApplyTo(mns_MenuStrip)` so it is themed exactly like the rest (colouring items by hand gave a different dark).
4. Write a setting only when the user changes it (the thumbnail size is the example: `_thumbnailSizeChangedByUser`). Don't seed
   a default row or write the current value back at start-up or on close: a user who never chose a value then simply gets the
   default and the app works the same, and a value chosen earlier is kept whichever mode the app is in.
5. Make it take effect at once (set the `HelperVariables` field and refresh whatever shows it), and re-apply it after the Settings
   dialog closes (`SyncViewFromSettings` is the example), since a settings import can change stored values behind the menu's back.

## Keeping the designer loadable

The WinForms designer does **not** instantiate the form you are designing. It instantiates that
form's *base class* — a plain `System.Windows.Forms.Form` — applies the designer surface to it, and
then constructs each child control for real. Two consequences follow, and both have bitten this
project:

1. **A control constructor runs inside Visual Studio.** Anything it touches must work with no
   application state: no settings database, no ExifTool, no main window.
2. **`Application.OpenForms["FrmMainApp"]` is populated while you design the form, but the entry is a
   plain `Form`.** A hard cast to `FrmMainApp` therefore throws `InvalidCastException`, and because
   it throws while the designer is building `lvw_FileList`, the designer then reports the control as
   "undeclared or never assigned" and the whole surface fails to load.

Use `FrmMainApp.Instance` (a null-safe `as` lookup) rather than casting, and null-check it in
anything reachable from a control constructor.

> **Do not click "Ignore and Continue" on a designer load error.** Once the designer has decided a
> control could not be created, saving the form can rewrite `*.Designer.cs` without it, silently
> deleting layout. Fix the load error first, then open the designer.

To check a control is design-time safe without opening Visual Studio, construct it in a process that
has a plain `Form` named `FrmMainApp` in `Application.OpenForms` — that is precisely the designer's
state.

## SQLite native library

`System.Data.SQLite` 2.x P/Invokes `e_sqlite3` but ships no native binary. `GeoTagNinja.csproj` references `SQLitePCLRaw.lib.e_sqlite3` with its build assets excluded (they reject AnyCPU and copy to a subfolder .NET Framework never probes) and copies the win-x64 DLL flat via a `Content` item. Bump the version in both places together. If the DLL is missing, startup now shows an error and exits (`FrmMainApp.AbortStartup`) instead of hanging.
