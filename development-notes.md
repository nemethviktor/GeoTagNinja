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

## Still outstanding

- **`Helpers/HelperVariables.cs`** is the last loose file at the `Helpers/` root, and deliberately so:
  it is a bag of ~50 mutable statics mixing user settings, per-session scratch state and well-known
  paths, referenced from 49 files. Splitting it into settings / session state / paths is worthwhile
  but is a behavioural change, not a move, so it was left for its own pass.
- **`FrmMainApp` doubles as a service locator.** `FrmMainApp.Log`, `.DirectoryElements`,
  `.NullStringEquivalentGeneric` and friends are reached from `Model/` and `Helpers/`, so those layers
  now carry a `using GeoTagNinja.View.Forms;`. That dependency was always there; moving the forms into
  their own namespace merely made it visible. Extracting the shared state is the fix.
- **`HelperControlAndMessageBoxHandling`** kept its name while moving to `Helpers/UI/`. Renaming it
  touches 253 references and is pure churn on top of an already large diff; worth doing on its own.

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
