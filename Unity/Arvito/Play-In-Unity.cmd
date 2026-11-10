@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Build-Playtest.ps1" -PlayInEditor %*
if errorlevel 1 (
  echo.
  echo Unity could not be started. Read the error above.
  pause
  exit /b 1
)
