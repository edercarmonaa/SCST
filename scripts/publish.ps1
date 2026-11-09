param([string]$CertificateThumbprint)
. "$PSScriptRoot\common.ps1"
try {
    $output=Join-Path $TaskRoot 'artifacts\publish'
    dotnet publish (Join-Path $TaskRoot 'src\SCST.Web') -c Release -r win-x64 --self-contained true -o $output -p:PublishSingleFile=false
    if($LASTEXITCODE -ne 0){throw 'La publicación .NET no terminó correctamente.'}
    Copy-Item -LiteralPath (Join-Path $TaskRoot 'scripts'),(Join-Path $TaskRoot 'docs'),(Join-Path $TaskRoot 'README.md') -Destination (Split-Path $output) -Recurse -Force
    $manifest=Get-ChildItem -LiteralPath $output -File -Recurse | ForEach-Object {[pscustomobject]@{Path=$_.FullName.Substring($output.Length+1);Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}}
    $manifestFile=Join-Path (Split-Path $output) 'manifest.json';$manifest | ConvertTo-Json | Set-Content -LiteralPath $manifestFile -Encoding UTF8
    if($CertificateThumbprint){
        Add-Type -AssemblyName System.Security
        $cert=Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificateThumbprint"
        $content=New-Object Security.Cryptography.Pkcs.ContentInfo(,[IO.File]::ReadAllBytes($manifestFile));$cms=New-Object Security.Cryptography.Pkcs.SignedCms($content,$true);$signer=New-Object Security.Cryptography.Pkcs.CmsSigner($cert);$cms.ComputeSignature($signer);[IO.File]::WriteAllBytes("$manifestFile.p7s",$cms.Encode())
    }
    Write-Host 'Publicación autocontenida generada en artifacts\publish. Para distribuir actualizaciones, firme el manifiesto con un certificado autorizado.';exit 0
} catch {Write-Error "Publicación fallida: $($_.Exception.Message)";exit 1}
