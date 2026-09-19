#!/usr/bin/env python3
"""Convert DSP Locale files and prototype tables into LiveStreamAssist JS text data."""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from pathlib import Path


DSP_APPID = "1366540"
DEFAULT_UNITY_VERSION = "2022.3.7"
KIND_FROM_SET = {
    "ItemProtoSet": "item",
    "TechProtoSet": "tech",
    "RecipeProtoSet": "recipe",
    "SignalProtoSet": "signal",
    "VeinProtoSet": "vein",
    "EnemyProtoSet": "enemy",
    "ModelProtoSet": "model",
    "ThemeProtoSet": "theme",
    "AchievementProtoSet": "achievement",
    "TutorialProtoSet": "tutorial",
    "MilestoneProtoSet": "milestone",
    "GoalProtoSet": "goal",
    "AudioProtoSet": "audio",
    "PromptProtoSet": "prompt",
    "AdvisorTipProtoSet": "advisorTip",
    "JournalPatternProtoSet": "journalPattern",
    "CosmicMessageProtoSet": "cosmicMessage",
    "DoodadProtoSet": "doodad",
    "AbnormalityProtoSet": "abnormality",
    "FleetProtoSet": "fleet",
    "CreationPartProtoSet": "creationPart",
    "EffectEmitterProtoSet": "effectEmitter",
    "MIDIProtoSet": "midi",
    "PlayerProtoSet": "player",
    "VegeProtoSet": "vege",
}
EXTRA_STRING_KEYS = ("千分分隔符", "小数点", "杠等级")


def unescape_string(text: str) -> str:
    out = []
    i = 0
    while i < len(text):
        ch = text[i]
        if ch == "\\" and i + 1 < len(text):
            i += 1
            nxt = text[i]
            mapping = {"\\": "\\", "r": "\r", "n": "\n", "t": "\t", "v": "\v", "f": "\f"}
            if nxt in mapping:
                out.append(mapping[nxt])
            else:
                out.append("\\")
                out.append(nxt)
        else:
            out.append(ch)
        i += 1
    return "".join(out)


def first_field(line: str) -> str:
    tab = line.find("\t")
    raw = line if tab < 0 else line[:tab]
    return unescape_string(raw)


def translation_field(line: str) -> str | None:
    tab1 = line.find("	")
    if tab1 < 0:
        return None
    tab2 = line.find("	", tab1 + 1)
    if tab2 < 0:
        return None
    tab3 = line.find("	", tab2 + 1)
    if tab3 < 0:
        return None
    return unescape_string(line[tab3 + 1 :])


def read_locale_text(path: Path) -> str:
    data = path.read_bytes()
    if data.startswith((b'\xff\xfe', b'\xfe\xff')):
        return data.decode('utf-16')
    if data.startswith(b'\xef\xbb\xbf'):
        return data.decode('utf-8-sig')
    try:
        return data.decode('utf-8')
    except UnicodeDecodeError:
        return data.decode('utf-16')


def parse_header(header_path: Path) -> tuple[list[dict], list[str]]:
    text = read_locale_text(header_path)
    lines = text.splitlines()
    languages: list[dict] = []
    page_order: dict[str, int] = {}
    in_langs = False
    for line in lines:
        stripped = line.strip()
        if stripped.startswith("Version="):
            in_langs = True
            continue
        if in_langs and stripped and "," in stripped and not stripped.startswith("[") and "=" not in stripped.split(",", 1)[0]:
            parts = stripped.split(",")
            if len(parts) < 2:
                continue
            try:
                lcid = int(parts[0])
            except ValueError:
                continue
            if lcid <= 0 or any(lang["lcid"] == lcid for lang in languages):
                continue
            languages.append(
                {
                    "lcid": lcid,
                    "name": parts[1],
                    "abbr": parts[2] if len(parts) > 2 else parts[1].lower(),
                    "abbr2": parts[3] if len(parts) > 3 else (parts[2] if len(parts) > 2 else parts[1].lower()),
                    "fallback": int(parts[4]) if len(parts) > 4 and parts[4].isdigit() else 0,
                }
            )
            continue
        if "=" in stripped:
            key, _, value = stripped.partition("=")
            key = key.strip()
            try:
                page_order[key] = int(value.strip())
            except ValueError:
                continue
    pages = sorted(page_order, key=page_order.get)
    return languages, [name + ".txt" for name in pages]


