######################################################
# Author: Dennis Mitchell
# Last Modified: 2019-01-22
# Functionality: Creates service for ApiLauncher
# NOTE: Use USERPROFILE account for service account,
#       and ensure that this account is a local admin
######################################################

#define file/path variables
$api = "EDennis.AspNetCore.ApiLauncher" 
$svcDir = "$ENV:UserProfile\source\services"
$apiSvcDir = "$svcDir\$api"
$apiProjDir = "$ENV:UserProfile\source\repos\$api\$api"
$apiExePath = "$apiSvcDir\$api.exe"

#create the required directories
if (!(Test-Path -path $svcDir)) {
    New-Item $svcDir -Type Directory
}
if (Test-Path -path $apiSvcDir) {
    Remove-Item $apiSvcDir -Recurse 
}
New-Item $apiSvcDir -Type Directory 

#publish the ApiLauncher project
dotnet publish $apiProjDir --configuration Release --output $apiSvcDir

#delete the service, if it already exists
$service = Get-WmiObject -Class Win32_Service -Filter "Name='ApiLauncherService'"
if($service -ne $null){
    $service.delete()
}

#prompt for username and password #NOTE: -Credential $env:UserName may also work
$Credential = $host.ui.PromptForCredential("Need credentials", "Please enter your user name and password.", "", "NetBiosUserName")

#create the service
New-Service -Name "ApiLauncherService" -BinaryPathName "$apiExePath" -Credential $Credential -DisplayName "ApiLauncher Service" -StartupType Automatic -Description "Launches Apis via MQTT messages." -DependsOn NetLogon

#start the service
Start-Service -Name "ApiLauncherService"
