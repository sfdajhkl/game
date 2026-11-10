@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Build-Playtest.ps1" -OpenEditor %*
if errorlevel 1 (
  echo.
  echo Arvito could not open. Read the error above.
  echo Close this project's Unity Editor before package repair. Unity Hub may stay open.
  pause
  exit /b 1
)
