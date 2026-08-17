#!/usr/bin/env python3
"""Conservative C# inventory for Emuera function and instruction registries.

The inventory intentionally keeps raw source fragments alongside normalized
signatures. It is an evidence extractor, not a C# compiler: ambiguous
constructor-dependent signatures are marked as such instead of being guessed.
"""
from __future__ import annotations

import re
from pathlib import Path
from typing import Iterable

COMMAND_RE = re.compile(r'\["([A-Z][A-Z0-9_]*)"\]\s*=\s*new\s+([A-Za-z_][A-Za-z0-9_]*)')
IDENTIFIER_RE = re.compile(r'addFunction\s*\(\s*FunctionCode\.([A-Z][A-Z0-9_]*)')
CLASS_RE = re.compile(r'\bclass\s+([A-Za-z_][A-Za-z0-9_]*)\s*:\s*FunctionMethod\b')
RETURN_RE = re.compile(r'\bReturnType\s*=\s*typeof\s*\(\s*([^)]*?)\s*\)')
ARRAY_RE = re.compile(
    r'\bargumentTypeArray\s*=\s*(?:new\s+Type\s*\[\s*\]\s*)?\{(?P<body>.*?)\}',
    re.DOTALL,
)
COLLECTION_ARRAY_RE = re.compile(r'\bargumentTypeArray\s*=\s*\[(?P<body>.*?)\]', re.DOTALL)
ARG_EX_RE = re.compile(r'\bargumentTypeArrayEx\s*=\s*\[(?P<body>.*?)\]\s*;', re.DOTALL)
ARG_LIST_RE = re.compile(r'new\s+ArgTypeList\s*\{(?P<body>.*?)\}', re.DOTALL)
TYPEOF_RE = re.compile(r'typeof\s*\(\s*([^)]*?)\s*\)')
OMIT_START_RE = re.compile(r'\bOmitStart\s*=\s*(\d+)')


def read_text(path: Path) -> str:
    raw = path.read_bytes()
    for encoding in ("utf-8-sig", "utf-8", "cp932"):
        try:
            return raw.decode(encoding)
        except UnicodeDecodeError:
            continue
    return raw.decode("utf-8", errors="replace")


def _strip_comments(text: str) -> str:
    """Mask comments while preserving offsets and line breaks."""
    result = list(text)
    i = 0
    state = "code"
    while i < len(text):
        if state == "code":
            if text.startswith("//", i):
                result[i] = result[i + 1] = " "
                state = "line"
                i += 2
                continue
            if text.startswith("/*", i):
                result[i] = result[i + 1] = " "
                state = "block"
                i += 2
                continue
            if text[i] == '"':
                state = "string"
            i += 1
            continue
        if state == "line":
            if text[i] in "\r\n":
                state = "code"
            else:
                result[i] = " "
            i += 1
            continue
        if state == "block":
            if text.startswith("*/", i):
                result[i] = result[i + 1] = " "
                state = "code"
                i += 2
            else:
                if text[i] not in "\r\n":
                    result[i] = " "
                i += 1
            continue
        # String literals: braces in strings must not affect class balancing.
        if state == "string":
            if text[i] == "\\":
                if i + 1 < len(text):
                    i += 2
                else:
                    i += 1
            elif text[i] == '"':
                state = "code"
                i += 1
            else:
                i += 1
    return "".join(result)


