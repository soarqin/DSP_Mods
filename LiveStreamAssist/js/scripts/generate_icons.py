#!/usr/bin/env python3
"""Extract DSP item and tech icons into LiveStreamAssist JS icon data."""

from __future__ import annotations

import argparse
import base64
import io
import json
import sys
from pathlib import Path

from generate_text import DEFAULT_UNITY_VERSION, extract_proto_map, find_game_root

ICON_CLASSES = ("ItemProtoSet", "TechProtoSet")
ICON_SIZE = 80
TEXTURE_FILES = ("resources.assets", "sharedassets0.assets")


def collect_icon_names(game_root: Path, unity_version: str) -> dict[str, dict[int, str]]:
    """Map item and tech IDs to the texture name from their IconPath field."""
    names: dict[str, dict[int, str]] = {}
    for kind, protos in extract_proto_map(game_root, unity_version, ICON_CLASSES).items():
        mapping: dict[int, str] = {}
        for proto_id, proto in protos.items():
            icon_path = proto.get("IconPath")
            if not icon_path:
                continue
            mapping[proto_id] = icon_path.split("/")[-1]
        names[kind] = mapping
    return {kind: mapping for kind, mapping in names.items() if mapping}


def load_icon_textures(data_dir: Path, names: set[str]) -> dict[str, object]:
    """Load the named textures, preferring the standard square icon size."""
    import UnityPy

    textures: dict[str, tuple[object, bool]] = {}
    for filename in TEXTURE_FILES:
        path = data_dir / filename
        if not path.is_file():
            continue
        env = UnityPy.load(str(path))
        for obj in env.objects:
            if getattr(obj.type, "name", str(obj.type)) != "Texture2D":
                continue
            try:
                data = obj.read()
            except Exception:
                continue
            if data.m_Name not in names:
                continue
            square = data.m_Width == ICON_SIZE and data.m_Height == ICON_SIZE
            existing = textures.get(data.m_Name)
            if existing is None or (square and not existing[1]):
                textures[data.m_Name] = (data, square)
    return {name: value[0] for name, value in textures.items()}


def encode_icon(texture: object) -> str:
    try:
        image = texture.image
    except ImportError as exc:
        raise SystemExit(
            "Pillow is required to encode icons. Install LiveStreamAssist/js/scripts/requirements.txt."
        ) from exc
    if image.mode != "RGBA":
        image = image.convert("RGBA")
    buffer = io.BytesIO()
    image.save(buffer, "PNG", optimize=True)
    return "data:image/png;base64," + base64.b64encode(buffer.getvalue()).decode("ascii")


def build_payload(game_root: Path, unity_version: str) -> tuple[dict, list[str]]:
    names = collect_icon_names(game_root, unity_version)
    if not names.get("item") or not names.get("tech"):
        raise SystemExit("Prototype extraction did not produce item and tech icons; generated data was not written.")
    textures = load_icon_textures(game_root / "DSPGAME_Data", {name for mapping in names.values() for name in mapping.values()})
    icons: dict[str, dict[str, str]] = {}
    missing: list[str] = []
    for kind, mapping in names.items():
        icons[kind] = {}
        for proto_id, texture_name in sorted(mapping.items()):
            texture = textures.get(texture_name)
            if texture is None:
                missing.append(f"{kind} {proto_id} ({texture_name})")
                continue
            icons[kind][str(proto_id)] = encode_icon(texture)
    return {"version": "1.0.0", "icons": icons}, missing


def emit_js(payload: dict) -> str:
    body = json.dumps(payload, ensure_ascii=False, separators=(",", ":"))
    return (
        "/* Generated from a local Dyson Sphere Program install by generate_icons.py. Do not edit. */\n"
        "globalThis.LiveStreamAssistIconData = "
        + body
        + ";\n"
    )


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-root", help="DSP install directory containing DSPGAME_Data")
    parser.add_argument("--unity-version", default=DEFAULT_UNITY_VERSION)
    parser.add_argument(
        "--out",
        default=str(Path(__file__).resolve().parents[1] / "generated" / "dsp-icons.js"),
        help="Output JS path",
    )
    parser.add_argument("--json-out", help="Optional JSON output path")
    args = parser.parse_args(argv)
    game_root = find_game_root(args.game_root)
    if game_root is None:
        raise SystemExit("Dyson Sphere Program install not found. Pass --game-root.")
    payload, missing = build_payload(game_root, args.unity_version)
    for entry in missing:
        print(f"Warning: icon texture not found for {entry}", file=sys.stderr)
    out_path = Path(args.out)
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(emit_js(payload), encoding="utf-8", newline="\n")
    if args.json_out:
        json_path = Path(args.json_out)
        json_path.parent.mkdir(parents=True, exist_ok=True)
        json_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8", newline="\n")
    item_count = len(payload["icons"].get("item", {}))
    tech_count = len(payload["icons"].get("tech", {}))
    print(f"Wrote {out_path} (items={item_count}, techs={tech_count}, bytes={out_path.stat().st_size})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
