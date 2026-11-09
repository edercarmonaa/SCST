param([string]$Destination=(Join-Path (Split-Path $PSScriptRoot -Parent) '.local\public-review'))
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $root
try {
    $gitRoot=git rev-parse --show-toplevel
    if($LASTEXITCODE-ne 0 -or [IO.Path]::GetFullPath($gitRoot)-ne $root){throw 'El repositorio Git debe corresponder a la carpeta de estos fuentes.'}
    $target=[IO.Path]::GetFullPath($Destination)
    if($target-eq $root -or $target.StartsWith((Join-Path $root '.git')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Seleccione una carpeta nueva distinta del repositorio y de .git.'}
    if(Test-Path -LiteralPath $target){throw 'El destino ya existe. Use una carpeta nueva para conservar cualquier revisión anterior.'}
    $files=@(git ls-files --cached --others --exclude-standard | Sort-Object -Unique)
    if($LASTEXITCODE-ne 0){throw 'No se pudo leer la lista de fuentes Git.'}
    New-Item -ItemType Directory -Path $target|Out-Null
    $count=0
    foreach($path in $files){
        if(-not(Test-Path -LiteralPath $path -PathType Leaf)){continue}
        if($path-ne '.env.example' -and $path-match '(^|/)(\.git|\.local|artifacts|bin|obj|SCSTData)(/|$)|(^|/)(\.env($|\.)|credentials\.json$|service-account[^/]*\.json$|secrets\.(json|ya?ml)$|auth\.json$|key-[^/]*\.xml$)|\.(db|sqlite3?|scst|key|pem|pfx|p12|exe|dll|pdb|zip|xlsx|docx|pdf|dump|bak|backup)$'){throw "Archivo privado o regenerable dentro de la lista de fuentes: $path"}
        $source=Get-Item -LiteralPath $path
        if($source.Attributes-band [IO.FileAttributes]::ReparsePoint){throw 'No se exportan enlaces del sistema de archivos.'}
        $destinationFile=[IO.Path]::GetFullPath((Join-Path $target $path))
        if(-not $destinationFile.StartsWith($target+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Ruta fuera del destino de revisión.'}
        New-Item -ItemType Directory -Force -Path (Split-Path $destinationFile)|Out-Null
        Copy-Item -LiteralPath $source.FullName -Destination $destinationFile
        $count++
    }
    Write-Output "Copia preparada: $count archivos en $target"
    Write-Output 'No contiene .git ni historial. No se realizó commit, push, release o publicación.'
    Write-Output 'Revise el contenido y la licencia antes de inicializar y confirmar manualmente el nuevo repositorio.'
} catch {Write-Error $_.Exception.Message;exit 1} finally {Pop-Location}
