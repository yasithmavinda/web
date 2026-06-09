#!/bin/bash
# ════════════════════════════════════════════════════════════════
# azure-setup.sh — One-Time Azure Infrastructure Setup
# ════════════════════════════════════════════════════════════════
#
# WHAT THIS SCRIPT DOES:
# ───────────────────────
# Creates all Azure resources needed to host TaskFlow:
#
#  1. Resource Group        → A folder that holds all your resources
#  2. Azure Container Registry (ACR) → Stores your Docker images
#  3. Azure App Service Plan → The "server" tier (how much CPU/RAM)
#  4. Azure App Service (Backend)  → Hosts your ASP.NET Core API
#  5. Azure App Service (Frontend) → Hosts your React app via Nginx
#  6. Azure SQL Server + Database  → Managed SQL Server in the cloud
#  7. Service Principal            → GitHub Actions deploy credentials
#
# PREREQUISITES:
#   1. Install Azure CLI: https://docs.microsoft.com/cli/azure/install-azure-cli
#   2. Login: az login
#   3. Make executable: chmod +x azure-setup.sh
#   4. Run: ./azure-setup.sh
#
# ── CONFIGURATION ─────────────────────────────────────────────
# ⚠️  Change these values to match your preferences

RESOURCE_GROUP="taskflow-rg"
LOCATION="eastus"                          # Azure region (eastus, westeurope, etc.)
APP_SERVICE_PLAN="taskflow-plan"
BACKEND_APP_NAME="taskflow-api-$(date +%s)"    # Must be globally unique
FRONTEND_APP_NAME="taskflow-web-$(date +%s)"   # Must be globally unique
ACR_NAME="taskflowacr$(date +%s)"             # Must be globally unique, lowercase only
SQL_SERVER_NAME="taskflow-sql-$(date +%s)"    # Must be globally unique
SQL_DB_NAME="TaskFlowDB"
SQL_ADMIN_USER="sqladmin"
SQL_ADMIN_PASSWORD="TaskFlow@Azure2024!"   # ⚠️  Change this!

set -e  # Exit on any error
set -u  # Exit on undefined variables

echo "════════════════════════════════════════"
echo "  TaskFlow Azure Infrastructure Setup   "
echo "════════════════════════════════════════"

# ── STEP 1: Create Resource Group ─────────────────────────────
echo ""
echo "📁 Step 1: Creating Resource Group..."
az group create \
  --name     "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output   table

echo "✅ Resource group created: $RESOURCE_GROUP"

# ── STEP 2: Create Azure Container Registry ───────────────────
echo ""
echo "🐳 Step 2: Creating Container Registry..."
az acr create \
  --resource-group "$RESOURCE_GROUP" \
  --name           "$ACR_NAME" \
  --sku            Basic \
  --admin-enabled  true \
  --output         table

ACR_LOGIN_SERVER=$(az acr show --name "$ACR_NAME" --query loginServer -o tsv)
ACR_USERNAME=$(az acr credential show --name "$ACR_NAME" --query username -o tsv)
ACR_PASSWORD=$(az acr credential show --name "$ACR_NAME" --query "passwords[0].value" -o tsv)

echo "✅ Container Registry: $ACR_LOGIN_SERVER"

# ── STEP 3: Create App Service Plan ──────────────────────────
echo ""
echo "⚙️  Step 3: Creating App Service Plan (B2 tier)..."
az appservice plan create \
  --resource-group "$RESOURCE_GROUP" \
  --name           "$APP_SERVICE_PLAN" \
  --sku            B2 \
  --is-linux \
  --output         table

# Tier options: F1 (Free, no custom domain), B1 ($13/mo), B2 ($27/mo), P1v3 ($73/mo)
echo "✅ App Service Plan created: $APP_SERVICE_PLAN"

# ── STEP 4: Create Backend Web App ───────────────────────────
echo ""
echo "🔧 Step 4: Creating Backend App Service..."
az webapp create \
  --resource-group         "$RESOURCE_GROUP" \
  --plan                   "$APP_SERVICE_PLAN" \
  --name                   "$BACKEND_APP_NAME" \
  --deployment-container-image-name nginx \
  --output                 table

# Enable logging
az webapp log config \
  --resource-group         "$RESOURCE_GROUP" \
  --name                   "$BACKEND_APP_NAME" \
  --application-logging    filesystem \
  --level                  information \
  --web-server-logging     filesystem

# Configure container registry
az webapp config container set \
  --resource-group             "$RESOURCE_GROUP" \
  --name                       "$BACKEND_APP_NAME" \
  --docker-registry-server-url "https://$ACR_LOGIN_SERVER" \
  --docker-registry-server-user "$ACR_USERNAME" \
  --docker-registry-server-password "$ACR_PASSWORD"

echo "✅ Backend: https://$BACKEND_APP_NAME.azurewebsites.net"

