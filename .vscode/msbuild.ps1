# Helper for the VS Code tasks: locates full-framework MSBuild (the legacy csproj cannot be built with
# `dotnet build`) and runs one action. Keep in step with the Build and test section of CLAUDE.md.
param(
    [Parameter(Mandatory)][ValidateSet('Restore', 'Build', 'Run', 'Test')][string]$Action
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
    'Test' {
        # --no-build --no-restore is mandatory: dotnet cannot build the main project.
        & dotnet test 'GeoTagNinja.Tests\GeoTagNinja.Tests.csproj' --no-build --no-restore
        exit $LASTEXITCODE
    }
}
