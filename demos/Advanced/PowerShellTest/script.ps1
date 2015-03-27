$time = [System.DateTime]::UtcNow.ToString("yyyy-MM-dd--hh-mm-ss")
$machine = [System.Environment]::MachineName
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$currentDir = Get-Location

Write-Host "Running as `"$currentUser`" on $machine at $time"

$file = "result-$time.txt"
$ignore = New-Item -Type File -Name $file 
Set-Content -Path $file -Value "Running as `"$currentUser`" on $machine at $time, Dir is $currentDir"
