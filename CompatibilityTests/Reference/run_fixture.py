#!/usr/bin/env python3
"""Run and compare deterministic Emuera reference fixtures.

The runner deliberately requires the caller to provide the real process
command. It never fabricates expected output and never marks a fixture
verified merely because a fixture file exists.

Examples:
  python CompatibilityTests/Reference/run_fixture.py capture \
    --manifest CompatibilityTests/REGRESSION_TESTS.json \
    --fixture dt.create.basic --runtime reference \
    --command "path/to/reference.exe path/to/fixture"

  python CompatibilityTests/Reference/run_fixture.py verify \
    --manifest CompatibilityTests/REGRESSION_TESTS.json \
    --fixture dt.create.basic
"""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import shlex
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_EXPECTED = ROOT / "CompatibilityTests" / "Expected"


def load_manifest(path: Path) -> dict:
    data = json.loads(path.read_text(encoding="utf-8"))
    if data.get("schema_version") != 2:
        raise SystemExit("REGRESSION_TESTS.json must use schema_version 2")
    return data


def fixture(data: dict, fixture_id: str) -> dict:
    for item in data.get("fixtures", []):
        if item.get("id") == fixture_id:
            return item
    raise SystemExit(f"Unknown fixture: {fixture_id}")


def output_path(expected_root: Path, fixture_id: str, runtime: str) -> Path:
    safe_id = fixture_id.replace("/", "_").replace("\\", "_")
    return expected_root / f"{safe_id}.{runtime}.json"


def capture(args: argparse.Namespace) -> None:
    manifest_path = Path(args.manifest).resolve()
    data = load_manifest(manifest_path)
    item = fixture(data, args.fixture)
    if args.runtime not in {"reference", "uemuera"}:
        raise SystemExit("--runtime must be reference or uemuera")
    if not args.command:
        raise SystemExit("capture requires --command for the actual runtime process")

    command = shlex.split(args.command, posix=False)
    started = dt.datetime.now(dt.timezone.utc).isoformat()
    try:
        proc = subprocess.run(
            command,
            cwd=str(ROOT),
            text=True,
            capture_output=True,
            timeout=args.timeout,
            check=False,
        )
    except subprocess.TimeoutExpired as exc:
        raise SystemExit(f"fixture timed out after {args.timeout}s: {exc}")

    payload = {
        "schema_version": 1,
        "fixture_id": item["id"],
        "runtime": args.runtime,
        "command": command,
        "started_at": started,
        "exit_code": proc.returncode,
        "stdout": proc.stdout,
        "stderr": proc.stderr,
        "evidence": "actual_process_capture",
    }
    target = Path(args.expected_root).resolve() / output_path(Path(args.expected_root).resolve(), item["id"], args.runtime).name
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"captured": str(target), "exit_code": proc.returncode}, ensure_ascii=False))
    if proc.returncode != 0:
        raise SystemExit(proc.returncode)


def canonical(payload: dict) -> dict:
    return {
        "exit_code": payload.get("exit_code"),
        "stdout": payload.get("stdout", ""),
        "stderr": payload.get("stderr", ""),
    }


def verify(args: argparse.Namespace) -> None:
    manifest_path = Path(args.manifest).resolve()
    data = load_manifest(manifest_path)
    item = fixture(data, args.fixture)
    expected_root = Path(args.expected_root).resolve()
    ref_path = output_path(expected_root, item["id"], "reference")
    local_path = output_path(expected_root, item["id"], "uemuera")
    if not ref_path.exists() or not local_path.exists():
        raise SystemExit("verify requires both reference and uemuera captures")

    reference = json.loads(ref_path.read_text(encoding="utf-8"))
    local = json.loads(local_path.read_text(encoding="utf-8"))
    if reference.get("evidence") != "actual_process_capture" or local.get("evidence") != "actual_process_capture":
        raise SystemExit("capture evidence is invalid")
    equal = canonical(reference) == canonical(local)
    if not equal:
        print(json.dumps({"verified": False, "reason": "output_mismatch", "reference": canonical(reference), "uemuera": canonical(local)}, ensure_ascii=False, indent=2))
        raise SystemExit(2)

    item["reference_status"] = "verified"
    item["local_status"] = "tested"
    item["evidence"] = {
        "reference_capture": str(ref_path.relative_to(ROOT)).replace("\\", "/"),
        "uemuera_capture": str(local_path.relative_to(ROOT)).replace("\\", "/"),
        "reference_sha256": hashlib.sha256(ref_path.read_bytes()).hexdigest(),
        "uemuera_sha256": hashlib.sha256(local_path.read_bytes()).hexdigest(),
        "comparison": "exact_console_and_exit_code",
    }
    manifest_path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"verified": True, "fixture_id": item["id"]}, ensure_ascii=False))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("action", choices=["capture", "verify"])
    parser.add_argument("--manifest", default=str(ROOT / "CompatibilityTests" / "REGRESSION_TESTS.json"))
    parser.add_argument("--expected-root", default=str(DEFAULT_EXPECTED))
    parser.add_argument("--fixture", required=True)
    parser.add_argument("--runtime", choices=["reference", "uemuera"])
    parser.add_argument("--command")
    parser.add_argument("--timeout", type=int, default=120)
    args = parser.parse_args()
    if args.action == "capture":
        capture(args)
    else:
        verify(args)
    return 0


if __name__ == "__main__":
    sys.exit(main())
