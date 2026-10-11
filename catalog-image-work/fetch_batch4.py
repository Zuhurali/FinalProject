import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "batch4"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
queries = [
    "Dodge Challenger SRT Hellcat rear",
    "Rolls-Royce Wraith interior",
    "Rolls-Royce Phantom VIII interior",
    "Ford Mustang convertible rear 2024",
    "Volkswagen Golf R Mk7 interior",
    "Honda Odyssey 2018 interior",
]

def curl(url: str) -> bytes:
    return subprocess.run(["curl.exe", "-skL", "-A", ua, "--max-time", "40", url], capture_output=True).stdout

picked = []
for query in queries:
    time.sleep(2.0)
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query", "format": "json", "list": "search",
        "srsearch": query, "srnamespace": "6", "srlimit": "2",
    })
    raw = curl(url)
    try:
        hits = json.loads(raw.decode("utf-8")).get("query", {}).get("search", [])
    except Exception as exc:
        print("fail", query, exc, raw[:120], flush=True)
        continue
    titles = [hit["title"] for hit in hits[:1]]
    picked.extend(titles)
    print(query, "->", titles, flush=True)

records = []
for title in picked:
    time.sleep(2.0)
    info_url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query", "format": "json", "titles": title,
        "prop": "imageinfo", "iiprop": "url|extmetadata|size",
    })
    raw = curl(info_url)
    try:
        page = next(iter(json.loads(raw.decode("utf-8"))["query"]["pages"].values()))
        image = page["imageinfo"][0]
    except Exception as exc:
        print("info fail", title, exc, raw[:120], flush=True)
        continue
    meta = image.get("extmetadata") or {}
    license_name = (meta.get("LicenseShortName") or {}).get("value") or ""
    short = license_name.upper()
    ok = short in {"CC0", "PUBLIC DOMAIN"} or short.startswith("CC BY-SA") or (
        short.startswith("CC BY") and "NC" not in short and "ND" not in short
    )
    record = {
        "title": title,
        "license": license_name,
        "author": (meta.get("Artist") or {}).get("value"),
        "filePage": image.get("descriptionurl"),
        "acceptedLicense": ok,
    }
    if ok:
        safe = title.removeprefix("File:").replace(" ", "_").replace("/", "-").replace(":", "-").replace(",", "")
        dest = incoming / safe
        file_url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(title.removeprefix("File:")) + "?width=1100"
        time.sleep(1.0)
        subprocess.run(["curl.exe", "-skL", "-A", ua, "--max-time", "70", "-o", str(dest), file_url], capture_output=True)
        record["localFile"] = dest.name
        record["bytes"] = dest.stat().st_size if dest.exists() else 0
    records.append(record)
    print(license_name, record.get("bytes"), title, flush=True)

(root / "catalog-image-work" / "batch4-licenses.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
print("done", len(records))
