#!/usr/bin/env bash
set -euo pipefail

# TukiFact — VPS deploy (nginx-proxy + acme-companion adapter)
#
# Assumes the shared gateway stack (nginx-proxy + acme-companion + the external
# `nginx-proxy_default` network) is already running on the VPS. DO NOT install
# Caddy. DO NOT bind ports 80/443 — the shared gateway owns them.
#
# Usage:
#   VPS_HOST=184.174.39.116 VPS_USER=root ./scripts/deploy.sh
#
# Required env: VPS_HOST, VPS_USER. Optional: VPS_PORT (22), REMOTE_DIR (/opt/tukifact).
#
# This script:
#   1. Validates the local docker/.env.prod exists and the compose is valid.
#   2. Rsyncs docker/, scripts/, and .env.prod to ${REMOTE_DIR} on the VPS.
#   3. Triggers a remote `docker compose build && up -d`.
#   4. Installs the backup cron the first time it sees the host.
#   5. Smoke-tests the public URLs.

VPS_HOST="${VPS_HOST:?VPS_HOST is required (e.g. 184.174.39.116)}"
VPS_USER="${VPS_USER:-root}"
VPS_PORT="${VPS_PORT:-22}"
REMOTE_DIR="${REMOTE_DIR:-/opt/tukifact}"
LOCAL_ENV_FILE="${LOCAL_ENV_FILE:-docker/.env.prod}"

SSH="ssh -p ${VPS_PORT} ${VPS_USER}@${VPS_HOST}"
RSYNC="rsync -azP -e 'ssh -p ${VPS_PORT}'"

echo "=== TukiFact deploy → ${VPS_USER}@${VPS_HOST}:${REMOTE_DIR} ==="

# 1. Local sanity
if [ ! -f "${LOCAL_ENV_FILE}" ]; then
    echo "ERROR: ${LOCAL_ENV_FILE} not found. Copy docker/.env.prod.example and fill it in."
    exit 1
fi

echo "→ Validating compose..."
docker compose -f docker/docker-compose.prod.yml --env-file "${LOCAL_ENV_FILE}" config > /dev/null

# 2. Ship the bundle
echo "→ Syncing files to VPS..."
$SSH "mkdir -p ${REMOTE_DIR}"

# Use --include/--exclude to ship only what the VPS needs.
rsync -azP -e "ssh -p ${VPS_PORT}" \
    --include='docker/' \
    --include='docker/**' \
    --include='scripts/' \
    --include='scripts/**' \
    --exclude='*' \
    ./ "${VPS_USER}@${VPS_HOST}:${REMOTE_DIR}/"

# .env.prod is treated separately so it never lands on disk via globbing.
rsync -azP -e "ssh -p ${VPS_PORT}" "${LOCAL_ENV_FILE}" "${VPS_USER}@${VPS_HOST}:${REMOTE_DIR}/docker/.env.prod"
$SSH "chmod 600 ${REMOTE_DIR}/docker/.env.prod"

# 3. Build + up (build on the VPS so we don't push images around)
echo "→ Building and starting services on VPS..."
$SSH "cd ${REMOTE_DIR} && docker compose -f docker/docker-compose.prod.yml --env-file docker/.env.prod build"
$SSH "cd ${REMOTE_DIR} && docker compose -f docker/docker-compose.prod.yml --env-file docker/.env.prod up -d"

# 4. Backup cron (idempotent)
echo "→ Ensuring backup cron is installed..."
$SSH "cp ${REMOTE_DIR}/scripts/backup.cron /etc/cron.d/tukifact-backup && chmod 644 /etc/cron.d/tukifact-backup && systemctl reload cron 2>/dev/null || systemctl restart cron"

# 5. Smoke (gives Let's Encrypt up to 90s to issue on first deploy)
echo "→ Waiting 30s for containers to settle..."
sleep 30

echo "=== Smoke test ==="
for url in \
    "https://tukifact.com.pe/" \
    "https://app.tukifact.com.pe/login" \
    "https://app.tukifact.com.pe/api/health"
do
    code=$(curl -s -o /dev/null -w "%{http_code}" --max-time 10 "$url" || true)
    echo "  $url → HTTP ${code}"
done

echo ""
echo "=== Deploy complete ==="
echo "If any URL returned 502/000, give acme-companion up to 2 min to issue the cert,"
echo "then re-run: docker compose -f ${REMOTE_DIR}/docker/docker-compose.prod.yml logs -f"
