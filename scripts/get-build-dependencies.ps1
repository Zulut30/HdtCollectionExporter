param([switch]$ReferenceOnly)
$ErrorActionPreference = 'Stop'
$root = Join-Path (Split-Path $PSScriptRoot) 'artifacts\dependencies'
New-Item -ItemType Directory -Force -Path $root | Out-Null
if(-not $ReferenceOnly) {
    $archive = Join-Path $root 'hdt-v1.55.6.zip'
    if(-not (Test-Path -LiteralPath $archive)) {
        Invoke-WebRequest 'https://github.com/HearthSim/Hearthstone-Deck-Tracker/releases/download/v1.55.6/Hearthstone.Deck.Tracker-v1.55.6.zip' -OutFile $archive
    }
    $expected = '3EBF12FA3D5E842F1FA8CE433758640E3CE2903A400DAC5FB096822149D2BB60'
    if((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expected) { throw 'Pinned HDT archive checksum mismatch.' }
    if(-not (Test-Path -LiteralPath (Join-Path $root 'hdt\Hearthstone Deck Tracker\Hearthstone Deck Tracker.exe'))) {
        Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $root 'hdt') -Force
    }
}
$reference = Join-Path $root 'net472\build\.NETFramework\v4.7.2\mscorlib.dll'
if(-not (Test-Path -LiteralPath $reference)) {
    $package = Join-Path $root 'net472.zip'
    Invoke-WebRequest 'https://api.nuget.org/v3-flatcontainer/microsoft.netframework.referenceassemblies.net472/1.0.3/microsoft.netframework.referenceassemblies.net472.1.0.3.nupkg' -OutFile $package
    Expand-Archive -LiteralPath $package -DestinationPath (Join-Path $root 'net472') -Force
}
