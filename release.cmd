@echo off
setlocal EnableExtensions EnableDelayedExpansion
pushd "%~dp0"

set "REPO=Nereziel/swyf-mody"
set "DIST=%~dp0dist"
set "VERSION=%~1"
set "GAME_DIR=%~2"

echo SWYF mody - GitHub release
echo.

if not defined VERSION (
    set /p "VERSION=Verze release (napr. 1.3.1): "
)
if not defined VERSION (
    echo Verze nebyla zadana.
    goto :fail
)

if /I "%VERSION:~0,1%"=="v" (
    set "TAG=%VERSION%"
) else (
    set "TAG=v%VERSION%"
)

if not defined GAME_DIR (
    set /p "GAME_DIR=Zadej cestu ke hre Scam With Your Friends: "
)
if not defined GAME_DIR (
    echo Cesta ke hre nebyla zadana.
    goto :fail
)
if not exist "%GAME_DIR%\Scam With Your Friends_Data\Managed\ScriptsAssDef.dll" (
    echo Na zadane ceste nebyly nalezeny herni knihovny.
    goto :fail
)

where gh >nul 2>nul
if errorlevel 1 (
    echo GitHub CLI ^(gh^) neni nainstalovane nebo neni v PATH.
    echo Nainstaluj GitHub CLI a jednou spust: gh auth login
    goto :fail
)

gh auth status >nul 2>&1
if errorlevel 1 (
    echo GitHub CLI neni prihlasene.
    echo Spust: gh auth login
    goto :fail
)

where git >nul 2>nul
if errorlevel 1 (
    echo Git neni dostupny v PATH.
    goto :fail
)

set "DIRTY="
for /f "delims=" %%S in ('git status --porcelain --untracked-files=no 2^>nul') do set "DIRTY=1"
if defined DIRTY (
    echo Repo obsahuje necommitnute zmeny. Pred releasem je commitni nebo vrat.
    goto :fail
)

echo Kontroluji aktualni main...
git fetch origin main --quiet
if errorlevel 1 (
    echo Nepodarilo se nacist origin/main.
    goto :fail
)
for /f %%H in ('git rev-parse HEAD') do set "LOCAL_HEAD=%%H"
for /f %%H in ('git rev-parse origin/main') do set "REMOTE_HEAD=%%H"
if /I not "!LOCAL_HEAD!"=="!REMOTE_HEAD!" (
    echo Lokalni HEAD neodpovida origin/main.
    echo Nejdriw proved git pull a release spust znovu.
    goto :fail
)

set "CREATE_RELEASE=1"
gh release view "%TAG%" --repo "%REPO%" >nul 2>&1
if not errorlevel 1 (
    echo Release %TAG% uz existuje.
    set /p "REPLACE=Nahradit v nem .swyfmod soubory? [y/N]: "
    if /I not "!REPLACE!"=="Y" if /I not "!REPLACE!"=="ANO" (
        echo Release zrusen.
        goto :ok
    )
    set "CREATE_RELEASE=0"
)

if not exist "%DIST%" mkdir "%DIST%"
del /q "%DIST%\*.swyfmod" >nul 2>&1

echo.
echo Sestavuji vsechny mody...
call "%~dp0build.cmd" -GameDir "%GAME_DIR%" -Mod All
if errorlevel 1 (
    echo Build selhal. Release nebyl vytvoren.
    goto :fail
)

set /a COUNT=0
for %%F in ("%DIST%\*.swyfmod") do (
    if exist "%%~fF" set /a COUNT+=1
)
if not "!COUNT!"=="3" (
    echo Ocekavany pocet balicku je 3, ale nalezeno bylo !COUNT!.
    echo Release nebyl vytvoren.
    goto :fail
)

if "!CREATE_RELEASE!"=="1" (
    echo.
    echo Vytvarim GitHub Release %TAG%...
    gh release create "%TAG%" --repo "%REPO%" --target main --title "SWYF mody %TAG%" --generate-notes
    if errorlevel 1 (
        echo Vytvoreni GitHub Release selhalo.
        goto :fail
    )
)

echo.
echo Nahravam balicky...
for %%F in ("%DIST%\*.swyfmod") do (
    if exist "%%~fF" (
        echo   %%~nxF
        gh release upload "%TAG%" "%%~fF" --repo "%REPO%" --clobber
        if errorlevel 1 (
            echo Nahrani %%~nxF selhalo.
            goto :fail
        )
    )
)

echo.
echo Release %TAG% je hotovy.
gh release view "%TAG%" --repo "%REPO%" --web

:ok
popd
exit /b 0

:fail
popd
exit /b 1
