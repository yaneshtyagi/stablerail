#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

docker compose up -d --wait

for _ in $(seq 1 30); do
  if python3 -c 'import socket; s=socket.create_connection(("127.0.0.1", 9324), 2); s.close()'; then
    exit 0
  fi
  sleep 1
done

docker compose logs queue
echo "Timed out waiting for the local queue on port 9324." >&2
exit 1
