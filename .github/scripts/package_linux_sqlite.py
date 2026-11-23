#!/usr/bin/env python3
"""Add the Linux x64 SQLite native library to a JPRM plugin package and verify it."""
from __future__ import annotations

import io
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
        raise SystemExit("usage: package_linux_sqlite.py <jprm-package.zip> <libe_sqlite3.so>")

    package_path = Path(sys.argv[1])
    native_path = Path(sys.argv[2])
    if not package_path.is_file():
        raise SystemExit(f"JPRM package not found: {package_path}")
    if not native_path.is_file():
        raise SystemExit(f"Linux x64 SQLite native library not found: {native_path}")

    with zipfile.ZipFile(package_path, "r") as outer:
        outer_entries = [(entry, outer.read(entry)) for entry in outer.namelist()]
    plugin_entries = [name for name, _ in outer_entries if name.lower().endswith(".zip")]
    if len(plugin_entries) != 1:
        raise SystemExit(f"Expected one plugin ZIP inside JPRM output, found: {plugin_entries}")

    plugin_name = plugin_entries[0]
    plugin_bytes = dict(outer_entries)[plugin_name]
    with zipfile.ZipFile(io.BytesIO(plugin_bytes), "r") as plugin:
        entries = {name: plugin.read(name) for name in plugin.namelist()}

    entries["libe_sqlite3.so"] = native_path.read_bytes()
    missing = REQUIRED - entries.keys()
    if missing:
        raise SystemExit("Plugin package is missing required files: " + ", ".join(sorted(missing)))

    rebuilt_plugin = io.BytesIO()
    with zipfile.ZipFile(rebuilt_plugin, "w", zipfile.ZIP_DEFLATED) as plugin:
        for name, data in entries.items():
            plugin.writestr(name, data)

    rebuilt_outer = package_path.with_suffix(".rebuilt.zip")
    with zipfile.ZipFile(rebuilt_outer, "w", zipfile.ZIP_DEFLATED) as outer:
        for name, data in outer_entries:
            outer.writestr(name, rebuilt_plugin.getvalue() if name == plugin_name else data)
    rebuilt_outer.replace(package_path)

    print("Verified plugin package contents:")
    for name in sorted(REQUIRED):
        print(f" - {name} ({len(entries[name])} bytes)")


if __name__ == "__main__":
    main()
