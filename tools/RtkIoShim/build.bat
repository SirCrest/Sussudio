@echo off
setlocal EnableExtensions
REM Build RTK_IO_x64 shim DLL. Must be run from an x64 VS Developer Prompt.

cd /d "%~dp0"

if /I not "%VSCMD_ARG_TGT_ARCH%"=="x64" (
    echo BUILD FAILED: run from an x64 Developer Command Prompt.
    exit /b 1
)

where cl.exe >nul 2>nul || (
    echo BUILD FAILED: cl.exe was not found on PATH.
    exit /b 1
)

where dumpbin.exe >nul 2>nul || (
    echo BUILD FAILED: dumpbin.exe was not found on PATH.
    exit /b 1
)

if not exist rtk_io_shim.cpp (
    echo BUILD FAILED: rtk_io_shim.cpp not found.
    exit /b 1
)

if not exist rtk_io_shim.def (
    echo BUILD FAILED: rtk_io_shim.def not found.
    exit /b 1
)

del /q RTK_IO_x64.dll RTK_IO_x64.lib RTK_IO_x64.exp rtk_io_shim.obj 2>nul

cl /LD /EHsc /O2 /nologo rtk_io_shim.cpp /Fe:RTK_IO_x64.dll /link /NOLOGO /MACHINE:X64 /DEF:rtk_io_shim.def
if errorlevel 1 (
    echo BUILD FAILED: compiler or linker returned an error.
    exit /b 1
)

if not exist RTK_IO_x64.dll (
    echo BUILD FAILED: RTK_IO_x64.dll was not produced.
    exit /b 1
)

dumpbin /headers RTK_IO_x64.dll | findstr /C:"machine (x64)" >nul || (
    echo BUILD FAILED: RTK_IO_x64.dll is not an x64 image.
    exit /b 1
)

if not exist RTK_IO_x64.lib (
    echo BUILD FAILED: RTK_IO_x64.lib was not produced.
    exit /b 1
)

if not exist RTK_IO_x64.exp (
    echo BUILD FAILED: RTK_IO_x64.exp was not produced.
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "$ErrorActionPreference = 'Stop'; $dll = Get-FileHash -Algorithm SHA256 -LiteralPath 'RTK_IO_x64.dll' -ErrorAction Stop; if ([string]::IsNullOrWhiteSpace($dll.Hash)) { throw 'RTK_IO_x64.dll hash was empty.' }; Write-Host ('RTK_IO_x64.dll SHA256=' + $dll.Hash); if (Test-Path -LiteralPath 'RTK_IO_x64_real.dll') { $real = Get-FileHash -Algorithm SHA256 -LiteralPath 'RTK_IO_x64_real.dll' -ErrorAction Stop; if ([string]::IsNullOrWhiteSpace($real.Hash)) { throw 'RTK_IO_x64_real.dll hash was empty.' }; Write-Host ('RTK_IO_x64_real.dll SHA256=' + $real.Hash) } else { Write-Host 'RTK_IO_x64_real.dll not present; deploy by renaming the vendor DLL before copying the shim.' }"
if errorlevel 1 (
    echo BUILD FAILED: unable to fingerprint build artifacts.
    exit /b 1
)

echo BUILD SUCCEEDED: RTK_IO_x64.dll x64 shim is ready.
echo Deploy: copy RTK_IO_x64.dll to the target output directory.
echo         rename the vendor RTK_IO_x64.dll to RTK_IO_x64_real.dll first.
