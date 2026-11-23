#!/usr/bin/env python3
"""Add the Linux x64 SQLite native library to a JPRM plugin ZIP and verify it."""
from __future__ import annotations

import sys
import zipfile
from pathlib import Path

REQUIRED = {
    "Jellyfin.Plugin.ElArcaWrapped.dll",
    "Microsoft.Data.Sqlite.dll",
    "SQLitePCLRaw.batteries_v2.dll",
    "SQLitePCLRaw.core.dll",
    "SQLitePCLRaw.provider.e_sqlite3.dll",
    "libe_sqlite3.so",
    "meta.json",
}


def main() -> None:
    if len(sys.argv) != 3:
        raise SystemExit("usage: package_linux_sqlite.py <plugin-package.zip> <libe_sqlite3.so>")

    package_path = Path(sys.argv[1])
    native_path = Path(sys.argv[2])
    if not package_path.is_file():
        raise SystemExit(f"JPRM package not found: {package_path}")
    if not native_path.is_file():
        raise SystemExit(f"Linux x64 SQLite native library not found: {native_path}")

    with zipfile.ZipFile(package_path, "r") as package:
        entries = {name: package.read(name) for name in package.namelist()}

    entries["libe_sqlite3.so"] = native_path.read_bytes()
    missing = REQUIRED - entries.keys()
    if missing:
        raise SystemExit("Plugin package is missing required files: " + ", ".join(sorted(missing)))

    rebuilt = package_path.with_suffix(".rebuilt.zip")
    with zipfile.ZipFile(rebuilt, "w", zipfile.ZIP_DEFLATED) as package:
        for name, data in entries.items():
            package.writestr(name, data)
    rebuilt.replace(package_path)

    print("Verified plugin package contents:")
    for name in sorted(REQUIRED):
        print(f" - {name} ({len(entries[name])} bytes)")


if __name__ == "__main__":
    main()
