[Reflection.Assembly]::LoadWithPartialName("Microsoft.WindowsAzure.ServiceRuntime") 
if (![Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::IsAvailable)
{
	Write-Host "Not running in fabric, exiting"
	return;
}

$currentDir = (Get-Location -PSProvider FileSystem).ProviderPath
$currentDir =  [System.IO.Path]::Combine($currentDir, "SetupScripts")
Set-Location $currentDir
$logdirname = "setuplogs"
mkdir $logdirname

$time = [System.DateTime]::UtcNow.ToString("yyyy-MM-dd--hh-mm-ss")
$deploymentid = [Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::DeploymentId
$roleid = [Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::CurrentRoleInstance.Id
$id = "$time-$deploymentid-$roleid"
$machine = [System.Environment]::MachineName
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name

Start-Transcript -Path "$currentDir\$logdirname\$id-setup.log"
Write-Host "Installing pre-requisites for cloud machine, Deployment $deploymentid Role $roleid"
Write-Host "Running as `"$currentUser`" on $machine"
Write-Host "currentDir = $currentDir"

Write-Host "##############################################################################"
Write-Host "# Installing ASP.NET MVC3                                                    #"
Write-Host "##############################################################################"
. "$currentDir\Install-ASPNETMVC3.ps1"    $id
Write-Host "##############################################################################"
Write-Host "# Installing WIF                                                             #"
Write-Host "##############################################################################"
. "$currentDir\Install-WIF.ps1"           $id
Write-Host "##############################################################################"
Write-Host "# Downloading SysInternals utilities                                         #"
Write-Host "##############################################################################"
. "$currentDir\Download-SysInternals.ps1" $id
Write-Host "##############################################################################"
Write-Host "# Setting directory permissions                                              #"
Write-Host "##############################################################################"
. "$currentDir\Set-Permissions.ps1"
Write-Host "##############################################################################"
Write-Host "# Installing UserPrerequisites                                               #"
Write-Host "##############################################################################"
. "$currentDir\Install-UserPrerequisites.ps1"           $id
Write-Host "##############################################################################"
Write-Host "# Finished Setup                                                             #"
Write-Host "##############################################################################"

Stop-Transcript
