#!/usr/bin/env python3
"""Generate missing Unity .meta files with deterministic GUIDs.

GUID = md5("web3fps-game-foundation:" + path relative to the package root),
so every machine that runs this produces identical GUIDs and cross-machine
prefab/scene references to package scripts stay stable.

Skips: existing .meta files (their GUIDs are already referenced), the Samples~
subtree (folders ending in ~ are hidden from the asset database and must not
carry metas), and .DS_Store noise.
"""
import hashlib
import os
import sys

PACKAGE_ROOT = os.environ.get("PACKAGE_ROOT", os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "UnityWeb3FpsGameFoundation")))
SALT = "web3fps-game-foundation:"

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

FILE_META = """fileFormatVersion: 2
guid: {guid}
"""


def guid_for(rel_path: str) -> str:
    return hashlib.md5((SALT + rel_path.replace(os.sep, "/")).encode("utf-8")).hexdigest()


def is_hidden(rel_path: str) -> bool:
    parts = rel_path.replace(os.sep, "/").split("/")
    return any(part.endswith("~") or part.startswith(".") for part in parts if part)


def main() -> int:
    created = 0
    skipped_existing = 0
    for dirpath, dirnames, filenames in os.walk(PACKAGE_ROOT):
        rel_dir = os.path.relpath(dirpath, PACKAGE_ROOT)
        if rel_dir == ".":
            rel_dir = ""
        if rel_dir and is_hidden(rel_dir):
            dirnames[:] = []
            continue
        dirnames[:] = [d for d in dirnames if not is_hidden(os.path.join(rel_dir, d))]

        targets = []
        for d in dirnames:
            targets.append((os.path.join(rel_dir, d), True))
        for f in filenames:
            if f.endswith(".meta") or f == ".DS_Store":
                continue
            targets.append((os.path.join(rel_dir, f), False))

        for rel_path, is_folder in targets:
            meta_path = os.path.join(PACKAGE_ROOT, rel_path) + ".meta"
            if os.path.exists(meta_path):
                skipped_existing += 1
                continue
            template = FOLDER_META if is_folder else FILE_META
            with open(meta_path, "w", newline="\n") as handle:
                handle.write(template.format(guid=guid_for(rel_path)))
            created += 1

    print(f"created {created} meta files, kept {skipped_existing} existing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
