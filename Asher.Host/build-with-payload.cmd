@echo off
setlocal
set CONFIG=%1
if "%CONFIG%"=="" set CONFIG=Debug

echo Building Asher install payload dependencies (%CONFIG% x86)...

dotnet build "%~dp0..\Asher.SDK\Asher.SDK.csproj" -c %CONFIG% -p:Platform=x86
if errorlevel 1 exit /b 1

dotnet build "%~dp0..\Asher.Runtime\Asher.Runtime.csproj" -c %CONFIG% -p:Platform=x86
if errorlevel 1 exit /b 1

dotnet build "%~dp0..\Asher.Launcher\Asher.Launcher.csproj" -c %CONFIG% -p:Platform=x86
if errorlevel 1 exit /b 1

dotnet build "%~dp0Asher.Host.csproj" -c %CONFIG% -p:Platform=x86
if errorlevel 1 exit /b 1

echo Asher.Host build complete with install-payload.
endlocal
