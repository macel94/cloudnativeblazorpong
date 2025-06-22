<#
.SYNOPSIS
  Deploys infra.bicep, uploads your repo into the Azure Files share, then deploys services.bicep.

.PARAMETER ResourceGroupName
  Name of the resource group to create/use.

.PARAMETER Location
  Azure region (e.g. westeurope).

.PARAMETER BaseName
  (Optional) Base name for infra resources. Defaults to “cnblazorpong<random>”.

.PARAMETER SqlAdminPassword
  SecureString SA password for SQL; passed into services.bicep.

.PARAMETER RepoPath
  Path to the local Git repo (defaults to current directory).
#>

param(
  [Parameter(Mandatory)]
  [string]$ResourceGroupName,

  [Parameter(Mandatory)]
  [string]$Location,

  [string]$BaseName = "cnblazorpong$(Get-Random)",

  [Parameter(Mandatory)]
  [SecureString]$SqlAdminPassword,

  [string]$RepoPath = "./.."
)

#–– Verify Azure CLI ––
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
  Write-Error "Azure CLI (az) not found. Install it: https://aka.ms/InstallAzureCli"
  exit 1
}

#–– Ensure logged in ––
try { az account show --output none } 
catch {
  Write-Host "Logging into Azure…" -ForegroundColor Yellow
  az login | Out-Null
}

#–– Ensure Resource Group exists ––
if (-not (az group exists --name $ResourceGroupName | ConvertFrom-Json)) {
  Write-Host "Creating RG '$ResourceGroupName' in $Location…" -ForegroundColor Cyan
  az group create --name $ResourceGroupName --location $Location | Out-Null
}

#–– Plain-text SQL password for CLI ––
$ptr      = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword)
$sqlPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto($ptr)

#–– 1) Deploy infra.bicep ––
Write-Host "`n🚀 Deploying infra.bicep…" -ForegroundColor Cyan
if($PSScriptRoot) {
  $infraPath = Join-Path $PSScriptRoot "infra.bicep"
}
else{
  $infraPath = "./infra.bicep"
}

Write-Host "Using infra.bicep from: $infraPath"

if (-not (Test-Path $infraPath)) {
  Write-Error "Infra Bicep file not found at '$infraPath'."
  exit 1
}

$infraOutputs = az deployment group create `
  --resource-group $ResourceGroupName `
  --template-file $infraPath `
  --parameters location=$Location baseName=$BaseName `
  --query properties.outputs `
  --output json | ConvertFrom-Json

$envId  = $infraOutputs.containerAppsEnvironmentId.value
$mount  = $infraOutputs.storageMountName.value
$appins = $infraOutputs.applicationInsightsConnectionString.value

Write-Host "✅ Infra deployed."
Write-Host " • containerAppsEnvironmentId = $envId"
Write-Host " • storageMountName           = $mount"
if ($appins) {
  Write-Host " • appInsightsConnStr         = $($appins.Substring(0, [Math]::Min(40, $appins.Length)))…"
} else {
  Write-Error "Application Insights connection string not found in outputs."
  exit 1
}

#–– 2) Upload entire repo into the File share ––
$saName    = "${BaseName}sa"

# Get storage account key
$key = az storage account keys list `
  --resource-group $ResourceGroupName `
  --account-name $saName `
  --query "[0].value" -o tsv

#–– 3) Deploy services.bicep ––
Write-Host "`n🚀 Deploying services.bicep…" -ForegroundColor Cyan
az deployment group create `
  --resource-group $ResourceGroupName `
  --template-file "$PSScriptRoot/services.bicep" `
  --parameters `
      location=$Location `
      containerAppsEnvironmentId=$envId `
      storageMountName=$mount `
      applicationInsightsConnectionString="$appins" `
      sqlAdminPassword="$sqlPlain" `
  --output none

Write-Host "`n🎉 All done! Your infra, files and services are live in '$ResourceGroupName'." -ForegroundColor Green
