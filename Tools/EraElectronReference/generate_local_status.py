#!/usr/bin/env python3
"""Generate conservative local EraElectron source-wiring evidence."""
import argparse
import json
from pathlib import Path
from datetime import datetime, timezone
from extract_api import apply_local_implementation_status

HERE = Path(__file__).resolve().parent
REPO_ROOT = HERE.parent.parent
DEFAULT_USAGE = REPO_ROOT / 'ReferenceParity' / 'EraElectron' / 'ERAUMA_USAGE.generated.json'
DEFAULT_OUTPUT = REPO_ROOT / 'ReferenceParity' / 'EraElectron' / 'LOCAL_IMPLEMENTATION.generated.json'

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--usage', default=str(DEFAULT_USAGE))
    ap.add_argument('--output', default=str(DEFAULT_OUTPUT))
    ap.add_argument('--repo-root', default=str(REPO_ROOT))
    args = ap.parse_args()

    usage = json.loads(Path(args.usage).read_text(encoding='utf-8-sig'))
    apis = [{'name': x['name'], 'call_count': x.get('call_count', 0),
             'uEmuera_status': 'MISSING', 'test_status': 'MISSING'}
            for x in usage.get('era_api_usage', [])]
    apply_local_implementation_status(apis, Path(args.repo_root))

    counts = {}
    for item in apis:
        item['status'] = item.pop('uEmuera_status', 'MISSING')
        counts[item['status']] = counts.get(item['status'], 0) + 1

    result = {
        'generated_at': datetime.now(timezone.utc).isoformat(),
        'source_usage': str(Path(args.usage)),
        'game_name': usage.get('game_name'),
        'game_version': usage.get('game_version'),
        'ere_min_version': usage.get('ere_min_version'),
        'note': 'Source wiring only; reference/integration tests are required for VERIFIED status.',
        'summary': counts,
        'apis': apis,
    }
    out = Path(args.output)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(result, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    print(f'Local implementation report → {out}')

if __name__ == '__main__':
    main()
