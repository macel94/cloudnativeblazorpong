#!/bin/bash
# Wait for the SQL Server to come up
#sleep 15s

# Deploy DACPAC to the database
#/opt/sqlpackage/sqlpackage /a:Publish /tsn:azuresql /tdn:pongfreesqlserver /tu:sa /tp:$SA_PASSWORD /sf:/v0.dacpac