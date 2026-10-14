@echo off
setlocal
cd /d "%~dp0"
echo Preparando un ZIP de la version 0.1.0 de Solaris AntiCLI.
echo No usa GitHub CLI ni publica automaticamente: solo compila y empaqueta.
echo.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0PREPARAR_RELEASE.ps1"
set "rc=%errorlevel%"
echo.
if not "%rc%"=="0" (
  echo No se genero el paquete. Verifica el error de compilacion mostrado.
) else (
  echo Paquete listo en artifacts\release.
)
pause
exit /b %rc%