# ── STEP 5: Create Frontend Web App ──────────────────────────
echo ""
echo "⚛️  Step 5: Creating Frontend App Service..."
az webapp create \
  --resource-group         "$RESOURCE_GROUP" \
  --plan                   "$APP_SERVICE_PLAN" \
  --name                   "$FRONTEND_APP_NAME" \
  --deployment-container-image-name nginx \
  --output                 table

az webapp config container set \
  --resource-group              "$RESOURCE_GROUP" \
  --name                        "$FRONTEND_APP_NAME" \
  --docker-registry-server-url  "https://$ACR_LOGIN_SERVER" \
  --docker-registry-server-user "$ACR_USERNAME" \
  --docker-registry-server-password "$ACR_PASSWORD"

echo "✅ Frontend: https://$FRONTEND_APP_NAME.azurewebsites.net"

# ── STEP 6: Create Azure SQL Server + Database ───────────────
echo ""
echo "🗄️  Step 6: Creating Azure SQL Database..."
az sql server create \
  --resource-group       "$RESOURCE_GROUP" \
  --name                 "$SQL_SERVER_NAME" \
  --location             "$LOCATION" \
  --admin-user           "$SQL_ADMIN_USER" \
  --admin-password       "$SQL_ADMIN_PASSWORD" \
  --output               table

# Allow Azure services to connect (needed for App Service)
az sql server firewall-rule create \
  --resource-group       "$RESOURCE_GROUP" \
  --server               "$SQL_SERVER_NAME" \
  --name                 AllowAzureServices \
  --start-ip-address     0.0.0.0 \
  --end-ip-address       0.0.0.0

# Create database (S0 = ~$15/mo, Basic = ~$5/mo for dev)
az sql db create \
  --resource-group       "$RESOURCE_GROUP" \
  --server               "$SQL_SERVER_NAME" \
  --name                 "$SQL_DB_NAME" \
  --service-objective    S0 \
  --output               table

SQL_CONNECTION="Server=tcp:$SQL_SERVER_NAME.database.windows.net,1433;Initial Catalog=$SQL_DB_NAME;Persist Security Info=False;User ID=$SQL_ADMIN_USER;Password=$SQL_ADMIN_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

echo "✅ SQL Database created"

# ── STEP 7: Configure Backend Environment Variables ──────────
echo ""
echo "🔑 Step 7: Configuring backend app settings..."
az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" \
  --name           "$BACKEND_APP_NAME" \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    "ConnectionStrings__DefaultConnection=$SQL_CONNECTION" \
    JwtSettings__Issuer=TaskFlow \
    JwtSettings__Audience=TaskFlowApp \
    JwtSettings__AccessTokenMinutes=15 \
    JwtSettings__RefreshTokenDays=7 \
    "AllowedOrigins=https://$FRONTEND_APP_NAME.azurewebsites.net" \
  --output table

echo "⚠️  Remember to set JwtSettings__SecretKey manually (it's a secret!)"
echo "    az webapp config appsettings set --resource-group $RESOURCE_GROUP --name $BACKEND_APP_NAME --settings JwtSettings__SecretKey=YOUR_SECRET"

# ── STEP 8: Create GitHub Actions Service Principal ──────────
echo ""
echo "🔐 Step 8: Creating GitHub Actions credentials..."
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

az ad sp create-for-rbac \
  --name                   "taskflow-github-actions" \
  --role                   Contributor \
  --scopes                 "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP" \
  --sdk-auth \
  > azure-credentials.json

echo "✅ Service principal created!"

# ── PRINT SUMMARY ─────────────────────────────────────────────
echo ""
echo "════════════════════════════════════════════════════════"
echo "  ✅ Azure Setup Complete!"
echo "════════════════════════════════════════════════════════"
echo ""
echo "📋 Add these to GitHub Secrets (Settings → Secrets → Actions):"
echo ""
echo "   Secret Name               Value"
echo "   ─────────────────────     ─────────────────────────────────"
echo "   AZURE_CREDENTIALS        → Contents of azure-credentials.json"
echo "   AZURE_BACKEND_APP        → $BACKEND_APP_NAME"
echo "   AZURE_FRONTEND_APP       → $FRONTEND_APP_NAME"
echo "   AZURE_RESOURCE_GROUP     → $RESOURCE_GROUP"
echo "   AZURE_SQL_CONNECTION     → (shown above)"
echo "   JWT_SECRET               → (generate with: openssl rand -base64 64)"
echo ""
echo "🌐 Your Apps:"
echo "   Frontend:  https://$FRONTEND_APP_NAME.azurewebsites.net"
echo "   Backend:   https://$BACKEND_APP_NAME.azurewebsites.net"
echo "   Swagger:   https://$BACKEND_APP_NAME.azurewebsites.net/swagger"
echo ""
echo "⚠️  IMPORTANT: Delete azure-credentials.json after adding to GitHub Secrets!"
echo "   rm azure-credentials.json"
echo ""
