@echo off
setlocal

if not defined LOCALAPPDATA (
  echo PrivacyDot could not locate the installation folder.
  exit /b 1
)

"%SystemRoot%\System32\taskkill.exe" /IM PrivacyDot.exe /F >nul 2>nul
del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\PrivacyDot.lnk" >nul 2>nul
del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\PrivacyDot.lnk" >nul 2>nul
cd /d "%SystemRoot%\Temp"
rmdir /S /Q "%LOCALAPPDATA%\PrivacyDot" >nul 2>nul

echo PrivacyDot uninstalled.
pause
