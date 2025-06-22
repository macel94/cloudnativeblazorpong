@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Environment base name')
param baseName string = 'cnblazorpong${uniqueString(resourceGroup().id)}'

var logAnalyticsWorkspaceName    = '${baseName}-logs'
var applicationInsightsName      = '${baseName}-insights'
var containerAppsEnvironmentName = '${baseName}-env'
var storageAccountName           = '${baseName}sa'
var fileShareName                = 'configurations'
var storageMountName             = 'configfiles'   // <- logical mount name inside ACA

// ---- Logs & APM ------------------------------------------------------------
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2025-02-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ---- Storage ---------------------------------------------------------------
resource sa 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource fileSvc 'Microsoft.Storage/storageAccounts/fileServices@2024-01-01' = {
  parent: sa
  name: 'default'
}

resource fileShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2024-01-01' = {
  parent: fileSvc
  name: fileShareName
  properties: { shareQuota: 5 }  // GiB
}

// ---- ACA Environment -------------------------------------------------------
resource env 'Microsoft.App/managedEnvironments@2025-02-02-preview' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey:  logAnalytics.listKeys().primarySharedKey
      }
    }
    workloadProfiles: [
      // Consumption only – fine for dev
      { name: 'Consumption', workloadProfileType: 'Consumption' }
    ]
  }
}

// Azure Files mount – the *only* correct place for it
resource envStorage 'Microsoft.App/managedEnvironments/storages@2025-02-02-preview' = {
  parent: env
  name: storageMountName           // logical name referenced by apps
  properties: {
    azureFile: {
      accountName: sa.name
      accountKey:  sa.listKeys().keys[0].value
      shareName:   fileShare.name
      accessMode:  'ReadWrite'
    }
  }
}

// ---------------------------------------------------------------------------
// Outputs consumed by services.bicep
// ---------------------------------------------------------------------------
output containerAppsEnvironmentId          string = env.id
output storageMountName                    string = envStorage.name
output applicationInsightsConnectionString string = appInsights.properties.ConnectionString
output sqlAdminPasswordHint                string = 'Must be supplied when deploying services.bicep'
