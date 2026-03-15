@echo off
REM Build RTK_IO_x64 shim DLL
REM Must be run from VS Developer Command Prompt or with vcvars64 set

cl /LD /EHsc /O2 rtk_io_shim.cpp /Fe:RTK_IO_x64.dll /link /DEF:rtk_io_shim.def
if errorlevel 1 (
    echo BUILD FAILED
    exit /b 1
)
echo BUILD SUCCEEDED
echo Deploy: copy RTK_IO_x64.dll to EgavdsAudioProbe output directory
echo         rename original RTK_IO_x64.dll to RTK_IO_x64_real.dll first!
