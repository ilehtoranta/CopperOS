#!/usr/bin/env python3
"""Generate the MorphOS 3.20 MUI ABI/class inventory.

The output contains identifiers and ABI facts only. It never copies SDK prose,
examples, comments, or complete declarations. The caller must supply a locally
installed, licensed SDK include/autodoc tree.
"""
from __future__ import annotations

import argparse
import ast
import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Any

KNOWN_PREFIXES = (
    "MUIA_", "MUIM_", "MUIC_", "MUIV_", "MUIF_", "MUIO_", "MUII_",
    "MUIX_", "MUIE_", "MUIP_", "MUIMASTER_", "MADF_", "MUI_",
)


def text(path: Path) -> str:
    return path.read_text(encoding="latin-1")


def logical_lines(source: str):
    lines = source.splitlines()
    index = 0
    while index < len(lines):
        start = index + 1
        value = lines[index]
        while value.rstrip().endswith("\\") and index + 1 < len(lines):
            value = value.rstrip()[:-1] + " " + lines[index + 1].strip()
            index += 1
        yield start, value
        index += 1


def normalize_expression(value: str) -> str:
    value = re.sub(r"/\*.*?\*/", "", value)
    value = re.sub(r"//.*", "", value)
    return " ".join(value.strip().split())


def canonical_value_kind(declaration: str) -> str | None:
    """Reduce an SDK declaration fragment to a redistributable ABI category."""
    value = declaration.upper()
    if "STRPTR" in value or "STRING" in value:
        return "string-pointer"
    if "BOOL" in value:
        return "boolean"
    if "HOOK" in value and "*" in value:
        return "hook-pointer"
    if ("TAGITEM" in value or "TAGLIST" in value) and "*" in value:
        return "tag-list-pointer"
    if "OBJECT" in value and "*" in value:
        return "mui-object-pointer"
    if "*" in value or re.search(r"\b(?:APTR|CONST_APTR)\b", value):
        return "pointer"
    if re.search(r"\b(?:ULONG|UWORD|UBYTE|IPTR|TAG)\b", value):
        return "unsigned-integer"
    if re.search(r"\b(?:LONG|WORD|BYTE)\b", value):
        return "signed-integer"
    return None


def documentation_facts(line: str) -> dict[str, Any]:
    """Extract facts from a compact SDK declaration annotation, never its prose."""
    comments = [
        (block or single).strip()
        for block, single in re.findall(r"/\*(.*?)\*/|//(.*)$", line)
        if (block or single).strip()
    ]
    for comment in comments:
        match = re.search(
            r"\bV(?P<version>\d+)\s+(?P<access>[iIsSgG.\-*]{3})(?:\s+(?P<declaration>.*))?",
            comment,
        )
        if not match:
            continue
        token = match.group("access").lower()
        access = [
            operation
            for operation, marker in zip(("initialize", "set", "get"), token)
            if marker not in ".-*"
        ]
        facts: dict[str, Any] = {
            "introduced_version": int(match.group("version")),
            "access": access,
        }
        value_kind = canonical_value_kind(match.group("declaration") or "")
        if value_kind:
            facts["value_kind"] = value_kind
        return facts
    return {}


def extract_macros(path: Path, include_root: Path) -> list[dict[str, Any]]:
    result: list[dict[str, Any]] = []
    conditions: list[str] = []
    for line_number, line in logical_lines(text(path)):
        stripped = line.strip()
        if stripped.startswith(("#if ", "#ifdef ", "#ifndef ")):
            conditions.append(stripped[1:])
            continue
        if stripped.startswith("#else"):
            if conditions:
                conditions[-1] = "else(" + conditions[-1] + ")"
            continue
        if stripped.startswith("#endif"):
            if conditions:
                conditions.pop()
            continue
        match = re.match(r"\s*#define\s+([A-Za-z_]\w*)(\(([^)]*)\))?\s*(.*)$", line)
        if not match:
            continue
        name, _, parameters, expression = match.groups()
        if name.startswith("_") or name.endswith("_H") or name in {"End", "Child", "SubWindow"}:
            continue
        expression = normalize_expression(expression)
        category = next((prefix[:-1].lower() for prefix in KNOWN_PREFIXES if name.startswith(prefix)), "helper")
        result.append({
            "name": name,
            "category": category,
            "parameters": [] if parameters is None else [item.strip() for item in parameters.split(",") if item.strip()],
            "expression": expression,
            "source": path.relative_to(include_root).as_posix(),
            "line": line_number,
            "conditions": list(conditions),
            "obsolete_surface": any("MUI_OBSOLETE" in item or "OBSOLETE_URLTEXT" in item for item in conditions),
            **documentation_facts(line),
        })
    return result


