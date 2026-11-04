@echo off
rem Sets up the Visual C++ (x64) environment that NativeAOT links with (publish_windows.bat), then the
rem publish runs with -p:IlcUseEnvironmentalTools=true. Done here rather than by the ILCompiler, whose own
rem lookup breaks when vswhere.exe is missing (its error text ends up in the linker command).
rem Visual Studio (with "Desktop development with C++") is found in this order: an already set-up developer
rem prompt, BOOKWORM_VS_DIR, vswhere, then the usual install folders (and D:\VisualStudio).
if defined VCToolsInstallDir exit /b 0
set "BOOKWORM_VCVARS="
if defined BOOKWORM_VS_DIR if exist "%BOOKWORM_VS_DIR%\VC\Auxiliary\Build\vcvars64.bat" set "BOOKWORM_VCVARS=%BOOKWORM_VS_DIR%\VC\Auxiliary\Build\vcvars64.bat"
set "BOOKWORM_VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not defined BOOKWORM_VCVARS if exist "%BOOKWORM_VSWHERE%" for /f "usebackq tokens=*" %%i in (`"%BOOKWORM_VSWHERE%" -latest -prerelease -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "BOOKWORM_VCVARS=%%i\VC\Auxiliary\Build\vcvars64.bat"
if not defined BOOKWORM_VCVARS if exist "D:\VisualStudio\VC\Auxiliary\Build\vcvars64.bat" set "BOOKWORM_VCVARS=D:\VisualStudio\VC\Auxiliary\Build\vcvars64.bat"
if not defined BOOKWORM_VCVARS for /d %%d in ("%ProgramFiles%\Microsoft Visual Studio\*") do for /d %%e in ("%%d\*") do if not defined BOOKWORM_VCVARS if exist "%%e\VC\Auxiliary\Build\vcvars64.bat" set "BOOKWORM_VCVARS=%%e\VC\Auxiliary\Build\vcvars64.bat"
if not defined BOOKWORM_VCVARS for /d %%d in ("%ProgramFiles(x86)%\Microsoft Visual Studio\*") do for /d %%e in ("%%d\*") do if not defined BOOKWORM_VCVARS if exist "%%e\VC\Auxiliary\Build\vcvars64.bat" set "BOOKWORM_VCVARS=%%e\VC\Auxiliary\Build\vcvars64.bat"
if not defined BOOKWORM_VCVARS (
    echo Visual Studio with the C++ build tools wasn't found. Install "Desktop development with C++",
    echo or set BOOKWORM_VS_DIR to the Visual Studio folder.
    exit /b 1
)
call "%BOOKWORM_VCVARS%" >nul 2>nul
rem vcvars sets Platform=x64, which MSBuild would pick up (bin\x64\... folders); the projects use AnyCPU.
set "Platform="
if not defined VCToolsInstallDir (
    echo Could not set up Visual C++ from "%BOOKWORM_VCVARS%".
    exit /b 1
)
exit /b 0
