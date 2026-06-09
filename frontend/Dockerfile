# ════════════════════════════════════════════════════════════════
# Dockerfile — TaskFlow Frontend (React + Vite → Nginx)
# ════════════════════════════════════════════════════════════════
#
# MULTI-STAGE BUILD:
#
# Stage 1 "builder":  Use Node.js to install packages and
#                     compile React into static HTML/CSS/JS files
#
# Stage 2 "server":   Use Nginx (a fast web server) to serve
#                     those static files — no Node.js needed!
#
# ┌────────────────────────────────────────────────────────────┐
# │  Stage 1: Node.js Build     │  Stage 2: Nginx Serve        │
# │  node:20 (~1.1GB)           │  nginx:alpine (~25MB)         │
# │  ├── node_modules           │  ├── dist/ (built files)     │
# │  ├── React source code      │  └── nginx.conf              │
# │  └── Vite build tools       │                               │
# └────────────────────────────────────────────────────────────┘
#
# Final image size: ~30MB vs 1.1GB!

# ── STAGE 1: BUILD ───────────────────────────────────────────────
FROM node:20-alpine AS builder
WORKDIR /app

# Copy package files first (layer caching trick)
# node_modules won't be reinstalled unless package.json changes
COPY package.json package-lock.json* ./

# Install production + dev dependencies (we need dev tools to build)
RUN npm ci --frozen-lockfile

# Copy all source files
COPY . .

# Build the production bundle using Vite
# This creates the dist/ folder with optimized static files
# VITE_API_URL is set to empty so the nginx proxy handles API routing
ARG VITE_API_URL=/api/v1
ARG VITE_APP_NAME=TaskFlow
ENV VITE_API_URL=${VITE_API_URL}
ENV VITE_APP_NAME=${VITE_APP_NAME}

RUN npm run build

# ── STAGE 2: SERVE WITH NGINX ────────────────────────────────────
FROM nginx:1.25-alpine AS server
WORKDIR /usr/share/nginx/html

# Security: Remove default nginx configuration
RUN rm -rf /etc/nginx/conf.d/default.conf

# Copy our custom nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy the built React app from Stage 1
COPY --from=builder /app/dist .

# Security: Run nginx as non-root
# Change ownership of nginx pid and cache files
RUN chown -R nginx:nginx /usr/share/nginx/html && \
    chown -R nginx:nginx /var/cache/nginx && \
    chown -R nginx:nginx /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown nginx:nginx /var/run/nginx.pid

USER nginx

EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD wget -qO- http://localhost/health.html || exit 1

CMD ["nginx", "-g", "daemon off;"]