def extract_structures(path: Path, include_root: Path) -> list[dict[str, Any]]:
    source = text(path)
    result: list[dict[str, Any]] = []
    pattern = re.compile(r"\bstruct\s+((?:MUI|MUIP|MUIS)_?[A-Za-z0-9_]+)\s*\{")
    for match in pattern.finditer(source):
        name = match.group(1)
        brace = source.find("{", match.start())
        depth = 0
        end = brace
        while end < len(source):
            if source[end] == "{":
                depth += 1
            elif source[end] == "}":
                depth -= 1
                if depth == 0:
                    break
            end += 1
        if depth != 0:
            continue
        body = source[brace + 1:end]
        body = re.sub(r"/\*.*?\*/", " ", body, flags=re.S)
        body = re.sub(r"//.*", " ", body)
        body = re.sub(r"(?m)^\s*#.*$", " ", body)
        fields: list[dict[str, str]] = []
        for part in body.split(";"):
            declaration = " ".join(part.split())
            if not declaration:
                continue
            identifiers = re.findall(r"[A-Za-z_]\w*", declaration)
            if identifiers:
                fields.append({"name": identifiers[-1], "declaration": declaration})
        result.append({
            "name": name,
            "source": path.relative_to(include_root).as_posix(),
            "line": source.count("\n", 0, match.start()) + 1,
            "fields": fields,
        })
    return result


class SafeEvaluator(ast.NodeVisitor):
    BIN = {
        ast.Add: lambda a, b: a + b, ast.Sub: lambda a, b: a - b,
        ast.Mult: lambda a, b: a * b, ast.LShift: lambda a, b: a << b,
        ast.RShift: lambda a, b: a >> b, ast.BitOr: lambda a, b: a | b,
        ast.BitAnd: lambda a, b: a & b, ast.BitXor: lambda a, b: a ^ b,
    }
    UNARY = {ast.USub: lambda a: -a, ast.UAdd: lambda a: a, ast.Invert: lambda a: ~a}

    def __init__(self, symbols: dict[str, int | str] | None = None):
        self.symbols = symbols or {}

    def visit_Expression(self, node):
        return self.visit(node.body)

    def visit_Constant(self, node):
        if isinstance(node.value, (int, str)):
            return node.value
        raise ValueError

    def visit_Name(self, node):
        if node.id in self.symbols:
            return self.symbols[node.id]
        raise ValueError

    def visit_BinOp(self, node):
        operation = self.BIN.get(type(node.op))
        if operation is None:
            raise ValueError
        return operation(self.visit(node.left), self.visit(node.right))

    def visit_UnaryOp(self, node):
        operation = self.UNARY.get(type(node.op))
        if operation is None:
            raise ValueError
        return operation(self.visit(node.operand))

    def generic_visit(self, node):
        raise ValueError


def evaluate(expression: str, symbols: dict[str, int | str] | None = None) -> int | str | None:
    if not expression:
        return None
    value = re.sub(r"/\*.*", "", expression).strip()
    value = re.sub(r"//.*", "", value).strip()
    value = re.sub(r"\b0+([1-9][0-9]*)\b", r"\1", value)
    value = re.sub(r"\b(0x[0-9A-Fa-f]+|\d+)[uUlL]+\b", r"\1", value)
    value = re.sub(
        r"\((?:U?LONG|LONGBITS|Tag|IPTR|ULONG_PTR|WORD|UWORD|BYTE|UBYTE|"
        r"STRPTR|CONST_STRPTR|APTR|CONST_APTR)\)",
        "",
        value,
    )
    value = value.replace("TRUE", "1").replace("FALSE", "0").replace("NULL", "0")
    try:
        return SafeEvaluator(symbols).visit(ast.parse(value, mode="eval"))
    except (SyntaxError, ValueError, TypeError, OverflowError):
        return None


