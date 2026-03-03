#!/usr/bin/env bash
set -euo pipefail

BASE_DIR="$(cd "$(dirname "$0")" && pwd)"
BUNDLE_DIR="${BASE_DIR}/bundle"
API_SRC="${BUNDLE_DIR}/api-publish"
WEB_SRC="${BUNDLE_DIR}/web-dist"

TARGET_ROOT="/opt/pms"
TARGET_API="${TARGET_ROOT}/api"
TARGET_WEB="${TARGET_ROOT}/www"

NGINX_CONF_SRC="${BASE_DIR}/nginx/pms.cn.conf"
NGINX_CONF_DST="/etc/nginx/conf.d/pms.cn.conf"
SYSTEMD_SRC="${BASE_DIR}/systemd/pms-api.service"
SYSTEMD_DST="/etc/systemd/system/pms-api.service"

if [[ ! -d "${API_SRC}" ]]; then
  echo "[ERROR] Missing ${API_SRC}"
  exit 1
fi

if [[ ! -d "${WEB_SRC}" ]]; then
  echo "[ERROR] Missing ${WEB_SRC}"
  exit 1
fi

echo "[1/6] Prepare directories"
sudo mkdir -p "${TARGET_API}" "${TARGET_WEB}"

echo "[2/6] Deploy API files"
sudo rsync -a --delete "${API_SRC}/" "${TARGET_API}/"

echo "[3/6] Deploy Web files"
sudo rsync -a --delete "${WEB_SRC}/" "${TARGET_WEB}/"

echo "[4/6] Install service and nginx config"
sudo install -m 644 "${SYSTEMD_SRC}" "${SYSTEMD_DST}"
sudo install -m 644 "${NGINX_CONF_SRC}" "${NGINX_CONF_DST}"

echo "[5/6] Reload services"
sudo systemctl daemon-reload
sudo systemctl enable pms-api
sudo systemctl restart pms-api
sudo nginx -t
sudo systemctl restart nginx

echo "[6/6] Status"
sudo systemctl --no-pager --full status pms-api | sed -n '1,20p'
sudo systemctl --no-pager --full status nginx | sed -n '1,20p'

echo "[DONE] Domestic deployment complete."
