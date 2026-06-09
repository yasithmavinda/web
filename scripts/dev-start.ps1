# ════════════════════════════════════════════════════════════════
# dev-start.ps1 — Windows PowerShell Quick Start
# ════════════════════════════════════════════════════════════════
# Run: .\scripts\dev-start.ps1

param(
    [switch]$Rebuild = $false,
    [switch]$Clean   = $false
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "╔═══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     TaskFlow — Dev Environment         ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ── Step 1: Check Docker ──────────────────────────────────────
Write-Host "[1/5] Checking Docker..." -ForegroundColor Cyan
try {
    docker info | Out-Null
    Write-Host "✅ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not running! Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# ── Step 2: Create .env ───────────────────────────────────────
Write-Host "[2/5] Setting up environment..." -ForegroundColor Cyan
if (-not (Test-Path ".env")) {
    Copy-Item ".env.example" ".env"
    Write-Host "📄 Created .env from .env.example" -ForegroundColor Yellow
    Write-Host "   Edit .env to configure your secrets!" -ForegroundColor Yellow
} else {
    Write-Host "✅ .env already exists" -ForegroundColor Green
}

# ── Step 3: Frontend dependencies ────────────────────────────
Write-Host "[3/5] Checking frontend dependencies..." -ForegroundColor Cyan
if (-not (Test-Path "frontend\node_modules")) {
    Write-Host "   Installing npm packages..." -ForegroundColor Yellow
    Push-Location frontend
    npm install --silent
    Pop-Location
    Write-Host "✅ Frontend dependencies installed" -ForegroundColor Green
} else {
    Write-Host "✅ node_modules already present" -ForegroundColor Green
}

# ── Step 4: Start containers ──────────────────────────────────
Write-Host "[4/5] Starting containers..." -ForegroundColor Cyan

if ($Clean) {
    Write-Host "   Cleaning volumes..." -ForegroundColor Yellow
    docker compose down -v
}

$buildFlag = if ($Rebuild) { "--build" } else { "" }
Invoke-Expression "docker compose -f docker-compose.yml up -d $buildFlag"

# ── Step 5: Health checks ─────────────────────────────────────
Write-Host "[5/5] Waiting for services..." -ForegroundColor Cyan

$services = @(
    @{ name = "SQL Server"; url = ""; type = "sql" },
    @{ name = "Backend";    url = "http://localhost:5000/health" },
    @{ name = "Frontend";   url = "http://localhost/health.html" }
)

foreach ($svc in $services) {
    Write-Host -NoNewline "   $($svc.name.PadRight(12))"
    $healthy = $false
    for ($i = 0; $i -lt 30; $i++) {
        try {
            if ($svc.type -eq "sql") {
                $result = docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd `
                    -S localhost -U sa -P "TaskFlow@Dev123!" -Q "SELECT 1" -No -C 2>$null
                if ($LASTEXITCODE -eq 0) { $healthy = $true; break }
            } else {
                $response = Invoke-WebRequest -Uri $svc.url -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
                if ($response.StatusCode -eq 200) { $healthy = $true; break }
            }
        } catch { }
        Write-Host -NoNewline "."
        Start-Sleep -Seconds 2
    }
    if ($healthy) {
        Write-Host " ✅" -ForegroundColor Green
    } else {
        Write-Host " ⚠️  (may still be starting)" -ForegroundColor Yellow
    }
}

# ── Summary ───────────────────────────────────────────────────
Write-Host ""
Write-Host "╔═══════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║   ✅ TaskFlow is running!              ║" -ForegroundColor Green
Write-Host "╚═══════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "   🌐 App (Frontend) : " -NoNewline; Write-Host "http://localhost" -ForegroundColor Cyan
Write-Host "   🔧 API (Backend)  : " -NoNewline; Write-Host "http://localhost:5000" -ForegroundColor Cyan
Write-Host "   📖 Swagger UI     : " -NoNewline; Write-Host "http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "   🗄️  Database       : " -NoNewline; Write-Host "localhost:1433" -ForegroundColor Cyan
Write-Host ""
Write-Host "   📧 Default login  : " -NoNewline; Write-Host "admin@taskflow.com / Admin@123!" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Stop:  docker compose down"
Write-Host "   Logs:  docker compose logs -f"
Write-Host ""