def expand_macro_calls(expression: str, functions: dict[str, tuple[list[str], str]]) -> str:
    def make_id(match: re.Match[str]) -> str:
        parts = [item.strip() for item in match.group(1).split(",")]
        if len(parts) != 4:
            return match.group(0)
        try:
            values = [ast.literal_eval(item) for item in parts]
        except (SyntaxError, ValueError):
            return match.group(0)
        bytes_: list[int] = []
        for item in values:
            if isinstance(item, str) and len(item) == 1:
                bytes_.append(ord(item))
            elif isinstance(item, int) and 0 <= item <= 255:
                bytes_.append(item)
            else:
                return match.group(0)
        result = sum(item << shift for item, shift in zip(bytes_, (24, 16, 8, 0)))
        return str(result)

    value = re.sub(r"\bMAKE_ID\(([^()]*)\)", make_id, expression)
    for _ in range(32):
        changed = False
        for name, (parameters, body) in functions.items():
            pattern = re.compile(rf"\b{re.escape(name)}\(([^()]*)\)")

            def replace(match: re.Match[str]) -> str:
                nonlocal changed
                arguments = [item.strip() for item in match.group(1).split(",")]
                if len(arguments) != len(parameters):
                    return match.group(0)
                expanded = body
                for parameter, argument in zip(parameters, arguments):
                    expanded = re.sub(rf"##\s*{re.escape(parameter)}\b", argument, expanded)
                    expanded = re.sub(rf"\b{re.escape(parameter)}\s*##", argument, expanded)
                    expanded = re.sub(rf"\b{re.escape(parameter)}\b", f"({argument})", expanded)
                expanded = expanded.replace("##", "")
                changed = True
                return f"({expanded})"

            value = pattern.sub(replace, value)
        if not changed:
            break
    return value


def csharp_constants(paths: list[Path]) -> tuple[dict[str, Any], set[str], set[str]]:
    constants: dict[str, Any] = {}
    types: set[str] = set()
    callables: set[str] = set()
    for path in paths:
        stack: list[tuple[str, int]] = []
        depth = 0
        pending: str | None = None
        for line in path.read_text(encoding="utf-8").splitlines():
            class_match = re.search(
                r"\b(?:public|internal)\s+(?:(?:readonly|static|unsafe|partial)\s+)*(?:class|struct|enum)\s+(\w+)",
                line,
            )
            if class_match:
                pending = class_match.group(1)
                types.add(pending)
            opens = line.count("{")
            closes = line.count("}")
            if pending is not None and opens:
                stack.append((pending, depth + 1))
                pending = None
            const_match = re.search(r"\bconst\s+(?:string|u?int|short|ushort|byte|sbyte|long|ulong)\s+(\w+)\s*=\s*(.+?);", line)
            if const_match:
                name, expression = const_match.groups()
                path_name = ".".join([item[0] for item in stack] + [name])
                constants[path_name] = evaluate(expression)
                constants[path_name + "#expression"] = normalize_expression(expression)
            callable_match = re.search(
                r"\bpublic\s+static\s+(?:extern\s+)?[A-Za-z0-9_<>]+\s+(\w+)\s*\(",
                line,
            )
            if callable_match:
                callables.add(".".join([item[0] for item in stack] + [callable_match.group(1)]))
            depth += opens - closes
            while stack and depth < stack[-1][1]:
                stack.pop()
    return constants, types, callables


