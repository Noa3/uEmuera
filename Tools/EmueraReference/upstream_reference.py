#!/usr/bin/env python3
"""Track and extract the current Emuera.EM+EE upstream reference.

This is a developer-side tool. It never downloads or commits reference
executables. A local source checkout may be supplied for API extraction; the
remote Git repository is used only for immutable commit/tag metadata.

Typical workflow:

    python Tools/EmueraReference/upstream_reference.py \
        --source C:/path/to/emuera.em \
        --update

Without --source the tool can still resolve the current upstream release
metadata, but it will not fabricate a command inventory from documentation.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import re
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from csharp_inventory import command_index, inventory, signature_fingerprint  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
REFERENCE_REGISTRY = ROOT / "ReferenceParity" / "feature_registry.json"
OUT_REFERENCE = ROOT / "ReferenceParity" / "UPSTREAM_REFERENCE.generated.json"
OUT_COMMANDS = ROOT / "ReferenceParity" / "UPSTREAM_COMMANDS.generated.json"
OUT_DELTA = ROOT / "ReferenceParity" / "UPSTREAM_DELTA.generated.md"
REPOSITORY = "https://gitlab.com/EvilMask/emuera.em"
TAG_RE = re.compile(r"(?:Emuera(?:\.NET)?(?P<emuera>[0-9]+)?\+v(?P<base>[0-9]+))?\+EMv(?P<em>[0-9]+)\+EEv(?P<ee>[0-9]+)", re.IGNORECASE)


def read_text(path: Path) -> str:
    raw = path.read_bytes()
    for encoding in ("utf-8-sig", "utf-8", "cp932"):
        try:
            return raw.decode(encoding)
        except UnicodeDecodeError:
            continue
    return raw.decode("utf-8", errors="replace")


def load_json(path: Path, default):
    if not path.exists():
        return default
    return json.loads(read_text(path))


def git(args: list[str], cwd: Path | None = None) -> str:
    return subprocess.check_output(["git", *args], cwd=cwd, text=True, stderr=subprocess.STDOUT).strip()


def parse_tag(tag: str | None) -> dict:
    if not tag:
        return {"tag": None, "emuera_version": None, "em_version": None, "ee_version": None}
    match = TAG_RE.search(tag)
    if not match:
        return {"tag": tag, "emuera_version": None, "em_version": None, "ee_version": None}
    return {
        "tag": tag,
        "emuera_version": match.group("emuera"),
        "base_version": match.group("base"),
        "em_version": match.group("em"),
        "ee_version": match.group("ee"),
    }


def discover_remote(repository: str) -> dict:
    output = git(["ls-remote", "--heads", "--tags", repository])
    refs = []
    for line in output.splitlines():
        parts = line.split("\t", 1)
        if len(parts) != 2:
            continue
        commit, ref = parts
        if ref.endswith("^{}"):  # annotated tag dereference is duplicated
            continue
        if not ref.startswith("refs/tags/"):
            continue
        tag = ref[len("refs/tags/"):]
        version = parse_tag(tag)
        if version.get("ee_version") is None:
            continue
        refs.append({"commit": commit, **version})
    if not refs:
        raise RuntimeError("No Emuera.EM+EE release tags with EM/EE versions were found")
    # Prefer the highest EE, then EM, then base revision. This is explicit and
    # repeatable; it does not silently switch to an arbitrary branch head.
    refs.sort(key=lambda item: (
        int(item.get("ee_version") or -1),
        int(item.get("em_version") or -1),
        int(item.get("base_version") or -1),
        item.get("tag") or "",
    ), reverse=True)
    selected = refs[0]
    return {
        "repository": repository,
        "resolved_from": "git ls-remote --heads --tags",
        "current_release": selected,
        "release_tags_seen": len(refs),
    }


def source_metadata(source: Path, remote: dict) -> dict:
    source = source.resolve()
    commit = git(["rev-parse", "HEAD"], cwd=source)
    commit_date = git(["show", "-s", "--format=%cI", "HEAD"], cwd=source)
    try:
        exact_tag = git(["describe", "--tags", "--exact-match", "HEAD"], cwd=source)
    except (OSError, subprocess.CalledProcessError):
        exact_tag = None
    selected = remote.get("current_release", {})
    tag_info = parse_tag(exact_tag or selected.get("tag"))
    if commit == selected.get("commit"):
        selected_source = "current_release_tag"
    else:
        selected_source = "provided_checkout"
    return {
        "repository": remote.get("repository", REPOSITORY),
        "revision": commit,
        "commit_date": commit_date,
        "tag": exact_tag,
        "version_source": selected_source,
        "em_version": tag_info.get("em_version"),
        "ee_version": tag_info.get("ee_version"),
        "documentation_version": tag_info.get("tag"),
        "capture_date": commit_date,
        "source_path_recorded": False,
    }


def local_metadata() -> dict:
    try:
        revision = git(["rev-parse", "HEAD"], cwd=ROOT)
    except (OSError, subprocess.CalledProcessError):
        revision = "unknown"
    return {"uemuera_revision": revision}


def compare_commands(reference: dict, local: dict, verified_commands: set[str]) -> list[dict]:
    ref = command_index(reference)
    uem = command_index(local)
    rows = []
    for name in sorted(set(ref) | set(uem)):
        in_ref = name in ref
        in_uem = name in uem
        if in_ref and not in_uem:
            status = "MISSING_IN_UEMUERA"
        elif not in_ref and in_uem:
            status = "UEMUERA_EXTENSION"
        elif name in verified_commands:
            status = "VERIFIED"
        elif signature_fingerprint(ref[name]) != signature_fingerprint(uem[name]):
            status = "SIGNATURE_CHANGED"
        else:
            status = "IMPLEMENTED_UNVERIFIED"
        rows.append({
            "name": name,
            "status": status,
            "reference": ref.get(name),
            "uemuera": uem.get(name),
            "signature_match": bool(in_ref and in_uem and signature_fingerprint(ref[name]) == signature_fingerprint(uem[name])),
            "reference_verified": name in verified_commands,
        })
    return rows


def verified_command_names() -> set[str]:
    registry = load_json(ROOT / "CompatibilityTests" / "REGRESSION_TESTS.json", {"fixtures": []})
    names: set[str] = set()
    for fixture in registry.get("fixtures", []):
        if fixture.get("reference_status") != "verified":
            continue
        commands = fixture.get("commands", fixture.get("command", []))
        if isinstance(commands, str):
            commands = [commands]
        if isinstance(commands, list):
            names.update(str(command).upper() for command in commands)
    return names


def markdown_delta(payload: dict) -> str:
    rows = payload["delta"]
    counts: dict[str, int] = {}
    for row in rows:
        counts[row["status"]] = counts.get(row["status"], 0) + 1
    lines = [
        "# Generated EM+EE upstream delta",
        "",
        "> This report compares extracted registrations and signatures. It does not turn source presence into reference verification.",
        "",
        f"- Reference: `{payload['reference']['em_version'] or '?'} / {payload['reference']['ee_version'] or '?'}`",
        f"- Reference commit: `{payload['reference']['revision']}`",
        f"- uEmuera revision: `{payload['uemuera_revision']}`",
        "",
        "## Counts",
        "",
        "| Status | Count |",
        "|---|---:|",
    ]
    for status in ("VERIFIED", "IMPLEMENTED_UNVERIFIED", "SIGNATURE_CHANGED", "MISSING_IN_UEMUERA", "UEMUERA_EXTENSION", "PLATFORM_LIMITED"):
        if status in counts:
            lines.append(f"| `{status}` | {counts[status]} |")
    lines += ["", "## Delta", "", "| Status | Command | Signature match |", "|---|---|---|"]
    for row in rows:
        if row["status"] == "IMPLEMENTED_UNVERIFIED" and row["signature_match"]:
            continue
        lines.append(f"| `{row['status']}` | `{row['name']}` | `{str(row['signature_match']).upper()}` |")
    lines += ["", "## Interpretation", "", "- `VERIFIED` requires a fixture with `reference_status: verified` and an explicit `commands` field.", "- `SIGNATURE_CHANGED` means both sides expose the command but extracted return/argument metadata differs or is ambiguous.", "- `UEMUERA_EXTENSION` is not a defect by itself; it identifies commands absent from the selected upstream snapshot.", ""]
    return "\n".join(lines)


def write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, help="local Emuera.EM checkout used for source extraction")
    parser.add_argument("--repository", default=REPOSITORY)
    parser.add_argument("--check", action="store_true")
    parser.add_argument("--update", action="store_true", help="write generated metadata and command delta")
    args = parser.parse_args()

    remote = discover_remote(args.repository)
    registry = load_json(REFERENCE_REGISTRY, {})
    configured = registry.get("reference", {})
    reference = source_metadata(args.source, remote) if args.source else {
        "repository": args.repository,
        "revision": remote["current_release"]["commit"],
        "commit_date": None,
        "tag": remote["current_release"]["tag"],
        "version_source": "remote_release_metadata_only",
        "em_version": remote["current_release"].get("em_version"),
        "ee_version": remote["current_release"].get("ee_version"),
        "documentation_version": remote["current_release"].get("tag"),
        "capture_date": None,
        "source_path_recorded": False,
    }
    reference["configured_revision"] = configured.get("revision")
    reference["stale"] = configured.get("revision") not in (None, reference["revision"])
    local = inventory(ROOT / "Assets" / "Scripts")
    if args.source:
        upstream = inventory(args.source)
        extraction = "source_checkout"
    else:
        previous = load_json(OUT_COMMANDS, {})
        upstream = previous.get("inventory", {"commands": []})
        extraction = "previous_snapshot_or_empty"
    delta = compare_commands(upstream, local, verified_command_names())
    payload = {
        "schema_version": 1,
        "reference": reference,
        "uemuera": local_metadata(),
        "remote_discovery": remote,
        "extraction": extraction,
        "delta_count": len(delta),
        "status_counts": {status: sum(row["status"] == status for row in delta) for status in sorted({row["status"] for row in delta})},
        "delta": delta,
    }
    command_payload = {
        "schema_version": 1,
        "reference": reference,
        "uemuera_revision": local_metadata()["uemuera_revision"],
        "extraction": extraction,
        "inventory": upstream,
    }
    contents = {
        OUT_REFERENCE: json.dumps({"schema_version": 1, **reference, "uemuera_revision": local_metadata()["uemuera_revision"], "remote_discovery": remote}, indent=2, ensure_ascii=False) + "\n",
        OUT_COMMANDS: json.dumps(command_payload, indent=2, ensure_ascii=False) + "\n",
        OUT_DELTA: markdown_delta({"reference": reference, "uemuera_revision": local_metadata()["uemuera_revision"], "delta": delta}),
    }
    changed = []
    for path, content in contents.items():
        old = read_text(path) if path.exists() else None
        if old != content:
            changed.append(path.relative_to(ROOT).as_posix())
            if args.update and not args.check:
                write(path, content)
    print(json.dumps({"reference": reference, "changed": changed, "delta_count": len(delta), "status_counts": payload["status_counts"]}, ensure_ascii=False, indent=2))
    if args.check and changed:
        return 1
    if not args.update and changed:
        print("Use --update to write generated artifacts.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
