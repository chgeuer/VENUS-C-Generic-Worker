@set msbuild=%windir%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe
%msbuild% %~dp0build.csproj /p:Configuration=Debug;Platform="Mixed Platforms";TargetFrameworkVersion=v4.0 /target:PackageComputeService /verbosity:quiet /nologo
pause