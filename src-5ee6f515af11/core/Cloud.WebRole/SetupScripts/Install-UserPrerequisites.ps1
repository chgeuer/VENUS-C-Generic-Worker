$id = $args[0]

add-type -path ..\Microsoft.WindowsAzure.StorageClient.dll
add-type -path ..\Ionic.Zip.dll

function downloadFromBlob([string]$containerName, [string]$blobName, [string]$connectionString)
{
	$currentDir = (Get-Location -PSProvider FileSystem).ProviderPath + [System.IO.Path]::DirectorySeparatorChar
	try
	{
		[Microsoft.WindowsAzure.StorageClient.CloudStorageAccountStorageClientExtensions]::CreateCloudBlobClient([Microsoft.WindowsAzure.CloudStorageAccount]::Parse($connectionString)).GetContainerReference($containerName).GetBlobReference($blobName).DownloadToFile("$currentDir$blobName")
	} 
	catch [Microsoft.WindowsAzure.StorageClient.StorageException]
	{
		Write-Error "File $blobName in container $containerName does not exist"
		return $FALSE
	}
	return $TRUE
}
			
[string]$myConnectionString=[System.Environment]::GetEnvironmentVariable("GW_STARTUP_CONNECTION_STRING")
[string]$containerName="userprerequisites"
[string]$blobName="UserPrerequisites.zip"

[string]$userDir="UserSetup"
[string]$tempDir="temp"
[string]$userScript="UserPrerequisites.ps1"


if (! (Test-Path -path $userDir))
{
	new-item -name $userDir -type directory
}
Push-Location $userDir

if (Test-Path -path $blobName)
{
	Write-Host "removing old $blobName file"
	Remove-Item -name $blobName -force
}

Write-Host "Downloading $blobName from container $containerName"
if (! (downloadFromBlob $containerName $blobName $myConnectionString))
{
	Write-Error "Download failed"
	pop-location
	return $TRUE
}
Write-Host "Download finished"

Write-Host "Unpacking $blobName into $tempDir"
if (Test-Path -path $tempDir)
{
	remove-item -name $tempDir -force -recurse
}
new-item -name $tempDir -type directory


$userDirFull = get-location
$zip_FileFull = [System.IO.Path]::Combine($userDirFull, $blobName)
$zip_destination = [System.IO.Path]::Combine($userDirFull, $tempDir)

if (! ([Ionic.Zip.ZipFile]::IsZipFile($zip_FileFull, $TRUE)))
{
	Write-Error "Zip file is not valid."
	pop-location
	return $TRUE
}

[Ionic.Zip.ZipFile] $zip = new-object Ionic.Zip.ZipFile $zip_FileFull
$zip.ExtractAll($zip_destination, [Ionic.Zip.ExtractExistingFileAction]::OverwriteSilently)

Write-Host "Unpacking finished"



Push-location $tempDir
$tempDirFull = get-location
$userScriptFull = [System.IO.Path]::Combine($tempDirFull, $userScript)
get-childitem -recurse
if (Test-Path -path "$userScript" -pathtype leaf)
{
	Write-Host "Run user script"
	. "$userScriptFull"
	Write-Host "Run user script finished"
}
else
{
	Write-Error "User script $userScript not found"
}
Pop-location

Pop-location
