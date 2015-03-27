@echo off
set msbuild=%windir%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
%msbuild% %1 /p:Configuration=Debug