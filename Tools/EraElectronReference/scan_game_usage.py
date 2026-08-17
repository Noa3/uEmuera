#!/usr/bin/env python3
"""
EraElectron game usage scanner.

Scans all .js files inside an ERE game's ere/ source directory and generates
a structured report of:
  - era.* API call frequencies
  - require() dependency graph (internal + external)
  - module structure

Usage:
    python scan_game_usage.py <game-root> [--output ERAUMA_USAGE.generated.json]

The game-root should be the directory that contains ere/ and package.json.
"""
import re
import json
import sys
import argparse
from pathlib import Path
from collections import defaultdict
from datetime import datetime, timezone

RE_ERA_CALL  = re.compile(r'\bera\.([a-zA-Z_][a-zA-Z0-9_.]*)\s*[\(\.]')
RE_REQUIRE   = re.compile(r"(?:require|import)\s*\(\s*['\"]([^'\"]+)['\"]\s*\)")
RE_IMPORT_ES = re.compile(r"^\s*import\s+.+?\s+from\s+['\"]([^'\"]+)['\"]", re.MULTILINE)
RE_KOJO      = re.compile(r"\.kojo$")


def classify_dep(dep: str) -> str:
    if dep.startswith('#/'):
        return 'internal'
    if dep.startswith('./') or dep.startswith('../'):
        return 'relative'
    # Node built-ins (partial list)
    builtins = {'fs', 'path', 'crypto', 'os', 'events', 'util', 'stream',
                'worker_threads', 'process', 'url', 'http', 'https', 'buffer',
                'child_process', 'assert', 'readline', 'zlib', 'net', 'tls',
                'dns', 'querystring', 'module', 'vm', 'perf_hooks'}
    if dep.split('/')[0] in builtins:
        return 'node_builtin'
    return 'npm_package'


def scan_game(game_root: Path) -> dict:
    ere_dir = game_root / 'ere'
    if not ere_dir.is_dir():
        print(f'WARNING: {ere_dir} not found; scanning game root instead.', file=sys.stderr)
        ere_dir = game_root

    era_calls:  dict[str, int]       = defaultdict(int)
    deps:       dict[str, int]       = defaultdict(int)
    dep_kinds:  dict[str, str]       = {}
    kojo_files: list[str]            = []
    js_files:   list[str]            = []
    errors:     list[str]            = []

    for f in ere_dir.rglob('*.js'):
        rel = str(f.relative_to(ere_dir)).replace('\\', '/')
        js_files.append(rel)
        try:
            text = f.read_text(encoding='utf-8', errors='replace')
        except Exception as e:
            errors.append(f'{rel}: {e}')
            continue

        for m in RE_ERA_CALL.finditer(text):
            era_calls[m.group(1)] += 1

        for m in RE_REQUIRE.finditer(text):
            dep = m.group(1)
            deps[dep] += 1
            dep_kinds[dep] = classify_dep(dep)

        for m in RE_IMPORT_ES.finditer(text):
            dep = m.group(1)
            deps[dep] += 1
            dep_kinds[dep] = classify_dep(dep)

    for f in ere_dir.rglob('*.kojo'):
        kojo_files.append(str(f.relative_to(ere_dir)).replace('\\', '/'))

    # Read package.json for metadata
    pkg_file = game_root / 'package.json'
    pkg = {}
    if pkg_file.exists():
        try:
            pkg = json.loads(pkg_file.read_text(encoding='utf-8', errors='replace'))
        except Exception:
            pass

    # Read .ere-min-version
    min_ver_file = game_root / '.ere-min-version'
    ere_min_version = ''
    if min_ver_file.exists():
        ere_min_version = min_ver_file.read_text(encoding='utf-8').strip()

    # Sort era calls by frequency desc
    era_sorted = sorted(era_calls.items(), key=lambda x: -x[1])

    # Group deps
    internal_deps = {k: v for k, v in deps.items() if dep_kinds.get(k) == 'internal'}
    npm_deps      = {k: v for k, v in deps.items() if dep_kinds.get(k) == 'npm_package'}
    node_builtins = {k: v for k, v in deps.items() if dep_kinds.get(k) == 'node_builtin'}
    relative_deps = {k: v for k, v in deps.items() if dep_kinds.get(k) == 'relative'}

    return {
        'generated_at':      datetime.now(timezone.utc).isoformat(),
        'game_name':         pkg.get('name', game_root.name),
        'game_version':      pkg.get('version', 'unknown'),
        'ere_min_version':   ere_min_version,
        'game_root':         str(game_root),
        'js_file_count':     len(js_files),
        'kojo_file_count':   len(kojo_files),
        'kojo_files':        kojo_files[:20],
        'total_era_calls':   sum(era_calls.values()),
        'era_api_usage': [
            {'name': k, 'call_count': v}
            for k, v in era_sorted
        ],
        'era_apis_used':     sorted(era_calls.keys()),
        'era_apis_count':    len(era_calls),
        'internal_modules': {
            'count':   len(internal_deps),
            'top_20':  sorted(internal_deps.items(), key=lambda x: -x[1])[:20],
        },
        'npm_packages': {
            'count':   len(npm_deps),
            'packages': sorted(npm_deps.keys()),
        },
        'node_builtins': {
            'count':    len(node_builtins),
            'builtins': sorted(node_builtins.keys()),
        },
        'dev_dependencies': list(pkg.get('devDependencies', {}).keys()),
        'scan_errors':      errors,
    }


def main():
    ap = argparse.ArgumentParser(description='Scan ERE game JavaScript usage.')
    ap.add_argument('game_root', help='Path to game root (containing ere/ and package.json)')
    ap.add_argument('--output', default='ERAUMA_USAGE.generated.json',
                    help='Output JSON file')
    args = ap.parse_args()

    root = Path(args.game_root)
    if not root.exists():
        print(f'ERROR: {root} does not exist', file=sys.stderr)
        sys.exit(1)

    result = scan_game(root)
    out = Path(args.output)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding='utf-8')
    print(f'Scanned {result["js_file_count"]} JS files, '
          f'{result["era_apis_count"]} era.* APIs used → {out}')


if __name__ == '__main__':
    main()
