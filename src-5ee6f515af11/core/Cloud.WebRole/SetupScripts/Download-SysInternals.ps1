$id = $args[0]
$currentDir = (Get-Location -PSProvider FileSystem).ProviderPath

function InstallSysInternalsComponent([System.String] $filename, [System.String] $RegistryName)
{
	$binaryDir = (Get-Location -PSProvider FileSystem).ProviderPath + [System.IO.Path]::DirectorySeparatorChar 

	# [HKEY_CURRENT_USER\Software\Sysinternals\PsExec]
	# "EulaAccepted"=dword:00000001

	$f = Test-Path -Path "HKCU:\Software\SysInternals"
	if (!$f)
	{
		Write-Host "Create main key"
		$ignore = New-Item -Path "HKCU:\Software\SysInternals" -type Directory 
	}
	$f = Test-Path -Path "HKCU:\Software\SysInternals\$RegistryName"
	if (!$f)
	{
		Write-Host "Create sub key"
		$ignore = New-Item -Path "HKCU:\Software\SysInternals\$RegistryName" -type Directory
	}
	$ignore = New-ItemProperty -Path "HKCU:\Software\SysInternals\$RegistryName" -Name "EulaAccepted" -PropertyType DWord -Value 1 -Force

	$f = Test-Path $filename
	if (!$f)
	{
		$url = [System.String]::Format("http://live.sysinternals.com/{0}", $filename)
		$wc = (New-Object System.Net.WebClient)

		$path = [System.IO.Path]::Combine($binaryDir, $filename)
		Write-Host "Downloading application '$RegistryName' from $url to $path"
		$wc.DownloadFile($url, $path)
		Write-Host "Done..."
	}
}

InstallSysInternalsComponent "accesschk.exe"  "AccessChk"
InstallSysInternalsComponent "accessenum.exe" "AccessEnum"
InstallSysInternalsComponent "psexec.exe"     "PsExec"
InstallSysInternalsComponent "procexp.exe"    "Process Explorer"
InstallSysInternalsComponent "procmon.exe"    "Process Monitor"
InstallSysInternalsComponent "Dbgview.exe"    "DbgView"
