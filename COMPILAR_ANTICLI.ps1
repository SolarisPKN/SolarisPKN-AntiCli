param(
    [switch]$BuildOnly,
    [switch]$RunAfterBuild,
    [string]$DotNetExe = ''
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

"SolarisPKN-AntiCli | $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz') | Windows $env:COMPUTERNAME" |
    Set-Content -LiteralPath $log -Encoding UTF8

# dotnet.exe may exist on PATH even when no .NET SDK is installed.
# Prefer a user-local SDK when available, without changing system PATH or requesting UAC.
$candidates = @()
if ($DotNetExe) { $candidates += $DotNetExe }
if ($env:LOCALAPPDATA) {
    $candidates += (Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe')
}
if ($env:USERPROFILE) {
    $candidates += (Join-Path $env:USERPROFILE '.dotnet\dotnet.exe')
}
if ($env:ProgramFiles) {
    $candidates += (Join-Path $env:ProgramFiles 'dotnet\dotnet.exe')
}
$cmd = Get-Command dotnet.exe -ErrorAction SilentlyContinue
if ($cmd) { $candidates += $cmd.Source }

$script:SelectedDotNet = $null
$diagnostics = @()
foreach ($candidate in ($candidates | Where-Object { $_ } | Select-Object -Unique)) {
    if (!(Test-Path -LiteralPath $candidate -PathType Leaf)) { continue }
    $savedPreference = $ErrorActionPreference
    try {
        # PowerShell 5.1 normally turns native stderr into ErrorRecord.
        # Do not let the first diagnostic line interrupt error collection.
        $ErrorActionPreference = 'Continue'
        $lines = @(& $candidate --list-sdks 2>&1 | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $savedPreference
    }
    $diagnostics += ("[DOTNET] " + $candidate + " (exit " + $exitCode + ")")
    $diagnostics += $lines
    $validSdk = $false
    if ($exitCode -eq 0) {
        foreach ($line in $lines) {
            if ($line -match '^\s*(\d+)\.\d+\.\d+(-[^\s]+)?\s+\[' -and [int]$Matches[1] -ge 8) {
                $validSdk = $true
                break
            }
        }
    }
    if ($validSdk) {
        $script:SelectedDotNet = $candidate
        break
    }
}

foreach ($line in $diagnostics) {
    Write-Host $line
    Add-Content -LiteralPath $log -Value $line -Encoding UTF8
}
if (-not $script:SelectedDotNet) {
    $hint = 'No se encontro un SDK de .NET 8 o superior que funcione. Un dotnet.exe existente no garantiza tener SDK. ' +
        'Si EXO lo autoriza, usa un SDK portable en tu carpeta de usuario o compila con GitHub Actions. ' +
        'Revisa el diagnostico completo en ' + $log
    throw $hint
}
Write-Host ("SDK seleccionado: " + $script:SelectedDotNet) -ForegroundColor Green

function Invoke-DotNet([string]$stage, [string[]]$arguments) {
    Write-Host ''
    Write-Host ('[' + $stage + '] ' + $script:SelectedDotNet + ' ' + ($arguments -join ' ')) -ForegroundColor Cyan

    $savedPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $lines = @(& $script:SelectedDotNet @arguments 2>&1 | ForEach-Object { [string]$_ })
        $status = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $savedPreference
    }
    foreach ($line in $lines) {
        Write-Host $line
        Add-Content -LiteralPath $log -Value $line -Encoding UTF8
    }
    if ($status -ne 0) {
        throw ('Fallo ' + $stage + ' (codigo ' + $status + '). Diagnostico: ' + $log)
    }
}

Push-Location $root
try {
    Invoke-DotNet 'SDK' @('--version')
    Invoke-DotNet 'RESTAURAR' @('restore', $project, '--nologo')
    Invoke-DotNet 'COMPILAR' @('build', $project, '--configuration', 'Release',
        '--no-restore', '--no-incremental', '--nologo', "-bl:$(Join-Path $results 'anticli.binlog')")

    if (!$BuildOnly) {
        # Avalonia reflection/XAML: publish without trimming.
        # Self-contained win-x64 means the destination does not require the .NET runtime.
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
            throw "dotnet publish termino pero no aparecio: $exe"
        }
        Write-Host ("Ejecutable: " + $exe) -ForegroundColor Green
        Write-Host ("Tamano bytes: " + (Get-Item $exe).Length)
        if ($RunAfterBuild) {
            Start-Process -FilePath $exe -WorkingDirectory $publish
        }
    }
} finally {
    Pop-Location
}
