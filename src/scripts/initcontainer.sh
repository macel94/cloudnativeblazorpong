#!/usr/bin/env bash
set -euo pipefail

echo '🔧 Initializing Azure SQL…'

# 0) Check SA_PASSWORD
if [[ -z "${SA_PASSWORD:-}" ]]; then
  echo '❌ SA_PASSWORD is not set. Please set it.' >&2
  exit 1
fi
echo '🔑 SA_PASSWORD is set.'

# 1) Wait for SQL Server
until /opt/mssql-tools/bin/sqlcmd -S azuresql -U sa -P "$SA_PASSWORD" -Q "SELECT 1"; do
  echo '⏳ Waiting for SQL Server…'
  sleep 2
done
echo '✅ SQL Server is up.'

. /etc/os-release
case "$VERSION_CODENAME" in
  buster)   ICU_PKG=libicu63 ;;
  bullseye) ICU_PKG=libicu67 ;;
  bookworm) ICU_PKG=libicu72 ;;
  *)        ICU_PKG=libicu-dev ;;  # fallback
esac

echo '⬇️ Installing sqlpackage dependencies…'
apt-get update \
  && apt-get install -y \
       curl \
       apt-transport-https \
       unzip \
       libunwind8 \
       "$ICU_PKG"

# 2) Download and install sqlpackage if not already installed
if [[ ! -f /opt/sqlpackage/sqlpackage ]]; then
  echo '🔍 sqlpackage not found, downloading…'
  curl -SL -o /tmp/sqlpackage.zip https://aka.ms/sqlpackage-linux
  unzip -q /tmp/sqlpackage.zip -d /opt/sqlpackage
  chmod +x /opt/sqlpackage/sqlpackage
else
  echo '✅ sqlpackage already installed.'
fi

# Publish the DACPAC
echo '📦 Publishing DACPAC…'
/opt/sqlpackage/sqlpackage \
  /Action:Publish \
  /SourceFile:/tmp/yourdb.dacpac \
  /TargetConnectionString:"Server=azuresql;Initial Catalog=BlazorpongDB;User ID=sa;Password=${SA_PASSWORD};Encrypt=False;TrustServerCertificate=True"

echo '🎉 Done.'