def expected_paths(name: str) -> list[str]:
    if name == "MUIMASTER_NAME":
        return ["MUIMaster.Name"]
    if name in {"MUI_MAXMAX", "MUIMASTER_VMIN", "MUIMASTER_VLATEST"}:
        return [f"MUIConstants.{name}"]
    if name.startswith("MUIC_"):
        cls = name[5:]
        return [f"{cls}.NameClass", f"{cls}.Name"]
    if name.startswith("MUIA_"):
        parts = name[5:].split("_", 1)
        if len(parts) == 2:
            field = "_" + parts[1] if parts[1][:1].isdigit() else parts[1]
            return [f"{parts[0]}.{field}", f"Attribute.{name[5:]}"]
        return [f"Attribute.{name[5:]}"]
    if name.startswith("MUIM_"):
        parts = name[5:].split("_", 1)
        if len(parts) == 2:
            return [f"{parts[0]}.Method.{parts[1]}", f"Method.{name[5:]}"]
        return [f"Method.{name[5:]}"]
    if name.startswith("MUIF_"):
        return ["Flag." + ".".join(name[5:].split("_"))]
    if name.startswith("MUIV_"):
        parts = name[5:].split("_")
        global_path = f"Value.{'.'.join(parts)}"
        if len(parts) >= 3:
            return [
                f"{parts[0]}.Value.{parts[1]}.{'_'.join(parts[2:])}",
                f"{parts[0]}.Value.{'.'.join(parts[1:])}",
                global_path,
            ]
        if len(parts) == 2:
            return [f"{parts[0]}.Value.{parts[1]}", global_path]
        return [global_path]
    if name.startswith("MUIO_"):
        parts = name[5:].split("_")
        direct = f"MakeObject.{name[5:]}"
        if len(parts) == 1:
            return [direct, f"{direct}.Value"]
        return [direct, f"MakeObject.{parts[0]}.{'_'.join(parts[1:])}"]
    mappings = {"MUII_": "Image", "MUIE_": "Error", "MUIX_": "TextCode"}
    for prefix, cls in mappings.items():
        if name.startswith(prefix):
            return [f"{cls}.{name[len(prefix):]}"]
    return []


def compare_macros(
    macros: list[dict[str, Any]], constants: dict[str, Any], callables: set[str]
) -> None:
    functions: dict[str, tuple[list[str], str]] = {}
    for item in macros:
        if item["parameters"]:
            functions.setdefault(item["name"], (item["parameters"], item["expression"]))
    symbols: dict[str, int | str] = {"TAG_USER": 0x8000_0000, "TAG_IGNORE": 1}
    unresolved = [item for item in macros if not item["parameters"]]
    for _ in range(len(unresolved) + 1):
        changed = False
        for item in unresolved:
            expression = expand_macro_calls(item["expression"], functions)
            value = evaluate(expression, symbols)
            item["value"] = value
            if value is not None and symbols.get(item["name"]) != value:
                symbols[item["name"]] = value
                changed = True
        if not changed:
            break

    values_to_paths: dict[Any, list[str]] = {}
    for path, value in constants.items():
        if not path.endswith("#expression") and value is not None:
            values_to_paths.setdefault((type(value).__name__, value), []).append(path)
    for item in macros:
        value = item.get("value") if not item["parameters"] else None
        item["value"] = value
        if "URLTEXT_MCC_FALLBACK" in item["expression"]:
            item["profile_choice"] = "USE_HYPERLINK_MCC"
        if item["parameters"] and item["name"].startswith("MUIV_"):
            helper_path = f"MUIValueHelpers.{item['name']}"
            if helper_path in callables:
                item["csharp_member"] = helper_path
                item["comparison"] = "typed-helper-tested"
                continue
        if item["name"] == "MUIA_Urltext_NoOpenURLPrefs":
            item["csharp_member"] = "Urltext.NoOpenURLPrefs"
            item["profile_value"] = 1
            item["profile_choice"] = "USE_HYPERLINK_MCC"
            item["comparison"] = (
                "authority-malformed-profile-normalized"
                if constants.get("Urltext.NoOpenURLPrefs") == 1
                else "conflict"
            )
            continue
        if item["name"] == "MUIC_Urltext" and value == "Urltext.mcc":
            item["comparison"] = "profile-excluded-conditional"
            item["profile_choice"] = "USE_HYPERLINK_MCC"
            continue
        candidates = [f"MUIConstants.{item['name']}", *expected_paths(item["name"])]
        existing = next((path for path in candidates if path in constants), None)
        if existing:
            csharp_value = constants[existing]
            item["csharp_member"] = existing
            if value is None or csharp_value is None:
                item["comparison"] = "present-value-unverified"
            elif value == csharp_value or (isinstance(value, int) and isinstance(csharp_value, int) and (value & 0xFFFFFFFF) == (csharp_value & 0xFFFFFFFF)):
                item["comparison"] = "match"
            else:
                item["comparison"] = "conflict"
                item["csharp_value"] = csharp_value
        elif value is not None and (type(value).__name__, value) in values_to_paths:
            item["comparison"] = "present-value-unmapped"
            item["csharp_candidates"] = values_to_paths[(type(value).__name__, value)][:8]
        elif not item["expression"]:
            item["comparison"] = "preprocessor-marker-non-runtime"
        elif item["parameters"]:
            if item["name"] == "URLTEXT_MCC_FALLBACK" and any(
                condition.startswith("else(") for condition in item["conditions"]
            ):
                item["comparison"] = "profile-excluded-conditional"
                item["profile_choice"] = "USE_HYPERLINK_MCC"
            else:
                item["comparison"] = "c-preprocessor-helper-non-runtime"
        elif item["name"].startswith("MUIKEYF_"):
            item["comparison"] = "obsolete-profile-excluded"
            item["profile_reason"] = "MUI_OBSOLETE keys are not bitmasks"
        elif item["name"] in {"ihn_Signals", "ihn_Millis", "ihn_Current"}:
            item["comparison"] = "typed-field-alias"
            item["csharp_member"] = f"MUI_InputHandlerValue.{item['name']}"
        elif item["name"] == "MUIP_BoopsiQuery":
            item["comparison"] = "typed-type-alias"
            item["csharp_member"] = "MUIP_BoopsiQuery"
        elif item["name"].startswith("SCI_"):
            item["comparison"] = "external-header-alias"
            item["external_expression"] = item["expression"]
        else:
            item["comparison"] = "c-preprocessor-convenience-non-runtime"


