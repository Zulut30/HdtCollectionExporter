param(
    [string]$PluginPath,
    [string]$DataDirectory = (Join-Path $env:APPDATA 'HearthstoneDeckTracker')
)
$ErrorActionPreference = 'Stop'
if([string]::IsNullOrWhiteSpace($PluginPath)) {
    $bundledPlugin = Join-Path $PSScriptRoot 'HdtCollectionExporter.dll'
    $PluginPath = if(Test-Path -LiteralPath $bundledPlugin) { $bundledPlugin } else { Join-Path (Split-Path $PSScriptRoot) 'src\HdtCollectionExporter\bin\x64\Release\HdtCollectionExporter.dll' }
}
if(Get-Process HearthstoneDeckTracker -ErrorAction SilentlyContinue) { throw 'Fully close HDT before installing; settings and DLLs may otherwise be overwritten.' }
$PluginPath = (Resolve-Path -LiteralPath $PluginPath).Path
$pluginsDir = Join-Path $DataDirectory 'Plugins'
$backup = Join-Path $DataDirectory ('HdtCollectionExporter\backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Force -Path $pluginsDir, $backup | Out-Null
$destination = Join-Path $pluginsDir 'HdtCollectionExporter.dll'
if(Test-Path -LiteralPath $destination) { Copy-Item -LiteralPath $destination -Destination (Join-Path $backup 'HdtCollectionExporter.dll') }
$settingsPath = Join-Path $DataDirectory 'plugins.xml'
if(Test-Path -LiteralPath $settingsPath) {
    Copy-Item -LiteralPath $settingsPath -Destination (Join-Path $backup 'plugins.xml')
    [xml]$settings = Get-Content -LiteralPath $settingsPath -Raw
    $entries = @($settings.ArrayOfPluginSettings.PluginSettings | Where-Object {
        $_.FileName -match '(^|[\\/])HdtCollectionExporter\.dll$' -and
        $_.Name -in @('Collection Exporter by Manacost', 'Экспорт коллекции от Manacost')
    })
    if($entries.Count -gt 0) {
        $enabled = @($entries | Where-Object { $_.IsEnabled -eq 'true' }).Count -gt 0
        $entry = $entries[0]
        $entry.Name = 'Collection Exporter by Manacost'
        $entry.IsEnabled = $enabled.ToString().ToLowerInvariant()
        foreach($duplicate in $entries | Select-Object -Skip 1) { $duplicate.ParentNode.RemoveChild($duplicate) | Out-Null }
        $temporary = $settingsPath + '.manacost.tmp'
        try { $settings.Save($temporary); [IO.File]::Replace($temporary, $settingsPath, (Join-Path $backup 'plugins-before-replace.xml'), $true) }
        finally { if(Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary } }
    }
}
Copy-Item -LiteralPath $PluginPath -Destination $destination -Force
if((Get-FileHash -LiteralPath $PluginPath).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) { throw 'Installed DLL checksum mismatch.' }
Write-Host "Installed $destination"
Write-Host "Backup: $backup"
Write-Host 'Start HDT. If no previous plugin entry existed, enable Collection Exporter by Manacost once.'
