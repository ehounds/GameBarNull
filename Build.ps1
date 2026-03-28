# Build.ps1 - Compiles GameBarNull.exe, Setup.exe, and Uninstaller.exe
# Uses the .NET Framework csc.exe bundled with every Windows install (no SDK needed).
# Output goes to the dist\ folder.

$ErrorActionPreference = 'Stop'

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Definition
$srcDir     = Join-Path $scriptDir 'src'
$distDir    = Join-Path $scriptDir 'dist'
$manifest   = Join-Path $srcDir   'admin-manifest.xml'

# --- Locate csc.exe ---
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { $csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
if (-not (Test-Path $csc)) {
    Write-Error "csc.exe not found. .NET Framework 4.x is required (ships with Windows)."
    exit 1
}

Write-Host "Compiler  : $csc" -ForegroundColor Cyan
Write-Host "Output    : $distDir"
Write-Host ""

if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir | Out-Null }

function Compile($label, $sources, $output, [switch]$uac) {
    Write-Host "Building  $label..." -NoNewline
    $args = @(
        '/nologo',
        '/target:winexe',
        "/out:$output",
        '/reference:System.Windows.Forms.dll'
    )
    if ($uac) { $args += "/win32manifest:$manifest" }
    # Append source file(s) — accepts a single string or an array
    $args += $sources

    & $csc @args
    if ($LASTEXITCODE -ne 0) {
        Write-Host " FAILED" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host " OK" -ForegroundColor Green
}

# GameBarNull.exe  (no UAC manifest — protocol handler only, no UI)
Compile 'GameBarNull.exe' `
        (Join-Path $scriptDir 'GameBarNull.cs') `
        (Join-Path $distDir   'GameBarNull.exe')

# Setup.exe  (requires admin — UAC manifest embedded)
Compile 'Setup.exe' `
        @((Join-Path $srcDir 'Setup.cs'), (Join-Path $srcDir 'About.cs')) `
        (Join-Path $distDir 'Setup.exe') `
        -uac

# Uninstaller.exe  (requires admin — UAC manifest embedded)
Compile 'Uninstaller.exe' `
        @((Join-Path $srcDir 'Uninstaller.cs'), (Join-Path $srcDir 'About.cs')) `
        (Join-Path $distDir 'Uninstaller.exe') `
        -uac

Write-Host ""
Write-Host "Build complete." -ForegroundColor Green
Write-Host ""
Write-Host "dist\ contents:"
Get-ChildItem $distDir | ForEach-Object {
    Write-Host ("  {0,-22} {1,7} KB" -f $_.Name, [math]::Ceiling($_.Length / 1024))
}
Write-Host ""
Write-Host "To install, run Setup.exe from the dist\ folder (UAC prompt will appear)."
