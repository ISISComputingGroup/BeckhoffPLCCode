@echo off

REM Building the Beckhoff Builder
call msbuild.exe /p:Configuration=Release;Platform=x64 util_scripts/Builder/BeckhoffBuilder.sln

if %ERRORLEVEL% neq 0 goto PROBLEM

call .\util_scripts\Builder\bin\x64\Release\TestAutomationInterface.exe

if %ERRORLEVEL% neq 0 goto PROBLEM

GOTO :EOF

:PROBLEM

@echo Beckhoff Build Failed
exit /b 1
