# See http://msdn.microsoft.com/en-us/library/ms229747.aspx
# 
# FileSystemRights: ListDirectory ReadData CreateFiles WriteData CreateDirectories AppendData ReadExtendedAttributes WriteExtendedAttributes ExecuteFile Traverse DeleteSubdirectoriesAndFiles ReadAttributes WriteAttributes Write Delete ReadPermissions Read ReadAndExecute Modify ChangePermissions TakeOwnership Synchronize FullControl
# InheritanceFlags: None ContainerInherit ObjectInherit
# PropagationFlags: None NoPropagateInherit InheritOnly
# AccessControlType: Allow Deny

function GetCISUser([System.String] $folder)
{
	$perms = Get-Acl $folder
	$rules = [System.Object[]] $perms.Access | where { $_.IdentityReference.Value.StartsWith("CIS") } 
	if ($rules.Length -ge 1) 
	{
	   $ruleOriginal = [System.Security.AccessControl.FileSystemAccessRule] ($rules[0])
	}
	else 
	{
	   $ruleOriginal = [System.Security.AccessControl.FileSystemAccessRule] $rules
	}
	$cisUserName = [System.Security.Principal.NTAccount] $ruleOriginal.IdentityReference

	$cisUserName
}

function BlockInheritedACLandDeleteEntries([System.String] $folder)
{
	$perms = Get-Acl -Path $folder

	$isProtected = $true
	$preserveInheritance = $false
	$perms.SetAccessRuleProtection($isProtected, $preserveInheritance)

	$inheritanceFlags = [System.Security.AccessControl.InheritanceFlags] "ContainerInherit, ObjectInherit"
	$propagationFlags = [System.Security.AccessControl.PropagationFlags] "None"
	$accessControlType = [System.Security.AccessControl.AccessControlType] "Allow"
	
	# Grant Administrators FullControl
	$username = "BUILTIN\Administrators"
	$accessMask = [System.Security.AccessControl.FileSystemRights] "FullControl"
	$ruleNew = ( New-Object System.Security.AccessControl.FileSystemAccessRule $username, $accessMask, $inheritanceFlags, $propagationFlags, $accessControlType )
	$perms.AddAccessRule($ruleNew)
	
	# Grant SYSTEM FullControl
	$username = "SYSTEM"
	$accessMask = [System.Security.AccessControl.FileSystemRights] "FullControl"
	$ruleNew = ( New-Object System.Security.AccessControl.FileSystemAccessRule $username, $accessMask, $inheritanceFlags, $propagationFlags, $accessControlType )
	$perms.AddAccessRule($ruleNew)

	Set-Acl -Path $folder -AclObject $perms
}

function GrantPrivilege([System.String] $folder, [System.String] $username, [System.Security.AccessControl.FileSystemRights] $accessMask)
{
	$inheritanceFlags = [System.Security.AccessControl.InheritanceFlags] "ContainerInherit, ObjectInherit"
	$propagationFlags = [System.Security.AccessControl.PropagationFlags] "None"
	$accessControlType = [System.Security.AccessControl.AccessControlType] "Allow"
	
	$ruleNew = ( New-Object System.Security.AccessControl.FileSystemAccessRule $username, $accessMask, $inheritanceFlags, $propagationFlags, $accessControlType )

	$perms = Get-Acl -Path $folder
	$perms.AddAccessRule($ruleNew)
	Set-Acl -Path $folder -AclObject $perms

	Write-Host "Granted $accessMask to $username for `"$folder`""
}

function SetRegularPermissions([System.String] $folder)
{
	BlockInheritedACLandDeleteEntries -folder $folder

	$cisuser = GetCISUser -folder $folder
	if ($cisuser -ne $null) 
	{	
		GrantPrivilege -folder $folder -username $cisuser -accessMask "FullControl"
	}
	else
	{
		Write-Host "Could not determine cisuser, ignoring $folder"
	}

	GrantPrivilege -folder $folder -username "BUILTIN\Users" -accessMask "ReadData, ListDirectory, ReadExtendedAttributes, ExecuteFile, Traverse, ReadAttributes, ReadPermissions, Read, ReadAndExecute"
}

function CreateUsersFolder([System.String] $folder, [System.String] $username)
{
	$userfolder = [System.IO.Path]::Combine($folder, $username)
	$ignore = mkdir $userfolder

	BlockInheritedACLandDeleteEntries -folder $userfolder 

	$cisuser = GetCISUser -folder $folder
	if ($cisuser -ne $null) 
	{
		GrantPrivilege -folder $userfolder -username $cisuser                 -accessMask "FullControl"
	}

	$machine = [System.Environment]::MachineName
	$actualuser = "$machine\$username"
	GrantPrivilege -folder $userfolder -username $actualuser              -accessMask "FullControl"
}

$machine = [System.Environment]::MachineName
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name
Write-Host "Running as `"$currentUser`" on $machine"

[Reflection.Assembly]::LoadWithPartialName("Microsoft.WindowsAzure.ServiceRuntime") 
if (![Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::IsAvailable)
{
	Write-Host "Set-Permissions.ps1 - Not running in fabric, exiting"
	return;
}

$gwappsfolder = ( [Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::GetLocalResource("GWApps") ).RootPath
$gwusersfolder = ( [Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::GetLocalResource("GWUsers") ).RootPath
$gwshareddatasetsfolder = ( [Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment]::GetLocalResource("GWData") ).RootPath

$cisuser = GetCISUser -folder $gwusersfolder
Write-Host "Azure Account is $cisuser"

SetRegularPermissions -folder $gwappsfolder
SetRegularPermissions -folder $gwusersfolder
SetRegularPermissions -folder $gwshareddatasetsfolder
