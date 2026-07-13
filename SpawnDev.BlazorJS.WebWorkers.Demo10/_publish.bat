@echo off
echo Building publish version
dotnet publish "%~dp0/." --configuration Release -o "%~dp0/bin/Publish" || goto :ERROR

echo Success
exit /b 0

:ERROR
echo Failed
exit /b 1