# Helper for the VS Code tasks: locates full-framework MSBuild (the legacy csproj cannot be built with
# `dotnet build`) and runs one action. Keep in step with the Build and test section of CLAUDE.md.
param(
    [Parameter(Mandatory)][ValidateSet('Restore', 'Build', 'Run', 'Test', 'Installer')][string]$Action
)
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)

# Output lives outside the repo (see Directory.Build.props).
$exe = Join-Path $env:LOCALAPPDATA 'build\GeoTagNinja\GeoTagNinja\bin\Debug\GeoTagNinja.exe'

function Stop-StaleExifTool {
    # A leftover stay_open exiftool locks files in the build output.
    $stale = Get-Process -Name exiftool -ErrorAction SilentlyContinue
    if ($stale) {
        Write-Host 'Stopping stale exiftool processes...'
        $stale | Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

function Get-MSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $found = $null
    if (Test-Path $vswhere) {
        $found = & $vswhere -latest -prerelease -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    }
    if (-not $found) {
        throw 'No Visual Studio MSBuild found. Install Visual Studio or Build Tools with the .NET desktop build tools workload.'
    }
    return $found
}

switch ($Action) {
    'Restore' {
        $msb = Get-MSBuild
        Write-Host "Using MSBuild: $msb"
        & $msb 'GeoTagNinja.sln' /t:Restore
        exit $LASTEXITCODE
    }
    'Build' {
        Stop-StaleExifTool
        $msb = Get-MSBuild
        Write-Host "Using MSBuild: $msb"
        & $msb 'GeoTagNinja.sln' /t:Build /p:Configuration=Debug /p:Platform=x64
        exit $LASTEXITCODE
    }
    'Run' {
        Stop-StaleExifTool
        if (-not (Test-Path $exe)) { throw "Build output not found at $exe. Run the build task first." }
        Start-Process -FilePath $exe -WorkingDirectory (Split-Path -Parent $exe)
    }
    'Installer' {
        # Release solution build (app, then the tests: a failing test fails it), then the WiX installer. The installer is
        # deliberately not in the .sln: Visual Studio cannot open a .wixproj without the HeatWave extension, and this
        # keeps VS out of the picture. Restore is done here because the plain Restore action runs in Debug.
        Stop-StaleExifTool
        $msb = Get-MSBuild
        Write-Host "Using MSBuild: $msb"
        & $msb 'GeoTagNinja.sln' /t:Build /p:Configuration=Release /p:Platform=x64
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        & $msb 'GeoTagNinja.Setup\GeoTagNinja.Setup.wixproj' -restore /t:Build /p:Configuration=Release
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        # The "current" build number is the third part of the exe's version (1.19.<build>.x, it auto-increments), which is
        # also what the MSI is stamped with and what the release tag uses (b<build>). Cross-check it against the newest
        # changelog entry, then leave a copy of the MSI in a per-build folder (the file name must stay
        # GeoTagNinja_Setup.msi: the in-app updater downloads it by that name).
        $root = Join-Path $env:LOCALAPPDATA 'build\GeoTagNinja'
        $msi = Join-Path $root 'GeoTagNinja.Setup\bin\x64\Release\GeoTagNinja_Setup.msi'
        $releaseExe = Join-Path $root 'GeoTagNinja\bin\Release\GeoTagNinja.exe'
        $build = (Get-Item $releaseExe).VersionInfo.FileBuildPart
        $top = Select-String -Path 'changelog.md' -Pattern '^\*\*Build (\d+)' | Select-Object -First 1
        if ($top -and [int]$top.Matches[0].Groups[1].Value -ne $build) {
            Write-Warning "The exe is build $build but the top changelog entry is build $($top.Matches[0].Groups[1].Value)."
        }
        $dest = Join-Path $root "release\b$build"
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item -LiteralPath $msi -Destination $dest -Force
        Write-Host "Build $build installer: $(Join-Path $dest 'GeoTagNinja_Setup.msi')"
        exit 0
    }
    'Test' {
        # --no-build --no-restore is mandatory: dotnet cannot build the main project.
        & dotnet test 'GeoTagNinja.Tests\GeoTagNinja.Tests.csproj' --no-build --no-restore
        exit $LASTEXITCODE
    }
}
