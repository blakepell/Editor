<#
.SYNOPSIS
    Increments the date-based build version in NEdit.csproj and publishes the AOT binary.

.DESCRIPTION
    Version format: YYYY.M.D.N
      - YYYY.M.D  — today's date (no leading zeros)
      - N         — daily build counter, starting at 1 and incrementing for each
                    publish on the same calendar day; resets to 1 on a new day

.PARAMETER Runtime
    The .NET runtime identifier to target. Defaults to linux-x64.

.PARAMETER Configuration
    The build configuration. Defaults to Release.
#>
param(
    [string]$Runtime       = "linux-x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$scriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$csprojPath = Join-Path $scriptDir "src\NEdit.csproj"

# Read the project file
$content = Get-Content $csprojPath -Raw -Encoding UTF8

# Build today's date prefix with no leading zeros
$today        = Get-Date
$todayPrefix  = "$($today.Year).$($today.Month).$($today.Day)"

# Determine the next build counter
if ($content -match '<Version>(\d+)\.(\d+)\.(\d+)\.(\d+)</Version>') {
    $vYear  = [int]$Matches[1]
    $vMonth = [int]$Matches[2]
    $vDay   = [int]$Matches[3]
    $vBuild = [int]$Matches[4]

    if ($vYear -eq $today.Year -and $vMonth -eq $today.Month -and $vDay -eq $today.Day) {
        $newBuild = $vBuild + 1
    } else {
        $newBuild = 1
    }
} else {
    $newBuild = 1
}

$newVersion = "$todayPrefix.$newBuild"
Write-Host "Version: $newVersion" -ForegroundColor Cyan

# Write the updated version back to the project file
$updated = $content -replace '<Version>[^<]+</Version>', "<Version>$newVersion</Version>"
$updated | Out-File $csprojPath -Encoding UTF8 -NoNewline

# Publish
Write-Host "Publishing ($Runtime / $Configuration)..." -ForegroundColor Cyan
dotnet publish $csprojPath -r $Runtime -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Done. Build $newVersion" -ForegroundColor Green
