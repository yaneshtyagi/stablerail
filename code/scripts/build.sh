#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"
dotnet build StableRail.slnx -c Release --nologo
"$root/scripts/test-frontend.sh"
