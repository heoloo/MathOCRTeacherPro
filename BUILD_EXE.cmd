@echo off
setlocal
cd /d "%~dp0"
echo ===============================================
echo MathOCR Teacher Pro - Windows EXE Builder
echo ===============================================
echo.
where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: .NET 8 SDK was not found on THIS BUILD PC.
  echo You only need the SDK on the computer that builds the EXE.
  echo After publishing, the generated EXE runs without Python and without .NET installation.
  pause
  exit /b 1
)
dotnet publish MathOCRTeacherPro.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
if errorlevel 1 (
  echo BUILD FAILED.
  pause
  exit /b 1
)
echo.
echo BUILD COMPLETE.
echo EXE:
echo bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\MathOCRTeacherPro.exe
pause
