@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-channels.ps1" %*
pause
