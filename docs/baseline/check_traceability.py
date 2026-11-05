#!/usr/bin/env python3
"""Check the slice-1 traceability baseline. Does not claim product acceptance."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parent
REQUIRED_FIELDS = (
    "Priority",
    "Requirement",
    "Prototype evidence",
    "Production gap",
    "Slice",
    "Verification evidence",
)
P0_IDS = [
    "TEN-001", "TEN-002", "TEN-003", "TEN-004",
    "PAY-001", "PAY-002", "PAY-003", "PAY-004", "PAY-005", "PAY-006",
    "PRV-001", "PRV-002", "PRV-003", "PRV-004",
    "RTE-001", "RTE-002", "RTE-003", "RTE-004", "RTE-005",
    "MON-001", "MON-002", "MON-003", "MON-004", "MON-005", "MON-006",
    "WHK-001", "WHK-002", "WHK-003", "WHK-004",
    "SET-001", "SET-002", "SET-003", "SET-004", "SET-005",
    "CMP-001", "CMP-002", "CMP-003", "CMP-004",
    "OPS-001", "OPS-002", "OPS-003", "OPS-004", "OPS-005", "OPS-006",
    "API-001",
    "NFR-001", "NFR-002", "NFR-003", "NFR-004", "NFR-005",
    "NFR-006", "NFR-007", "NFR-008", "NFR-009", "NFR-010", "NFR-011",
    "COM-001", "COM-002",
    *[f"AT-{index:03d}" for index in range(1, 15)],
]
DEFERRED_IDS = [
    "TEN-005", "TEN-006", "PAY-007", "PRV-005",
    "RTE-006", "RTE-007", "RTE-008", "CMP-005", "SET-006",
]
STATUSES = (
    "created",
    "requires_payment",
    "detected",
    "confirming",
    "succeeded",
    "expired",
    "cancelled",
)
ADRS = [
    "ADR-001-non-custodial.md",
    "ADR-002-modular-monolith.md",
    "ADR-003-provider-adapters.md",
    "ADR-004-postgresql-authority.md",
    "ADR-005-outbox-queue.md",
    "ADR-006-at-least-once.md",
    "ADR-007-chain-finality.md",
    "ADR-008-immutable-journal.md",
    "ADR-009-deterministic-routing.md",
    "ADR-010-separate-states.md",
    "ADR-011-ambiguous-execution.md",
    "ADR-012-versioned-policy.md",
]


def sections(text: str) -> dict[str, str]:
    parts = re.split(r"^### ", text, flags=re.M)
    found = {}
    for part in parts[1:]:
        identifier, _, body = part.partition("\n")
        found[identifier.strip()] = body
    return found


def field(body: str, name: str) -> str | None:
    match = re.search(rf"^- \*\*{re.escape(name)}:\*\* (.+)$", body, re.M)
    return match.group(1).strip() if match else None


def main() -> int:
    errors: list[str] = []
    requirements = (ROOT / "REQUIREMENTS.md").read_text()
    found = sections(requirements)

    for identifier in P0_IDS:
        body = found.get(identifier)
        if body is None:
            errors.append(f"missing section {identifier}")
            continue
        for name in REQUIRED_FIELDS:
            value = field(body, name)
            if not value:
                errors.append(f"{identifier} missing {name}")
                continue
            if name == "Priority" and not value.startswith("P0"):
                errors.append(f"{identifier} priority is not P0: {value}")
            if name == "Slice":
                numbers = [int(item) for item in re.findall(r"\d+", value)]
                if not numbers or any(number < 2 or number > 20 for number in numbers):
                    errors.append(f"{identifier} slice is outside 2-20: {value}")
            if name == "Verification evidence" and re.search(
                r"\b(passed|certified|approved)\b", value, re.I
            ):
                errors.append(f"{identifier} claims a gate that slice 1 cannot close")

    for identifier in DEFERRED_IDS:
        body = found.get(identifier)
        if body is None:
            errors.append(f"missing deferred section {identifier}")
            continue
        priority = field(body, "Priority") or ""
        slice_owner = field(body, "Slice") or ""
        if priority.startswith("P0"):
            errors.append(f"{identifier} is listed as deferred but marked P0")
        if "deferred" not in slice_owner.lower():
            errors.append(f"{identifier} must stay deferred, found slice {slice_owner}")

    state = (ROOT / "STATE_MACHINE.md").read_text()
    for status in STATUSES:
        if f"`{status}`" not in state:
            errors.append(f"state machine missing {status}")
    if "Forbidden transitions" not in state:
        errors.append("state machine missing forbidden transitions")

    adr_dir = ROOT / "adr"
    for name in ADRS:
        path = adr_dir / name
        if not path.is_file():
            errors.append(f"missing {name}")
            continue
        text = path.read_text()
        if "Production gate" not in text or "Decision" not in text:
            errors.append(f"{name} is missing Decision or Production gate")

    for name in ("CONFLICTS.md", "DEPENDENCIES.md", "CHECKLIST.md"):
        if not (ROOT / name).is_file():
            errors.append(f"missing {name}")

    if errors:
        print("\n".join(errors))
        return 1
    print(
        f"baseline ok: {len(P0_IDS)} P0/AT rows, "
        f"{len(DEFERRED_IDS)} deferred rows, {len(ADRS)} ADRs"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