def parse_ppc_vectors(path: Path) -> list[dict[str, Any]]:
    source = text(path)
    rows: list[dict[str, Any]] = []
    pattern = re.compile(r"#define\s+(MUI_\w+)\([^\n]*\)\s*\\\s*\n\s*LP\d+(?:NR)?\((\d+),\s*(?:[^,]+,\s*)?(MUI_\w+),(?P<body>.*?)(?=\n\n#define|\Z)", re.S)
    for match in pattern.finditer(source):
        name = match.group(1)
        registers = re.findall(r"__p\d+,\s*([ad][0-7])\b", match.group("body"), re.I)
        rows.append({"name": name, "lvo": -int(match.group(2)), "registers": [item.upper() for item in registers], "authority": "ppcinline/muimaster.h"})
    return rows


def parse_csharp_functions(path: Path) -> dict[str, dict[str, Any]]:
    source = path.read_text(encoding="utf-8")
    result: dict[str, dict[str, Any]] = {}
    pattern = re.compile(r"\[AmigaLvo\((-?\d+)\)\](?P<body>.*?public\s+static\s+extern\s+[^\s]+\s+(?P<name>MUI_\w+)\s*\((?P<args>.*?)\);)", re.S)
    for match in pattern.finditer(source):
        body = match.group("body")
        registers = re.findall(r"\[M68kRegister\(M68kRegister\.([AD][0-7])\)\]", match.group("args"))
        result[match.group("name")] = {"lvo": int(match.group(1)), "registers": registers, "return_register": "D0" if "[return: M68kRegister(M68kRegister.D0)]" in body else None}
    return result


def compare_vectors(vectors: list[dict[str, Any]], csharp: dict[str, dict[str, Any]]) -> None:
    for row in vectors:
        actual = csharp.get(row["name"])
        if actual is None:
            row["comparison"] = "missing"
        elif actual["lvo"] != row["lvo"] or actual["registers"] != row["registers"]:
            row["comparison"] = "conflict"
            row["csharp"] = actual
        else:
            row["comparison"] = "match"


