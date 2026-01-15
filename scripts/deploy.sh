#!/bin/bash

# ============================================
# Manual Deploy Script for CorpProcure
# ============================================

set -e

APP_DIR="/opt/corpprocure"
COMPOSE_FILE="docker-compose.prod.yml"

echo "================================================"
echo "   CorpProcure Deployment"
echo "================================================"

cd ${APP_DIR}

# Pull latest code
echo "[1/5] Pulling latest changes..."
git pull origin main

# Build new image
echo "[2/5] Building Docker image..."
docker compose -f ${COMPOSE_FILE} build --no-cache app

# Stop old container
echo "[3/5] Stopping old container..."
docker compose -f ${COMPOSE_FILE} stop app

# Start new container
echo "[4/5] Starting new container..."
docker compose -f ${COMPOSE_FILE} up -d app

# Cleanup
echo "[5/5] Cleaning up old images..."
docker image prune -f

# Status
echo ""
echo "================================================"
echo "   Deployment Complete!"
echo "================================================"
docker compose -f ${COMPOSE_FILE} ps

# Health check
echo ""
echo "Running health check in 10 seconds..."
sleep 10
if curl -s -f http://localhost:3000/health > /dev/null; then
    echo "✅ Health check passed!"
else
    echo "❌ Health check failed!"
    exit 1
fi
