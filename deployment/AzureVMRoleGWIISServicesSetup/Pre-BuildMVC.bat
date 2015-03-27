@echo off
set msbuild=%windir%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
%msbuild% /t:Package %1 /p:Configuration=Debug /p:Platform=AnyCPU 
set mypath=%2
set mypath=%mypath::=_C%
xcopy %2\PrecompiledWeb\Archive\Content\%mypath%\core\AzureVMRole\AzureVMRoleSecureJobManagement\obj\Debug\Package\PackageTmp %2\PrecompiledWeb\AzureVMRoleSecureJobManagement /S /I /Q /Y
rd/s/q %2\PrecompiledWeb\Archive
del %2\PrecompiledWeb\*.* /q
