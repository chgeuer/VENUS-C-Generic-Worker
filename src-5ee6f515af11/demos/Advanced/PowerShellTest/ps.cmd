echo "Running %1"

echo "Greets" > greeting.txt

dir > dir.txt

REM %windir%\system32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Unrestricted -File %1 > res.txt 2>&1
powershell  -ExecutionPolicy Unrestricted -File %1 > res.txt 2>&1
