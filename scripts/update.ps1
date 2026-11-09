param([Parameter(Mandatory=$true)][string]$PackageDirectory,[Parameter(Mandatory=$true)][string]$TrustedSignerThumbprint)
. "$PSScriptRoot\common.ps1"
try {
    Test-TaskExecutable
    Add-Type -AssemblyName System.Security
    $package=(Resolve-Path -LiteralPath $PackageDirectory).Path
    $manifestFile=Join-Path $package 'manifest.json'
    $content=New-Object Security.Cryptography.Pkcs.ContentInfo(,[IO.File]::ReadAllBytes($manifestFile))
    $cms=New-Object Security.Cryptography.Pkcs.SignedCms($content,$true)
    $cms.Decode([IO.File]::ReadAllBytes("$manifestFile.p7s"));$cms.CheckSignature($true)
    if($cms.SignerInfos.Count-ne 1 -or $cms.SignerInfos[0].Certificate.Thumbprint-ne $TrustedSignerThumbprint){throw 'La firma no corresponde al certificado autorizado.'}
    $source=Join-Path $package 'publish';$source=[IO.Path]::GetFullPath($source)
    $manifest=Get-Content -LiteralPath $manifestFile -Raw -Encoding UTF8 | ConvertFrom-Json
    foreach($entry in $manifest){$file=[IO.Path]::GetFullPath((Join-Path $source $entry.Path));if(-not $file.StartsWith($source+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'El manifiesto contiene una ruta fuera del paquete.'};if((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash-ne $entry.Sha256){throw "Integridad incorrecta: $($entry.Path)"}}
    $config=Get-Content -LiteralPath (Join-Path $source 'SCST.Web.runtimeconfig.json') -Raw | ConvertFrom-Json
    if($config.runtimeOptions.tfm-ne 'net9.0'){throw 'El paquete no es compatible con .NET 9.'}
    Stop-TaskServer
    & $TaskExe '--maintenance=backup';if($LASTEXITCODE-ne 0){throw 'No se generó el respaldo previo. Actualización cancelada.'}
    $target=Split-Path $TaskExe;$previous=Join-Path (Split-Path $target) ('Previous_'+[DateTime]::Now.ToString('yyyyMMdd_HHmmss'))
    New-Item -ItemType Directory -Path $previous|Out-Null
    Copy-Item -Path (Join-Path $target '*') -Destination $previous -Recurse
    foreach($entry in $manifest){$destination=[IO.Path]::GetFullPath((Join-Path $target $entry.Path));if(-not $destination.StartsWith($target+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Ruta de destino inválida.'};New-Item -ItemType Directory -Force (Split-Path $destination)|Out-Null;Copy-Item -LiteralPath (Join-Path $source $entry.Path) -Destination $destination -Force}
    & $TaskExe '--maintenance=migrate'
    if($LASTEXITCODE-ne 0){Copy-Item -Path (Join-Path $previous '*') -Destination $target -Recurse -Force;throw 'Falló la migración. Se recuperaron los binarios anteriores. Use restore.ps1 con el respaldo previo antes de iniciar.'}
    Write-Host 'Actualización aplicada. Los datos se conservaron. Inicie SCST y verifique su operación.';exit 0
} catch {Write-Error "Actualización cancelada: $($_.Exception.Message)";exit 1}
