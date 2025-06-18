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

  [string]$RepoPath = ".\.."
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
$infraOutputs = az deployment group create `
  --resource-group $ResourceGroupName `
  --template-file ./infra.bicep `
  --parameters location=$Location baseName=$BaseName `
  --query properties.outputs `
  --output json | ConvertFrom-Json

$envId  = $infraOutputs.containerAppsEnvironmentId.value
$mount  = $infraOutputs.storageMountName.value
$appins = $infraOutputs.applicationInsightsConnectionString.value

Write-Host "✅ Infra deployed."
Write-Host " • containerAppsEnvironmentId = $envId"
Write-Host " • storageMountName           = $mount"
Write-Host " • appInsightsConnStr         = $($appins.Substring(0,40))…"

#–– 2) Upload entire repo into the File share ––
$saName    = "${BaseName}sa"
$shareName = "configurations"
$fullPath  = Resolve-Path $RepoPath

Write-Host "`n📂 Uploading '$fullPath' → file share '$shareName' in storage account '$saName'…" -ForegroundColor Cyan

# Get storage account key
$key = az storage account keys list `
  --resource-group $ResourceGroupName `
  --account-name $saName `
  --query "[0].value" -o tsv

# Batch-upload all files (preserves folder structure)
az storage file upload-batch `
  --account-name $saName `
  --account-key  $key `
  --destination   $shareName `
  --source        $fullPath `
  --overwrite     true

Write-Host "✅ Files uploaded to Azure Files."

#–– 3) Deploy services.bicep ––
Write-Host "`n🚀 Deploying services.bicep…" -ForegroundColor Cyan
az deployment group create `
  --resource-group $ResourceGroupName `
  --template-file ./services.bicep `
  --parameters `
      location=$Location `
      containerAppsEnvironmentId=$envId `
      storageMountName=$mount `
      applicationInsightsConnectionString="$appins" `
      sqlAdminPassword="$sqlPlain" `
  --output none

Write-Host "`n🎉 All done! Your infra, files and services are live in '$ResourceGroupName'." -ForegroundColor Green
