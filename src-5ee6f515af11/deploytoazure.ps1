param ([string]$action = "", [string]$DeployDir = ".\core\Cloud\bin\Debugapp.publish")

echo "Setting up variables..."
$thumbprintInUpperCase = "2ABE7E381CA712923D5A6D7642C68DDC99AC47D0"
$serviceName = "genericworker"
$subid = "88147e04-43f4-48d0-b40c-c34cd2abd03a"
$cert = (get-item cert:\CurrentUser\My\$thumbprintInUpperCase)
$pkg = "$DeployDir\Cloud.cspkg" #build the pkgfile by using the cspack tool that comes with the azure SDK
$cscfg = "$DeployDir\ServiceConfiguration.cscfg"

#$pkg = "C:\Users\t-luwo\dev\genericworker\core\Cloud\bin\Debug\app.publish\Cloud.cspkg" #build the pkgfile by using the cspack tool that comes with the azure SDK
#$cscfg = "C:\Users\t-luwo\dev\genericworker\core\Cloud\bin\Debug\app.publish\ServiceConfiguration.cscfg"
$label = "TestLabelProduction"
$timeout = 3600 #One Hour Timeout

if ((Get-PSSnapin -Name WAPPSCmdlets -ErrorAction SilentlyContinue) -eq $null )
{
    echo "Load Plugin..."
    Add-PsSnapin WAPPSCmdlets
}
else
{
    echo "Plugin already loaded..."
}

echo "Get Hosted Service..."
$hostedservice = Get-HostedService -ServiceName "$serviceName" -SubscriptionId $subid -Certificate $cert

if($action -eq "deploy"){
    echo "Deploy from $pkg..."
    New-Deployment -subscriptionId $subid -certificate $cert -serviceName $serviceName -slot production -package $pkg -configuration $cscfg -name TestDeploy -label $label | Get-OperationStatus –WaitToComplete



    echo "Start up deployment..."
    $hostedservice | Get-Deployment production | Set-DeploymentStatus running | Get-OperationStatus –WaitToComplete

    echo "Wait for Ready roles..."
    $ready = $False
    $startTime = ((Get-Date -UFormat %s) -Replace("[,\.,`n]\d*", ""))
    $timeDiff = 0

    while(!$ready -and ($timeDiff -lt $timeout)){
        if($timeDiff -lt 60){
            echo ("Waiting since {0} seconds..." -f $timeDiff)
        }else{
            echo ("Waiting since {0} minutes..." -f [system.math]::floor($timeDiff/60))
        }
        Start-Sleep -s 60 #sleep for 60 seconds, because it will properly take > 15 minutes
        $deployment = echo $hostedservice | Get-Deployment production
        $ready = ($deployment.RoleInstanceList[0].InstanceStatus -eq "Ready") -and ($deployment.Label -eq $label)
        $timeDiff = (((Get-Date -UFormat %s) -Replace("[,\.,`n]\d*", "")) - $startTime)
    }

    if($timeDiff -ge $timeout){
        echo "Timeout!"
        exit 1
    }else{
        echo "Change Host Switch to Cloud"
        set-itemproperty -path "hklm:\SOFTWARE\Microsoft EMIC\Cloud\VENUS-C" -name "Microsoft.EMIC.Cloud.Development.Host.Switch" -value "Cloud"
    }
}ElseIf ($action -eq "remove"){
    echo "Change Host Switch to Development"
    set-itemproperty -path "hklm:\SOFTWARE\Microsoft EMIC\Cloud\VENUS-C" -name "Microsoft.EMIC.Cloud.Development.Host.Switch" -value "DevelopmentFabric"
    echo "Stopping deployment..."
    $hostedservice | Get-Deployment production | Set-DeploymentStatus suspended | Get-OperationStatus –WaitToComplete
    echo "Remove deployment..."
    $hostedservice | Get-Deployment production | Remove-Deployment | Get-OperationStatus –WaitToComplete
}else{
    echo "Unknown action $action. Use -action [deploy] or [remove]"
}


echo "Finished..."
exit 0
