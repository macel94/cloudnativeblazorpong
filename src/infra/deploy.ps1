<#
.SYNOPSIS
  Deploys infra.bicep then services.bicep using Azure CLI in PowerShell Core.

.DESCRIPTION
  1. Ensures the resource group exists.
  2. Deploys infra.bicep, captures its outputs.
  3. Deploys services.bicep, wiring infra outputs into its parameters.

.PARAMETER ResourceGroupName
  Name of the resource group to create/use.

.PARAMETER Location
  Azure region (e.g. westeurope).

.PARAMETER BaseName
  (Optional) Base name for infra resources. Defaults to “cnblazorpong<random>”.

.PARAMETER SqlAdminPassword
  SecureString SA password for SQL; passed into services.bicep.

.EXAMPLE
  # Prompt for SQL password securely:
  $pwd = Read-Host -AsSecureString "Enter SA password"
  .\deploy.ps1 -ResourceGroupName rg-blazorpong -Location westeurope -SqlAdminPassword $pwd
#>

param(
    [Parameter(Mandatory)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory)]
    [string]$Location,

    [string]$BaseName = "cnblazorpong$(Get-Random)",

    [Parameter(Mandatory)]
    [securestring]$SqlAdminPassword
)

#–– Check Azure CLI installed ––
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI (az) not found. Please install it: https://aka.ms/InstallAzureCli"
    exit 1
}

#–– Ensure logged in ––
try {
    az account show --output none
} catch {
    Write-Host "Not logged in. Running az login…" -ForegroundColor Yellow
    az login | Out-Null
}

#–– Ensure Resource Group exists ––
$rgExists = az group exists --name $ResourceGroupName | ConvertFrom-Json
if (-not $rgExists) {
    Write-Host "Creating resource group '$ResourceGroupName' in $Location…" -ForegroundColor Cyan
    az group create --name $ResourceGroupName --location $Location | Out-Null
}

#–– Convert SecureString to plain text for CLI ––
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword)
$sqlPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto($ptr)

#–– 1) Deploy infra.bicep ––
Write-Host "🚀 Deploying infra.bicep…" -ForegroundColor Cyan
$infraOutputs = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file ./infra.bicep `
    --parameters location=$Location baseName=$BaseName `
    --query properties.outputs `
    --output json | ConvertFrom-Json

$envId    = $infraOutputs.containerAppsEnvironmentId.value
$mount    = $infraOutputs.storageMountName.value
$appins   = $infraOutputs.applicationInsightsConnectionString.value

Write-Host "✅ Infra deployed."
Write-Host " • containerAppsEnvironmentId = $envId"
Write-Host " • storageMountName           = $mount"
Write-Host " • applicationInsightsConnStr = $($appins.Substring(0,40))…"

#–– 2) Deploy services.bicep ––
Write-Host "🚀 Deploying services.bicep…" -ForegroundColor Cyan
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

Write-Host "🎉 All done! Services are up in '$ResourceGroupName'." -ForegroundColor Green