@echo off
setlocal

taskkill /IM PrivacyDot.exe /F >nul 2>nul
del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Privacy Dot.lnk" >nul 2>nul
del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\Privacy Dot.lnk" >nul 2>nul
cd /d "%TEMP%"
rmdir /S /Q "%LOCALAPPDATA%\PrivacyDot" >nul 2>nul

echo PrivacyDot uninstalled.
pause
