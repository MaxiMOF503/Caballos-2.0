param([Parameter(Mandatory)][string]$Method,[Parameter(Mandatory)][string]$Name,[switch]$Graphics,[switch]$Play,[switch]$DisableBurst)
$ErrorActionPreference='Stop'
$project='C:\Caballos\Validation\MainMenu'
$lock=Join-Path $project 'Temp\UnityLockfile'
if(Test-Path -LiteralPath $lock){
    try{$handle=[IO.File]::Open($lock,'Open','ReadWrite','None');$handle.Dispose()}
    catch{throw 'La copia aislada está en uso.'}
}
$log=Join-Path 'C:\Caballos\Validation\BetaV02' ($Name+'.log')
$env:CABALLOS_VALIDATION_SAVE_PATH=Join-Path 'C:\Caballos\Validation\ValidationSave' ([guid]::NewGuid().ToString())
$env:CABALLOS_VALIDATION_PREFS_PREFIX='Caballos.BetaV02.'+[guid]::NewGuid().ToString()+'.'
$arguments=@('-batchmode','-projectPath',$project,'-executeMethod',$Method,'-logFile',$log)
if($DisableBurst){$arguments+='--burst-disable-compilation'}
if(!$Graphics){$arguments+='-nographics'}
if(!$Play){$arguments+='-quit'}
$editor='C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe'
$process=Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output "Unity PID $($process.Id), log $log"
if(!$process.WaitForExit(900000)){throw "Timeout de Unity PID $($process.Id)."}
Get-Content -LiteralPath $log -Tail 9
exit $process.ExitCode
