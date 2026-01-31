#!/usr/bin/env bash
set -euo pipefail

# BillerJacket Azure Infrastructure Provisioning
# Reuses existing resources where possible:
#   - SQL Server: dockjacket-api-server (Central US)
#   - App Service Plan: docjacket-app (Central US, Linux)
#   - Resource Group: DefaultResourceGroup-CUS

RESOURCE_GROUP="DefaultResourceGroup-CUS"
SQL_SERVER="dockjacket-api-server"
DB_NAME="billerjacket"
WEBAPP_NAME="billerjacket-web"
APP_PLAN="docjacket-app"
RUNTIME="DOTNETCORE:10.0"

echo "=== BillerJacket Infrastructure Provisioning ==="
echo ""

# --------------------------------------------------
# Step 1: Create SQL Database
# --------------------------------------------------
echo "[1/4] Creating SQL Database '$DB_NAME' on server '$SQL_SERVER'..."
az sql db create \
  --name "$DB_NAME" \
  --server "$SQL_SERVER" \
  --resource-group "$RESOURCE_GROUP" \
  --service-objective Basic \
  --max-size 2GB \
  --output table

echo "Database created."
echo ""

# --------------------------------------------------
# Step 2: Create App Service
# --------------------------------------------------
echo "[2/4] Creating App Service '$WEBAPP_NAME' on plan '$APP_PLAN'..."
az webapp create \
  --name "$WEBAPP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --plan "$APP_PLAN" \
  --runtime "$RUNTIME" \
  --output table

echo "App Service created."
echo ""

# --------------------------------------------------
# Step 3: Enable Managed Identity
# --------------------------------------------------
echo "[3/4] Assigning Managed Identity to '$WEBAPP_NAME'..."
IDENTITY_OUTPUT=$(az webapp identity assign \
  --name "$WEBAPP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --output json)

PRINCIPAL_ID=$(echo "$IDENTITY_OUTPUT" | jq -r '.principalId')
echo "Managed Identity assigned. Principal ID: $PRINCIPAL_ID"
echo ""

# --------------------------------------------------
# Step 4: Set connection string
# --------------------------------------------------
echo "[4/4] Setting connection string..."
echo ""
echo "NOTE: You must provide the SQL admin password."
echo "Run the following command manually, replacing <password>:"
echo ""
echo "  az webapp config connection-string set \\"
echo "    --name $WEBAPP_NAME \\"
echo "    --resource-group $RESOURCE_GROUP \\"
echo "    --connection-string-type SQLAzure \\"
echo "    --settings DefaultConnection=\"Server=tcp:$SQL_SERVER.database.windows.net,1433;Initial Catalog=$DB_NAME;Persist Security Info=False;User ID=dockjacket-api-server-admin;Password=<password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;\""
echo ""

# --------------------------------------------------
# Verification
# --------------------------------------------------
echo "=== Verification ==="
echo ""
echo "Checking database..."
az sql db show \
  --name "$DB_NAME" \
  --server "$SQL_SERVER" \
  --resource-group "$RESOURCE_GROUP" \
  --query "{Name:name, Status:status, Tier:currentServiceObjectiveName, MaxSize:maxSizeBytes}" \
  --output table

echo ""
echo "Checking web app..."
az webapp show \
  --name "$WEBAPP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "{Name:name, State:state, DefaultHostName:defaultHostName}" \
  --output table

echo ""
echo "=== Done ==="
echo ""
echo "Next steps:"
echo "  1. Set the connection string (see command above)"
echo "  2. Configure Key Vault access if needed:"
echo "     az role assignment create --role 'Key Vault Secrets User' --assignee $PRINCIPAL_ID --scope <keyvault-resource-id>"
echo "  3. For local dev, create the billerjacket database in Docker:"
echo "     docker exec -it roofingjacket-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'RoofJacket_Dev123!' -C -Q 'CREATE DATABASE billerjacket'"
echo "  4. Run EF Core migrations against the new database"
