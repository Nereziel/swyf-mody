@echo off
setlocal

set "SCRIPT=%~dp0build-package.ps1"

if not "%~1"=="" goto run_args

echo SWYF mody - lokalni build
echo.
set /p "GAME_DIR=Zadej cestu ke hre Scam With Your Friends: "
if not defined GAME_DIR (
    echo Cesta ke hre nebyla zadana.
    exit /b 1
)

echo.
echo Dostupne mody: All, Speech, TTS
set /p "MOD=Co sestavit [All]: "
if not defined MOD set "MOD=All"

call :run_ps -GameDir "%GAME_DIR%" -Mod "%MOD%"
set "CODE=%errorlevel%"
echo.
if "%CODE%"=="0" (
    echo Build dokoncen.
    echo V dist\ najdes .swyfmod pro Mod Hub a .zip pro rucni instalaci do Mods.
) else (
    echo Build selhal s kodem %CODE%.
)
pause
exit /b %CODE%

:run_args
call :run_ps %*
exit /b %errorlevel%

:run_ps
where pwsh >nul 2>nul
if %errorlevel%==0 (
    pwsh -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
) else (
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
)
exit /b %errorlevel%
