@echo off
setlocal

if not defined LOCALAPPDATA (
  echo PrivacyDot could not locate the local application data folder.
  exit /b 1
)

set "APPNAME=PrivacyDot"
set "APPDIR=%LOCALAPPDATA%\PrivacyDot"
set "TARGET=%APPDIR%\PrivacyDot.exe"
set "DOTNET48_URL=https://go.microsoft.com/fwlink/?linkid=2088631"

if not exist "%~dp0PrivacyDot.exe" goto invalid_payload
if not exist "%~dp0PrivacyDot.exe.config" goto invalid_payload
if not exist "%~dp0CreateShortcuts.ps1" goto invalid_payload
if not exist "%~dp0UninstallPrivacyDot.cmd" goto invalid_payload

call :check_dotnet48
if errorlevel 1 (
  echo PrivacyDot requires Microsoft .NET Framework 4.8.
  echo Opening the official Microsoft download page...
  start "" "%DOTNET48_URL%"
  pause
  exit /b 1
)

"%SystemRoot%\System32\taskkill.exe" /IM PrivacyDot.exe /F >nul 2>nul

if not exist "%APPDIR%" mkdir "%APPDIR%"
if errorlevel 1 goto install_failed
copy /Y "%~dp0PrivacyDot.exe" "%APPDIR%\" >nul
if errorlevel 1 goto install_failed
copy /Y "%~dp0PrivacyDot.exe.config" "%APPDIR%\" >nul
if errorlevel 1 goto install_failed
copy /Y "%~dp0UninstallPrivacyDot.cmd" "%APPDIR%\" >nul
if errorlevel 1 goto install_failed

"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "%~dp0CreateShortcuts.ps1" "%TARGET%"
if errorlevel 1 goto install_failed

start "" "%TARGET%"
echo PrivacyDot installed successfully.
exit /b 0

:invalid_payload
echo The PrivacyDot installer payload is incomplete.
exit /b 1

:install_failed
echo PrivacyDot could not be installed.
exit /b 1

:check_dotnet48
set "RELEASE="
for /f "tokens=3" %%R in ('%SystemRoot%\System32\reg.exe query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release 2^>nul') do set "RELEASE=%%R"
if not defined RELEASE (
  for /f "tokens=3" %%R in ('%SystemRoot%\System32\reg.exe query "HKLM\SOFTWARE\Wow6432Node\Microsoft\NET Framework Setup\NDP\v4\Full" /v Release 2^>nul') do set "RELEASE=%%R"
)
if not defined RELEASE exit /b 1
set /a RELEASE_NUM=%RELEASE% >nul 2>nul
if %RELEASE_NUM% LSS 528040 exit /b 1
exit /b 0