def _balanced_body(masked: str, opening: int) -> str:
    depth = 0
    for index in range(opening, len(masked)):
        char = masked[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return masked[opening + 1:index]
    return masked[opening + 1:]


def _type_name(value: str) -> str:
    value = re.sub(r'\s+', ' ', value.strip())
    aliases = {
        "Int64": "int64",
        "long": "int64",
        "String": "string",
        "string": "string",
        "Int32": "int32",
        "int": "int32",
        "Boolean": "bool",
        "bool": "bool",
        "Single": "float",
        "double": "double",
    }
    return aliases.get(value, value)


def _standard_overloads(body: str, source: str) -> list[dict]:
    overloads: list[dict] = []
    for match in ARRAY_RE.finditer(body):
        raw = match.group("body").strip()
        overloads.append({
            "arguments": [_type_name(x) for x in TYPEOF_RE.findall(raw)],
            "optional_from": None,
            "kind": "fixed",
            "raw": raw,
            "source": source,
        })
    for match in COLLECTION_ARRAY_RE.finditer(body):
        raw = match.group("body").strip()
        # Exclude a collection expression already captured by ARRAY_RE.
        if "typeof" not in raw:
            continue
        overloads.append({
            "arguments": [_type_name(x) for x in TYPEOF_RE.findall(raw)],
            "optional_from": None,
            "kind": "fixed",
            "raw": raw,
            "source": source,
        })
    return overloads


def _extended_overloads(body: str, source: str) -> list[dict]:
    overloads: list[dict] = []
    for match in ARG_EX_RE.finditer(body):
        raw = match.group("body").strip()
        argtype_matches = re.findall(r'ArgTypes\s*=\s*\{([^}]*)\}', raw)
        omit_match = OMIT_START_RE.search(raw)
        if argtype_matches:
            for arg_text in argtype_matches:
                overloads.append({
                    "arguments": [re.sub(r'\s+', ' ', item.strip()) for item in arg_text.split(',') if item.strip()],
                    "optional_from": int(omit_match.group(1)) if omit_match else None,
                    "kind": "extended",
                    "raw": raw,
                    "source": source,
                })
        else:
            overloads.append({
                "arguments": [],
                "optional_from": None,
                "kind": "extended_unparsed",
                "raw": raw,
                "source": source,
            })
    return overloads


def _method_signatures(texts: dict[str, str]) -> dict[str, dict]:
    methods: dict[str, dict] = {}
    for path, text in texts.items():
        masked = _strip_comments(text)
        for match in CLASS_RE.finditer(masked):
            class_name = match.group(1)
            opening = masked.find("{", match.end())
            if opening < 0:
                continue
            body = _balanced_body(masked, opening)
            source = path
            overloads = _standard_overloads(body, source) + _extended_overloads(body, source)
            # An empty argument array is meaningful and must remain distinct
            # from a method whose signature could not be extracted.
            returns = sorted({_type_name(x) for x in RETURN_RE.findall(body)})
            entry = methods.setdefault(class_name, {
                "class": class_name,
                "return_types": [],
                "overloads": [],
                "sources": [],
            })
            entry["return_types"] = sorted(set(entry["return_types"]) | set(returns))
            entry["overloads"].extend(overloads)
            if source not in entry["sources"]:
                entry["sources"].append(source)
    for entry in methods.values():
        unique = {}
        for overload in entry["overloads"]:
            key = (tuple(overload.get("arguments", [])), overload.get("optional_from"), overload.get("kind"), overload.get("raw"))
            unique[key] = overload
        entry["overloads"] = sorted(unique.values(), key=lambda item: (item.get("kind", ""), item.get("arguments", []), item.get("optional_from") or -1, item.get("raw", "")))
        if not entry["overloads"]:
            entry["signature_status"] = "not_extracted"
        elif len(entry["overloads"]) == 1 and len(entry["return_types"]) <= 1:
            entry["signature_status"] = "extracted"
        else:
            entry["signature_status"] = "ambiguous_class_level"
    return methods


def inventory(root: Path) -> dict:
    root = root.resolve()
    files = sorted(path for path in root.rglob("*.cs") if "Library" not in path.parts and "obj" not in path.parts and "bin" not in path.parts)
    texts = {path: read_text(path) for path in files}
    creator_registrations = {}
    identifier_functions = set()
    builtin_codes = set()
    for path, text in texts.items():
        for match in COMMAND_RE.finditer(text):
            creator_registrations[match.group(1)] = {
                "method_class": match.group(2),
                "source": path.relative_to(root).as_posix(),
            }
        identifier_functions.update(IDENTIFIER_RE.findall(text))
        if path.name == "BuiltInFunctionCode.cs":
            builtin_codes.update(re.findall(r'^\s*([A-Z][A-Z0-9_]+),?\s*$', _strip_comments(text), re.MULTILINE))
    methods = _method_signatures({path.relative_to(root).as_posix(): text for path, text in texts.items()})
    names = sorted(set(creator_registrations) | identifier_functions | builtin_codes)
    commands = []
    for name in names:
        registration = creator_registrations.get(name, {})
        method_class = registration.get("method_class")
        method = methods.get(method_class) if method_class else None
        commands.append({
            "name": name,
            "registered": {
                "creator": name in creator_registrations,
                "function_identifier": name in identifier_functions,
                "builtin_code": name in builtin_codes,
            },
            "method_class": method_class,
            "signature": method,
            "sources": sorted(set(
                ([registration["source"]] if registration.get("source") else [])
                + (["<FunctionIdentifier>"] if name in identifier_functions else [])
                + (["<BuiltInFunctionCode>"] if name in builtin_codes else [])
            )),
        })
    return {
        "root": root.as_posix(),
        "file_count": len(files),
        "creator_registrations": len(creator_registrations),
        "identifier_functions": len(identifier_functions),
        "builtin_codes": len(builtin_codes),
        "methods": methods,
        "commands": commands,
    }


def signature_fingerprint(command: dict) -> tuple:
    signature = command.get("signature") or {}
    overloads = signature.get("overloads") or []
    return (
        tuple(signature.get("return_types") or []),
        tuple(sorted((tuple(item.get("arguments") or []), item.get("optional_from"), item.get("kind")) for item in overloads)),
    )


def command_index(inventory_payload: dict) -> dict[str, dict]:
    return {command["name"]: command for command in inventory_payload.get("commands", [])}
