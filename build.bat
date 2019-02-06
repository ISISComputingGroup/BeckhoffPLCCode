@echo off

if exist "C:\Program files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build" (
    set "VCVARALLDIR=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build"
)
if exist "C:\Program files (x86)\Microsoft Visual Studio\2017\Professional\VC\Auxiliary\Build" (
    set "VCVARALLDIR=C:\Program files (x86)\Microsoft Visual Studio\2017\Professional\VC\Auxiliary\Build"
)

call "%VCVARALLDIR%\vcvarsall.bat" x64

REM Building the Beckhoff Builder
call msbuild.exe /p:Configuration=Release;Platform=x64 util_scripts/Builder/BeckhoffBuilder.sln

if %ERRORLEVEL% neq 0 goto :PROBLEM

call .\util_scripts\Builder\bin\x64\Release\BeckhoffBuilder.exe "%~dp0\PLC Development.sln"

if %ERRORLEVEL% neq 0 goto :PROBLEM

GOTO :EOF

:PROBLEM

@echo Beckhoff Build Failed
exit /b 1
