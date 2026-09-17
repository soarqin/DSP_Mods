#!/usr/bin/env python3
from __future__ import annotations

import sys
import tempfile
from pathlib import Path
from unittest.mock import patch

SCRIPT_DIR = Path(__file__).resolve().parent
sys.path.insert(0, str(SCRIPT_DIR))

from generate_text import (
    build_payload,
    find_game_root,
    first_field,
    load_language_strings,
    load_name_keys,
    parse_header,
    read_locale_text,
    translation_field,
    unescape_string,
)
from kmg import THIN_SPACE, format_kmg, write_kmg


def assert_eq(actual, expected, label: str) -> None:
    if actual != expected:
        raise SystemExit(f"{label}: expected {expected!r}, got {actual!r}")


def test_unescape() -> None:
    assert_eq(unescape_string("plain"), "plain", "plain")
    assert_eq(unescape_string("a\\nb"), "a\nb", "newline")
    assert_eq(unescape_string("a\\tb"), "a\tb", "tab")
    assert_eq(unescape_string("c:\\\\x"), "c:\\x", "backslash")


def test_header_and_locale() -> None:
    game = find_game_root(None)
    if game is None:
        print("skip locale tests: DSP not found")
        return
    languages, pages = parse_header(game / "Locale" / "Header.txt")
    lcids = {lang["lcid"] for lang in languages}
    if 1033 not in lcids or 2052 not in lcids:
        raise SystemExit(f"missing core languages: {lcids}")
    if "prototype.txt" not in pages:
        raise SystemExit("prototype.txt missing from page order")
    iron = None
    for line in read_locale_text(game / "Locale" / "1033" / "prototype.txt").splitlines():
        if first_field(line) == "铁矿":
            iron = translation_field(line)
            break
    assert_eq(iron, "Iron Ore", "iron ore en")


def test_locale_bom_and_empty_translations() -> None:
    with tempfile.TemporaryDirectory() as directory:
        locale_dir = Path(directory)
        locale_file = locale_dir / "prototype.txt"
        source = "first\t\tPrototype\tTranslated\nempty\t\tPrototype\t\n"
        for encoding in ("utf-16", "utf-8-sig"):
            locale_file.write_text(source, encoding=encoding, newline="\n")
            assert_eq(read_locale_text(locale_file), source, f"decode {encoding}")
            keys = load_name_keys(locale_dir, ["prototype.txt"])
            assert_eq(keys, ["first", "empty"], f"keys {encoding}")
            values = load_language_strings(locale_dir, ["prototype.txt"], keys)
            assert_eq(values, {"first": "Translated", "empty": ""}, f"translations {encoding}")
        locale_file.write_bytes(b"\xfe\xff" + source.encode("utf-16-be"))
        assert_eq(read_locale_text(locale_file), source, "decode utf-16-be")


def test_header_page_order() -> None:
    with tempfile.TemporaryDirectory() as directory:
        header = Path(directory) / "Header.txt"
        header.write_text(
            "[Localization Project]\nVersion=1.1\n1033,English,en-US,en,2052,0\n\n"
            "keys=0\ncustom=0\nbase=0\nprototype=-1\n",
            encoding="utf-16",
        )
        _, pages = parse_header(header)
        assert_eq(pages, ["prototype.txt", "keys.txt", "custom.txt", "base.txt"], "stable page order")


def test_incomplete_prototypes_are_rejected() -> None:
    with tempfile.TemporaryDirectory() as directory:
        game = Path(directory)
        locale_dir = game / "Locale"
        locale_dir.mkdir()
        (locale_dir / "Header.txt").write_text(
            "Version=1.1\n1033,English,en-US,en,2052,0\n\nprototype=-1\n", encoding="utf-8"
        )
        with patch("generate_text.extract_proto_ids", return_value={}):
            try:
                build_payload(game, "2022.3.7")
            except SystemExit:
                return
        raise SystemExit("incomplete prototypes must not overwrite generated data")


def test_kmg() -> None:
    cases = [
        (0, "count", "0"),
        (12, "count", "12"),
        (9999, "count", "9999"),
        (10000, "count", "10.0k"),
        (12345, "count", "12.3k"),
        (999999, "count", "999k"),
        (1000000, "count", "1.00M"),
        (1234567890, "count", "1.23G"),
        (-12345, "count", "-12.3k"),
        (12345, "power", "12.3kW"),
        (123, "power", "123W"),
        (0, "power", "0W"),
        (12345, "energy", "12.3kJ"),
        (999, "si1000", "999"),
        (1000, "si1000", "1.00k"),
        (1234, "si1000", "1.23k"),
    ]
    for value, kind, expected in cases:
        assert_eq(format_kmg(value, kind=kind), expected, f"kmg {value} {kind}")
    raw = write_kmg(12345, blank=True)
    if THIN_SPACE + "k" not in raw:
        raise SystemExit(f"blank thin space missing: {raw!r}")


def main() -> None:
    test_unescape()
    test_kmg()
    test_locale_bom_and_empty_translations()
    test_header_page_order()
    test_incomplete_prototypes_are_rejected()
    test_header_and_locale()
    print("ok")


if __name__ == "__main__":
    main()
