# ════════════════════════════════════════════════════════════════
# Makefile — TaskFlow Developer Commands
# ════════════════════════════════════════════════════════════════
#
# WHAT IS A MAKEFILE?
# ────────────────────
# A collection of shortcuts for complex commands.
# Instead of typing a long docker command, just type:
#   make dev       ← start the whole stack
#   make stop      ← stop everything
#   make logs      ← see all logs
#   make clean     ← delete containers and volumes
#
# Run: make help  to see all available commands

.PHONY: help dev stop restart logs clean build push migrate seed test lint health

# Default target when you just type "make"
.DEFAULT_GOAL := help

# ── Colors for terminal output ──────────────────────────────────
CYAN  := \033[36m
GREEN := \033[32m
YELLOW:= \033[33m
RED   := \033[31m
RESET := \033[0m
BOLD  := \033[1m

# ── Configuration ───────────────────────────────────────────────
COMPOSE         := docker compose
COMPOSE_FILE    := docker-compose.yml
COMPOSE_PROD    := docker-compose.yml -f docker-compose.prod.yml
BACKEND_SERVICE := backend
FRONTEND_SERVICE:= frontend
DB_SERVICE      := sqlserver

# ════════════════════════════════════════════════════════════════
# HELP
# ════════════════════════════════════════════════════════════════
help: ## Show this help message
	@echo ""
	@echo "$(BOLD)$(CYAN)TaskFlow — Developer Commands$(RESET)"
	@echo "══════════════════════════════════════════"
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ":.*?## "}; {printf "  $(CYAN)%-18s$(RESET) %s\n", $$1, $$2}'
	@echo ""

# ════════════════════════════════════════════════════════════════
# DEVELOPMENT
# ════════════════════════════════════════════════════════════════
dev: ## 🚀 Start the full stack (database + backend + frontend)
	@echo "$(GREEN)Starting TaskFlow...$(RESET)"
	@cp -n .env.example .env 2>/dev/null || true
	$(COMPOSE) -f $(COMPOSE_FILE) up -d
	@echo ""
	@echo "$(GREEN)✅ TaskFlow is running!$(RESET)"
	@echo "   🌐 Frontend : http://localhost"
	@echo "   🔧 Backend  : http://localhost:5000"
	@echo "   📖 Swagger  : http://localhost:5000/swagger"
	@echo "   🗄️  Database : localhost:1433"
	@echo ""

dev-build: ## 🔨 Rebuild images then start (use after code changes)
	$(COMPOSE) -f $(COMPOSE_FILE) up -d --build

stop: ## ⏹️  Stop all containers (keep data)
	$(COMPOSE) -f $(COMPOSE_FILE) down
	@echo "$(YELLOW)Containers stopped. Data preserved.$(RESET)"

restart: stop dev ## ♻️  Stop then start all containers

# ════════════════════════════════════════════════════════════════
# LOGS
# ════════════════════════════════════════════════════════════════
logs: ## 📋 Show logs from all containers (Ctrl+C to exit)
	$(COMPOSE) -f $(COMPOSE_FILE) logs -f --tail=100

logs-backend: ## 📋 Show backend logs only
	$(COMPOSE) -f $(COMPOSE_FILE) logs -f --tail=100 $(BACKEND_SERVICE)

logs-frontend: ## 📋 Show frontend/nginx logs only
	$(COMPOSE) -f $(COMPOSE_FILE) logs -f --tail=100 $(FRONTEND_SERVICE)

logs-db: ## 📋 Show SQL Server logs only
	$(COMPOSE) -f $(COMPOSE_FILE) logs -f --tail=50 $(DB_SERVICE)

# ════════════════════════════════════════════════════════════════
# DATABASE
# ════════════════════════════════════════════════════════════════
migrate: ## 🗄️  Run Entity Framework database migrations
	@echo "$(CYAN)Running database migrations...$(RESET)"
	$(COMPOSE) -f $(COMPOSE_FILE) exec $(BACKEND_SERVICE) \
		dotnet ef database update \
		--project /app \
		--connection "$${ConnectionStrings__DefaultConnection}"
	@echo "$(GREEN)✅ Migrations applied$(RESET)"

seed: ## 🌱 Seed database with sample data
	@echo "$(CYAN)Seeding database...$(RESET)"
	$(COMPOSE) -f $(COMPOSE_FILE) exec $(BACKEND_SERVICE) \
		dotnet run --project /app -- --seed
	@echo "$(GREEN)✅ Database seeded$(RESET)"

db-shell: ## 🗄️  Open a SQL Server shell (sqlcmd)
	$(COMPOSE) -f $(COMPOSE_FILE) exec $(DB_SERVICE) \
		/opt/mssql-tools18/bin/sqlcmd \
		-S localhost -U sa \
		-P "$${SQL_SA_PASSWORD:-TaskFlow@Dev123!}" \
		-No -C

db-backup: ## 💾 Backup the database to ./backups/
	@mkdir -p backups
	$(COMPOSE) -f $(COMPOSE_FILE) exec $(DB_SERVICE) \
		/opt/mssql-tools18/bin/sqlcmd \
		-S localhost -U sa \
		-P "$${SQL_SA_PASSWORD:-TaskFlow@Dev123!}" \
		-No -C \
		-Q "BACKUP DATABASE [TaskFlowDB] TO DISK = '/var/opt/mssql/backup/TaskFlowDB_$$(date +%Y%m%d_%H%M%S).bak'"
	@echo "$(GREEN)✅ Backup created$(RESET)"

# ════════════════════════════════════════════════════════════════
# TESTING
# ════════════════════════════════════════════════════════════════
test: ## 🧪 Run all backend tests
	@echo "$(CYAN)Running backend tests...$(RESET)"
	cd backend && dotnet test -c Release --logger "console;verbosity=normal"

test-frontend: ## 🧪 Run frontend tests
	@echo "$(CYAN)Running frontend tests...$(RESET)"
	cd frontend && npm run test -- --run

test-all: test test-frontend ## 🧪 Run ALL tests (backend + frontend)

lint: ## 🔍 Lint frontend code (ESLint)
	cd frontend && npm run lint

# ════════════════════════════════════════════════════════════════
# BUILD & DOCKER
# ════════════════════════════════════════════════════════════════
build: ## 🔨 Build Docker images locally
	@echo "$(CYAN)Building Docker images...$(RESET)"
	$(COMPOSE) -f $(COMPOSE_FILE) build --no-cache
	@echo "$(GREEN)✅ Images built$(RESET)"

build-backend: ## 🔨 Build backend image only
	docker build -t taskflow-backend:local ./backend

build-frontend: ## 🔨 Build frontend image only
	docker build -t taskflow-frontend:local ./frontend

# ════════════════════════════════════════════════════════════════
# HEALTH & STATUS
# ════════════════════════════════════════════════════════════════
health: ## 🏥 Check health of all services
	@echo "$(CYAN)Checking service health...$(RESET)"
	@echo ""
	@echo "Docker Containers:"
	$(COMPOSE) -f $(COMPOSE_FILE) ps
	@echo ""
	@echo "Backend Health:"
	@curl -sf http://localhost:5000/health 2>/dev/null | python3 -m json.tool || echo "$(RED)Backend not responding$(RESET)"
	@echo ""
	@echo "Frontend Health:"
	@curl -sf http://localhost/health.html 2>/dev/null && echo "$(GREEN)Frontend OK$(RESET)" || echo "$(RED)Frontend not responding$(RESET)"

status: ## 📊 Show container status
	$(COMPOSE) -f $(COMPOSE_FILE) ps

ps: status ## 📊 Alias for status

# ════════════════════════════════════════════════════════════════
# CLEANUP
# ════════════════════════════════════════════════════════════════
clean: ## 🧹 Stop containers and DELETE all data (careful!)
	@echo "$(RED)⚠️  This will DELETE all database data!$(RESET)"
	@read -p "Are you sure? (y/N): " confirm && [ "$$confirm" = "y" ] || exit 1
	$(COMPOSE) -f $(COMPOSE_FILE) down -v --remove-orphans
	@echo "$(GREEN)✅ Cleaned up$(RESET)"

clean-images: ## 🧹 Remove locally built Docker images
	docker rmi taskflow-backend:local taskflow-frontend:local 2>/dev/null || true
	@echo "$(GREEN)✅ Images removed$(RESET)"

prune: ## 🧹 Docker system prune (removes all unused Docker data)
	docker system prune -f
	@echo "$(GREEN)✅ Docker pruned$(RESET)"

# ════════════════════════════════════════════════════════════════
# PRODUCTION
# ════════════════════════════════════════════════════════════════
prod-up: ## ☁️  Start production stack (uses docker-compose.prod.yml)
	$(COMPOSE) -f $(COMPOSE_PROD) up -d

prod-down: ## ☁️  Stop production stack
	$(COMPOSE) -f $(COMPOSE_PROD) down

# ════════════════════════════════════════════════════════════════
# SETUP
# ════════════════════════════════════════════════════════════════
setup: ## ⚙️  First-time setup (copy .env, install dependencies)
	@echo "$(CYAN)Setting up TaskFlow...$(RESET)"
	@cp -n .env.example .env 2>/dev/null && echo "Created .env from .env.example" || echo ".env already exists"
	@cd frontend && npm install
	@echo ""
	@echo "$(GREEN)✅ Setup complete! Run 'make dev' to start.$(RESET)"

install-frontend: ## 📦 Install frontend npm packages
	cd frontend && npm install

update-frontend: ## 📦 Update frontend npm packages
	cd frontend && npm update
