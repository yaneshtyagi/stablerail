# StableRail implementation baseline

This directory is the slice-1 baseline for the product under `code/`. It compares the PRD (v1.0, 26 October 2026) with the executable prototype and records decisions later slices must follow. The prototype in `prototype/` stays the behavioral reference. It is not the production authority.

| Artifact | Purpose |
| --- | --- |
| [REQUIREMENTS.md](REQUIREMENTS.md) | P0 requirement and AT-001–AT-014 traceability matrix |
| [STATE_MACHINE.md](STATE_MACHINE.md) | Acceptance lifecycle and independent exception, compliance, provider-outcome and settlement dimensions |
| [CONFLICTS.md](CONFLICTS.md) | Prototype shortcuts that production must not inherit |
| [DEPENDENCIES.md](DEPENDENCIES.md) | External inputs, development defaults and production gate effects |
| [CHECKLIST.md](CHECKLIST.md) | Ordered slices 1–20 |
| [adr/](adr/) | ADR-001–ADR-012 |

No production gate in the PRD §21 list is passed. Verification of this baseline is `python3 docs/baseline/check_traceability.py`.
