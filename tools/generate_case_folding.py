"""Development-only: freeze Python casefold data; the WPF app does not use Python."""
import json
import unicodedata
from pathlib import Path

target = Path(__file__).resolve().parents[1] / "ReciteWords" / "Resources" / "CaseFolding.json"
mapping = {}
for codepoint in range(0x110000):
    character = chr(codepoint)
    folded = character.casefold()
    if character != folded:
        mapping[str(codepoint)] = folded
target.write_text(json.dumps(mapping, ensure_ascii=True, separators=(",", ":")) + "\n", encoding="utf-8")
print(f"Unicode {unicodedata.unidata_version}: {len(mapping)} mappings -> {target}")
