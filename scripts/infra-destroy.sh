#!/usr/bin/env bash
set -euo pipefail

# BillerJacket Azure Infrastructure Teardown
# Only removes BillerJacket-specific resources — shared resources are left intact.

RESOURCE_GROUP="DefaultResourceGroup-CUS"
SQL_SERVER="dockjacket-api-server"
DB_NAME="billerjacket"
WEBAPP_NAME="billerjacket-web"

echo "=== BillerJacket Infrastructure Teardown ==="
echo ""
echo "This will delete:"
echo "  - SQL Database: $DB_NAME (on $SQL_SERVER)"
echo "  - App Service:  $WEBAPP_NAME"
echo ""
echo "Shared resources (SQL Server, App Service Plan, Resource Group) will NOT be removed."
echo ""
read -p "Are you sure? (y/N) " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
  echo "Aborted."
  exit 0
fi

echo ""

# --------------------------------------------------
# Step 1: Delete App Service
# --------------------------------------------------
echo "[1/2] Deleting App Service '$WEBAPP_NAME'..."
az webapp delete \
  --name "$WEBAPP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --keep-empty-plan

echo "App Service deleted."
echo ""

# --------------------------------------------------
# Step 2: Delete SQL Database
# --------------------------------------------------
echo "[2/2] Deleting SQL Database '$DB_NAME'..."
az sql db delete \
  --name "$DB_NAME" \
  --server "$SQL_SERVER" \
  --resource-group "$RESOURCE_GROUP" \
  --yes

echo "Database deleted."
echo ""

echo "=== Teardown Complete ==="
