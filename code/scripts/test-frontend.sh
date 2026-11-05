#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root/operations"

node_major="$(node -p 'process.versions.node.split(".")[0]')"
if [[ "$node_major" != "26" ]]; then
  echo "The operations app requires Node.js 26 LTS. Current: $(node -v)." >&2
  exit 1
fi

pnpm install --frozen-lockfile
pnpm lint
pnpm test
pnpm build
