@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Build-Playtest.ps1" %*
if errorlevel 1 (
  echo.
  echo Build failed. Please send Builds\unity-build.log for diagnosis.
)
pause
