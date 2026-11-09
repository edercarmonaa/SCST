param([Parameter(Mandatory=$true)][string]$Backup)
. "$PSScriptRoot\common.ps1"
try {
    Test-TaskExecutable
    $env:SCST_BACKUP=(Resolve-Path -LiteralPath $Backup).Path
    Write-Host 'Esta operación sustituirá los datos actuales. Se creará un respaldo previo.'
    if((Read-Host 'Escriba RESTAURAR para continuar') -cne 'RESTAURAR'){throw 'Restauración cancelada.'}
    Stop-TaskServer
    & $TaskExe '--maintenance=restore'
    if($LASTEXITCODE -ne 0){throw 'La restauración no terminó. Se conservaron los datos o la copia de recuperación.'}
    exit 0
} catch {Write-Error $_.Exception.Message;exit 1} finally {Remove-Item Env:\SCST_BACKUP -ErrorAction SilentlyContinue}
