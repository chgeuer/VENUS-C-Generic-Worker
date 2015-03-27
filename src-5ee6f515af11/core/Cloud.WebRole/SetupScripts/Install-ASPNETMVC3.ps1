$id = $args[0]
$currentDir = (Get-Location -PSProvider FileSystem).ProviderPath

$binaryDir = (Get-Location -PSProvider FileSystem).ProviderPath + [System.IO.Path]::DirectorySeparatorChar 
$webclient = (New-Object System.Net.WebClient)

if(Test-Path "${Env:ProgramFiles(x86)}\Microsoft ASP.NET\ASP.NET MVC 3")
{
	# http://stackoverflow.com/questions/4750305/how-to-check-if-asp-net-mvc-3-is-installed
	Write-Host "Already installed: ASP.NET MVC3"
}
else 
{
	$mvc3Path = $binaryDir + "AspNetMVC3Setup.exe"
	$aspnetMvc3Download = "http://download.microsoft.com/download/3/4/A/34A8A203-BD4B-44A2-AF8B-CA2CFCB311CC/AspNetMVC3Setup.exe"

	Write-Host "Downloading ASP.NET MVC3 to $mvc3Path"
	$webclient.downloadfile($aspnetMvc3Download, "$mvc3Path")

	Write-Host "Installing ASP.NET MVC3"
	Start-Process -FilePath $mvc3Path /q

	if(Test-Path "${Env:ProgramFiles(x86)}\Microsoft ASP.NET\ASP.NET MVC 3")
	{
		Write-Host "Successfully installed ASP.NET MVC3"
	}
}
