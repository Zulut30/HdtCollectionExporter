param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Release',
    [string]$HDTInstallDir,
    [string]$ReferenceRoot,
    [switch]$PinnedDependencies
)
$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
if($PinnedDependencies) {
    & (Join-Path $repo 'scripts\get-build-dependencies.ps1')
    $HDTInstallDir = Join-Path $repo 'artifacts\dependencies\hdt\Hearthstone Deck Tracker'
    $ReferenceRoot = Join-Path $repo 'artifacts\dependencies\net472\build'
}
if(-not $HDTInstallDir) {
    $running = Get-Process HearthstoneDeckTracker -ErrorAction SilentlyContinue | Select-Object -First 1
    if($running -and $running.Path) { $HDTInstallDir = Split-Path $running.Path }
    else {
        $searchRoots = @((Join-Path $env:LOCALAPPDATA 'HearthstoneDeckTracker'), 'D:\Apps\HearthstoneDeckTracker')
        $HDTInstallDir = $searchRoots | Where-Object { Test-Path -LiteralPath $_ } |
            ForEach-Object { Get-ChildItem -LiteralPath $_ -Directory -Filter 'app-*' } |
            Sort-Object { [version]($_.Name -replace '^app-', '') } -Descending |
            Select-Object -First 1 -ExpandProperty FullName
    }
}
$hdtExe = @('HearthstoneDeckTracker.exe', 'Hearthstone Deck Tracker.exe') |
    ForEach-Object { if($HDTInstallDir) { Join-Path $HDTInstallDir $_ } } |
    Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if(-not $hdtExe) { throw 'HDT was not found. Use -HDTInstallDir or -PinnedDependencies.' }
foreach($dependency in @('HearthDb.dll','Newtonsoft.Json.dll')) {
    if(-not (Test-Path -LiteralPath (Join-Path $HDTInstallDir $dependency))) { throw "Missing HDT dependency: $dependency" }
}
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = if(Test-Path -LiteralPath $vswhere) { & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1 }
if(-not $msbuild) { $msbuild = (Get-Command msbuild.exe -ErrorAction SilentlyContinue).Source }
if(-not $msbuild) { throw 'Install Visual Studio Build Tools with the .NET desktop workload. The unsafe legacy compiler fallback has been retired.' }
if(-not $ReferenceRoot -and -not (Test-Path -LiteralPath (Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2'))) {
    & (Join-Path $repo 'scripts\get-build-dependencies.ps1') -ReferenceOnly
    $ReferenceRoot = Join-Path $repo 'artifacts\dependencies\net472\build'
}
$arguments = @((Join-Path $repo 'HdtCollectionExporter.sln'), '/m', '/t:Rebuild', '/nologo', '/verbosity:minimal',
    "/p:Configuration=$Configuration", '/p:Platform=x64', "/p:HDTInstallDir=$HDTInstallDir", "/p:HDTExecutable=$hdtExe")
if($ReferenceRoot) { $arguments += "/p:TargetFrameworkRootPath=$ReferenceRoot\" }
& $msbuild @arguments
if($LASTEXITCODE -ne 0) { throw "MSBuild failed ($LASTEXITCODE)." }
Write-Host "Built x64 $Configuration against $hdtExe"
