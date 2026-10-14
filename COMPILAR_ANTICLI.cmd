@echo off
setlocal
echo Compilando SolarisPKN-AntiCli en Release...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0COMPILAR_ANTICLI.ps1" %*
set "status=%errorlevel%"
if not "%status%"=="0" (
  echo.
  echo ERROR: compilacion o publicacion fallida. Ver: artifacts\build\ultima-compilacion.log
  pause
  exit /b %status%
)
echo.
echo AntiCLI compilado. Consulta artifacts\release\win-x64\SolarisPKN.AntiCli.exe
pause
