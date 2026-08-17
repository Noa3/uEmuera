#!/usr/bin/env python3
"""
EraElectron SDK API extractor.

Reads era-electron.js (the SDK stub file shipped with ERA games) and extracts
the full era.* API surface into a structured JSON manifest.

Only JSDoc annotations and function signatures are extracted — no implementation
code is read or reproduced.

Usage:
    python extract_api.py <path-to-era-electron.js> [--output API.generated.json]

The output is suitable for ReferenceParity/EraElectron/API.generated.json.
"""
import re
import json
import sys
import argparse
from pathlib import Path
from datetime import datetime, timezone

# ---------------------------------------------------------------------------
# Regex patterns
# ---------------------------------------------------------------------------

RE_FUNC = re.compile(
    r'/\*\*(.*?)\*/\s*'            # JSDoc block
    r'(?:async\s+)?([a-zA-Z_][a-zA-Z0-9_]*)\s*\(',   # function name
    re.DOTALL,
)

RE_ASYNC = re.compile(r'\basync\b')
RE_PARAM = re.compile(r'@param\s+\{([^}]+)\}\s+([^\s*]+)\s*(.*?)(?=@|\*/|$)', re.DOTALL)
RE_RETURN = re.compile(r'@returns?\s+\{([^}]+)\}')
RE_DEPRECATED = re.compile(r'@deprecated\s*(.*?)(?=@|\*/|$)', re.DOTALL)
RE_VERSION = re.compile(r"version:\s*\{\s*sdk:\s*'([^']+)'")
RE_PROP_BOOL = re.compile(r'([a-zA-Z_][a-zA-Z0-9_]*):\s*true\b')

# Classifications (hand-curated from JSDoc inspection)
SAVE_APIS     = {'saveData', 'loadData', 'rmData', 'saveGlobal', 'loadGlobal', 'resetGlobal'}
RENDER_APIS   = {'print', 'println', 'printAndWait', 'printButton', 'printImage',
                 'printWholeImage', 'printProgress', 'printMultiColumns', 'printInColRows',
                 'printLineChart', 'drawLine', 'replaceText', 'replaceInColRows',
                 'notify', 'setAlign', 'setColor', 'setBack', 'setOverlay',
                 'setHorizontalAlign', 'setVerticalAlign', 'setOffset', 'setWidth',
                 'setToBottom', 'setMask', 'setTitle', 'clear'}
DATA_APIS     = {'get', 'set', 'add', 'addCharacter', 'removeCharacter',
                 'resetCharacter', 'resetData', 'addCharacterForTrain',
                 'beginTrain', 'endTrain', 'nextTurnInTrain',
                 'getAddedCharacters', 'getAllCharacters', 'getCharactersInTrain'}
INPUT_APIS    = {'input', 'waitAnyKey', 'printAndWait'}
AUDIO_APIS    = {'playMusic', 'stopMusic', 'resumeMusic'}
PLATFORM_APIS = {'isLandscape', 'setTitle', 'quit'}
DEBUG_APIS    = {'isDebug', 'toggleDebug', 'logger'}
TIMING_APIS   = {'delay'}


def classify(name):
    cats = []
    if name in RENDER_APIS:  cats.append('render')
    if name in DATA_APIS:    cats.append('data')
    if name in INPUT_APIS:   cats.append('input')
    if name in AUDIO_APIS:   cats.append('audio')
    if name in SAVE_APIS:    cats.append('save')
    if name in PLATFORM_APIS: cats.append('platform')
    if name in DEBUG_APIS:   cats.append('debug')
    if name in TIMING_APIS:  cats.append('timing')
    return cats or ['misc']


