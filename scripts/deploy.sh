#!/usr/bin/env bash
set -euo pipefail

# TukiFact — Deployment Script
# Usage: ./scripts/deploy.sh [staging|production]

ENV="${1:-staging}"
echo "=== Deploying TukiFact ($ENV) ==="

# Validate .env exists
if [ ! -f "docker/.env" ]; then
    echo "ERROR: docker/.env not found. Copy from docker/.env.example and configure."
    exit 1
fi

# Build images
echo "Building Docker images..."
docker compose -f docker/docker-compose.prod.yml build --no-cache

# Run migrations
echo "Running database migrations..."
docker compose -f docker/docker-compose.prod.yml run --rm api \
    dotnet ef database update --no-build

# Deploy
echo "Starting services..."
docker compose -f docker/docker-compose.prod.yml up -d

# Wait for health
echo "Waiting for services to be healthy..."
sleep 10

# Health check
echo "=== Health Check ==="
curl -sf http://localhost/health || echo "WARNING: Health check failed"
curl -sf http://localhost/api/ping || echo "WARNING: Ping failed"

echo ""
echo "=== Deployment Complete ==="
echo "API:  http://localhost/api/ping"
echo "Web:  http://localhost/"
echo "Health: http://localhost/health"
