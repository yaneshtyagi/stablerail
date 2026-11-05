# StableRail product

Product implementation lives here. Slice 1 is documentation only; application code starts in slice 2.

Decisions, the requirement matrix, the state machine and unresolved external gates are in [`docs/baseline`](../docs/baseline/README.md). The prototype in `../prototype` remains an executable reference and is not imported as production authority.

Runtime targets recorded for slice 2, after checking official support pages on 5 November 2026:

- ASP.NET Core on .NET 10 LTS (support through 14 November 2028). The installed SDK is 10.0.301. Microsoft’s support policy listed runtime patch 10.0.14 as current on 25 October 2026. Slice 2 must target `net10.0` and a current patch. .NET 8 LTS support ends 10 November 2026, so it is not the product target.
- PostgreSQL 18 as the system of record (community support through 14 November 2030). The local `psql` client is 16.15, which is still a supported minor, and is not the container pin.
- Node.js 26 LTS for the operations UI toolchain. The installed Node.js 25 is end of life and must not build the product. The prototype continues to use its existing Node.js 22 toolchain.
