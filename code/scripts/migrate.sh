#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

dotnet tool restore
export ConnectionStrings__PostgreSql="${ConnectionStrings__PostgreSql:-Host=localhost;Port=54329;Database=stablerail;Username=stablerail;Password=stablerail}"
dotnet ef database update \
  --project src/Infrastructure/StableRail.Infrastructure.csproj \
  --startup-project src/Infrastructure/StableRail.Infrastructure.csproj \
  --connection "$ConnectionStrings__PostgreSql"
