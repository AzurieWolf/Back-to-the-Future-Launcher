@echo off
setlocal EnableExtensions EnableDelayedExpansion
title Launcher - Install Requirements

echo Checking for the .NET SDK...
where dotnet >nul 2>nul
if errorlevel 1 goto install_dotnet

set "sdk_major="
for /f "tokens=1 delims=." %%V in ('dotnet --version 2^>nul') do set "sdk_major=%%V"
if defined sdk_major (
    if !sdk_major! geq 8 (
        echo Compatible .NET SDK !sdk_major! is already installed.
        goto success
    )
)

:install_dotnet
where winget >nul 2>nul
if errorlevel 1 (
    echo.
    echo ERROR: Windows Package Manager ^(winget^) is not available.
    echo Install the .NET 8 SDK manually from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    goto failure
)

echo A compatible .NET SDK was not found. Installing .NET 8 SDK...
winget install --id Microsoft.DotNet.SDK.8 --exact --source winget --accept-package-agreements --accept-source-agreements
if errorlevel 1 (
    echo.
    echo ERROR: The .NET 8 SDK installation failed.
    goto failure
)

echo.
echo .NET 8 SDK installed successfully.
echo Open a new terminal before building if dotnet is not immediately available.

:success
echo.
echo All build requirements are installed.
pause
exit /b 0

:failure
echo.
pause
exit /b 1
