# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

GeoTagNinja is a Windows WinForms photo geotagger (.NET Framework 4.8, x64, `LangVersion` preview) that reads and writes metadata through a long-running ExifTool process. Do not propose upgrading off .NET Framework 4.8.

## Build and test

`GeoTagNinja.csproj` is a **legacy (non-SDK) project**, so it must be built with full-framework MSBuild. `GeoTagNinja.Tests` is SDK-style net48 (NUnit 3).

```powershell
$msb = "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"
& $msb GeoTagNinja.sln /t:Restore
& $msb GeoTagNinja.sln /t:Build /p:Configuration=Debug

dotnet test GeoTagNinja.Tests\GeoTagNinja.Tests.csproj --no-build --no-restore
dotnet test GeoTagNinja.Tests\GeoTagNinja.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~DateValueTests"   # one fixture
```

- A plain `dotnet build` / `dotnet test` cannot build the main project (a wall of CS0246). Always use `--no-build --no-restore` with `dotnet test`.
- Both toolchains share `obj/project.assets.json`. Keep `<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>` in the csproj, or MSBuild restores break.
- Build output goes to `%LOCALAPPDATA%\build\GeoTagNinja\<project>\bin|obj\` (see `Directory.Build.props`), not the repo, because the repo lives on Google Drive. The exe is at `%LOCALAPPDATA%\build\GeoTagNinja\GeoTagNinja\bin\Debug\GeoTagNinja.exe`.
- A Release build of the main project runs the tests automatically (`RunTestsOnRelease` target in the test csproj).
- There is no CI and no lint step. A green Debug and Release build, passing tests, and a launch check are the regression signal. Smoke test: `Start-Process -PassThru` the exe, wait about 15s, check `Responding`, then `CloseMainWindow()`.

## Architecture

Folders map one-to-one onto namespaces. Moving a file means changing its namespace too. `project-layout.md` has the full map, and `development-notes.md` has how-tos and known loose ends.

- **`Model/`** is the domain, independent of display. `DirectoryElement` is one file with its attributes and pending edits. `DirectoryElementCollection` is the browsed folder and drives the ExifTool read pass. `SourcesAndAttributes` maps ExifTool tags to attributes and their CLR types. `ExifToolWrapper` is the `stay_open` ExifTool process.
- **`View/`** holds `Forms/` (every `Frm*` dialog), `Controls/`, `FileList/` and `Dialogs/`. Forms own layout and event wiring only. Logic that another form might need belongs in `Helpers/` or `Model/`.
- **`Helpers/`** is organised by subject: `Exif/` (read, write, track files), `Data/` (SQLite settings and in-memory DataTables), `Geo/`, `Map/` (Leaflet in WebView2), `UI/`, and others. `UI/ThemeHelper` is the single entry point for dark mode.

### Value pipeline (read `architecture-values.md` before touching value code)

ExifTool raw tag strings go through `ExifTagSetParser`, then `TagsToModelValueTransformations`, then `AttributeValueFormatter.TryParse`, and become typed CLR values (DateTime, double, int, string) held in the model. They only become text again at the edges, through `AttributeValueFormatter.Format(value, ValueFormatContext)`. The contexts are `Display`, `RoundTrip` (invariant, for text this app re-parses) and `ExifTool`.

- Photos and GPX-track sidecars both use `ExifTagSetParser`. Do not add a second reader.
- Never re-parse a value you rendered as text. Pass the typed value through. A per-culture allow-list is always the wrong fix.
- Numbers are deliberately invariant in all contexts, including `Display`.
- `DirectoryElement` keeps up to four versions per attribute (`Original`, `Stage1…`, `Stage2…`, `Stage3ReadyToWrite`). The newest-first ordering lives only in `DirectoryElement.VersionsNewestFirst`.
- `GPSAltitude` is held in the user's display unit. `T2M_Altitude` converts on read and `WriteSaveToFile` converts back on write.
- The blank sentinel for strings is `"-"` (`FrmMainApp.NullStringEquivalentGeneric`). `WriteSaveToFile` maps it to empty so it is never written to a file.

### Known structural debt

- `FrmMainApp` (about 4k lines) doubles as a service locator (`Log`, `DirectoryElements`, the `Null*Equivalent` constants), reached from Model and Helpers.
- `HelperVariables` holds about 50 mutable statics for settings, session state and paths.
- The write path, `UpdateArgsFile`, dedupes arguments by substring `Contains`, which can suppress a legitimately different argument.

## Conventions

- **Every argument is passed by name** (`value:`, `path1:`), literals included. `GeoTagNinja.sln.DotSettings` enforces this, not the compiler.
- Source files are **UTF-8 with BOM and CRLF**. `sed -i` and most heredocs write LF, so re-encode after editing that way.
- Adding a checkbox to `FrmSettings` touches four places: the control in the designer and resx, a caption in `Resources/Languages/Strings.resx`, a `UserSetting…` static plus its mapping in `AppStartup.AppStartupApplyDefaults`, and the code that reads it. See `development-notes.md`.
- Every change also updates `changelog.md`, the relevant root-level docs (`*.md`), and the code comments. Skim `readme.md` and fix or flag anything badly stale.
- **`.vscode/` is part of the build contract.** `tasks.json`, `launch.json` and `.vscode/msbuild.ps1` hard-code the MSBuild lookup, the `%LOCALAPPDATA%\build\...` exe path, the x64 platform and the test command. Any change to the build setup (output paths in `Directory.Build.props` or the csproj, platforms or configurations, restore or test commands, target framework, new projects) must update them in the same change. Verify by running the scripts: `powershell -File .vscode\msbuild.ps1 -Action Restore`, then `Build`, then `Test`. Also keep the `readme.md` "Building the main app in VS Code" task list in step. `GeoTagNinja_Setup/` (a `.vdproj`, IDE-only) is in the solution but outside this workflow: MSBuild skips it with warning MSB4078, so expect that warning in every solution build.

## Traps that build cleanly and fail at runtime

- **Moving a Form.** Update the csproj `Include` path (the legacy csproj needs it), and move the `.cs`, `.Designer.cs` and `.resx` together with the namespace. A `.resx` manifest name comes from the namespace and class of the `.cs` it is `DependentUpon`, but `ComponentResourceManager` looks it up by `typeof(Form).FullName`. A mismatch gives `MissingManifestResourceException` when the form opens. Verify with `Assembly.GetManifestResourceNames()`, where every name must equal `<FullName>.resources`.
- **Designer safety.** The WinForms designer instantiates the form's base class, a plain `Form`, then builds each child control for real. Control constructors must not need app state, and must not cast `Application.OpenForms["FrmMainApp"]`. Use the null-safe `FrmMainApp.Instance` and null-check it. Never click "Ignore and Continue" on a designer load error, because saving can strip controls from `*.Designer.cs`.

## Tests

Tests cover pure logic only, with no files, ExifTool, SQLite or UI. Every value fixture runs once per locale via `[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]`. The locales in `Cultures.cs` are chosen to break assumptions about date order, decimal separator and digit grouping. When touching value parsing or formatting, add the case to the culture matrix first. The maintainer develops in a dot-decimal, month-first locale, so locale bugs are invisible to them. See `GeoTagNinja.Tests/README.md`.
