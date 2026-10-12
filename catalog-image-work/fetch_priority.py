import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "priority"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

queries = [
    ("phantom-front", "Rolls-Royce Phantom VIII"),
    ("phantom-rear", "Rolls-Royce Phantom VIII rear"),
    ("wraith-interior", "Rolls-Royce Wraith interior"),
    ("wraith-rear", "Rolls-Royce Wraith rear"),
    ("g12-interior", "BMW 7 Series G12 interior"),
    ("g12-rear", "BMW 7 Series G11 rear"),
    ("750li-interior", "BMW 750Li interior"),
    ("a8-front", "Audi A8 D5 front"),
    ("a8-interior", "Audi A8 D5 interior"),
    ("amg-gt-front", "Mercedes-AMG GT C190"),
    ("amg-gt-interior", "Mercedes-AMG GT interior"),
    ("350z-front", "Nissan 350Z Z33"),
    ("350z-interior", "Nissan 350Z interior"),
    ("911-997", "Porsche 911 997"),
    ("granturismo", "Maserati GranTurismo"),
    ("model3-front", "Tesla Model 3"),
    ("model3-interior", "Tesla Model 3 interior"),
    ("highlander-front", "Toyota Highlander XU70"),
    ("highlander-interior", "Toyota Highlander interior"),
]

def curl(url: str) -> bytes:
    return subprocess.run(
        ["curl.exe", "-skL", "-A", ua, "--max-time", "45", url],
        capture_output=True,
    ).stdout


def accepted(license_name: str) -> bool:
    short = (license_name or "").upper()
    return short in {"CC0", "PUBLIC DOMAIN"} or short.startswith("CC BY-SA") or (
        short.startswith("CC BY") and "NC" not in short and "ND" not in short
    )


seen_titles = set()
records = []
for key, query in queries:
    time.sleep(1.4)
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "list": "search",
        "srsearch": query,
        "srnamespace": "6",
        "srlimit": "8",
    })
    raw = curl(url)
    try:
        hits = json.loads(raw.decode("utf-8")).get("query", {}).get("search", [])
    except Exception as exc:
        records.append({"key": key, "query": query, "error": str(exc)})
        continue
    for hit in hits:
        title = hit.get("title")
        if not title or title in seen_titles:
            continue
        seen_titles.add(title)
        time.sleep(1.1)
        info_url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
            "action": "query",
            "format": "json",
            "titles": title,
            "prop": "imageinfo",
            "iiprop": "url|extmetadata|size",
        })
        info_raw = curl(info_url)
        try:
            page = next(iter(json.loads(info_raw.decode("utf-8"))["query"]["pages"].values()))
            image = page["imageinfo"][0]
        except Exception as exc:
            records.append({"key": key, "title": title, "error": str(exc)})
            continue
        meta = image.get("extmetadata") or {}
        license_name = (meta.get("LicenseShortName") or {}).get("value") or ""
        record = {
            "key": key,
            "query": query,
            "title": title,
            "license": license_name,
            "author": (meta.get("Artist") or {}).get("value"),
            "filePage": image.get("descriptionurl"),
            "width": image.get("width"),
            "height": image.get("height"),
            "acceptedLicense": accepted(license_name),
        }
        if record["acceptedLicense"]:
            safe = title.removeprefix("File:").replace(" ", "_").replace("/", "-").replace(":", "-").replace(",", "")
            dest = incoming / f"{key}__{safe}"
            file_url = (
                "https://commons.wikimedia.org/wiki/Special:FilePath/"
                + urllib.parse.quote(title.removeprefix("File:"))
                + "?width=1400"
            )
            time.sleep(0.8)
            subprocess.run(
                ["curl.exe", "-skL", "-A", ua, "--max-time", "80", "-o", str(dest), file_url],
                capture_output=True,
            )
            record["localFile"] = dest.name
            record["bytes"] = dest.stat().st_size if dest.exists() else 0
        records.append(record)

out = root / "catalog-image-work" / "priority-licenses.json"
out.write_text(json.dumps(records, indent=2), encoding="utf-8")
accepted_count = sum(1 for item in records if item.get("acceptedLicense"))
print(f"records={len(records)} accepted={accepted_count}")
