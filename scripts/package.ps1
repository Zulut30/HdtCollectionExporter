param([string]$Version = '1.6.0')
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$dll = Join-Path $repo 'src\HdtCollectionExporter\bin\x64\Release\HdtCollectionExporter.dll'
if(([Reflection.AssemblyName]::GetAssemblyName($dll)).Version.ToString(3) -ne $Version) { throw 'Release version and DLL version differ.' }
$output = Join-Path $repo 'artifacts\release'
$stage = Join-Path $output ('package-' + $Version)
New-Item -ItemType Directory -Force -Path $output, $stage | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $output 'HdtCollectionExporter.dll') -Force
Copy-Item -LiteralPath $dll -Destination $stage -Force
foreach($guide in @('INSTALL.en.md','INSTALL.ru.md','RELEASE_NOTES-1.6.0.md')) { Copy-Item -LiteralPath (Join-Path $repo "docs\$guide") -Destination $stage -Force }
Copy-Item -LiteralPath (Join-Path $repo 'scripts\install.ps1') -Destination $stage -Force
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath (Join-Path $output "HdtCollectionExporter-v$Version.zip") -Force
$macStage = Join-Path $output ('macos-' + $Version)
New-Item -ItemType Directory -Force -Path $macStage | Out-Null
Copy-Item -Path (Join-Path $repo 'macos\HSTrackerManacostExporter\*.swift') -Destination $macStage -Force
Copy-Item -Path (Join-Path $repo 'docs\HSTRACKER_MACOS.*.md') -Destination $macStage -Force
Compress-Archive -Path (Join-Path $macStage '*') -DestinationPath (Join-Path $output "HSTrackerManacostExporter-v$Version-source.zip") -Force
$hashes = @('HdtCollectionExporter.dll', "HdtCollectionExporter-v$Version.zip", "HSTrackerManacostExporter-v$Version-source.zip") | ForEach-Object {
    $hash = Get-FileHash -LiteralPath (Join-Path $output $_) -Algorithm SHA256
    "$($hash.Hash.ToLowerInvariant())  $_"
}
$hashes | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Encoding ascii
Write-Host "Release files: $output"
