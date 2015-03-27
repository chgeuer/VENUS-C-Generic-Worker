@echo off

Setlocal EnableDelayedExpansion

if "%1" == "" goto Usage
if "%2" == "" goto Usage
if not "%3" == "" goto Usage

goto GotParams

:Usage

echo Adds a header to all files with a given extension in the current working directory'S tree.
echo Parameters: LicenseFile FileExtension
echo Example: prepend_license.bat license.txt c

exit /b 1

:GotParams


for /r %%f in (*.%2) do (

  echo %%f
  copy %%f %temp%\prepend_license.txt
  copy /y %1 + %temp%\prepend_license.txt %%f
  del %temp%\prepend_license.txt
)