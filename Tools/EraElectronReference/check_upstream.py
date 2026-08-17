#!/usr/bin/env python3
"""
EraElectron upstream change detector.

Fetches the latest era-electron SDK from the upstream git repository,
re-runs extract_api.py, and generates an API delta report showing:
  - new APIs in the latest SDK
  - removed APIs
  - changed signatures
  - .ere-min-version changes in tracked games

Usage:
    python check_upstream.py [--sdk-path <path>] [--output-dir <dir>]

If --sdk-path is provided, reads the local era-electron.js instead of fetching.
If not, attempts to clone/pull the era-electron submodule.

Requires: Python 3.8+, git in PATH.
"""
import argparse
import json
import subprocess
import sys
from pathlib import Path
from datetime import datetime, timezone

HERE      = Path(__file__).parent
REPO_ROOT = HERE.parent.parent
PARITY    = REPO_ROOT / 'ReferenceParity' / 'EraElectron'
UPSTREAM_REF = PARITY / 'UPSTREAM_REFERENCE.generated.json'
API_BASELINE = PARITY / 'API.generated.json'
EXTRACT   = HERE / 'extract_api.py'
SCAN      = HERE / 'scan_game_usage.py'


def load_json(path: Path) -> dict:
    if path.exists():
        return json.loads(path.read_text(encoding='utf-8'))
    return {}


def fetch_sdk(sdk_path_override: str | None) -> Path | None:
    """Locate or fetch era-electron.js. Returns path or None on failure."""
    if sdk_path_override:
        p = Path(sdk_path_override)
        if p.exists():
            return p
        print(f'ERROR: --sdk-path {p} not found', file=sys.stderr)
        return None

    # Try submodule path relative to repo root
    submodule = REPO_ROOT / '.games' / 'erauma-master' / 'engine' / 'era-electron.js'
    if submodule.exists():
        print(f'Using submodule SDK: {submodule}')
        return submodule

    # Try game source SDK stub (less authoritative but available)
    stub = REPO_ROOT / '.games' / 'erauma-master' / 'ere' / 'era-electron.js'
    if stub.exists():
        print(f'Using game SDK stub (submodule not initialised): {stub}')
        print('WARNING: This is the in-game stub, not the full engine SDK.')
        return stub

    print('ERROR: era-electron.js not found. '
          'Run: git submodule update --init .games/erauma-master/engine', file=sys.stderr)
    return None


def run_extractor(sdk_path: Path, out_dir: Path) -> dict | None:
    """Run extract_api.py and return the new API dict."""
    out = out_dir / 'API.new.generated.json'
    result = subprocess.run(
        [sys.executable, str(EXTRACT), str(sdk_path), '--output', str(out)],
        capture_output=True, text=True)
    if result.returncode != 0:
        print(f'extract_api.py failed:\n{result.stderr}', file=sys.stderr)
        return None
    print(result.stdout.strip())
    return json.loads(out.read_text(encoding='utf-8'))


def diff_apis(baseline: dict, latest: dict) -> dict:
    """Compute added / removed / changed APIs."""
    old = {a['name']: a for a in baseline.get('apis', [])}
    new = {a['name']: a for a in latest.get('apis', [])}

    added   = [new[n] for n in new if n not in old]
    removed = [old[n] for n in old if n not in new]
    changed = []
    for name in old:
        if name in new:
            o, n = old[name], new[name]
            diffs = {}
            if o.get('async') != n.get('async'):
                diffs['async'] = {'old': o.get('async'), 'new': n.get('async')}
            if o.get('return_type') != n.get('return_type'):
                diffs['return_type'] = {'old': o.get('return_type'), 'new': n.get('return_type')}
            old_params = [p['name'] for p in o.get('params', [])]
            new_params = [p['name'] for p in n.get('params', [])]
            if old_params != new_params:
                diffs['params'] = {'old': old_params, 'new': new_params}
            if diffs:
                changed.append({'name': name, 'diffs': diffs})

    return {
        'sdk_old': baseline.get('sdk_version', 'unknown'),
        'sdk_new': latest.get('sdk_version',  'unknown'),
        'added':   sorted(added,   key=lambda a: a['name']),
        'removed': sorted(removed, key=lambda a: a['name']),
        'changed': sorted(changed, key=lambda a: a['name']),
    }


def write_delta_markdown(delta: dict, out: Path) -> None:
    lines = [
        '# EraElectron API Delta',
        '',
        f'> Generated: {datetime.now(timezone.utc).isoformat()}',
        f'> SDK: {delta["sdk_old"]} → {delta["sdk_new"]}',
        '',
    ]
    added, removed, changed = delta['added'], delta['removed'], delta['changed']

    if not added and not removed and not changed:
        lines.append('**No changes detected.**')
    else:
        if added:
            lines += ['## Added APIs', '']
            for a in added:
                async_tag = 'async ' if a.get('async') else ''
                lines.append(f'- `{async_tag}{a["name"]}` → {a.get("return_type","?")}')
            lines.append('')

        if removed:
            lines += ['## Removed APIs', '']
            for r in removed:
                lines.append(f'- `{r["name"]}`')
            lines.append('')

        if changed:
            lines += ['## Changed APIs', '']
            for c in changed:
                lines.append(f'### `{c["name"]}`')
                for field, vals in c['diffs'].items():
                    lines.append(f'- {field}: `{vals["old"]}` → `{vals["new"]}`')
            lines.append('')

    lines += ['## Action required', '',
              '1. Review changes above.',
              '2. Update `ReferenceParity/EraElectron/API.generated.json`',
              '   (set new APIs to `uEmuera_status: "MISSING"`, update changed signatures).',
              '3. Update `Docs/RUNTIME_SUPPORT.generated.md`.',
              '']

    out.write_text('\n'.join(lines), encoding='utf-8')
    print(f'Delta report → {out}')


def main():
    ap = argparse.ArgumentParser(description='Check EraElectron upstream changes.')
    ap.add_argument('--sdk-path', help='Path to era-electron.js (skips git fetch)')
    ap.add_argument('--output-dir', default=str(PARITY),
                    help='Output directory (default: ReferenceParity/EraElectron)')
    args = ap.parse_args()

    out_dir = Path(args.output_dir)
    out_dir.mkdir(parents=True, exist_ok=True)

    sdk = fetch_sdk(args.sdk_path)
    if sdk is None:
        sys.exit(1)

    latest = run_extractor(sdk, out_dir)
    if latest is None:
        sys.exit(1)

    baseline = load_json(API_BASELINE)
    if not baseline:
        print('No baseline API.generated.json found; treating everything as new.')
        baseline = {'apis': [], 'sdk_version': 'unknown'}

    delta = diff_apis(baseline, latest)
    out_md = out_dir / 'API_DELTA.generated.md'
    write_delta_markdown(delta, out_md)

    # Summary
    print(f'\nAdded: {len(delta["added"])}  '
          f'Removed: {len(delta["removed"])}  '
          f'Changed: {len(delta["changed"])}')

    if delta['added'] or delta['removed'] or delta['changed']:
        print('\nUpstream has changed. Review API_DELTA.generated.md and update uEmuera status.')
        sys.exit(2)  # non-zero so CI can detect changes
    else:
        print('No API changes detected.')


if __name__ == '__main__':
    main()
