@echo off
rem Builds a ready-to-run copy of WARC Archiver in the "publish" folder.
rem
rem   publish.cmd            small build; the PC needs the ".NET Desktop Runtime 10" (x64)
rem   publish.cmd portable   bundles .NET (about 150 MB); runs on any Windows 10/11 x64 PC with nothing installed
rem
rem config.json is copied next to WarcArchiver.exe and can be edited there.
setlocal
set "OUT=%~dp0publish"
if /i "%~1"=="portable" (
    dotnet publish "%~dp0src\WarcArchiver.App" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%OUT%"
) else (
    dotnet publish "%~dp0src\WarcArchiver.App" -c Release -r win-x64 --self-contained false -o "%OUT%"
)
if errorlevel 1 (
    echo.
    echo Build failed.
    exit /b 1
)
echo.
echo Done: %OUT%\WarcArchiver.exe
