param([string]$InstallPath=(Join-Path $env:LOCALAPPDATA 'Programs\SCST'),[string]$ShortcutDirectory)
. "$PSScriptRoot\common.ps1"
try {
    $source=Join-Path $TaskRoot 'artifacts\publish'
    if(-not(Test-Path -LiteralPath $source)){ $source=Join-Path $TaskRoot 'publish' }
    if(-not(Test-Path -LiteralPath $source)){ $source=Join-Path $TaskRoot 'app' }
    if(-not(Test-Path -LiteralPath (Join-Path $source 'SCST.Web.exe'))){throw 'Primero genere la publicación autocontenida.'}
    $destination=[IO.Path]::GetFullPath($InstallPath)
    if($TaskData.StartsWith($destination+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Los datos deben quedar fuera de los binarios instalados.'}
    New-Item -ItemType Directory -Force -Path $destination,$TaskData,(Join-Path $destination 'app'),(Join-Path $destination 'scripts') | Out-Null
    Copy-Item -Path (Join-Path $source '*') -Destination (Join-Path $destination 'app') -Recurse -Force
    Copy-Item -Path (Join-Path $PSScriptRoot '*.ps1') -Destination (Join-Path $destination 'scripts') -Force
    $acl=Get-Acl -LiteralPath $TaskData;$acl.SetAccessRuleProtection($true,$false)
    $sid=[Security.Principal.WindowsIdentity]::GetCurrent().User
    $rule=New-Object Security.AccessControl.FileSystemAccessRule($sid,'FullControl','ContainerInherit,ObjectInherit','None','Allow');$acl.AddAccessRule($rule);Set-Acl -LiteralPath $TaskData -AclObject $acl
    $shell=New-Object -ComObject WScript.Shell
    $shortcutPaths=@((Join-Path ([Environment]::GetFolderPath('Desktop')) 'SCST.lnk'),(Join-Path ([Environment]::GetFolderPath('StartMenu')) 'SCST.lnk'))
    if($ShortcutDirectory){New-Item -ItemType Directory -Force -Path $ShortcutDirectory|Out-Null;$shortcutPaths=@((Join-Path ([IO.Path]::GetFullPath($ShortcutDirectory)) 'SCST.lnk'))}
    foreach($shortcutPath in $shortcutPaths){$shortcut=$shell.CreateShortcut($shortcutPath);$shortcut.TargetPath='powershell.exe';$shortcut.Arguments="-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$(Join-Path $destination 'scripts\run.ps1')`"";$shortcut.WorkingDirectory=$destination;$shortcut.Save()}
    Write-Host 'SCST instalado. Abra el acceso directo para configurar la institución y la cuenta administradora.';exit 0
} catch { Write-Error "Instalación fallida: $($_.Exception.Message)";exit 1 }
