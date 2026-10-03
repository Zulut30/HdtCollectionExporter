param(
    [Parameter(Mandatory = $true)][string]$PluginPath,
    [Parameter(Mandatory = $true)][string]$HdtDirectory
)

$ErrorActionPreference = 'Stop'
if (-not [Environment]::Is64BitProcess) { throw 'Run this check in a 64-bit process.' }
$HdtDirectory = (Resolve-Path -LiteralPath $HdtDirectory).Path
$resolveHandler = [System.ResolveEventHandler] {
    param($sender, $eventArgs)
    $name = [System.Reflection.AssemblyName]::new($eventArgs.Name).Name
    foreach ($extension in @('.dll', '.exe')) {
        $candidate = Join-Path $HdtDirectory ($name + $extension)
        if ($name -eq 'HearthstoneDeckTracker' -and -not (Test-Path -LiteralPath $candidate)) { $candidate = Join-Path $HdtDirectory 'Hearthstone Deck Tracker.exe' }
        if (Test-Path -LiteralPath $candidate) {
            return [System.Reflection.Assembly]::LoadFrom($candidate)
        }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolveHandler)
try {
    $assembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $PluginPath).Path)
    $types = $assembly.GetTypes()
    $pluginTypes = @($types | Where-Object {
        -not $_.IsAbstract -and $_.GetInterface('Hearthstone_Deck_Tracker.Plugins.IPlugin')
    })
    if ($pluginTypes.Count -ne 1) { throw "Expected one plugin entry; found $($pluginTypes.Count)." }
    $pluginNames = @($pluginTypes | ForEach-Object {
        $plugin = [Activator]::CreateInstance($_)
        if ($plugin.Version -ne [Version]'1.6.0.0') { throw 'Unexpected plugin version.' }
        $plugin.Name
    })

    $settingsType = $assembly.GetType('HdtCollectionExporter.Settings.PluginSettings', $true)
    $providerType = $assembly.GetType('HdtCollectionExporter.Services.HdtCollectionProvider', $true)
    $serviceType = $assembly.GetType('HdtCollectionExporter.Services.CollectionExportService', $true)
    $windowType = $assembly.GetType('HdtCollectionExporter.UI.ExportWindow', $true)
    $textType = $assembly.GetType('HdtCollectionExporter.UI.ExportWindowText', $true)
    $settings = [Activator]::CreateInstance($settingsType)
    $provider = [Activator]::CreateInstance($providerType)
    $service = [Activator]::CreateInstance($serviceType, [object[]]@($provider))
    $windowTitles = @()
    foreach ($language in @('English', 'Russian')) {
        $text = $textType.GetMethod($language).Invoke($null, $null)
        $window = [Activator]::CreateInstance($windowType, [object[]]@($settings, $service, [Action]{}, $text))
        foreach ($control in @('OutputFolderTextBox','StatusText','HistoryTab','SummaryTab','ExportButton','LanguageBox')) { if ($window.FindName($control) -eq $null) { throw "Missing WPF control: $control" } }
        if ($window.FindName('ExportButton').IsEnabled) { throw 'Export should be disabled until collection data is read.' }
        $windowTitles += $window.Title
        $window.Close()
    }
    [pscustomobject]@{
        Is64BitProcess = [Environment]::Is64BitProcess
        ProcessorArchitecture = $assembly.GetName().ProcessorArchitecture.ToString()
        LoadedTypes = $types.Count
        PluginEntries = $pluginNames
        InitializedWindows = $windowTitles
    } | ConvertTo-Json -Depth 4
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolveHandler)
}
