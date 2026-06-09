#!/bin/bash
# ════════════════════════════════════════════════════════════════
# dev-start.ps1 / dev-start.sh — Local Development Quick Start
# ════════════════════════════════════════════════════════════════
# Run this script once to start the entire stack:
#   Windows:  .\scripts\dev-start.ps1
#   Mac/Linux: bash scripts/dev-start.sh

set -e

# Colors
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo ""
echo -e "${CYAN}╔═══════════════════════════════════════╗${NC}"
echo -e "${CYAN}║     TaskFlow — Dev Environment         ║${NC}"
echo -e "${CYAN}╚═══════════════════════════════════════╝${NC}"
echo ""

# ── Step 1: Check Docker is running ──────────────────────────
echo -e "${CYAN}[1/5]${NC} Checking Docker..."
if ! docker info > /dev/null 2>&1; then
    echo -e "${RED}❌ Docker is not running! Please start Docker Desktop first.${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Docker is running${NC}"

# ── Step 2: Create .env if not exists ────────────────────────
echo -e "${CYAN}[2/5]${NC} Setting up environment..."
if [ ! -f ".env" ]; then
    cp .env.example .env
    echo -e "${YELLOW}📄 Created .env from .env.example${NC}"
    echo -e "${YELLOW}   Edit .env to set your secrets before production!${NC}"
else
    echo -e "${GREEN}✅ .env already exists${NC}"
fi

# ── Step 3: Install frontend dependencies ───────────────────
echo -e "${CYAN}[3/5]${NC} Installing frontend dependencies..."
if [ -d "frontend/node_modules" ]; then
    echo -e "${GREEN}✅ node_modules already installed${NC}"
else
    cd frontend && npm install --silent && cd ..
    echo -e "${GREEN}✅ Frontend dependencies installed${NC}"
fi

# ── Step 4: Start containers ─────────────────────────────────
echo -e "${CYAN}[4/5]${NC} Starting Docker containers..."
docker compose -f docker-compose.yml up -d --build

# ── Step 5: Wait for health checks ──────────────────────────
echo -e "${CYAN}[5/5]${NC} Waiting for services to be healthy..."
echo -n "   SQL Server "
for i in {1..30}; do
    if docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd \
        -S localhost -U sa -P "${SQL_SA_PASSWORD:-TaskFlow@Dev123!}" \
        -Q "SELECT 1" -No -C > /dev/null 2>&1; then
        echo -e " ${GREEN}✅${NC}"
        break
    fi
    echo -n "."
    sleep 2
done

echo -n "   Backend    "
for i in {1..20}; do
    if curl -sf http://localhost:5000/health > /dev/null 2>&1; then
        echo -e " ${GREEN}✅${NC}"
        break
    fi
    echo -n "."
    sleep 3
done

echo -n "   Frontend   "
for i in {1..10}; do
    if curl -sf http://localhost/health.html > /dev/null 2>&1; then
        echo -e " ${GREEN}✅${NC}"
        break
    fi
    echo -n "."
    sleep 2
done

# ── Summary ───────────────────────────────────────────────────
echo ""
echo -e "${GREEN}╔═══════════════════════════════════════╗${NC}"
echo -e "${GREEN}║   ✅ TaskFlow is running!              ║${NC}"
echo -e "${GREEN}╚═══════════════════════════════════════╝${NC}"
echo ""
echo -e "   🌐 App (Frontend):   ${CYAN}http://localhost${NC}"
echo -e "   🔧 API (Backend):    ${CYAN}http://localhost:5000${NC}"
echo -e "   📖 Swagger UI:       ${CYAN}http://localhost:5000/swagger${NC}"
echo -e "   🗄️  Database:         ${CYAN}localhost:1433${NC}"
echo ""
echo -e "   📧 Default Login:    ${YELLOW}admin@taskflow.com / Admin@123!${NC}"
echo ""
echo -e "   Stop with:          ${CYAN}docker compose down${NC}"
echo -e "   View logs with:     ${CYAN}docker compose logs -f${NC}"
echo ""
