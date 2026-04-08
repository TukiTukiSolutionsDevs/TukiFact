#!/usr/bin/env bash
set -euo pipefail

# TukiFact — Backup Script
# Creates timestamped backups of PostgreSQL and MinIO

BACKUP_DIR="${BACKUP_DIR:-/backups/tukifact}"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
PG_CONTAINER="tukifact-postgres"
MINIO_CONTAINER="tukifact-minio"

mkdir -p "$BACKUP_DIR/postgres" "$BACKUP_DIR/minio"

echo "=== TukiFact Backup — $TIMESTAMP ==="

# PostgreSQL dump
echo "Backing up PostgreSQL..."
docker exec "$PG_CONTAINER" pg_dump -U tukifact -d tukifact --format=custom \
    > "$BACKUP_DIR/postgres/tukifact_${TIMESTAMP}.dump"
echo "  PostgreSQL: $BACKUP_DIR/postgres/tukifact_${TIMESTAMP}.dump"

# MinIO (copy all buckets)
echo "Backing up MinIO buckets..."
for BUCKET in tukifact-xml tukifact-pdf tukifact-cdr tukifact-certs; do
    docker exec "$MINIO_CONTAINER" mc mirror "local/$BUCKET" "/tmp/$BUCKET" 2>/dev/null || true
    docker cp "$MINIO_CONTAINER:/tmp/$BUCKET" "$BACKUP_DIR/minio/${BUCKET}_${TIMESTAMP}" 2>/dev/null || true
done
echo "  MinIO: $BACKUP_DIR/minio/"

# Cleanup old backups (keep last 30 days)
find "$BACKUP_DIR" -type f -mtime +30 -delete 2>/dev/null || true

echo "=== Backup Complete ==="
ls -lh "$BACKUP_DIR/postgres/tukifact_${TIMESTAMP}.dump"
