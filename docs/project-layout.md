# Where things live

Folders map one-to-one onto namespaces (`Helpers/FileSystem/` is `GeoTagNinja.Helpers.FileSystem`),
as `.editorconfig` asks for with `dotnet_style_namespace_match_folder`. If you move a file, move its
namespace with it; the build will not warn you, but the next reader will suffer.

```
Program.cs                     entry point
SingleInstance_PipeServer.cs   named-pipe guard that forwards a second launch to the running instance

Model/                         the domain: what a file's metadata is, independent of how it is shown
  DirectoryElement             one file, its attributes, and the pending edits to them
  DirectoryElementCollection   the folder being browsed; also drives the ExifTool read pass
  SourcesAndAttributes         which ExifTool tags feed which attribute, and with what type
  AttributeValueFormatter      the only place typed values become text - see architecture-values.md
  ExifTagSetParser             raw ExifTool tags -> typed values; the only reader, see architecture-values.md
  TagsToModelValueTransformations  per-attribute clean-up of raw ExifTool output
  ExifToolWrapper              the stay_open ExifTool process
  Favourite / GeoSetterFavourite / MapWebMessage / AppSettingContainer   plain data carriers

View/
  Forms/      every Frm* dialog, plus EditFileFormLauncher
  Controls/   custom controls (ImagePreview)
  FileList/   the main file list view and its collaborators
  Dialogs/    reusable message-box style dialogs

Helpers/
  API/           GeoNames and GitHub HTTP calls and their response types
  Data/          SQLite settings, custom rules, and the session's in-memory DataTables
  Exif/          reading, writing and track-file handling via ExifTool
  FileSystem/    enumeration, checksums, locking, cloud placeholders, supported extensions
  Geo/           coordinate and field-of-view maths
  Localisation/  languages, countries, time zones, resource lookup
  Map/           the Leaflet layer list
  Startup/       first-run and per-launch setup
  Text/          string odds and ends
  UI/            control-tree walking, form placement, localised control text
  Update/        AutoUpdater.NET callbacks
  HelperVariables.cs   application-wide mutable state (see below)
```

## Rules of thumb

- **Name a folder after a subject, not a mechanism.** The folders that had to be dissolved were
  `Helpers/NonStatic/` (grouped four unrelated classes because none of them were `static`) and
  `Helpers/Generic/` (grouped things because they were hard to name). Neither told you anything about
  what the code did.
- **A type goes where its subject lives, not where it is first used.** `SettingsImportExportOptions`
  described the settings file but lived in `FrmSettings.cs` because that is where the export button
  is; it now sits with the import/export code that actually reads it.
- **Don't name a namespace after a framework type.** `GeoTagNinja.View.ListView` shadowed
  `System.Windows.Forms.ListView` for everything under `GeoTagNinja.View`, so a plain `ListView`
  silently stopped resolving to the control. It is now `GeoTagNinja.View.FileList`.
- **Forms own layout and event wiring, nothing else.** Anything a form knows that another form might
  also need belongs in `Helpers/` or `Model/`.

## Moving a form

Forms are the one case where the layout has a hidden constraint. A `.resx` is compiled to a manifest
resource named after the **namespace and class of the `.cs` file it is `DependentUpon`**, while
`ComponentResourceManager` looks it up by `typeof(TheForm).FullName`. Move the folder and the
namespace together and the two stay aligned; change one without the other and the form throws
`MissingManifestResourceException` at runtime, with a clean build. After moving a form, check with:

```powershell
$asm = [Reflection.Assembly]::LoadFrom("bin\Debug\GeoTagNinja.exe")
$asm.GetManifestResourceNames() | Where-Object { $_ -match 'Frm' }
```

Each name must equal `<type FullName>.resources`.

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
