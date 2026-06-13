@echo off
setlocal
cd /d "%~dp0"
chcp 65001 >nul

powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\darklove-cli.ps1" %*
set "exitCode=%errorlevel%"

if not "%exitCode%"=="0" (
    echo.
    echo Darklove komut satiri istemcisi hata ile kapandi. Kod: %exitCode%
    if "%~1"=="" pause
)

exit /b %exitCode%
