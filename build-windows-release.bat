@echo off
setlocal enabledelayedexpansion

echo ===========================================
echo   oniDash Windows Single-File Build Script
echo ===========================================

cd /d "%~dp0"

echo [1/3] Building frontend assets...
cd frontend\oniDash.Web
call npm install
if %ERRORLEVEL% neq 0 (
    echo npm install failed.
    exit /b %ERRORLEVEL%
)
call npm run build
if %ERRORLEVEL% neq 0 (
    echo Frontend build failed.
    exit /b %ERRORLEVEL%
)

cd ..\..

echo [2/3] Copying dist to API wwwroot...
if not exist "backend\oniDash.Api\wwwroot" mkdir "backend\oniDash.Api\wwwroot"
xcopy /E /Y /I "frontend\oniDash.Web\dist\*" "backend\oniDash.Api\wwwroot\"

echo [3/3] Publishing standalone single-file executable...
dotnet publish backend\oniDash.Api\oniDash.Api.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o ".\dist\windows"

if %ERRORLEVEL% neq 0 (
    echo dotnet publish failed.
    exit /b %ERRORLEVEL%
)

echo.
echo Standalone executable produced at:
echo dist\windows\oniDash.Api.exe
echo.
echo Rename or attach this file to your GitHub Release!
