#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Deploys Docker Compose to Azure Container Apps - Everything containerized
.DESCRIPTION
    Simplified deployment script running all services in Container Apps including Redis
.PARAMETER ResourceGroupName
    The name of the Azure Resource Group
.PARAMETER Location
    Azure region for deployment
.PARAMETER ComposePath
    Path to docker-compose.yml file
.PARAMETER AppName
    Base name for the container app (default: blazorpong)
.EXAMPLE
    .\Deploy-ContainerApps-All-In-One.ps1 -ResourceGroupName "rg-blazorpong-dev" -Location "swedencentral"
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("swedencentral", "italynorth", "northeurope")]
    [string]$Location,
    
    [Parameter(Mandatory = $false)]
    [string]$ComposePath = "./docker-compose.yml",
    
    [Parameter(Mandatory = $false)]
    [string]$AppName = "blazorpong",

    [Parameter(Mandatory = $false)]
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Dev configuration - everything containerized
$script:DevConfig = @{
    LogRetentionDays = 30  # Minimum retention for cost
}

$script:ResourceNames = @{
    ContainerEnv = "cae-$AppName-dev"
    LogWorkspace = "law-$AppName-dev"
}

function Write-Step {
    param([string]$Message)
    Write-Host "`n=== $Message ==="
}

function Test-Prerequisites {
    Write-Step "Validating Prerequisites"
    
    if ($PSVersionTable.PSVersion.Major -lt 7) {
        throw "PowerShell Core 7.0+ required"
    }
    
    try {
        $azVersion = az version --output json | ConvertFrom-Json
        Write-Host "✓ Azure CLI: $($azVersion.'azure-cli')"
        
        # Ensure containerapp extension supports compose
        $caExtension = az extension list --query "[?name=='containerapp']" | ConvertFrom-Json
        if (-not $caExtension -or [version]$(([string]$caExtension[0].version).replace("b","")) -lt [version]"0.3.0") {
            Write-Host "Updating containerapp extension..."
            az extension add --name containerapp --upgrade --only-show-errors
        }
    }
    catch {
        throw "Azure CLI not found. Please install Azure CLI. Error: $($_.Exception.Message)"
    }
    
    if (-not (Test-Path $ComposePath)) {
        throw "Docker Compose file not found: $ComposePath"
    }
    
    $composeContent = Get-Content $ComposePath -Raw
    if ($composeContent -notmatch "services:") {
        throw "Invalid Docker Compose file format"
    }
    Write-Host "✓ Docker Compose file validated"
}

function Initialize-Azure {
    Write-Step "Initializing Azure Context"
    
    $currentSub = az account show --query "name" -o tsv 2>$null
    if (-not $currentSub) {
        throw "Not logged into Azure. Run 'az login' first."
    }
    Write-Host "✓ Subscription: $currentSub"
    
    # Register only required providers
    $providers = @("Microsoft.App", "Microsoft.OperationalInsights")
    $providers | ForEach-Object {
        az provider register --namespace $_ --only-show-errors
    }
}

function New-ResourceGroup {
    Write-Step "Setting up Resource Group"
    
    $exists = az group exists --name $ResourceGroupName
    if ($exists -eq "false") {
        if ($PSCmdlet.ShouldProcess($ResourceGroupName, "Create Resource Group")) {
            az group create --name $ResourceGroupName --location $Location --only-show-errors
            Write-Host "✓ Created: $ResourceGroupName"
        }
    } else {
        Write-Host "✓ Using existing: $ResourceGroupName"
    }
}

function New-LogAnalyticsWorkspace {
    Write-Step "Creating Log Analytics Workspace"
    
    $workspaceName = $script:ResourceNames.LogWorkspace
    
    if ($PSCmdlet.ShouldProcess($workspaceName, "Create Log Analytics")) {
        $workspace = az monitor log-analytics workspace create `
            --resource-group $ResourceGroupName `
            --workspace-name $workspaceName `
            --location $Location `
            --retention-time $script:DevConfig.LogRetentionDays `
            --query "customerId" -o tsv --only-show-errors
        
        Write-Host "✓ Created Log Analytics: $workspaceName ($($script:DevConfig.LogRetentionDays)-day retention)"
        return $workspace
    }
}

function New-ContainerAppsEnvironment {
    param([string]$WorkspaceId)
    
    Write-Step "Creating Container Apps Environment"
    
    $envName = $script:ResourceNames.ContainerEnv
    
    if ($PSCmdlet.ShouldProcess($envName, "Create Container Apps Environment")) {
        az containerapp env create `
            --name $envName `
            --resource-group $ResourceGroupName `
            --location $Location `
            --logs-workspace-id $WorkspaceId `
            --only-show-errors
        
        Write-Host "✓ Created Container Apps Environment: $envName"
        return $envName
    }
}

