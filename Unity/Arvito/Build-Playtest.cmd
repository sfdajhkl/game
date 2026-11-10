@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Build-Playtest.ps1" %*
if errorlevel 1 (
  echo.
  echo Build did not complete. Read the error above.
  echo If Unity started, also check Builds\unity-build.log.
)
pause
