@echo off
setlocal

if exist "C:\Program files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build" (
    set "VCVARALLDIR=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build"
)
if exist "C:\Program files (x86)\Microsoft Visual Studio\2017\Professional\VC\Auxiliary\Build" (
    set "VCVARALLDIR=C:\Program files (x86)\Microsoft Visual Studio\2017\Professional\VC\Auxiliary\Build"
)

REM check if msbuild is already in path
call msbuild.exe "/ver"
if %ERRORLEVEL% equ 0 goto :STARTBUILD

call "%VCVARALLDIR%\vcvarsall.bat" x64

:STARTBUILD

REM Building the Beckhoff Builder

call msbuild.exe /p:Configuration=Release;Platform=x64 /t:clean util_scripts/AutomationTools/AutomationTools.sln
if %ERRORLEVEL% neq 0 goto :PROBLEM

call msbuild.exe /p:Configuration=Release;Platform=x64 util_scripts/AutomationTools/AutomationTools.sln
if %ERRORLEVEL% neq 0 goto :PROBLEM

call .\util_scripts\AutomationTools\bin\x64\Release\AutomationTools.exe "%~dp0\PLC Development.sln" clean
if %ERRORLEVEL% neq 0 goto :PROBLEM

call .\util_scripts\AutomationTools\bin\x64\Release\AutomationTools.exe "%~dp0\PLC Development.sln" build
if %ERRORLEVEL% neq 0 goto :PROBLEM

call .\util_scripts\AutomationTools\bin\x64\Release\AutomationTools.exe "%~dp0\dummy_PLC\TestPLC.sln" clean
if %ERRORLEVEL% neq 0 goto :PROBLEM

call .\util_scripts\AutomationTools\bin\x64\Release\AutomationTools.exe "%~dp0\dummy_PLC\TestPLC.sln" build
if %ERRORLEVEL% neq 0 goto :PROBLEM

GOTO :EOF

:PROBLEM

@echo Beckhoff Build Failed: %ERRORLEVEL%
exit /b 1
