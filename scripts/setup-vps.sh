#!/bin/bash

# ============================================
# VPS Setup Script for CorpProcure
# Ubuntu 22.04 LTS
# ============================================

set -e

echo "================================================"
echo "   CorpProcure VPS Setup Script"
echo "================================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration - CHANGE THESE!
DOMAIN="roihansori.com"
EMAIL="roihansori34@gmail.com"
APP_DIR="/opt/corpprocure"

# ============================================
# 1. Update System
# ============================================
echo -e "${YELLOW}[1/7] Updating system...${NC}"
apt update && apt upgrade -y

# ============================================
# 2. Install Docker
# ============================================
echo -e "${YELLOW}[2/7] Installing Docker...${NC}"

# Remove old versions
apt remove -y docker docker-engine docker.io containerd runc 2>/dev/null || true

# Install dependencies
apt install -y apt-transport-https ca-certificates curl gnupg lsb-release

# Add Docker GPG key
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

# Add Docker repository
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker
apt update
apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Start Docker
systemctl start docker
systemctl enable docker

echo -e "${GREEN}Docker installed successfully!${NC}"
docker --version

# ============================================
# 3. Install Docker Compose
# ============================================
echo -e "${YELLOW}[3/7] Installing Docker Compose...${NC}"
apt install -y docker-compose-plugin

# Create symlink for docker-compose command
ln -sf /usr/libexec/docker/cli-plugins/docker-compose /usr/local/bin/docker-compose 2>/dev/null || true

echo -e "${GREEN}Docker Compose installed!${NC}"
docker compose version

# ============================================
# 4. Setup Firewall (UFW)
# ============================================
echo -e "${YELLOW}[4/7] Configuring firewall...${NC}"

apt install -y ufw

ufw default deny incoming
ufw default allow outgoing
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 8081/tcp  # Jenkins (optional, remove for production)

echo "y" | ufw enable

echo -e "${GREEN}Firewall configured!${NC}"
ufw status

# ============================================
# 5. Create Application Directory
# ============================================
echo -e "${YELLOW}[5/7] Creating application directory...${NC}"

mkdir -p ${APP_DIR}
mkdir -p ${APP_DIR}/nginx/ssl
mkdir -p ${APP_DIR}/scripts
mkdir -p ${APP_DIR}/data

echo -e "${GREEN}Directory created at ${APP_DIR}${NC}"

# ============================================
# 6. Install Certbot (Let's Encrypt)
# ============================================
echo -e "${YELLOW}[6/7] Installing Certbot...${NC}"

apt install -y certbot

echo -e "${GREEN}Certbot installed!${NC}"

# ============================================
# 7. Create Deploy Script
# ============================================
echo -e "${YELLOW}[7/7] Creating deploy script...${NC}"

cat > ${APP_DIR}/scripts/deploy.sh << 'DEPLOY_SCRIPT'
#!/bin/bash
set -e

APP_DIR="/opt/corpprocure"
cd ${APP_DIR}

echo "Pulling latest changes..."
git pull origin main

echo "Building and deploying..."
docker compose -f docker-compose.prod.yml build --no-cache app
docker compose -f docker-compose.prod.yml up -d

echo "Cleaning up old images..."
docker image prune -f

echo "Deployment complete!"
docker compose -f docker-compose.prod.yml ps
DEPLOY_SCRIPT

chmod +x ${APP_DIR}/scripts/deploy.sh

# ============================================
# 8. Create SSL Setup Script
# ============================================
cat > ${APP_DIR}/scripts/setup-ssl.sh << EOF
#!/bin/bash
set -e

DOMAIN="${DOMAIN}"
EMAIL="${EMAIL}"

echo "Obtaining SSL certificate for \${DOMAIN}..."

# Stop nginx temporarily
docker compose -f /opt/corpprocure/docker-compose.prod.yml stop nginx 2>/dev/null || true

# Get certificate
certbot certonly --standalone -d \${DOMAIN} -d jenkins.\${DOMAIN} --email \${EMAIL} --agree-tos --non-interactive

echo "SSL certificate obtained!"
echo "Certificate location: /etc/letsencrypt/live/\${DOMAIN}/"

# Restart nginx
docker compose -f /opt/corpprocure/docker-compose.prod.yml up -d nginx

# Setup auto-renewal cron
(crontab -l 2>/dev/null; echo "0 0 1 * * certbot renew --quiet && docker compose -f /opt/corpprocure/docker-compose.prod.yml restart nginx") | crontab -
echo "Auto-renewal cron job added!"
EOF

chmod +x ${APP_DIR}/scripts/setup-ssl.sh

# ============================================
# Complete!
# ============================================
echo ""
echo -e "${GREEN}================================================${NC}"
echo -e "${GREEN}   VPS Setup Complete!${NC}"
echo -e "${GREEN}================================================${NC}"
echo ""
echo "Next steps:"
echo "1. Clone your repository:"
echo "   cd ${APP_DIR}"
echo "   git clone YOUR_GITHUB_REPO ."
echo ""
echo "2. Update configuration:"
echo "   - Edit nginx/nginx.conf and replace YOUR_DOMAIN"
echo "   - Create .env file with your secrets"
echo ""
echo "3. Get SSL certificate:"
echo "   ${APP_DIR}/scripts/setup-ssl.sh"
echo ""
echo "4. Start the application:"
echo "   docker compose -f docker-compose.prod.yml up -d"
echo ""
echo "5. Get Jenkins initial password:"
echo "   docker exec jenkins cat /var/jenkins_home/secrets/initialAdminPassword"
echo ""
