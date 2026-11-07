@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

if not exist ".tmp" mkdir ".tmp"
set TEMP=%~dp0.tmp
set TMP=%~dp0.tmp
set NUGET_PACKAGES=%~dp0.tmp\pkgs

echo Building Laocoon ...
dotnet build "src\Laocoon.csproj" -c Release
if errorlevel 1 goto fail

copy /y "src\bin\Release\net48\Laocoon.exe" "Laocoon.exe" >nul
echo.
echo Done: %~dp0Laocoon.exe
dir /b "Laocoon.exe"
goto end

:fail
echo.
echo Build FAILED.

:end
pause
