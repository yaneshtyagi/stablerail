#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"
"$root/scripts/dependencies.sh"
"$root/scripts/migrate.sh"
"$root/scripts/test-backend.sh"
"$root/scripts/test-frontend.sh"