def class_inventory(autodoc_root: Path, macros: list[dict[str, Any]]) -> list[dict[str, Any]]:
    class_ids = {item["name"][5:]: evaluate(item["expression"]) for item in macros if item["name"].startswith("MUIC_")}
    rows: list[dict[str, Any]] = []
    for path in sorted(autodoc_root.glob("MUI_*.doc"), key=lambda item: item.name.lower()):
        class_name = path.stem[4:]
        component_match = re.search(r"(?m)^([A-Za-z0-9]+)\.(mui|mcc)/", text(path))
        component = component_match.group(1) + "." + component_match.group(2) if component_match else None
        external = bool(component and component.lower().endswith(".mcc"))
        rows.append({
            "class": class_name,
            "class_id": class_ids.get(class_name),
            "component": component,
            "packaging": "external-component" if external else "loader-transparent-packaging-unverified",
            "evidence": "autodoc-section-prefix" if component else "official-index-autodoc-filename",
            "implementation_disposition": "planned",
        })
    return rows


def summarize(rows: list[dict[str, Any]], key: str) -> dict[str, int]:
    result: dict[str, int] = {}
    for row in rows:
        value = str(row.get(key, "unknown"))
        result[value] = result.get(value, 0) + 1
    return dict(sorted(result.items()))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--sdk-include", type=Path, required=True)
    parser.add_argument("--sdk-autodoc", type=Path, required=True)
    parser.add_argument("--coppersharp", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    include_root = args.sdk_include.resolve()
    authority_files = [include_root / "libraries" / "mui.h"] + sorted((include_root / "mui").glob("*.h"))
    for required in [include_root / "libraries" / "mui.h", include_root / "clib" / "muimaster_protos.h", include_root / "fd" / "muimaster_lib.fd", include_root / "ppcinline" / "muimaster.h"]:
        if not required.is_file():
            parser.error(f"missing SDK authority file: {required}")

    macros = [row for path in authority_files for row in extract_macros(path, include_root)]
    structures = [row for path in authority_files for row in extract_structures(path, include_root)]
    csharp_files = sorted((args.coppersharp / "Sdk.Amiga" / "MUIMaster").glob("*.cs"))
    constants, csharp_types, callables = csharp_constants(csharp_files)
    compare_macros(macros, constants, callables)
    for structure in structures:
        structure["comparison"] = "present" if structure["name"] in csharp_types else "missing"

    vectors = parse_ppc_vectors(include_root / "ppcinline" / "muimaster.h")
    compare_vectors(vectors, parse_csharp_functions(args.coppersharp / "Sdk.Amiga" / "MUIMaster" / "MUIMaster.cs"))
    classes = class_inventory(args.sdk_autodoc, macros)

    inventory = {
        "schema": 1,
        "profile": "MorphOs320M68k",
        "authorities": [
            "MorphOS 3.20 SDK libraries/mui.h and mui/*.h",
            "MorphOS 3.20 SDK clib/muimaster_protos.h",
            "MorphOS 3.20 SDK ppcinline/muimaster.h",
            "MorphOS 3.20 SDK fd/muimaster_lib.fd (known stale for MUI_RequestObjectA)",
            "MorphOS 3.20 SDK MUI autodoc index",
        ],
        "copyright_policy": "Identifiers and ABI facts only; no SDK prose, examples, comments, or source text are embedded.",
        "summary": {
            "public_vectors": len(vectors),
            "vector_comparison": summarize(vectors, "comparison"),
            "official_standard_classes": len(classes),
            "class_packaging": summarize(classes, "packaging"),
            "macro_declarations": len(macros),
            "macro_categories": summarize(macros, "category"),
            "macro_comparison": summarize(macros, "comparison"),
            "structures_and_messages": len(structures),
            "structure_comparison": summarize(structures, "comparison"),
        },
        "vectors": vectors,
        "classes": classes,
        "macros": macros,
        "structures_and_messages": structures,
        "known_authority_conflicts": [{
            "item": "MUI_RequestObjectA",
            "disposition": "admit-at-lvo-minus-756",
            "evidence": "present in clib prototype and ppcinline vector 756; omitted by stale fd file",
        }],
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(inventory, indent=2, sort_keys=False) + "\n", encoding="utf-8")
    print(json.dumps(inventory["summary"], indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
