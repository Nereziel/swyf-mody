"""Read SWYF Unity localization bundles without modifying game files.

Requires UnityPy (tested with 1.25.4). IDs are strings to preserve 64-bit precision.
"""

import argparse
import hashlib
import json
from pathlib import Path

import UnityPy


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("game_dir", type=Path)
    parser.add_argument("output_dir", type=Path)
    args = parser.parse_args()
    source = args.game_dir / "Scam With Your Friends_Data/StreamingAssets/aa/StandaloneWindows64"
    output = args.output_dir
    output.mkdir(parents=True, exist_ok=True)
    names = {
        "shared": "localization-assets-shared_assets_all.bundle",
        "en": "localization-string-tables-english(en)_assets_all.bundle",
        "cs": "localization-string-tables-czech(cs)_assets_all.bundle",
    }
    trees = {}
    manifest = {}
    for locale, name in names.items():
        path = source / name
        manifest[locale] = {"path": str(path), "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
        env = UnityPy.load(str(path))
        trees[locale] = {
            obj.path_id: obj.read_typetree()
            for obj in env.objects if obj.type.name == "MonoBehaviour"
        }

    localized = {}
    for locale in ("en", "cs"):
        localized[locale] = {}
        for tree in trees[locale].values():
            assert tree["m_LocaleId"]["m_Code"] == locale
            shared_id = tree["m_SharedData"]["m_PathID"]
            collection = trees["shared"][shared_id]["m_TableCollectionName"]
            assert collection not in localized[locale], collection
            entries = tree["m_TableData"]
            values = {str(entry["m_Id"]): entry["m_Localized"] for entry in entries}
            assert len(entries) == len(values), "Duplicate entry ID"
            localized[locale][collection] = values

    rows = []
    stats = {}
    for collection in sorted(set(localized["en"]) | set(localized["cs"])):
        shared = next(t for t in trees["shared"].values() if t.get("m_TableCollectionName") == collection)
        keys = {str(entry["m_Id"]): entry["m_Key"] for entry in shared["m_Entries"]}
        en = localized["en"].get(collection, {})
        cs = localized["cs"].get(collection, {})
        ids = set(keys) | set(en) | set(cs)
        collection_rows = []
        for entry_id in sorted(ids, key=lambda i: (keys.get(i, ""), int(i))):
            row = {"table": collection, "id": entry_id, "key": keys.get(entry_id),
                   "en": en.get(entry_id), "cs": cs.get(entry_id)}
            rows.append(row)
            collection_rows.append(row)
        stats[collection] = {
            "keys": len(ids), "en_entries": len(en), "cs_entries": len(cs),
            "missing_en": sum(r["en"] is None for r in collection_rows),
            "missing_cs": sum(r["cs"] is None for r in collection_rows),
            "empty_cs": sum(r["cs"] == "" for r in collection_rows),
            "replacement_character_cs": sum("\ufffd" in r["cs"] for r in collection_rows if r["cs"] is not None),
        }

    write_json(output / "en-cs.json", rows)
    for locale in ("en", "cs"):
        write_json(output / f"{locale}.json", [
            {"table": row["table"], "id": row["id"], "key": row["key"], "text": row[locale]}
            for row in rows
        ])
    lines = ["SWYF — English / čeština", "Chybějící položka: [CHYBÍ]. Prázdný text: [PRÁZDNÉ].", ""]
    for row in rows:
        lines.append(f"[{row['table']}] {row['key']} (ID {row['id']})")
        for locale in ("en", "cs"):
            value = row[locale]
            text = "[CHYBÍ]" if value is None else ("[PRÁZDNÉ]" if value == "" else value)
            lines.append(f"{locale.upper()}: {text}")
        lines.append("")
    (output / "en-cs.txt").write_text("\n".join(lines), encoding="utf-8-sig")
    write_json(output / "manifest.json", {"sources": manifest, "tables": stats, "total_keys": len(rows)})

    # Verify UTF-8 round trip and exact preservation of every source entry.
    exported = json.loads((output / "en-cs.json").read_text(encoding="utf-8"))
    for locale in ("en", "cs"):
        for table, entries in localized[locale].items():
            recovered = {r["id"]: r[locale] for r in exported if r["table"] == table and r[locale] is not None}
            assert recovered == entries, (locale, table)
    for locale, name in names.items():
        assert hashlib.sha256((source / name).read_bytes()).hexdigest() == manifest[locale]["sha256"]
    print(json.dumps({"total_keys": len(rows), "tables": stats}, ensure_ascii=True, indent=2))


if __name__ == "__main__":
    main()
