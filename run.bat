@echo off
setlocal
title Launcher - Run
pushd "%~dp0"

if not exist "dist\Launcher.exe" (
    echo Launcher.exe has not been built yet.
    echo Starting the build now...
    echo.
    call build.bat
    if errorlevel 1 (
        popd
        exit /b 1
    )
)

if not exist "dist\launcher.ini" (
    echo ERROR: dist\launcher.ini was not found.
    echo Run build.bat to recreate the distributable files.
    popd
    pause
    exit /b 1
)

start "" /d "%~dp0dist" "%~dp0dist\Launcher.exe"
if errorlevel 1 (
    echo ERROR: The launcher could not be started.
    popd
    pause
    exit /b 1
)

popd
exit /b 0
