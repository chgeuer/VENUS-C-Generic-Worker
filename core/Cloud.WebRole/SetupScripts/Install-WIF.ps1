$id = $args[0]
$currentDir = (Get-Location -PSProvider FileSystem).ProviderPath

function SearchInstall($SearchVersion, $PathKey)
{
	$installObjects = ls -path $PathKey;
	$found = $FALSE;

	foreach($installEntry in $installObjects)
	{
		$entryProperty = Get-ItemProperty -LiteralPath registry::$installEntry
	   
		if($entryProperty.psobject.Properties -ne $null -and $entryProperty.psobject.Properties["(default)"].value -eq $searchVersion)
		{
			$found = $TRUE;
			break;
		}
	}

	$found;
}

function WIFInstalled2()
{
  $res1 = SearchInstall -SearchVersion '6.1.7600.0' -PathKey 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Windows Identity Foundation\Setup\';
  $res2 = SearchInstall -SearchVersion '6.1.7600.0' -PathKey 'HKLM:\SOFTWARE\Microsoft\Windows Identity Foundation\Setup\';
  (($res1 -eq $TRUE) -or ($res2 -eq $TRUE))
}

function WIFInstalled()
{
  $res1 = Get-WmiObject -Query 'select * from Win32_QuickFixEngineering' | where { $_.HotFixID -eq 'KB974405' }
  $res1
}

if (WIFInstalled) 
{
	Write-Host "Already installed: WIF"
}
else 
{
    $devMachine = ((Get-Item -LiteralPath "hklm:\SOFTWARE\Microsoft EMIC\Cloud\VENUS-C" -ErrorAction SilentlyContinue).GetValue("Microsoft.EMIC.Cloud.IsDevMachine", $null) -eq "true")
    
	$binaryDir = (Get-Location -PSProvider FileSystem).ProviderPath + [System.IO.Path]::DirectorySeparatorChar 

	$is64bit = [System.Environment]::GetEnvironmentVariable("ProgramW6432") -ne $null
	if ($is64bit) 
	{
		$wifaddress = "http://download.microsoft.com/download/D/7/2/D72FD747-69B6-40B7-875B-C2B40A6B2BDD/Windows6.1-KB974405-x64.msu"
		$wifPath = [System.IO.Path]::Combine($binaryDir, "Windows6.1-KB974405-x64.msu")
	}
	else 
	{
		# The 32bit version does not run on Azure, but might be relevant for a developer machine
		$wifaddress = "http://download.microsoft.com/download/D/7/2/D72FD747-69B6-40B7-875B-C2B40A6B2BDD/Windows6.1-KB974405-x86.msu"
		$wifPath = [System.IO.Path]::Combine($binaryDir, "Windows6.1-KB974405-x86.msu")
	}

	Write-Host "Downloading WIF to $wifPath"
	$webclient = (New-Object System.Net.WebClient)
	$webclient.downloadfile($wifaddress, "$wifPath")

	Write-Host "Installing WIF"

	if (!$devMachine)
	{
		# On developer machines, Windows Update is (of course) running. 
		# In the cloud, Windows Update is disabled by default. 
		# So we must turn on before installing WIF, and turn off later. 
		#
		sc.exe config wuauserv start= demand
		Start-Sleep -Seconds 10
	}

	wusa.exe "$wifPath" /quiet /norestart

	if (!$devMachine)
	{
		#
		# The wusa.exe call above does return before WIF might be fully installed, 
		# so we give Windows Update some seconds to finish. 
		# If we immediately turn off, the update request might not go through. 
		#
		Start-Sleep -Seconds 15
		sc.exe config wuauserv start= disabled   
	}
	
	if(WIFInstalled)
	{
		Write-Host "Successfully installed WIF"
	} 
}