def load_name_keys(names_dir: Path, pages: list[str]) -> list[str]:
    keys: list[str] = []
    seen: set[str] = set()
    for page in pages:
        path = names_dir / page
        if not path.is_file():
            continue
        for line in read_locale_text(path).splitlines():
            if not line:
                continue
            key = first_field(line)
            if not key or key in seen:
                continue
            seen.add(key)
            keys.append(key)
    return keys


def load_language_strings(lang_dir: Path, pages: list[str], keys: list[str]) -> dict[str, str]:
    index = {key: i for i, key in enumerate(keys)}
    values: list[str | None] = [None] * len(keys)
    for page in pages:
        path = lang_dir / page
        if not path.is_file():
            continue
        for line in read_locale_text(path).splitlines():
            if not line:
                continue
            key = first_field(line)
            if key not in index:
                continue
            slot = index[key]
            if values[slot] is not None:
                continue
            translated = translation_field(line)
            if translated is None:
                continue
            values[slot] = translated
    result = {}
    for key, value in zip(keys, values):
        result[key] = key if value is None else value
    return result


def steam_path() -> Path | None:
    if os.name != "nt":
        return None
    try:
        import winreg
    except ImportError:
        return None
    roots = [
        (winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam"),
        (winreg.HKEY_LOCAL_MACHINE, r"Software\Valve\Steam"),
        (winreg.HKEY_LOCAL_MACHINE, r"Software\Wow6432Node\Valve\Steam"),
    ]
    for hive, key in roots:
        try:
            with winreg.OpenKey(hive, key) as handle:
                value, _ = winreg.QueryValueEx(handle, "SteamPath")
        except OSError:
            continue
        if value:
            path = Path(str(value))
            if path.exists():
                return path
    return None


def parse_library_folders(vdf_text: str) -> list[Path]:
    block_re = re.compile(r'"(?:\d+)"\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}', re.S)
    path_re = re.compile(r'"path"\s+"([^"]+)"')
    app_re = re.compile(r'"' + DSP_APPID + r'"\s+"[^"]+"')
    paths = []
    for block in block_re.finditer(vdf_text):
        text = block.group(1)
        if not app_re.search(text):
            continue
        match = path_re.search(text)
        if not match:
            continue
        raw = match.group(1).replace("\\\\", "\\").replace("/", "\\")
        paths.append(Path(raw))
    return paths


def find_game_root(explicit: str | None) -> Path | None:
    if explicit:
        path = Path(explicit)
        if (path / "DSPGAME_Data").is_dir():
            return path
        raise SystemExit(f"DSP game root not found at {path}")
    env = os.environ.get("DSP_ROOT") or os.environ.get("DSPGAME_ROOT")
    if env:
        path = Path(env)
        if (path / "DSPGAME_Data").is_dir():
            return path
    steam = steam_path()
    if steam is None:
        return None
    vdf = steam / "steamapps" / "libraryfolders.vdf"
    if not vdf.is_file():
        return None
    for library in parse_library_folders(vdf.read_text(encoding="utf-8", errors="ignore")):
        candidate = library / "steamapps" / "common" / "Dyson Sphere Program"
        if (candidate / "DSPGAME_Data").is_dir():
            return candidate
    fallback = steam / "steamapps" / "common" / "Dyson Sphere Program"
    if (fallback / "DSPGAME_Data").is_dir():
        return fallback
    return None


def extract_proto_map(
    game_root: Path, unity_version: str, class_names: set[str] | None = None
) -> dict[str, dict[int, dict]]:
    """Extract prototype entries keyed by kind and ID, keeping the raw fields."""
    try:
        import UnityPy
        from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
    except ImportError as exc:
        raise SystemExit(
            "UnityPy and TypeTreeGeneratorAPI are required to extract prototype IDs. "
            "Install LiveStreamAssist/js/scripts/requirements.txt."
        ) from exc

    wanted = class_names or KIND_FROM_SET
    data_dir = game_root / "DSPGAME_Data"
    generator = TypeTreeGenerator(unity_version)
    generator.load_local_game(str(game_root))
    ggm = UnityPy.load(str(data_dir / "globalgamemanagers.assets"))
    script_ids = {}
    for obj in ggm.objects:
        if getattr(obj.type, "name", str(obj.type)) != "MonoScript":
            continue
        script = obj.read()
        class_name = getattr(script, "m_ClassName", "")
        if class_name in wanted:
            script_ids[obj.path_id] = class_name

    resources = UnityPy.load(str(data_dir / "resources.assets"))
    nodes = {
        class_name: generator.get_nodes_up("Assembly-CSharp.dll", class_name)
        for class_name in wanted
    }
    protos: dict[str, dict[int, dict]] = {KIND_FROM_SET[cn]: {} for cn in wanted}
    for obj in resources.objects:
        if getattr(obj.type, "name", str(obj.type)) != "MonoBehaviour":
            continue
        try:
            peek = obj.read_typetree(check_read=False)
        except Exception:
            continue
        script = peek.get("m_Script") or {}
        class_name = script_ids.get(script.get("m_PathID"))
        if class_name is None:
            continue
        kind = KIND_FROM_SET[class_name]
        tree = obj.read_typetree(nodes[class_name], check_read=False)
        for proto in tree.get("dataArray") or []:
            proto_id = proto.get("ID")
            if not proto_id:
                continue
            protos[kind][int(proto_id)] = proto
    return {kind: mapping for kind, mapping in protos.items() if mapping}


def extract_proto_ids(game_root: Path, unity_version: str) -> dict[str, dict[str, str]]:
    ids: dict[str, dict[str, str]] = {}
    for kind, protos in extract_proto_map(game_root, unity_version).items():
        mapping: dict[str, str] = {}
        for proto_id, proto in protos.items():
            name = proto.get("Name")
            if not name:
                continue
            mapping[str(proto_id)] = name
        ids[kind] = mapping
    return {kind: mapping for kind, mapping in ids.items() if mapping}


def collect_needed_keys(all_keys: list[str], ids: dict[str, dict[str, str]]) -> list[str]:
    needed = set(EXTRA_STRING_KEYS)
    for mapping in ids.values():
        needed.update(mapping.values())
    ordered = [key for key in all_keys if key in needed]
    for key in EXTRA_STRING_KEYS:
        if key not in ordered:
            ordered.append(key)
    return ordered


def build_payload(game_root: Path, unity_version: str) -> dict:
    locale_dir = game_root / "Locale"
    header = locale_dir / "Header.txt"
    if not header.is_file():
        raise SystemExit(f"Locale header not found: {header}")
    languages, pages = parse_header(header)
    keys = load_name_keys(locale_dir / "Names", pages)
    ids = extract_proto_ids(game_root, unity_version)
    if not ids.get("item") or not ids.get("tech"):
        raise SystemExit("Prototype extraction did not produce item and tech IDs; generated data was not written.")
    needed_keys = collect_needed_keys(keys, ids)
    strings: dict[str, dict[str, str]] = {key: {} for key in needed_keys}
    language_meta = {}
    for lang in languages:
        lang_dir = locale_dir / str(lang["lcid"])
        if not lang_dir.is_dir():
            continue
        table = load_language_strings(lang_dir, pages, keys)
        language_meta[str(lang["lcid"])] = lang
        for key in needed_keys:
            strings[key][str(lang["lcid"])] = table.get(key, key)
    return {
        "version": "1.0.0",
        "languages": language_meta,
        "ids": ids,
        "strings": strings,
    }


def emit_js(payload: dict) -> str:
    body = json.dumps(payload, ensure_ascii=False, separators=(",", ":"))
    return (
        "/* Generated from a local Dyson Sphere Program install by generate_text.py. Do not edit. */\n"
        "globalThis.LiveStreamAssistTextData = "
        + body
        + ";\n"
    )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-root", help="DSP install directory containing DSPGAME_Data and Locale")
    parser.add_argument("--unity-version", default=DEFAULT_UNITY_VERSION)
    parser.add_argument(
        "--out",
        default=str(Path(__file__).resolve().parents[1] / "generated" / "dsp-text.js"),
        help="Output JS path",
    )
    parser.add_argument("--json-out", help="Optional JSON output path")
    args = parser.parse_args(argv)
    game_root = find_game_root(args.game_root)
    if game_root is None:
        raise SystemExit("Dyson Sphere Program install not found. Pass --game-root.")
    payload = build_payload(game_root, args.unity_version)
    out_path = Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(emit_js(payload), encoding="utf-8", newline="\n")
    if args.json_out:
        json_path = Path(args.json_out)
        json_path.parent.mkdir(parents=True, exist_ok=True)
        json_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8", newline="\n")
    item_count = len(payload["ids"].get("item", {}))
    tech_count = len(payload["ids"].get("tech", {}))
    print(f"Wrote {out_path} (items={item_count}, techs={tech_count}, strings={len(payload['strings'])})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
