@description('Region')
param location string = resourceGroup().location

@description('ACA environment resource ID')
param containerAppsEnvironmentId string

@description('Azure Files mount name created in infra.bicep')
param storageMountName string

@description('App Insights connection string')
@secure()
param applicationInsightsConnectionString string

@description('SA password for the SQL container (same you pass to apps)')
@secure()
param sqlAdminPassword string

// ---------------------------------------------------------------------------
// Common handles
// ---------------------------------------------------------------------------
resource env 'Microsoft.App/managedEnvironments@2025-02-02-preview' existing = {
  name: last(split(containerAppsEnvironmentId, '/'))
}

// local logical volume name
var cfgVolName = 'cfgvol'

@description('Returns the Azure Files volume block used by services')
func cfgVolumes(storageMountName string) array => [
  {
    name: cfgVolName
    storageName: storageMountName
    storageType: 'AzureFile'
  }
]

// ---------------------------------------------------------------------------
// SignalR service
// ---------------------------------------------------------------------------
resource signalr 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'signalr'
  location: location
  properties: {
    environmentId: env.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
      }
      secrets: [
        { name: 'sql-password', value: sqlAdminPassword }
      ]
    }
    template: {
      containers: [
        {
          name: 'signalr'
          image: 'ghcr.io/macel94/cloudnativeblazorpong/blazorpong-signalr'
          resources: { cpu: json('1'), memory: '2Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Development' }
            { name: 'ConnectionStrings__Redis', value: 'redis:6379' }
            {
              name: 'ConnectionStrings__AzureSql'
              value: 'Server=azuresql,Database=BlazorpongDB,User=sa,Password=${sqlAdminPassword},TrustServerCertificate=True,Encrypt=False,'
            }
            { name: 'OTEL_EXPORTER_OTLP_ENDPOINT', value: 'http://collector:4317' }
            { name: 'OTEL_EXPORTER_OTLP_PROTOCOL', value: 'grpc' }
            { name: 'OTEL_RESOURCE_ATTRIBUTES', value: 'service.name=signalr' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', secretRef: 'appinsights' }
          ]
        }
      ]
      secrets: [
        { name: 'appinsights', value: applicationInsightsConnectionString }
      ]
    }
  }
}

// ---------------------------------------------------------------------------
// Web front-end
// ---------------------------------------------------------------------------
resource webapp 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'webapp'
  location: location
  dependsOn: [signalr]
  properties: {
    environmentId: env.id
    configuration: {
      ingress: { external: true, targetPort: 8080 }
    }
    template: {
      containers: [
        {
          name: 'webapp'
          image: 'ghcr.io/macel94/cloudnativeblazorpong/blazorpong-web'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Development' }
            { name: 'ASPNETCORE_FORWARDEDHEADERS_ENABLED', value: 'true' }
            { name: 'GameHubEndpoint', value: 'https://${signalr.properties.configuration.ingress.fqdn}/gamehub' }
            { name: 'OTEL_EXPORTER_OTLP_ENDPOINT', value: 'http://collector:4317' }
            { name: 'OTEL_EXPORTER_OTLP_PROTOCOL', value: 'grpc' }
            { name: 'OTEL_RESOURCE_ATTRIBUTES', value: 'service.name=webapp' }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', secretRef: 'appinsights' }
          ]
        }
      ]
      secrets: [{ name: 'appinsights', value: applicationInsightsConnectionString }]
    }
  }
}

// ---------------------------------------------------------------------------
// Redis service
// ---------------------------------------------------------------------------
resource redis 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'redis'
  location: location
  properties: {
    environmentId: env.id
    configuration: { ingress: { external: true, targetPort: 6379 } }
    template: {
      containers: [{ name: 'redis', image: 'redis:latest', resources: { cpu: json('0.5'), memory: '1Gi' } }]
    }
  }
}

// ---------------------------------------------------------------------------
// Azure SQL service
// ---------------------------------------------------------------------------
resource azuresql 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'azuresql'
  location: location
  properties: {
    environmentId: env.id
    configuration: {
      ingress: { external: true, targetPort: 1433 }
      secrets: [{ name: 'sql-password', value: sqlAdminPassword }]
    }
    template: {
      containers: [
        {
          name: 'azuresql'
          image: 'mcr.microsoft.com/mssql/server:2025-latest'
          resources: { cpu: json('2'), memory: '4Gi' }
          env: [
            { name: 'ACCEPT_EULA', value: 'Y' }
            { name: 'MSSQL_SA_PASSWORD', secretRef: 'sql-password' }
          ]
        }
      ]
    }
  }
}

