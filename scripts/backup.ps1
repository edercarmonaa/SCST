. "$PSScriptRoot\common.ps1"
try {Test-TaskExecutable;Stop-TaskServer;& $TaskExe '--maintenance=backup';if($LASTEXITCODE -ne 0){throw 'No fue posible generar el respaldo.'};exit 0} catch {Write-Error $_.Exception.Message;exit 1}
