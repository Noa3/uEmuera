#!/usr/bin/env python3
"""Generate conservative EM+EE parity reports from the current source tree.

This tool deliberately reports IMPLEMENTED_UNVERIFIED instead of FULL when the
source contains an implementation but no verified reference fixture. It is a
source inventory, not a documentation-by-hope generator.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import re
import subprocess
import sys
from pathlib import Path
from typing import Iterable

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "Tools" / "EmueraReference"))
from csharp_inventory import command_index, inventory, signature_fingerprint  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
REGISTRY = ROOT / "ReferenceParity" / "feature_registry.json"
TEST_REGISTRY = ROOT / "CompatibilityTests" / "REGRESSION_TESTS.json"
OUT_JSON = ROOT / "ReferenceParity" / "EMEE_FEATURES.generated.json"
OUT_MD = ROOT / "ReferenceParity" / "UPSTREAM_PARITY.generated.md"
OUT_STUBS = ROOT / "ReferenceParity" / "STUB_AUDIT.generated.md"
OUT_CONFORMANCE = ROOT / "ReferenceParity" / "CONFORMANCE_RESULTS.md"
OUT_GAMES = ROOT / "ReferenceParity" / "GAME_COMPATIBILITY.generated.md"
UPSTREAM_REFERENCE = ROOT / "ReferenceParity" / "UPSTREAM_REFERENCE.generated.json"
UPSTREAM_COMMANDS = ROOT / "ReferenceParity" / "UPSTREAM_COMMANDS.generated.json"

SOURCE_ROOTS = (ROOT / "Assets" / "Scripts", ROOT / "Assets" / "Tests")
CS_FILES = tuple(p for base in SOURCE_ROOTS if base.exists() for p in base.rglob("*.cs"))


def read_text(path: Path) -> str:
    raw = path.read_bytes()
    for encoding in ("utf-8-sig", "utf-8", "cp932"):
        try:
            return raw.decode(encoding)
        except UnicodeDecodeError:
            pass
    return raw.decode("utf-8", errors="replace")


def rel(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def evidence_for(patterns: Iterable[str], texts: dict[Path, str], *, all_required: bool = False) -> list[str]:
    found: list[str] = []
    for pattern in patterns:
        matches = [rel(path) for path, text in texts.items() if pattern in text]
        if matches:
            found.append(f"{pattern}: {matches[0]}")
        elif all_required:
            return []
    return found


def status(parser: str, argument: str, runtime: str, tests: str, required: bool = True) -> str:
    if not required:
        return "N/A"
    if parser == "MISSING":
        return "MISSING"
    if argument in ("MISSING", "PARTIAL"):
        return "PARSE_ONLY" if runtime == "MISSING" else "PARTIAL"
    if runtime == "MISSING":
        return "PARSE_ONLY"
    if tests != "FULL":
        return "IMPLEMENTED_UNVERIFIED"
    return "FULL"


def dimension(parser_present: bool, evidence: list[str], *, verified: bool = False) -> str:
    if not parser_present:
        return "MISSING"
    if not evidence:
        return "MISSING"
    return "FULL" if verified else "IMPLEMENTED_UNVERIFIED"


def git_revision() -> str:
    try:
        return subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
    except (OSError, subprocess.CalledProcessError):
        return "unknown"


def load_json(path: Path, default: object) -> object:
    if not path.exists():
        return default
    return json.loads(read_text(path))


def load_upstream_inventory() -> tuple[dict, dict]:
    """Return the last extracted upstream metadata and command inventory.

    The parity generator is deliberately offline. Updating the reference is an
    explicit developer action through Tools/EmueraReference; this generator
    consumes that immutable snapshot and reports when it is absent or stale.
    """
    reference = load_json(UPSTREAM_REFERENCE, {})
    commands = load_json(UPSTREAM_COMMANDS, {})
    return (
        reference if isinstance(reference, dict) else {},
        commands if isinstance(commands, dict) else {},
    )


def signature_parity(tokens: list[str], upstream: dict, local: dict) -> tuple[str, list[str], list[str]]:
    command_tokens = [token for token in tokens if re.fullmatch(r"[A-Z][A-Z0-9_]+", token)]
    if not command_tokens:
        return "N/A", [], []
    ref_index = command_index(upstream.get("inventory", upstream))
    local_index = command_index(local)
    missing_reference = [token for token in command_tokens if token not in ref_index]
    missing_local = [token for token in command_tokens if token not in local_index]
    if missing_reference or missing_local:
        return "MISSING", missing_reference, missing_local
    matches = []
    for token in command_tokens:
        ref_signature = ref_index[token].get("signature")
        local_signature = local_index[token].get("signature")
        # A missing or ambiguous extraction is not a match. This prevents the
        # old name-presence heuristic from producing false FULL results.
        matches.append(
            ref_signature is not None
            and local_signature is not None
            and ref_signature.get("signature_status") == "extracted"
            and local_signature.get("signature_status") == "extracted"
            and signature_fingerprint(ref_index[token]) == signature_fingerprint(local_index[token])
        )
    return ("FULL" if all(matches) else "PARTIAL"), [], []


def fixture_states(fixtures: list[dict]) -> dict[str, str]:
    if not fixtures:
        return {"local_tests": "MISSING", "reference_tests": "MISSING"}
    local = "TESTED_LOCAL" if all(f.get("local_status") in ("verified", "tested") for f in fixtures) else "REGISTERED"
    reference = "REFERENCE_VERIFIED" if all(f.get("reference_status") == "verified" for f in fixtures) else "MISSING"
    return {"local_tests": local, "reference_tests": reference}


def source_inventory() -> dict[str, object]:
    texts = {path: read_text(path) for path in CS_FILES}
    by_name = {path.name: text for path, text in texts.items()}
    creator = next((text for path, text in texts.items() if path.name == "Creator.cs"), "")
    identifier = next((text for path, text in texts.items() if path.name == "FunctionIdentifier.cs"), "")
    builtins = next((text for path, text in texts.items() if path.name == "BuiltInFunctionCode.cs"), "")
    argument_sources = "\n".join(text for path, text in texts.items() if "ArgumentBuilder" in path.name or path.name == "Argument.cs")
    html_sources = "\n".join(text for path, text in texts.items() if "Html" in path.name or "HTML" in text)
    runtime_registry = next((text for path, text in texts.items() if path.name == "RuntimeCapabilityRegistry.cs"), "")
    renderer_registry = next((text for path, text in texts.items() if path.name == "RendererCapabilityRegistry.cs"), "")
    capability_ids = next((text for path, text in texts.items() if path.name == "FeatureCapabilityIds.cs"), "")
    id_constants = {value: name for name, value in re.findall(r'const\s+string\s+(\w+)\s*=\s*"([^"]+)"', capability_ids)}
    tests = load_json(TEST_REGISTRY, {"fixtures": []})
    fixture_list = tests.get("fixtures", []) if isinstance(tests, dict) else []
    fixture_by_feature: dict[str, list[dict]] = {}
    for fixture in fixture_list:
        fixture_by_feature.setdefault(fixture.get("feature_id", ""), []).append(fixture)
    command_inventory = inventory(ROOT / "Assets" / "Scripts")

    return {
        "texts": texts,
        "creator_keys": sorted(set(re.findall(r'\["([A-Z][A-Z0-9_]+)"\]\s*=', creator))),
        "identifier_functions": sorted(set(re.findall(r'addFunction\(FunctionCode\.([A-Z][A-Z0-9_]+)', identifier))),
        "builtin_codes": sorted(set(re.findall(r'^\s*([A-Z][A-Z0-9_]+),?\s*$', builtins, re.MULTILINE))),
        "argument_sources": argument_sources,
        "html_sources": html_sources,
        "runtime_registry": runtime_registry,
        "renderer_registry": renderer_registry,
        "id_constants": id_constants,
        "fixture_by_feature": fixture_by_feature,
        "fixture_count": len(fixture_list),
        "command_inventory": command_inventory,
    }


def build_features(registry: dict, inv: dict[str, object], upstream: dict) -> list[dict]:
    texts: dict[Path, str] = inv["texts"]  # type: ignore[assignment]
    creator_keys: set[str] = set(inv["creator_keys"])  # type: ignore[arg-type]
    identifier_functions: set[str] = set(inv["identifier_functions"])  # type: ignore[arg-type]
    builtin_codes: set[str] = set(inv["builtin_codes"])  # type: ignore[arg-type]
    argument_sources = str(inv["argument_sources"])
    html_sources = str(inv["html_sources"])
    runtime_registry = str(inv["runtime_registry"])
    renderer_registry = str(inv["renderer_registry"])
    id_constants: dict[str, str] = inv["id_constants"]  # type: ignore[assignment]
    fixture_by_feature: dict[str, list[dict]] = inv["fixture_by_feature"]  # type: ignore[assignment]
    local_commands: dict = inv["command_inventory"]  # type: ignore[assignment]
    result = []

    for feature in registry.get("features", []):
        tokens = feature.get("parser_tokens", [])
        upper_tokens = [token for token in tokens if token.upper() == token and re.fullmatch(r"[A-Z0-9_]+", token)]
        parser_hits = [token for token in upper_tokens if token in creator_keys or token in identifier_functions or token in builtin_codes]
        parser_hits += [token for token in tokens if token not in upper_tokens and token.lower() in html_sources.lower()]
        parser = "FULL" if tokens and len(parser_hits) == len(tokens) else ("PARTIAL" if parser_hits else "MISSING")
        signature, signature_missing_reference, signature_missing_local = signature_parity(tokens, upstream, local_commands)
        argument_hits = evidence_for(tokens, {ROOT / "ArgumentBuilder.cs": argument_sources})
        if not argument_hits and parser != "MISSING":
            # For DT_*, MAP_*, XML_* features, check Creator.cs (where these commands are registered)
            if any(token.startswith(("DT_", "MAP_", "XML_")) for token in tokens):
                creator_text = next((t for p, t in texts.items() if p.name == "Creator.cs"), "")
                if creator_text:
                    argument_hits = evidence_for(tokens, {ROOT / "Creator.cs": creator_text})
            if not argument_hits:
                argument_hits = evidence_for(tokens, {ROOT / "Creator.Method.cs": next((t for p, t in texts.items() if p.name == "Creator.Method.cs"), "")})
        # Source text can show that argument code exists, but only the
        # extracted reference/local signature comparison can produce FULL.
        argument = signature if signature != "N/A" else ("FULL" if argument_hits else "MISSING")

        runtime_hits = evidence_for(feature.get("runtime_patterns", []), texts)
        runtime = "FULL" if runtime_hits else ("PARTIAL" if parser != "MISSING" else "MISSING")
        render_required = bool(feature.get("render_patterns"))
        render_hits = evidence_for(feature.get("render_patterns", []), texts)
        rendering = "N/A" if not render_required else ("FULL" if render_hits else "MISSING")
        input_required = bool(feature.get("input_patterns"))
        input_hits = evidence_for(feature.get("input_patterns", []), texts)
        input_state = "N/A" if not input_required else ("FULL" if input_hits else "MISSING")
        persist_required = bool(feature.get("persistence_patterns"))
        persist_hits = evidence_for(feature.get("persistence_patterns", []), texts)
        persistence = "N/A" if not persist_required else ("FULL" if persist_hits else "MISSING")
        feature_id = feature.get("id", "")
        id_constant = id_constants.get(feature_id, "")
        renderer_registered = feature_id in renderer_registry or (id_constant and f"FeatureCapabilityIds.{id_constant}" in renderer_registry)
        platform = "FULL" if renderer_registered or not render_required else "PARTIAL"

        fixtures = fixture_by_feature.get(feature["id"], [])
        verified = bool(fixtures) and all(f.get("reference_status") == "verified" for f in fixtures)
        tests = "FULL" if verified else ("IMPLEMENTED_UNVERIFIED" if fixtures else "MISSING")
        evidence_states = fixture_states(fixtures)
        overall = status(parser, argument, runtime, tests)
        result.append({
            "id": feature["id"],
            "name": feature.get("name", feature["id"]),
            "status": overall,
            "verification": {
                "registered": parser != "MISSING",
                "implemented": runtime != "MISSING",
                "signature_match": argument == "FULL",
                **evidence_states,
                "reference_version": upstream.get("reference", {}).get("ee_version") if isinstance(upstream, dict) else None,
            },
            "dimensions": {
                "parser": parser,
                "argument_parity": argument,
                "runtime": runtime,
                "rendering": rendering,
                "input": input_state,
                "persistence": persistence,
                "platform": platform,
                "tests": tests,
            },
            "evidence": {
                "parser": parser_hits,
                "argument_parity": argument_hits,
                "signature_missing_reference": signature_missing_reference,
                "signature_missing_local": signature_missing_local,
                "runtime": runtime_hits,
                "rendering": render_hits,
                "input": input_hits,
                "persistence": persist_hits,
                "runtime_registry": "registered" if "runtime.emuera" in runtime_registry else "missing",
                "renderer_registry": "registered" if feature["id"] in renderer_registry else "missing",
                "tests": [f.get("id") for f in fixtures],
            },
            "reference": registry.get("reference", {}),
        })
    return result


def audit_stubs() -> list[dict]:
    patterns = [
        ("NotImplementedException", "NotImplementedException"),
        ("NotSupportedException", "NotSupportedException"),
        ("Point.Empty", "Point.Empty"),
        ("TODO", "TODO"),
        ("FIXME", "FIXME"),
        ("NYI", "NYI"),
        ("simplified implementation", "simplified implementation"),
        ("simple implementation", "simple implementation"),
        ("return false", "return false"),
    ]
    findings: list[dict] = []
    for path in sorted(p for p in CS_FILES if "generated" not in p.name.lower()):
        lines = read_text(path).splitlines()
        in_block_comment = False
        for line_no, line in enumerate(lines, 1):
            code_line = line
            if in_block_comment:
                end = code_line.find("*/")
                if end < 0:
                    code_line = ""
                else:
                    code_line = code_line[end + 2:]
                    in_block_comment = False
            while "/*" in code_line:
                start = code_line.find("/*")
                end = code_line.find("*/", start + 2)
                if end < 0:
                    code_line = code_line[:start]
                    in_block_comment = True
                    break
                code_line = code_line[:start] + code_line[end + 2:]
            code_line = code_line.split("//", 1)[0]
            for kind, needle in patterns:
                if needle.lower() not in line.lower():
                    continue
                if kind == "return false" and not any(x in code_line.lower() for x in ("stub", "not implemented", "unsupported", "point.empty")):
                    continue
                lower = rel(path).lower()
                if not code_line.strip():
                    classification = "COMMENT_ONLY"
                elif kind.lower() == "point.empty" and "if (window == null" in code_line.lower():
                    classification = "VALID_PLATFORM_GUARD"
                elif "/tests/" in lower:
                    classification = "TEST_FIXTURE"
                elif "editor" in lower or "#if unity_editor" in line.lower():
                    classification = "EDITOR_ONLY"
                elif any(x in lower for x in ("forms.cs", "window.cs", "debugdialog")) and kind in ("return false", "notimplementedexception"):
                    classification = "VALID_PLATFORM_STUB"
                else:
                    classification = "PLAYER_REACHABLE"
                findings.append({"file": rel(path), "line": line_no, "kind": kind, "classification": classification, "text": line.strip()[:240]})
                break
    return findings


def write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def markdown_report(payload: dict) -> str:
    features = payload["features"]
    lines = [
        "# Generated EM+EE parity report",
        "",
        "> Generated from source registries and regression metadata. This file is not hand-maintained.",
        "> `FULL` requires a verified reference fixture; registration alone never produces `FULL`.",
        "",
        f"- uEmuera revision: `{payload['uemuera_revision']}`",
        f"- Reference: `EMv{payload['reference'].get('em_version') or '?'} / EEv{payload['reference'].get('ee_version') or '?'}`",
        f"- Reference Emuera.EM revision: `{payload['reference'].get('revision', 'unknown')}`",
        f"- Reference tag: `{payload['reference'].get('tag') or 'unknown'}`",
        f"- Generated: `{payload['generated_at']}`",
    ]
    if payload["reference"].get("stale"):
        lines += ["", "> **WARNING:** the configured reference snapshot is stale relative to the current upstream metadata. No new FULL claim is allowed until it is refreshed."]
    lines += [
        "",
        "| Feature | Parser | Arguments | Runtime | Rendering | Input | Persistence | Platform | Tests | Overall |",
        "|---|---|---|---|---|---|---|---|---|---|",
    ]
    for feature in features:
        d = feature["dimensions"]
        lines.append("| {id} | {parser} | {argument_parity} | {runtime} | {rendering} | {input} | {persistence} | {platform} | {tests} | {status} |".format(id=feature["id"], **d, status=feature["status"]))
    lines += ["", "## Evidence policy", "", "- `PARSE_ONLY` means a name is recognized but no runtime implementation was found.", "- `IMPLEMENTED_UNVERIFIED` means source evidence exists but reference execution has not been recorded.", "- `PARTIAL` means only some required source surfaces were found.", "- `N/A` is used only for dimensions that do not apply to a feature.", ""]
    return "\n".join(lines)


def stub_report(findings: list[dict]) -> str:
    lines = ["# Generated stub audit", "", "> Scanned `Assets/Scripts` and `Assets/Tests`. Review classification before fixing; not every `return false` is a defect.", "", "| Classification | Count |", "|---|---:|"]
    counts: dict[str, int] = {}
    for f in findings:
        counts[f["classification"]] = counts.get(f["classification"], 0) + 1
    for key in sorted(counts): lines.append(f"| {key} | {counts[key]} |")
    lines += ["", "| File | Line | Kind | Classification | Evidence |", "|---|---:|---|---|---|"]
    for f in findings:
        evidence = f["text"].replace("|", "\\|")
        lines.append(f"| `{f['file']}` | {f['line']} | `{f['kind']}` | `{f['classification']}` | `{evidence}` |")
    return "\n".join(lines) + "\n"


def conformance_report(inv: dict[str, object]) -> str:
    fixtures = load_json(TEST_REGISTRY, {"fixtures": []}).get("fixtures", [])
    lines = ["# Conformance results", "", "> Generated from `CompatibilityTests/REGRESSION_TESTS.json`. A fixture is not reference-tested until it contains `reference_status: verified` and captures from actual reference and uEmuera processes.", "", "| Fixture | Feature | Category | Reference status | Fixture file |", "|---|---|---|---|---|"]
    for f in fixtures:
        lines.append(f"| `{f.get('id','')}` | `{f.get('feature_id','')}` | `{f.get('category','UNKNOWN')}` | `{f.get('reference_status','pending')}` | `{f.get('fixture', f.get('erb',''))}` |")
    lines += ["", "## Required capture fields", "", "Each verified fixture must store `reference_version`, `reference_commit`, actual reference/uEmuera process captures, and deterministic comparable output. Render-tree and image evidence remain optional per category.", ""]
    return "\n".join(lines)


def game_report() -> str:
    return """# Generated game compatibility report

