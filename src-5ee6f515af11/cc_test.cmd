@echo off
del results.trx
set msbuild=%windir%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
%msbuild% %~dp0build.csproj /p:Configuration=Debug;Platform="Mixed Platforms";TargetFrameworkVersion=v4.0 /verbosity:quiet /nologo /target:CC_Test
pause