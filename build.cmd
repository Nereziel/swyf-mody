@echo off
setlocal

set "SCRIPT=%~dp0build-package.ps1"

where pwsh >nul 2>nul
if %errorlevel%==0 (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
) else (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
)

exit /b %errorlevel%
