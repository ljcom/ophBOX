#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="$SCRIPT_DIR/v0.3/tauri"

if [[ ! -f "$APP_DIR/package.json" ]]; then
  echo "Error: package.json tidak ditemukan di $APP_DIR" >&2
  exit 1
fi

cd "$APP_DIR"

if [[ ! -d node_modules ]]; then
  npm install
fi

exec npm run tauri dev
