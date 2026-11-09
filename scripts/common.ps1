$ErrorActionPreference = 'Stop'
$TaskRoot = Split-Path $PSScriptRoot -Parent
if (-not $env:SCST_DATA) { $env:SCST_DATA = Join-Path $env:LOCALAPPDATA 'SCSTData' }
$TaskData = [IO.Path]::GetFullPath($env:SCST_DATA)
$TaskPort=5186
if($env:SCST_PORT -and (-not [int]::TryParse($env:SCST_PORT,[ref]$TaskPort) -or $TaskPort-lt 1 -or $TaskPort-gt 65535)){throw 'SCST_PORT debe ser un puerto válido entre 1 y 65535.'}
$TaskUrl="http://127.0.0.1:$TaskPort"
$TaskExe = Join-Path $TaskRoot 'app\SCST.Web.exe'
if (-not (Test-Path -LiteralPath $TaskExe)) { $TaskExe = Join-Path $TaskRoot 'artifacts\publish\SCST.Web.exe' }
if (-not (Test-Path -LiteralPath $TaskExe)) { $TaskExe = Join-Path $TaskRoot 'publish\SCST.Web.exe' }
function Test-TaskExecutable { if (-not (Test-Path -LiteralPath $TaskExe)) { throw 'No se encontró la aplicación publicada. Ejecute publish.ps1 primero.' } }
function Get-TaskEventName {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $hash = [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($TaskData.ToUpperInvariant()))).Replace('-','') } finally { $sha.Dispose() }
    return "Local\SCST_$hash"
}
function Stop-TaskServer {
    $eventHandle = $null
    try { $eventHandle = [Threading.EventWaitHandle]::OpenExisting((Get-TaskEventName)); [void]$eventHandle.Set() } catch [Threading.WaitHandleCannotBeOpenedException] { return } finally { if ($eventHandle) { $eventHandle.Dispose() } }
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    do { Start-Sleep -Milliseconds 300; try { $lock = [IO.File]::Open((Join-Path $TaskData 'instance.lock'), 'OpenOrCreate', 'ReadWrite', 'None'); $lock.Dispose(); return } catch [IO.IOException] { } } while ([DateTime]::UtcNow -lt $deadline)
    throw 'El servidor no se detuvo. Espere antes de restaurar o actualizar.'
}