def parse_params(jsdoc_text):
    params = []
    for m in RE_PARAM.finditer(jsdoc_text):
        type_str = m.group(1).strip()
        raw_name = m.group(2).strip()
        desc     = m.group(3).strip().replace('\n', ' ').replace('  ', ' ')
        optional = raw_name.startswith('[') and raw_name.endswith(']')
        if optional:
            raw_name = raw_name[1:-1]
        # strip config.[] sub-params (they're in TextConfig etc. typedefs)
        if 'config.' in raw_name:
            continue
        rest_args = raw_name.startswith('...')
        name = raw_name.lstrip('.')
        params.append({
            'name': name,
            'type': type_str,
            'optional': optional,
            'rest': rest_args,
            'description': desc[:120],
        })
    return params


def extract_sdk_version(content):
    m = RE_VERSION.search(content)
    return m.group(1) if m else 'unknown'


def extract_apis(path: Path) -> dict:
    content = path.read_text(encoding='utf-8')
    sdk_version = extract_sdk_version(content)

    # Remove comment-only lines to simplify matching
    apis = []

    # Match all JSDoc + function name pairs
    for m in RE_FUNC.finditer(content):
        jsdoc = m.group(1)
        name  = m.group(2)
        # Skip internal helpers and logger sub-methods (captured separately)
        if name in ('f', 'assert', 'debug', 'error', 'info', 'warn'):
            continue

        is_async    = bool(RE_ASYNC.search(jsdoc)) or bool(RE_ASYNC.search(
            content[m.start():m.start()+20]))
        return_m    = RE_RETURN.search(jsdoc)
        return_type = return_m.group(1).strip() if return_m else 'void'
        depr_m      = RE_DEPRECATED.search(jsdoc)
        deprecated  = bool(depr_m)
        depr_note   = depr_m.group(1).strip()[:120] if depr_m else None
        params      = parse_params(jsdoc)

        apis.append({
            'name': name,
            'async': is_async,
            'return_type': return_type,
            'params': params,
            'categories': classify(name),
            'deprecated': deprecated,
            'deprecated_note': depr_note,
            'uEmuera_status': 'MISSING',
            'test_status': 'MISSING',
        })

    # Boolean properties (isEra)
    for m in RE_PROP_BOOL.finditer(content):
        n = m.group(1)
        if n not in {a['name'] for a in apis} and n not in ('true',):
            apis.append({
                'name': n,
                'async': False,
                'return_type': 'boolean',
                'params': [],
                'categories': ['misc'],
                'deprecated': False,
                'deprecated_note': None,
                'is_property': True,
                'uEmuera_status': 'MISSING',
                'test_status': 'MISSING',
            })

    # logger sub-object
    logger_methods = ['assert', 'debug', 'error', 'info', 'warn']
    for lm in logger_methods:
        apis.append({
            'name': f'logger.{lm}',
            'async': False,
            'return_type': 'void',
            'params': [{'name': 'msg', 'type': 'any', 'optional': False, 'rest': False,
                        'description': 'message to log'}],
            'categories': ['debug'],
            'deprecated': False,
            'deprecated_note': None,
            'uEmuera_status': 'MISSING',
            'test_status': 'MISSING',
        })

    return {
        'generated_at': datetime.now(timezone.utc).isoformat(),
        'sdk_version': sdk_version,
        'source_file': str(path),
        'api_count': len(apis),
        'apis': sorted(apis, key=lambda a: a['name']),
    }


def main():
    ap = argparse.ArgumentParser(description='Extract era-electron SDK API surface.')
    ap.add_argument('sdk_path', help='Path to era-electron.js')
    ap.add_argument('--output', default='API.generated.json',
                    help='Output JSON file (default: API.generated.json)')
    args = ap.parse_args()

    path = Path(args.sdk_path)
    if not path.exists():
        print(f'ERROR: {path} does not exist', file=sys.stderr)
        sys.exit(1)

    result = extract_apis(path)
    out = Path(args.output)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding='utf-8')
    print(f'Extracted {result["api_count"]} APIs (SDK {result["sdk_version"]}) → {out}')


if __name__ == '__main__':
    main()
