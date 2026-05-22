param(
    [string]$Configuration = "Release",
    [string]$HDTInstallDir
)

$ErrorActionPreference = "Stop"

if (-not $HDTInstallDir) {
    $root = Join-Path $env:LOCALAPPDATA "HearthstoneDeckTracker"
    $HDTInstallDir = Get-ChildItem $root -Directory -Filter "app-*" |
        Sort-Object { [version]($_.Name -replace "^app-", "") } -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

if (-not $HDTInstallDir -or -not (Test-Path (Join-Path $HDTInstallDir "HearthstoneDeckTracker.exe"))) {
    throw "HDTInstallDir was not found. Pass -HDTInstallDir 'C:\Path\To\HearthstoneDeckTracker\app-x.y.z'."
}

$targetingPack = Join-Path ${env:ProgramFiles(x86)} "Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2"
if (Test-Path $targetingPack) {
    $msbuild = Get-ChildItem "C:\Program Files (x86)\Microsoft Visual Studio" -Recurse -Filter MSBuild.exe |
        Where-Object { $_.FullName -like "*\MSBuild\Current\Bin\MSBuild.exe" } |
        Select-Object -First 1 -ExpandProperty FullName

    if (-not $msbuild) {
        $msbuild = "msbuild"
    }

    & $msbuild ".\HdtCollectionExporter.sln" `
        /m `
        /p:Configuration=$Configuration `
        /p:Platform=x86 `
        /p:HDTInstallDir="$HDTInstallDir"

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
    exit 0
}

Write-Host ".NET Framework 4.7.2 targeting pack was not found. Using local fallback compiler path."

$projectDir = Join-Path $PSScriptRoot "src\HdtCollectionExporter"
$objDir = Join-Path $projectDir "obj\x86\$Configuration"
$outDir = Join-Path $projectDir "bin\x86\$Configuration"
New-Item -ItemType Directory -Force -Path $objDir, $outDir | Out-Null

$legacyMsbuild = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
& $legacyMsbuild (Join-Path $projectDir "HdtCollectionExporter.csproj") `
    /p:Configuration=$Configuration `
    /p:Platform=x86 `
    /p:TargetFrameworkVersion=v4.0 `
    /p:HDTInstallDir="$HDTInstallDir" *> $null

$generatedCode = Join-Path $objDir "UI\ExportWindow.g.cs"
$generatedResources = Join-Path $objDir "HdtCollectionExporter.g.resources"
if (-not (Test-Path $generatedCode) -or -not (Test-Path $generatedResources)) {
    throw "WPF generated files were not created. Install the .NET Framework 4.7.2 Developer Pack and rerun build.ps1."
}

$csc = Get-ChildItem "C:\Program Files (x86)\Microsoft Visual Studio" -Recurse -Filter csc.exe |
    Where-Object { $_.FullName -like "*\Roslyn\csc.exe" } |
    Select-Object -First 1 -ExpandProperty FullName

if (-not $csc) {
    throw "Roslyn csc.exe was not found. Install Visual Studio Build Tools or the .NET Framework 4.7.2 Developer Pack."
}

$frameworkDir = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319"
$presentationFramework = Get-ChildItem (Join-Path $env:WINDIR "Microsoft.NET\assembly\GAC_MSIL\PresentationFramework") -Recurse -Filter PresentationFramework.dll | Select-Object -First 1 -ExpandProperty FullName
$presentationCore = Get-ChildItem (Join-Path $env:WINDIR "Microsoft.NET\assembly\GAC_32\PresentationCore") -Recurse -Filter PresentationCore.dll | Select-Object -First 1 -ExpandProperty FullName
$windowsBase = Get-ChildItem (Join-Path $env:WINDIR "Microsoft.NET\assembly\GAC_MSIL\WindowsBase") -Recurse -Filter WindowsBase.dll | Select-Object -First 1 -ExpandProperty FullName
$microsoftCSharp = Get-ChildItem (Join-Path $env:WINDIR "Microsoft.NET\assembly\GAC_MSIL\Microsoft.CSharp") -Recurse -Filter Microsoft.CSharp.dll | Select-Object -First 1 -ExpandProperty FullName

$refs = @(
    (Join-Path $HDTInstallDir "HearthstoneDeckTracker.exe"),
    (Join-Path $HDTInstallDir "HearthDb.dll"),
    (Join-Path $HDTInstallDir "Newtonsoft.Json.dll"),
    (Join-Path $frameworkDir "mscorlib.dll"),
    (Join-Path $frameworkDir "System.dll"),
    (Join-Path $frameworkDir "System.Core.dll"),
    (Join-Path $frameworkDir "System.Data.dll"),
    (Join-Path $frameworkDir "System.Xml.dll"),
    (Join-Path $frameworkDir "System.Xml.Linq.dll"),
    (Join-Path $frameworkDir "System.Xaml.dll"),
    (Join-Path $frameworkDir "System.Windows.Forms.dll"),
    (Join-Path $frameworkDir "netstandard.dll"),
    $presentationFramework,
    $presentationCore,
    $windowsBase,
    $microsoftCSharp
) | ForEach-Object { "/reference:$_" }

$sources = @(
    "HdtCollectionExporterPlugin.cs",
    "Models\CollectionCardRecord.cs",
    "Models\CollectionDeltaExportDocument.cs",
    "Models\CollectionExportDocument.cs",
    "Models\CollectionSnapshot.cs",
    "Models\ExportFormat.cs",
    "Models\ExportOptions.cs",
    "Models\ExportResult.cs",
    "Properties\AssemblyInfo.cs",
    "Services\CollectionExportService.cs",
    "Services\CollectionUnavailableException.cs",
    "Services\HdtCollectionProvider.cs",
    "Services\ICollectionProvider.cs",
    "Settings\PluginSettings.cs",
    "UI\ExportWindowText.cs",
    "UI\ExportWindow.xaml.cs",
    "obj\x86\$Configuration\UI\ExportWindow.g.cs"
)

Push-Location $projectDir
try {
    & $csc `
        /noconfig `
        /target:library `
        /platform:x86 `
        /langversion:latest `
        /optimize+ `
        "/out:$outDir\HdtCollectionExporter.dll" `
        "/resource:$generatedResources" `
        @refs `
        @sources

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}

Write-Host "Built $outDir\HdtCollectionExporter.dll"
