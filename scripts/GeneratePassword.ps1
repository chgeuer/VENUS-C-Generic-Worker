#
# .DESCRIPTION
# PowerShell script to encrypt password according to http://msdn.microsoft.com/en-us/library/gg432965.aspx
# .PARAMETER password
# Password you want to encrypt
# .PARAMETER thumbprint
# The thumbprint of the certificate you want your encrypted password linked to
# .PARAMETER thumbprintPath
# The location where your certificate is stored. Default: "\CurrentUser\My"
# .EXAMPLE
# encryptpw -password Test123 -thumbprint 123456789ABCDEF0123456789ABC
#
 
param ([string]$password = "", [string]$thumbprint = "", [string]$thumbprintPath = "")

while($true)
{
    if($password -cmatch '^(?=.*\d)(?=.*[a-z])(?=.*[A-Z]).{6,}$')
    {
        break
    }
    if($password -ne "")
    {
        echo "Password MUST be at least 6 characters long, and has to contain one upper-/lowercase and numeric character"
    }
    $password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($(Read-Host -assecureString "Passwort")))
}


if($thumbprint -eq "")
{
    while((($thumbprint = $(Read-Host "Thumbprint")) -replace " ", "") -inotmatch '[0-9a-f]')
    {
       echo "Malformed thumbprint"
    }
}

$_defaultThumbPath = "\CurrentUser\My"
if($thumbprintPath -eq ""){
    $thumbprintPath = $_defaultThumbPath
}
$thumbprintPath = $thumbprintPath -replace "cert:", ""

if(!(test-path cert:$thumbprintPath\$thumbprint))
{
    echo "Specified certificate couldn't be found! - Abort"
}
else
{
    [Reflection.Assembly]::LoadWithPartialName("System.Security") | out-null 
    $pass = [Text.Encoding]::UTF8.GetBytes($pass)
    $content = new-object Security.Cryptography.Pkcs.ContentInfo –argumentList (,$pass)
    $env = new-object Security.Cryptography.Pkcs.EnvelopedCms $content
    $env.Encrypt((new-object System.Security.Cryptography.Pkcs.CmsRecipient(gi cert:$thumbprintPath\$thumbprint)))
    echo "Encrypted Password: "
    [Convert]::ToBase64String($env.Encode())
}