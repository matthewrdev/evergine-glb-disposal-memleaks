#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TARGET_VERSION="2025.10.21.8"

python3 - "$SCRIPT_DIR" "$TARGET_VERSION" <<'PY'
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
version = sys.argv[2]
pattern = re.compile(
    r'(?P<prefix><PackageReference\s+Include="(?P<name>Evergine[^"]*)"\s+Version=")'
    r'(?P<value>[^"]+)'
    r'(?P<suffix>")'
)

for csproj in root.rglob("*.csproj"):
    text = csproj.read_text(encoding="utf-8")
    def repl(match):
        name = match.group("name")
        if "Draco" in name:
            return match.group(0)
        return f"{match.group('prefix')}{version}{match.group('suffix')}"

    updated, count = pattern.subn(repl, text)
    if count > 0:
        csproj.write_text(updated, encoding="utf-8")
        print(f"Updated {csproj}")

print("Done.")
PY
