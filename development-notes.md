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

## Telling upgrading users about a change

`Helpers/Startup/UpgradeNotices.cs` shows a one-off message to users who upgrade from an older build. The app stores the build it last ran (just `Version.Build`, e.g. `9778`, as `generic` / `lastRunBuild` in the settings table) and compares it with `UpgradeNotices.Registry` at startup.

To announce something, add an entry to `Registry` with the build to compare against and the English text. The text is deliberately in code, not `Strings.resx`: a build can go public before translators catch up.

- `showIfLastRunBuildAtMost` is the last build released *without* the change, so it must be lower than the build that ships the notice. A user whose stored build is `<=` it sees the message.
- It shows once: right after the box is dismissed the stored build is raised to the current one. Several due notices share one box.
- New users never see one. A fresh install is recognised by an empty `settings` table, which is why `UpgradeNotices.EvaluateAtStartup` has to run before the default settings are written (`AppStartup.AppStartupWriteDefaultSettings`).
- Users from before the tracking existed have no stored build and count as `LastBuildBeforeTracking` (9778).
- The stored build is never lowered, so running an older build doesn't make a notice reappear.

## Still outstanding

- **`Helpers/HelperVariables.cs`** is the last loose file at the `Helpers/` root, and deliberately so:
  it is a bag of ~50 mutable statics mixing user settings, per-session scratch state and well-known
  paths, referenced from 49 files. Splitting it into settings / session state / paths is worthwhile
  but is a behavioural change, not a move, so it was left for its own pass.
- **`FrmMainApp` doubles as a service locator.** `FrmMainApp.Log`, `.DirectoryElements`,
  `.NullStringEquivalentGeneric` and friends are reached from `Model/` and `Helpers/`, so those layers
  now carry a `using GeoTagNinja.View.Forms;`. That dependency was always there; moving the forms into
  their own namespace merely made it visible. Extracting the shared state is the fix.
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
