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
  UI/            control-tree walking, form placement, localised control text, and ThemeHelper
                 (the single entry point for dark mode - call it rather than styling controls by hand)
  Update/        AutoUpdater.NET callbacks
  HelperVariables.cs   application-wide mutable state (see below)

GeoTagNinja.Tests/             NUnit tests for the value-handling logic; see its README.md

```

## Rules of thumb

- **Name a folder after a subject, not a mechanism.**
- **A type goes where its subject lives, not where it is first used.**
- **Don't name a namespace after a framework type** (it shadows the framework type for everything beneath it).
- **Forms own layout and event wiring, nothing else.** Anything a form knows that another form might
  also need belongs in Helpers/ or Model/.

## Moving a form

Forms are the one case where the layout has a hidden constraint. A `.resx` is compiled to a manifest
resource named after the **namespace and class of the `.cs` file it is `DependentUpon`**, while
`ComponentResourceManager` looks it up by `typeof(TheForm).FullName`. Move the folder and the
namespace together and the two stay aligned; change one without the other and the form throws
`MissingManifestResourceException` at runtime, with a clean build. After moving a form, check with:

```powershell
$asm = [Reflection.Assembly]::LoadFrom("$env:LOCALAPPDATA\build\GeoTagNinja\GeoTagNinja\bin\Debug\GeoTagNinja.exe")
$asm.GetManifestResourceNames() | Where-Object { $_ -match 'Frm' }
```

Each name must equal `<type FullName>.resources`.
