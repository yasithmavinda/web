# ════════════════════════════════════════════════════════════════
# Dockerfile — TaskFlow Backend (ASP.NET Core 8 Web API)
# ════════════════════════════════════════════════════════════════
#
# WHAT IS A DOCKERFILE?
# ─────────────────────
# A Dockerfile is a recipe that tells Docker how to build
# a "container" — a lightweight, portable box that contains
# your application AND everything it needs to run.
#
# Think of it like shipping a pre-built IKEA item instead of
# flat-pack parts: the container already has everything inside.
#
# MULTI-STAGE BUILD (used here):
# ──────────────────────────────
# Stage 1 "build":   Use the BIG .NET SDK image to compile code
# Stage 2 "runtime": Copy ONLY the compiled output into a SMALL
#                    .NET runtime image
#
# Result: Final image is ~200MB instead of ~1GB
#
# ┌─────────────────────────────────────────────────────────┐
# │  Stage 1: Build         │  Stage 2: Runtime             │
# │  .NET SDK (700MB)       │  .NET Runtime (200MB)         │
# │  ├── Source code        │  ├── Published app only       │
# │  ├── NuGet packages     │  └── No SDK, no source        │
# │  └── Build tools        │                               │
# └─────────────────────────────────────────────────────────┘

# ── STAGE 1: RESTORE DEPENDENCIES ───────────────────────────────
# Use the official .NET 8 SDK image as the build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# COPY project files first (for Docker layer caching)
# Why? Docker caches each step. If only your source code changes
# but not .csproj files, Docker reuses the cached NuGet restore
# step → much faster builds.
COPY ["src/TaskFlow.API/TaskFlow.API.csproj",                         "src/TaskFlow.API/"]
COPY ["src/TaskFlow.Application/TaskFlow.Application.csproj",         "src/TaskFlow.Application/"]
COPY ["src/TaskFlow.Domain/TaskFlow.Domain.csproj",                   "src/TaskFlow.Domain/"]
COPY ["src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj",   "src/TaskFlow.Infrastructure/"]

# Restore NuGet packages (downloads dependencies)
RUN dotnet restore "src/TaskFlow.API/TaskFlow.API.csproj"

# ── STAGE 2: BUILD & PUBLISH ─────────────────────────────────────
# Now copy all source code (changes frequently → separate layer)
COPY . .

# Build the project in Release mode (optimized, no debug symbols)
RUN dotnet build "src/TaskFlow.API/TaskFlow.API.csproj" \
    -c Release \
    --no-restore \
    -o /app/build

# Publish = compiles + collects all files needed to run the app
RUN dotnet publish "src/TaskFlow.API/TaskFlow.API.csproj" \
    -c Release \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

# ── STAGE 3: RUNTIME ─────────────────────────────────────────────
# Use the SMALLER runtime-only image (no SDK needed to RUN the app)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Security: Create a non-root user to run the app
# Why? If an attacker compromises the app, they only get
# limited "app" user privileges, not full root access.
RUN groupadd --gid 1001 appgroup && \
    useradd  --uid 1001 --gid appgroup --shell /bin/false appuser

# Install curl for health checks (used by Docker and Azure)
RUN apt-get update && apt-get install -y --no-install-recommends curl && \
    rm -rf /var/lib/apt/lists/*

# Create log directory with correct permissions
RUN mkdir -p /app/logs && chown -R appuser:appgroup /app

# Copy published output from build stage
COPY --from=build --chown=appuser:appgroup /app/publish .

# Switch to non-root user
USER appuser

# Expose port 8080 (ASP.NET Core default in containers)
EXPOSE 8080

# Configure ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check: Docker will ping this endpoint every 30s
# If it fails 3 times → container is marked "unhealthy"
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "TaskFlow.API.dll"]
