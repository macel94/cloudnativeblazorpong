#!/usr/bin/env bash
set -euo pipefail

echo '🔧 Initializing PostgreSQL…'

# Wait for PostgreSQL to be ready
until PGPASSWORD="$POSTGRES_PASSWORD" psql -h postgres -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "SELECT 1" > /dev/null 2>&1; do
  echo '⏳ Waiting for PostgreSQL…'
  sleep 2
done
echo '✅ PostgreSQL is up.'

# Create the schema
echo '📦 Creating database schema…'
PGPASSWORD="$POSTGRES_PASSWORD" psql -h postgres -U "$POSTGRES_USER" -d "$POSTGRES_DB" <<-EOSQL
  -- Enable UUID extension
  CREATE EXTENSION IF NOT EXISTS "pgcrypto";

  -- Create Room table
  CREATE TABLE IF NOT EXISTS room (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ServerName" VARCHAR(50) NULL
  );

  -- Create Client table
  CREATE TABLE IF NOT EXISTS client (
    "Username" VARCHAR(50) NOT NULL,
    "RoomId" UUID NOT NULL,
    "Role" SMALLINT NULL,
    "ConnectionId" VARCHAR(50) NOT NULL,
    PRIMARY KEY ("Username", "ConnectionId"),
    CONSTRAINT fk_client_room FOREIGN KEY ("RoomId") REFERENCES room(id) ON DELETE CASCADE
  );
EOSQL

echo '🎉 Done.'