// ---------------------------------------------------------------------------
// Prometheus observer
// ---------------------------------------------------------------------------
resource prometheus 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'prometheus'
  location: location
  properties: {
    environmentId: env.id
    configuration: { ingress: { external: true, targetPort: 9090 } }
    template: {
      containers: [
        {
          name: 'prometheus'
          image: 'prom/prometheus:latest'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          volumeMounts: [{ name: cfgVolName, mountPath: '/etc/prometheus/prometheus.yaml', subPath: 'prometheus.yaml' }]
        }
      ]
      volumes: cfgVolumes(storageMountName)
    }
  }
}

// ---------------------------------------------------------------------------
// OpenTelemetry Collector observer
// ---------------------------------------------------------------------------
resource collector 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'collector'
  location: location
  properties: {
    environmentId: env.id
    configuration: { ingress: { external: true, targetPort: 4317 } }
    template: {
      containers: [
        {
          name: 'collector'
          image: 'otel/opentelemetry-collector-contrib:latest'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          command: ['--config=/etc/collector.yaml']
          volumeMounts: [{ name: cfgVolName, mountPath: '/etc/collector.yaml', subPath: 'otel-collector-config.yaml' }]
        }
      ]
      volumes: cfgVolumes(storageMountName)
    }
  }
}

// ---------------------------------------------------------------------------
// Tempo observer
// ---------------------------------------------------------------------------
resource tempo 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'tempo'
  location: location
  properties: {
    environmentId: env.id
    configuration: { ingress: { external: true, targetPort: 3200 } }
    template: {
      containers: [
        {
          name: 'tempo'
          image: 'grafana/tempo:latest'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          command: ['-config.file=/etc/tempo.yaml']
          volumeMounts: [{ name: cfgVolName, mountPath: '/etc/tempo.yaml', subPath: 'tempo.yaml' }]
        }
      ]
      volumes: cfgVolumes(storageMountName)
    }
  }
}

// ---------------------------------------------------------------------------
// Grafana observer
// ---------------------------------------------------------------------------
resource grafana 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'grafana'
  location: location
  properties: {
    environmentId: env.id
    configuration: { ingress: { external: true, targetPort: 3000 } }
    template: {
      containers: [
        {
          name: 'grafana'
          image: 'grafana/grafana:latest'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [
            { name: 'GF_AUTH_ANONYMOUS_ENABLED', value: 'true' }
            { name: 'GF_AUTH_ANONYMOUS_ORG_ROLE', value: 'Admin' }
            { name: 'GF_AUTH_DISABLE_LOGIN_FORM', value: 'true' }
            { name: 'GF_FEATURE_TOGGLES_ENABLE', value: 'traceqlEditor' }
          ]
          volumeMounts: [
            {
              name: cfgVolName
              mountPath: '/etc/grafana/provisioning/datasources/datasources.yaml'
              subPath: 'grafana-datasources.yaml'
            }
            {
              name: cfgVolName
              mountPath: '/etc/grafana/provisioning/dashboards/dashboards.yaml'
              subPath: 'dashboards.yaml'
            }
            { name: cfgVolName, mountPath: '/etc/grafana/dashboards', subPath: 'dashboards' }
          ]
        }
      ]
      volumes: cfgVolumes(storageMountName)
    }
  }
}

// ---------------------------------------------------------------------------
// Loki observer
// ---------------------------------------------------------------------------
resource loki 'Microsoft.App/containerApps@2025-02-02-preview' = {
  name: 'loki'
  location: location
  properties: {
    environmentId: env.id
    configuration: { ingress: { external: true, targetPort: 3100 } }
    template: {
      containers: [
        {
          name: 'loki'
          image: 'grafana/loki:latest'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          command: ['-config.file=/etc/loki.yaml']
          volumeMounts: [{ name: cfgVolName, mountPath: '/etc/loki.yaml', subPath: 'loki.yaml' }]
        }
      ]
      volumes: cfgVolumes(storageMountName)
    }
  }
}

// ---------------------------------------------------------------------------
// DB initialization job
// ---------------------------------------------------------------------------
resource dbInit 'Microsoft.App/jobs@2025-02-02-preview' = {
  name: 'azuresql-init'
  location: location
  dependsOn: [azuresql]
  properties: {
    environmentId: env.id
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 1800
      secrets: [{ name: 'sql-password', value: sqlAdminPassword }]
    }
    template: {
      containers: [
        {
          name: 'init'
          image: 'mcr.microsoft.com/mssql-tools'
          command: ['/usr/local/bin/azuresql-init.sh']
          resources: { cpu: json('0.5'), memory: '1Gi' }
          env: [{ name: 'SA_PASSWORD', secretRef: 'sql-password' }]
          volumeMounts: [
            { name: cfgVolName, mountPath: '/usr/local/bin/azuresql-init.sh', subPath: 'azuresql-init.sh' }
            { name: cfgVolName, mountPath: '/tmp/db.dacpac', subPath: 'Blazorpong.DB.AzureSql.dacpac' }
          ]
        }
      ]
      volumes: cfgVolumes(storageMountName)
    }
  }
}