function Deploy-DockerCompose {
    param([string]$EnvironmentName)
    
    Write-Step "Deploying Docker Compose to Container Apps"
    
    if ($PSCmdlet.ShouldProcess($ComposePath, "Deploy Docker Compose")) {
        Write-Host "Using native Azure Container Apps compose support..."
        
        try {
            az containerapp compose create `
                --compose-file-path $ComposePath `
                --resource-group $ResourceGroupName `
                --environment $EnvironmentName `
                --only-show-errors
            
            Write-Host "✓ Docker Compose deployed successfully"
            
            # Show deployed apps
            $apps = az containerapp list --resource-group $ResourceGroupName --query "[].{name:name,fqdn:properties.configuration.ingress.fqdn}" -o json | ConvertFrom-Json
            
            Write-Host "`nDeployed Applications:"
            foreach ($app in $apps) {
                if ($app.fqdn) {
                    Write-Host "  $($app.name): https://$($app.fqdn)"
                } else {
                    Write-Host "  $($app.name): (internal only)"
                }
            }
            
        }
        catch {
            Write-Host "Failed to deploy compose file: $($_.Exception.Message)"
            Write-Host "This might be due to compose file format - check the backup file if needed"
            throw
        }
    }
}

function Show-Summary {
    Write-Step "Deployment Summary"
    
    Write-Host "Environment: DEV (all containerized)"
    Write-Host "Resource Group: $ResourceGroupName"
    Write-Host "Location: $Location"
    
    # Show container apps with URLs
    $apps = az containerapp list --resource-group $ResourceGroupName --query "[].{name:name,fqdn:properties.configuration.ingress.fqdn}" -o json | ConvertFrom-Json
    
    Write-Host "`n📱 Container Apps:"
    foreach ($app in $apps) {
        if ($app.fqdn) {
            Write-Host "  $($app.name): https://$($app.fqdn)"
        } else {
            Write-Host "  $($app.name): (internal)"
        }
    }
    
    Write-Host "`n🔧 Services:"
    Write-Host "  Redis: Running as container app (internal)"
    Write-Host "  Log Analytics: $($script:ResourceNames.LogWorkspace) ($($script:DevConfig.LogRetentionDays) retention)"
    
    Write-Host "`n🗑️  Cleanup:"
    Write-Host "  az group delete --name $ResourceGroupName --yes --no-wait"
}

function Restore-ComposeBackup {
    Write-Host "`nLooking for compose file backups..."
    $backups = Get-ChildItem -Path "." -Filter "*.backup-*" | Where-Object { $_.Name -like "*docker-compose*" }
    
    if ($backups) {
        Write-Host "Available backups:"
        $backups | ForEach-Object { Write-Host "  $($_.Name)" }
        Write-Host "To restore: Copy-Item '<backup-name>' '$ComposePath'"
    }
}

# Main execution
function Invoke-Main {
    try {
        Write-Host "🚀 Azure Container Apps All-In-One Deployment"
        Write-Host "App: $AppName | Location: $Location"
        Write-Host "Strategy: Everything containerized (including Redis)"
        
        if ($DryRun) {
            Write-Host "🔍 DRY RUN MODE - No resources will be created"
            $WhatIfPreference = $true
        }
        
        Test-Prerequisites
        Initialize-Azure
        New-ResourceGroup
        
        $workspaceId = New-LogAnalyticsWorkspace
        $environmentName = New-ContainerAppsEnvironment -WorkspaceId $workspaceId
        
        Deploy-DockerCompose -EnvironmentName $environmentName
        
        Show-Summary
        
        Write-Host "`n✅ Deployment completed successfully!"
        Write-Host "⏱️  Total time: $((Get-Date) - $script:StartTime)"
        
    }
    catch {
        Write-Host "`n❌ Deployment failed: $($_.Exception.Message)"
        Write-Host "💡 Check Azure portal for any partial deployments"
        Restore-ComposeBackup
        exit 1
    }
}

$script:StartTime = Get-Date

if ($MyInvocation.InvocationName -ne '.') {
    Invoke-Main
}