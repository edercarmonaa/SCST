param([switch]$SkipHistory)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    if(-not(Get-Command git -ErrorAction SilentlyContinue)){throw 'Se necesita Git para revisar el índice y el historial.'}
    $gitRoot=git rev-parse --show-toplevel
    if($LASTEXITCODE-ne 0 -or [IO.Path]::GetFullPath($gitRoot)-ne $root){throw 'Inicialice y revise el repositorio de esta carpeta; no se permite utilizar el Git de una carpeta superior.'}
    $files=@(git ls-files --cached --others --exclude-standard | Sort-Object -Unique)
    if($LASTEXITCODE-ne 0){throw 'No se pudo leer el repositorio Git.'}
    $rules=[ordered]@{
        PrivateKey='-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----'
        ProviderToken='(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|AKIA[A-Z0-9]{16}|sk-(?:proj-)?[A-Za-z0-9_-]{24,}|AIza[A-Za-z0-9_-]{30,}|xox[baprs]-[A-Za-z0-9-]{20,}|sk_live_[A-Za-z0-9]{20,})'
        CredentialUrl='\b[a-z]+://[^\s/@:]+:[^\s/@]+@'
        PersonalSeed='Add\(CatalogKind\.Validador,\s*"'
    }
    $script:findings=0
    function Test-Text([string]$Text,[string]$Location) {
        foreach($rule in $rules.Keys){if([regex]::IsMatch($Text,$rules[$rule])){Write-Output "REVISAR [$rule] $Location (valor omitido)";$script:findings++}}
    }
    function Test-PrivatePath([string]$Path) {
        if($Path-eq '.env.example'){return $false}
        return $Path-match '(^|/)(\.env($|\.)|credentials\.json$|service-account[^/]*\.json$|secrets\.(json|ya?ml)$|auth\.json$|id_rsa$|id_ed25519$)|\.(db|sqlite3?|scst|key|pem|pfx|p12|exe|dll|zip|dump|bak|backup)$|(^|/)(bin|obj|SCSTData|\.local|artifacts)/'
    }
    foreach($path in $files){
        if(-not(Test-Path -LiteralPath $path -PathType Leaf)){continue}
        $item=Get-Item -LiteralPath $path
        if(Test-PrivatePath $path){Write-Output "REVISAR [PrivateFile] $path";$script:findings++}
        if($item.Length-gt 100MB){Write-Output "REVISAR [LargeFile] $path";$script:findings++}
        if($item.Extension-notmatch '^\.(png|jpg|jpeg|gif|ico|woff2?|ttf|exe|dll|zip|db|scst)$' -and $item.Length-lt 10MB){Test-Text ([IO.File]::ReadAllText($item.FullName)) "working:$path"}
    }
    Write-Output "Archivos actuales revisados: $($files.Count)"
    if(-not $SkipHistory){
        $objects=@(git rev-list --objects --all | git cat-file --batch-check='%(objecttype) %(objectname) %(objectsize) %(rest)')
        foreach($entry in $objects){
            $parts=$entry-split ' ',4
            if($parts[0]-ne 'blob'){continue}
            $size=[long]$parts[2];$path=$parts[3]
            if(Test-PrivatePath $path){Write-Output "REVISAR [HistoricalPrivateFile] blob:$($parts[1]) path:$path";$script:findings++}
            if($size-gt 100MB){Write-Output "REVISAR [HistoricalLargeFile] blob:$($parts[1]) path:$path";$script:findings++}
            if($path-notmatch '\.(png|jpg|jpeg|gif|ico|woff2?|ttf|exe|dll|zip|db|scst)$' -and $size-lt 10MB){$content=(git cat-file blob $parts[1]) -join "`n";Test-Text $content "blob:$($parts[1]) path:$path"}
        }
        Write-Output "Objetos históricos revisados: $($objects.Count)"
    }
    Write-Output 'No se imprimen valores. Las coincidencias requieren revisión humana; no equivalen a credenciales confirmadas.'
    if($script:findings-gt 0){Write-Output "Hallazgos para revisión: $script:findings";exit 2}
    Write-Output 'Sin coincidencias en las reglas automáticas. Continúe con la revisión manual de privacidad y licencias.';exit 0
} catch {Write-Error $_.Exception.Message;exit 1} finally {Pop-Location}
