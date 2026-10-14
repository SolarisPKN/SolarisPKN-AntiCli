param(
    [switch]$BuildOnly,
    [switch]$RunAfterBuild
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'src\SolarisPKN.AntiCli\SolarisPKN.AntiCli.csproj'
$results = Join-Path $root 'artifacts\build'
$publish = Join-Path $root 'artifacts\release\win-x64'
New-Item -ItemType Directory -Path $results -Force | Out-Null
$log = Join-Path $results 'ultima-compilacion.log'

if (!(Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "No existe el proyecto: $project"
}
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Falta dotnet en PATH. Instalá un SDK de .NET 8 o superior.'
}

"SolarisPKN-AntiCli | $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz') | Windows $env:COMPUTERNAME" |
    Set-Content -LiteralPath $log -Encoding UTF8

function Invoke-DotNet([string]$stage, [string[]]$arguments) {
    Write-Host ''
    Write-Host ('[' + $stage + '] dotnet ' + ($arguments -join ' ')) -ForegroundColor Cyan
    & dotnet @arguments 2>&1 | ForEach-Object {
        $line = [string]$_
        Write-Host $line
        Add-Content -LiteralPath $log -Value $line -Encoding UTF8
    }
    $status = $LASTEXITCODE
    if ($status -ne 0) {
        throw ('Falló ' + $stage + ' con salida ' + $status + ". Revisá: " + $log)
    }
}

Push-Location $root
try {
    Invoke-DotNet 'SDK' @('--version')
    Invoke-DotNet 'RESTAURAR' @('restore', $project, '--nologo')
    Invoke-DotNet 'COMPILAR' @('build', $project, '--configuration', 'Release',
        '--no-restore', '--no-incremental', '--nologo', "-bl:$(Join-Path $results 'anticli.binlog')")

    if (!$BuildOnly) {
        # No trimming: Avalonia and reflection-based XAML need their runtime types.
        # Self-contained win-x64: no .NET 8 runtime required on the destination.
        Invoke-DotNet 'PUBLICAR EXE' @('publish', $project, '--configuration', 'Release',
            '--runtime', 'win-x64', '--self-contained', 'true', '--nologo',
            '--output', $publish,
            '-p:PublishSingleFile=true',
            '-p:IncludeNativeLibrariesForSelfExtract=true',
            '-p:PublishTrimmed=false',
            '-p:PublishReadyToRun=false')
    }

    Write-Host ''
    Write-Host 'COMPILACION CORRECTA' -ForegroundColor Green
    Write-Host ("Registro: " + $log)
    if (!$BuildOnly) {
        $exe = Join-Path $publish 'SolarisPKN.AntiCli.exe'
        if (!(Test-Path -LiteralPath $exe -PathType Leaf)) {
            throw "dotnet publish finalizó pero no apareció el ejecutable: $exe"
        }
        Write-Host ("Ejecutable: " + $exe) -ForegroundColor Green
        Write-Host ("Tamaño bytes: " + (Get-Item $exe).Length)
        if ($RunAfterBuild) {
            Start-Process -FilePath $exe -WorkingDirectory $publish
        }
    }
} finally {
    Pop-Location
}