> The corpus registry is intentionally conservative. A game is compatible only after it runs through the conformance harness without emulator-specific patches.

| Suite | Corpus status | Evidence |
|---|---|---|
| Classic supplied games | NOT_REGISTERED | Add game descriptors and captured runs under `CompatibilityTests/Games/`. |
| Modern EM+EE games/forks | NOT_REGISTERED | Add unmodified game packages and verified reference/uEmuera runs. |

No compatibility claim is made by this generated inventory.
"""


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="fail if generated output differs")
    args = parser.parse_args()
    registry = load_json(REGISTRY, {})
    inv = source_inventory()
    findings = audit_stubs()
    upstream_reference, upstream_commands = load_upstream_inventory()
    reference = upstream_reference or registry.get("reference", {})
    generated_at = reference.get("capture_date") or "unknown"
    payload = {
        "schema_version": 2,
        "generated_at": generated_at,
        "uemuera_revision": git_revision(),
        "reference": reference,
        "source_inventory": {
            "function_method_creator_keys": inv["creator_keys"],
            "function_identifier_entries": inv["identifier_functions"],
            "built_in_function_codes": inv["builtin_codes"],
            "command_inventory_count": len(inv["command_inventory"].get("commands", [])),
            "regression_fixture_count": inv["fixture_count"],
        },
        "upstream_inventory": {
            "command_count": len(upstream_commands.get("inventory", {}).get("commands", [])),
            "extraction": upstream_commands.get("extraction", "missing"),
        },
        "features": build_features(registry, inv, upstream_commands),
        "stub_audit": {"count": len(findings), "player_reachable": sum(f["classification"] == "PLAYER_REACHABLE" for f in findings)},
    }
    outputs = {
        OUT_JSON: json.dumps(payload, indent=2, ensure_ascii=False) + "\n",
        OUT_MD: markdown_report(payload),
        OUT_STUBS: stub_report(findings),
        OUT_CONFORMANCE: conformance_report(inv),
        OUT_GAMES: game_report(),
    }
    changed = []
    for path, content in outputs.items():
        old = path.read_text(encoding="utf-8") if path.exists() else None
        if old != content:
            changed.append(rel(path))
            if not args.check:
                write(path, content)
    if args.check and changed:
        print("Generated files are stale:")
        print("\n".join(changed))
        return 1
    print(f"Generated {len(outputs)} parity artifacts from {len(CS_FILES)} C# files; {len(findings)} audit findings.")
    if changed:
        print("Updated:")
        print("\n".join(changed))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
