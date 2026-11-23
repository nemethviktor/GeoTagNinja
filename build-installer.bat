@echo off
rem Builds the GeoTagNinja installer in Release mode: Release build of the app, the tests, then the WiX .msi.
rem The build number is taken from the freshly built exe, and a copy of the .msi is left in
rem   %LOCALAPPDATA%\build\GeoTagNinja\release\b<build>\GeoTagNinja_Setup.msi
rem Same thing as the "Build installer (Release x64 + MSI)" VS Code task.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0.vscode\msbuild.ps1" -Action Installer
set RC=%ERRORLEVEL%
if not "%RC%"=="0" echo.& echo BUILD FAILED (exit code %RC%).
pause
exit /b %RC%
