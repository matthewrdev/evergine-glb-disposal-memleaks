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
pattern = re.compile(r'(<PackageReference\s+Include="Evergine[^"]*"\s+Version=")[^"]+(")')

for csproj in root.rglob("*.csproj"):
    text = csproj.read_text(encoding="utf-8")
    updated, count = pattern.subn(rf"\1{version}\2", text)
    if count > 0:
        csproj.write_text(updated, encoding="utf-8")
        print(f"Updated {csproj}")

print("Done.")
PY
