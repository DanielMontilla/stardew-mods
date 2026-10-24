#!/usr/bin/env pwsh
# Repo-wide verification gate. CI calls this script so the two cannot drift.
[CmdletBinding()]
param(
    [switch]$Fast
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

try {
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw 'restore failed' }

    dotnet format --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'format check failed' }

    dotnet build --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'build failed' }

    if ($Fast) {
        Write-Host 'Fast verify complete (tests skipped).'
        return
    }

    $testProjects = Get-ChildItem -Path $root -Recurse -Filter *.csproj |
        Where-Object { $_.Name -match 'Tests?\.csproj$' }

    if ($testProjects) {
        dotnet test --no-build --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'tests failed' }
    }
    else {
        Write-Host 'No test projects found; skipping tests.'
    }

    Write-Host 'Verify complete.'
}
finally {
    Pop-Location
}
