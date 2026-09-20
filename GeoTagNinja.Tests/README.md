# GeoTagNinja.Tests

Pure-logic tests. No image files, no ExifTool process, no SQLite, no UI — they run in about 150 ms
and touch nothing on your machine.

## Running them

In Visual Studio: Test Explorer, as normal.

From a shell, build first with **full-framework MSBuild**, then run:

```powershell
$msb = "C:\Program Files\Microsoft Visual Studio\18\Insiders\MSBuild\Current\Bin\MSBuild.exe"
& $msb GeoTagNinja.sln /t:Restore
& $msb GeoTagNinja.sln /t:Build /p:Configuration=Debug

dotnet test GeoTagNinja.Tests\GeoTagNinja.Tests.csproj --no-build --no-restore
```

### Why not a plain `dotnet test`?

Because it would try to build the main project, and it cannot. `GeoTagNinja.csproj` is a legacy
(non-SDK) WinForms project: building it needs the .NET Framework MSBuild for reference resolution
and the resx/WinForms tasks, which the SDK's CoreCLR MSBuild does not provide. A bare `dotnet test`
produces a wall of "type or namespace not found" errors for every NuGet package.

`--no-build --no-restore` sidesteps that: the assemblies are already there, so the SDK only has to
run the test host. `vstest.console.exe` from the VS install works equally well if you prefer.

If you ever convert `GeoTagNinja.csproj` to SDK-style (`<Project Sdk="Microsoft.NET.Sdk">` with
`<UseWindowsForms>true</UseWindowsForms>`), a plain `dotnet test` starts working and these flags can
go. That is a worthwhile change but not a small one - it touches the designer, the resx pipeline,
the SQLite interop copy targets and the LibRaw props.

### One gotcha

The two toolchains share `obj/project.assets.json`. An SDK-side restore used to leave the project
unbuildable by MSBuild with:

> Your project file doesn't list 'win-x64' as a "RuntimeIdentifier"

That is why `GeoTagNinja.csproj` now declares `<RuntimeIdentifiers>win-x64</RuntimeIdentifiers>`.
Several packages ship RID-specific native assets (Magick.NET, Sdcb.LibRaw, SQLite interop) and since
the move to PackageReference, NuGet needs that spelled out. Don't remove it.

## What they cover, and why

Every value-handling fixture is a `[TestFixtureSource]` over a dozen or so cultures — `en-US`, `en-GB`,
`en-AU`, `en-IN`, `es-AR`, `hu-HU` , `fi-FI`, `ar-SA`, `ar-EG`, `de-CH`, `fr-FR`, `ja-JP`, `th-TH`,— so each test runs once per locale. The set is chosen
in `Cultures.cs` to break every assumption the code has historically made: month-first vs day-first
vs year-first dates, dot vs comma decimals, and lakh vs thousand grouping plus some fancy cultures.

| Fixture | Guards against |
| --- | --- |
| `DateValueTests` | A timestamp meaning something different depending on where the user lives. Covers both ExifTool date layouts, so parsing no longer depends on `.ExifTool_config` having been found. |
| `NumericValueTests` | A decimal being misread. See below. |
| `MetadataRoundTripTests` | The two reported field bugs, expressed end-to-end through `DirectoryElement`: an inserted create-date, and a typed coordinate/altitude, reaching ExifTool unchanged. |
| `TagTransformationTests` | The per-tag clean-up of raw ExifTool output — rationals, units, the "35 mm equivalent" phrasing. |
| `ReadPipelineParityTests` | The track-file reader drifting away from the file reader again. They were separate implementations of the same transformations until they were merged; these assert both entry points give the same typed values for the same tags. |

## The bug these found on day one

`NumberStyles.Any` includes `AllowThousands`, and the invariant group separator is a comma. So

```csharp
double.Parse("5,4", NumberStyles.Any, CultureInfo.InvariantCulture)  // == 54
```

.NET does not validate group sizes, so `-33,8688` was accepted as `-338688`. A user whose decimal
separator is a comma — Hungary, Argentina, most of continental Europe — typing a latitude got it out
by four orders of magnitude, silently, with the photo landing in the wrong hemisphere.

The fix is `AttributeValueFormatter.MachineNumberStyles`, which is `NumberStyles.Float` precisely so
that grouping is *not* accepted: `5,4` now fails the invariant parse and falls through to being read
in the user's own culture, which is what they meant. ExifTool never emits digit grouping, so nothing
legitimate is lost.

## Adding to these

Put new value-handling tests in a `[TestFixtureSource(typeof(Cultures), nameof(Cultures.All))]`
fixture rather than a plain one. The cost is nil and it is the single highest-value axis for this
application.

Internals are visible here via `InternalsVisibleTo` in `Properties/AssemblyInfo.cs`, so `internal`
helper classes can be tested directly without being made public.
