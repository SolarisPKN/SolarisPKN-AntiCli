[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = $PSScriptRoot
$publish = Join-Path $root 'artifacts\release\win-x64'
$out = Join-Path $root 'artifacts\release'
$stage = Join-Path $out 'package-v0.1.0'
$archive = Join-Path $out 'SolarisPKN-AntiCli-v0.1.0-win-x64.zip'
$sumFile = Join-Path $out 'SHA256SUMS.txt'
$exe = Join-Path $publish 'SolarisPKN.AntiCli.exe'

Write-Host 'Compilando la version actual de AntiCLI (Windows x64)...' -ForegroundColor Cyan
& (Join-Path $root 'COMPILAR_ANTICLI.ps1')
if (-not $?) { throw 'La compilacion fallo: NO se empaquetara un EXE anterior.' }
if (!(Test-Path -LiteralPath $exe -PathType Leaf)) {
    throw "No se encontro el ejecutable recien compilado: $exe"
}

New-Item -ItemType Directory -Force -Path $out | Out-Null
if (Test-Path -LiteralPath $stage) {
    Remove-Item -LiteralPath $stage -Force -Recurse
}
New-Item -ItemType Directory -Force -Path $stage | Out-Null

# Never include debug symbols (PDB with local source paths), profiles or settings.
$allowedExtensions = @('.exe', '.dll', '.json', '.config', '.dat')
$skip = @('profiles.json', 'settings.json', 'appsettings.local.json')
Get-ChildItem -LiteralPath $publish -File | ForEach-Object {
    $file = $_
    $low = $file.Name.ToLowerInvariant()
    if ($allowedExtensions -contains $file.Extension.ToLowerInvariant() -and
        $skip -notcontains $low -and
        $low -notmatch 'secret|token|credential|private|password') {
        Copy-Item -LiteralPath $file.FullName -Destination $stage
    }
}

foreach ($name in @('LICENSE', 'README.md', 'README.es.md', 'RELEASE_NOTES.md',
                    'RELEASE_NOTES.es.md', 'THIRD_PARTY_NOTICES.md')) {
    $source = Join-Path $root $name
    if (!(Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Falta un documento legal o de publicacion: $name"
    }
    Copy-Item -LiteralPath $source -Destination $stage
}
Copy-Item -LiteralPath (Join-Path $root 'third_party') -Destination $stage -Recurse
if (!(Test-Path -LiteralPath (Join-Path $stage 'SolarisPKN.AntiCli.exe'))) {
    throw 'El paquete no contiene la aplicacion.'
}

if (Test-Path -LiteralPath $archive) {
    Remove-Item -LiteralPath $archive -Force
}
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $archive -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
("$hash  " + [IO.Path]::GetFileName($archive)) |
    Set-Content -LiteralPath $sumFile -Encoding Ascii

Write-Host ''
Write-Host 'PAQUETE CREADO (sin publicar en GitHub)' -ForegroundColor Green
Write-Host $archive
Write-Host ("SHA256: " + $hash)
Write-Host ''
Write-Host 'Ahora publica el codigo mediante GitHub Desktop.' -ForegroundColor Yellow
Write-Host 'Para crear una GitHub Release y adjuntar este ZIP: usa la web del repositorio.' -ForegroundColor Yellow
