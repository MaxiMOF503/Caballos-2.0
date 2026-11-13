$ErrorActionPreference='Stop'
$root='C:\Caballos\Validation\BetaV02'
$exe=Join-Path $root 'ExtractedZip\Caballos-2.0-Beta-v0.2\Caballos-2.0.exe'
$log=Join-Path $root 'release-extracted-smoke.log'
if(!(Test-Path -LiteralPath $exe)){throw 'Falta ejecutable extraído'}
if(Test-Path -LiteralPath $log){throw 'Conservar log de prueba anterior'}
$save=Join-Path $env:USERPROFILE 'AppData\LocalLow\DefaultCompany\Caballos 2.0\gameData.json'
$before=if(Test-Path -LiteralPath $save){(Get-FileHash -LiteralPath $save).Hash}else{'ABSENT'}
$started=Get-Date
$p=Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe) -ArgumentList @('-batchmode','-nographics','-noaudio','-quit','-logFile',$log) -WindowStyle Hidden -PassThru
Write-Output "Ejecutable final extraído, PID $($p.Id), observación headless de hasta 60 segundos."
$natural=$p.WaitForExit(60000)
if($natural){$exitCode=$p.ExitCode;if($exitCode -ne 0){throw "Player terminó con código $exitCode"}}
else{
    # Only the exact headless child launched above; never another user application.
    if($p.StartTime -lt $started.AddSeconds(-2)){throw 'Inicio inesperado de proceso'}
    $p.Kill();$p.WaitForExit();$exitCode=$null
}
$after=if(Test-Path -LiteralPath $save){(Get-FileHash -LiteralPath $save).Hash}else{'ABSENT'}
if($before -ne $after){throw 'El archivo de partida personal cambió'}
$contents=[IO.File]::ReadAllText($log)
if($contents -notmatch 'Initialize engine version: 6000.3.13f1'){throw 'No se observó inicialización de Unity'}
if($contents -match 'NullReferenceException|MissingReferenceException|TypeLoadException|DllNotFoundException|FileNotFoundException|EntryPointNotFoundException|Could not load|Failed to load|Fatal error|Crash!!!'){throw 'Error de arranque: revisar log'}
$text=@"
STARTUP PASS: ejecutable final extraído, inicialización Unity 6000.3.13f1 observada sin excepciones de carga.
Executable: $exe
PID: $($p.Id)
Natural exit: $natural
Natural exit code: $exitCode
Si Natural exit=False, se detuvo sólo el proceso headless propio tras 60 segundos. Esto NO valida salida normal mediante Escape.
Personal gameData.json unchanged: $($before -eq $after)
Gráficos, audio, mouse, teclado físico y otra PC sin Unity: pendientes manuales.
"@
$text | Tee-Object -FilePath (Join-Path $root 'release-smoke-results.txt')
