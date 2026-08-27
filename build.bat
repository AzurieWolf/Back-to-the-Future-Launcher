@echo off
setlocal
title Launcher - Build
pushd "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERROR: The .NET SDK is not installed.
    echo Run install-requirements.bat first.
    popd
    pause
    exit /b 1
)

if not exist "launcher.ico" (
    if not exist "launcher-icon-source.png" (
        echo ERROR: launcher.ico was not found.
        echo Add launcher.ico beside build.bat before building.
        popd
        pause
        exit /b 1
    )

    echo Creating launcher.ico from launcher-icon-source.png...
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0create-icon.ps1"
    if errorlevel 1 (
        echo ERROR: Could not create launcher.ico.
        popd
        pause
        exit /b 1
    )
)

echo Building standalone Windows x64 launcher...
dotnet publish BackToTheFutureLauncher.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -p:DebugType=None ^
    -p:DebugSymbols=false ^
    --output dist

if errorlevel 1 (
    echo.
    echo ERROR: Build failed.
    popd
    pause
    exit /b 1
)

echo.
echo Build complete:
echo %~dp0dist\Launcher.exe
echo.
echo Keep launcher.ini and your configured background image beside Launcher.exe.
popd
pause
exit /b 0
