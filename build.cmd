@echo off
powershell -ExecutionPolicy ByPass -NoProfile -command "& """%~dp0eng\common\Build.ps1""" -restore -build -projects %cd%\src\Swift.Bindings\src\Swift.Bindings.csproj %*"
exit /b %ErrorLevel%