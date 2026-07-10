@echo off
setlocal

set "APPNAME=PrivacyDot"
set "APPDIR=%LOCALAPPDATA%\PrivacyDot"
set "TARGET=%APPDIR%\PrivacyDot.exe"
set "DOTNET48_URL=https://go.microsoft.com/fwlink/?linkid=2088631"

call :check_dotnet48
if errorlevel 1 (
  echo PrivacyDot requires Microsoft .NET Framework 4.8.
  echo Opening the official Microsoft download page...
  start "" "%DOTNET48_URL%"
  pause
  exit /b 1
)

taskkill /IM PrivacyDot.exe /F >nul 2>nul

if not exist "%APPDIR%" mkdir "%APPDIR%"
copy /Y "%~dp0PrivacyDot.exe" "%APPDIR%\" >nul
copy /Y "%~dp0PrivacyDot.exe.config" "%APPDIR%\" >nul
copy /Y "%~dp0UninstallPrivacyDot.cmd" "%APPDIR%\" >nul

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0CreateShortcuts.ps1" "%TARGET%"

start "" "%TARGET%"
echo PrivacyDot installed successfully.
exit /b 0

:check_dotnet48
set "RELEASE="
for /f "tokens=3" %%R in ('reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release 2^>nul') do set "RELEASE=%%R"
if not defined RELEASE (
  for /f "tokens=3" %%R in ('reg query "HKLM\SOFTWARE\Wow6432Node\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release 2^>nul') do set "RELEASE=%%R"
)
if not defined RELEASE exit /b 1
set /a RELEASE_NUM=%RELEASE% >nul 2>nul
if %RELEASE_NUM% LSS 528040 exit /b 1
exit /b 0
