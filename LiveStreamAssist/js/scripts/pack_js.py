#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
OUT = ROOT / "livestream-assist.js"
parts = [
    SRC / "header.js",
    SRC / "format.js",
    SRC / "client.js",
    SRC / "query.js",
    SRC / "text.js",
    SRC / "icons.js",
    SRC / "footer.js",
]
text = "".join(path.read_text(encoding="utf-8") for path in parts)
if not text.endswith("\n"):
    text += "\n"
OUT.write_text(text, encoding="utf-8", newline="\n")
print(f"Wrote {OUT} ({OUT.stat().st_size} bytes)")
