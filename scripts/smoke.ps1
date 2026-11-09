param([string]$BaseUrl='http://127.0.0.1:5186')
$ErrorActionPreference='Stop'
$session=New-Object Microsoft.PowerShell.Commands.WebRequestSession
function Get-Page([string]$Path){Invoke-WebRequest -UseBasicParsing -Uri "$BaseUrl$Path" -WebSession $session}
function Get-Token($Html){$m=[regex]::Match($Html,'name="__RequestVerificationToken" type="hidden" value="([^"]+)"');if(-not $m.Success){throw 'Falta token antifalsificación.'};return [Net.WebUtility]::HtmlDecode($m.Groups[1].Value)}
function Post-Page([string]$Path,[hashtable]$Body,$Html){$Body['__RequestVerificationToken']=Get-Token $Html;Invoke-WebRequest -UseBasicParsing -Uri "$BaseUrl$Path" -Method Post -Body $Body -WebSession $session}
try {
    $page=Get-Page '/Setup'
    if($page.Content -notmatch 'Generar clave'){throw 'Esta prueba requiere una base NUEVA y aislada. No se ejecuta sobre sistemas configurados.'}
    $keyPage=Post-Page '/Setup?handler=Key' @{} $page.Content
    $recovery=[Net.WebUtility]::HtmlDecode([regex]::Match($keyPage.Content,'<pre class="recovery">([^<]+)</pre>').Groups[1].Value)
    if(-not $recovery){throw 'No se generó la clave DPAPI.'}
    $password=[Guid]::NewGuid().ToString()+' frase de prueba'
    $completionForm=@([regex]::Matches($keyPage.Content,'(?s)<form(?<attributes>[^>]+)>(?<contents>.*?)</form>')|Where-Object{$_.Groups['contents'].Value-match 'name="Recovery"'})
    if($completionForm.Count-ne 1){throw 'No se encontró el formulario de finalización.'}
    $completionAction=[Net.WebUtility]::HtmlDecode([regex]::Match($completionForm[0].Groups['attributes'].Value,'action="([^"]+)"').Groups[1].Value)
    if($completionAction-notmatch 'handler=Complete'){throw 'El formulario de finalización conserva el manejador de generación de clave.'}
    $page=Post-Page $completionAction @{Name='Administrador de pruebas';Username='smoke.admin';Password=$password;'Input.Dependency'='Institución de prueba';'Input.Unit'='Unidad de prueba';'Input.Area'='Área de prueba';'Input.Year'='2026';Recovery=$recovery;SavedRecovery='true'} $keyPage.Content
    if($page.Content -notmatch 'Iniciar sesi'){throw 'No finalizó la configuración inicial.'}
    $page=Post-Page '/Account/Login' @{Username='smoke.admin';Password=$password} $page.Content
    if($page.Content -notmatch 'Panel de control'){throw 'No inició sesión.'}
    foreach($path in @('/','/Tramites','/Tramites/Edit','/Oficios','/Reportes','/Import','/Trash','/Admin/Catalogs','/Admin/Users','/Admin/Settings','/Admin/Audit','/Admin/Backups','/Account/Profile')){ $p=Get-Page $path;if($p.StatusCode-ne 200){throw "Error en $path"};Write-Output "OK GET $path" }
    $edit=Get-Page '/Tramites/Edit'
    function CatalogId($field){return [regex]::Match($edit.Content,'(?s)name="Input\.'+$field+'".*?<option value="(\d+)"').Groups[1].Value}
    $detail=Post-Page '/Tramites/Edit' @{'Input.Folio'='000001';'Input.Names'='ANA';'Input.FirstSurname'='PÉREZ';'Input.Reception'='2026-01-01';'Input.TypeId'=(CatalogId 'TypeId');'Input.LevelId'=(CatalogId 'LevelId');'Input.StatusId'=(CatalogId 'StatusId')} $edit.Content
    if($detail.Content-notmatch 'Expediente guardado'){[IO.File]::WriteAllText((Join-Path $PSScriptRoot '..\.local\smoke-error.html'),$detail.Content);throw 'No se creó el expediente de prueba.'}
    $id=[regex]::Match($detail.BaseResponse.ResponseUri.AbsolutePath,'/(\d+)$').Groups[1].Value
    $follow=Post-Page "/Tramites/Detail/$id`?handler=Follow" @{'Follow.Date'='2026-01-02';'Follow.Activity'='Seguimiento de prueba'} $detail.Content
    if($follow.Content-notmatch 'Seguimiento guardado'){throw 'No guardó seguimiento.'}
    foreach($path in @('/Tramites?handler=Excel','/Reportes?handler=Excel','/Reportes?handler=Pdf',"/Tramites/Detail/$id`?handler=Pdf")){ $p=Get-Page $path;if($p.RawContentLength-lt 100){throw "Exportación vacía: $path"};Write-Output "OK EXPORT $path" }
    $page=Get-Page '/Admin/Users'
    $consultPassword=[Guid]::NewGuid().ToString()+' consulta temporal'
    $page=Post-Page '/Admin/Users' @{Name='Consulta de prueba';Username='smoke.consulta';Password=$consultPassword;Role='Consulta'} $page.Content
    if($page.Content-notmatch 'Usuario creado'){throw 'No creó usuario de consulta.'}
    $page=Post-Page '/Account/Logout' @{} $page.Content
    $page=Post-Page '/Account/Login' @{Username='smoke.consulta';Password=$consultPassword} $page.Content
    if($page.Content-notmatch 'Mi cuenta'){throw 'No exigió cambio de contraseña temporal.'}
    $newPassword=[Guid]::NewGuid().ToString()+' cambiada'
    $page=Post-Page '/Account/Profile' @{CurrentPassword=$consultPassword;NewPassword=$newPassword} $page.Content
    $page=Post-Page '/Account/Login' @{Username='smoke.consulta';Password=$newPassword} $page.Content
    foreach($path in @('/Admin/Users','/Admin/Backups','/Trash','/Tramites/Edit','/Import')){ $p=Get-Page $path;if($p.Content-notmatch 'Acceso restringido'){throw "Consulta accedió indebidamente: $path"};Write-Output "OK DENIED $path" }
    $p=Get-Page '/Tramites'
    $denied=Post-Page '/Tramites' @{Selected=$id;BulkStatus=(CatalogId 'StatusId');action='bulk'} $p.Content
    if($denied.Content-notmatch 'Acceso restringido'){throw 'La escritura de Consulta no fue rechazada.'}
    Write-Output 'OK permisos de escritura en servidor'
    Write-Output 'Prueba HTTP completada con datos sintéticos.'
}catch{Write-Error $_.Exception.Message;exit 1}
