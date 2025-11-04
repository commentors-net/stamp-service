@echo off
REM Quick Build Script for Secure Stamp Service
REM This script builds and creates a distribution package

echo ============================================
echo   Secure Stamp Service - Quick Build
echo ============================================
echo.

REM Check if dotnet is installed
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: .NET SDK not found!
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [1/4] Checking .NET version...
dotnet --version
echo.

echo [2/4] Restoring packages...
dotnet restore StampService.sln
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Restore failed!
    pause
    exit /b 1
)
echo.

echo [3/4] Building solution...
dotnet build StampService.sln -c Release
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
echo.

echo [4/4] Creating distribution package...
cd scripts
powershell -ExecutionPolicy Bypass -File Build-Distribution.ps1
cd ..
echo.

echo ============================================
echo            Build Complete!
echo ============================================
echo.
echo Distribution package created in current directory.
echo Look for: StampService-Distribution-YYYYMMDD-HHMMSS.zip
echo.
echo Next steps:
echo   1. Extract the ZIP file
echo   2. Follow instructions in QUICKSTART.md
echo   3. Or run Scripts\Install-StampService.ps1 as Admin
echo.
pause
