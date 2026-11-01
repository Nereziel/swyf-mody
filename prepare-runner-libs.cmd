@echo off
setlocal EnableExtensions

set "GAME=D:\Hry\Steam\steamapps\common\Scam With Your Friends"
set "OUT=%~dp0runner-libs"
set "MANAGED=%GAME%\Scam With Your Friends_Data\Managed"
set "BEPINEX=%GAME%\BepInEx\core"
set "OUT_MANAGED=%OUT%\Scam With Your Friends_Data\Managed"
set "OUT_BEPINEX=%OUT%\BepInEx\core"
set /a ERRORS=0

echo.
echo ==========================================
echo SWYF - priprava DLL pro GitHub runner
echo ==========================================
echo.
echo Zdroj:
echo   %GAME%
echo.
echo Cil:
echo   %OUT%
echo.

if not exist "%MANAGED%" (
    echo CHYBA: Managed adresar neexistuje:
    echo   %MANAGED%
    exit /b 1
)

if not exist "%BEPINEX%" (
    echo CHYBA: BepInEx core adresar neexistuje:
    echo   %BEPINEX%
    exit /b 1
)

if exist "%OUT%" rmdir /s /q "%OUT%"
mkdir "%OUT_MANAGED%" >nul 2>nul
mkdir "%OUT_BEPINEX%" >nul 2>nul

call :copydll "%MANAGED%" "%OUT_MANAGED%" "ScriptsAssDef.dll"
call :copydll "%MANAGED%" "%OUT_MANAGED%" "UnityEngine.dll"
call :copydll "%MANAGED%" "%OUT_MANAGED%" "UnityEngine.CoreModule.dll"
call :copydll "%MANAGED%" "%OUT_MANAGED%" "Newtonsoft.Json.dll"
call :copydll "%MANAGED%" "%OUT_MANAGED%" "MetaVoiceChat.Core.dll"
call :copydll "%MANAGED%" "%OUT_MANAGED%" "Kolkata.Speech.dll"
call :copydll "%MANAGED%" "%OUT_MANAGED%" "com.whisper.unity.dll"
call :copydll "%MANAGED%" "%OUT_MANAGED%" "Unity.Localization.dll"

call :copydll "%BEPINEX%" "%OUT_BEPINEX%" "BepInEx.dll"
call :copydll "%BEPINEX%" "%OUT_BEPINEX%" "0Harmony.dll"

echo.
if not "%ERRORS%"=="0" (
    echo ==========================================
    echo CHYBA: chybi %ERRORS% DLL.
    echo ==========================================
    exit /b 1
)

echo ==========================================
echo Hotovo. DLL jsou v:
echo   %OUT%
echo ==========================================
exit /b 0

:copydll
set "SRC=%~1\%~3"
set "DST=%~2\%~3"

if not exist "%SRC%" (
    echo [CHYBI] %~3
    set /a ERRORS+=1
    exit /b 0
)

copy /Y "%SRC%" "%DST%" >nul
if errorlevel 1 (
    echo [CHYBA] %~3
    set /a ERRORS+=1
) else (
    echo [OK]    %~3
)
exit /b 0
