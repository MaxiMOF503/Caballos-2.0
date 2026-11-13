$ErrorActionPreference='Stop'
$root='C:\Caballos\Builds\Caballos-2.0-Beta-v0.2'
$zip='C:\Caballos\Builds\Caballos-2.0-Beta-v0.2-Windows.zip'
$reportRoot='C:\Caballos\Validation\BetaV02'
foreach($required in @('Caballos-2.0.exe','Caballos-2.0_Data','UnityPlayer.dll')) {
    if(!(Test-Path -LiteralPath (Join-Path $root $required))) { throw "Missing required dependency: $required" }
}
if(!(Test-Path -LiteralPath (Join-Path $reportRoot 'release-build-report.txt'))) { throw 'Missing successful build report' }
$report=[IO.File]::ReadAllText((Join-Path $reportRoot 'release-build-report.txt'))
if($report -notmatch 'Result: Succeeded' -or $report -notmatch 'Errors: 0') { throw 'Build was not successful' }
if(Test-Path -LiteralPath $zip) { throw 'Existing ZIP preserved; choose a new output explicitly' }
# Preserve non-shipping build symbols outside the distribution, without deleting them.
foreach($directory in @(Get-ChildItem -LiteralPath $root -Directory | Where-Object Name -like '*DoNotShip*')) {
    $source=[IO.Path]::GetFullPath($directory.FullName)
    $dest=[IO.Path]::GetFullPath((Join-Path $reportRoot $directory.Name))
    if(!$source.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase) -or
       !$dest.StartsWith($reportRoot+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected symbol path' }
    if(Test-Path -LiteralPath $dest) { throw 'Symbol backup exists' }
    Move-Item -LiteralPath $source -Destination $dest
}
Copy-Item -LiteralPath (Join-Path $reportRoot 'LEEME.txt') -Destination (Join-Path $root 'LEEME.txt')
$exe=[IO.File]::ReadAllBytes((Join-Path $root 'Caballos-2.0.exe'))
$pe=[BitConverter]::ToInt32($exe,0x3c)
if([BitConverter]::ToUInt16($exe,$pe+4) -ne 0x8664) { throw 'Executable is not AMD64' }
$assembly=Join-Path $root 'Caballos-2.0_Data\Managed\Assembly-CSharp.dll'
if([Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($assembly)).Contains('BetaV02RuntimeProbe')) { throw 'Diagnostic code in release' }
$all=@(Get-ChildItem -LiteralPath $root -File -Recurse)
$bad=@($all | Where-Object { $_.FullName -match '\\(Library|Temp|Logs|Validation|Assets|ProjectSettings|Packages)\\|gameData\.json$|\.log$|\.cs$|\.pdb$' })
if($bad.Count) { $bad.FullName; throw 'Unexpected development files in distribution' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($root,$zip,[IO.Compression.CompressionLevel]::Optimal,$true)
$extracted=Join-Path $reportRoot 'ExtractedZip'
if(Test-Path -LiteralPath $extracted) { throw 'Extraction verification directory already exists' }
[IO.Compression.ZipFile]::ExtractToDirectory($zip,$extracted)
$copyRoot=Join-Path $extracted 'Caballos-2.0-Beta-v0.2'
foreach($file in $all) {
    $relative=[IO.Path]::GetRelativePath($root,$file.FullName)
    $copy=Join-Path $copyRoot $relative
    if(!(Test-Path -LiteralPath $copy) -or (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $copy).Hash) {
        throw "ZIP mismatch: $relative"
    }
}
if(@(Get-ChildItem -LiteralPath $copyRoot -File -Recurse).Count -ne $all.Count) { throw 'ZIP file count mismatch' }
$size=(Get-Item -LiteralPath $zip).Length
$hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
"ZIP PASS: $($all.Count) files, $size bytes; extracted files match SHA256.`nZIP SHA256: $hash`n$zip" |
    Tee-Object -FilePath (Join-Path $reportRoot 'package-results.txt')
