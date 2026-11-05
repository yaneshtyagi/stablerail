#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

dotnet tool restore
owner="${ConnectionStrings__PostgreSqlOwner:-Host=localhost;Port=54329;Database=stablerail;Username=stablerail;Password=stablerail}"
dotnet ef database update \
  --project src/Infrastructure/StableRail.Infrastructure.csproj \
  --startup-project src/Infrastructure/StableRail.Infrastructure.csproj \
  --connection "$owner"
docker compose exec -T postgres psql -U stablerail -d stablerail -v ON_ERROR_STOP=1 \
  -c "ALTER ROLE stablerail_app WITH LOGIN PASSWORD 'stablerail_app'"
