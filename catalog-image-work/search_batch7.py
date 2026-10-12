import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "priority7"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

queries = [
    "Nissan 350Z Z33 front",
    "Nissan Fairlady Z Z33",
    "Porsche 997 Carrera front",
    "Porsche 911 997 rear",
    "Porsche 997 interior dashboard",
    "Maserati GranTurismo front",
    "Maserati GranTurismo rear",
    "Maserati GranTurismo interior",
    "Toyota Highlander XU70 rear",
    "Toyota Highlander XU70 interior",
    "Rolls-Royce Wraith dashboard interior",
    "BMW G20 interior",
    "BMW M3 G80 interior",
    "BMW M5 F90 interior",
    "Honda Odyssey RL6 interior",
    "Kia Carnival KA4 interior",
    "Tesla Model S interior",
    "Ferrari 488 GTB interior",
    "McLaren 720S interior",
    "Porsche 718 Cayman interior",
]


def curl(url: str) -> bytes:
    return subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "25", "--connect-timeout", "10", url],
        capture_output=True,
    ).stdout


def accepted(license_name: str) -> bool:
    short = (license_name or "").upper()
    return short in {"CC0", "PUBLIC DOMAIN"} or short.startswith("CC BY-SA") or (
        short.startswith("CC BY") and "NC" not in short and "ND" not in short
    )


search_hits = []
for query in queries:
    time.sleep(2.8)
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "list": "search",
        "srsearch": query,
        "srnamespace": "6",
        "srlimit": "5",
    })
    raw = curl(url)
    try:
        hits = json.loads(raw.decode("utf-8")).get("query", {}).get("search", [])
        titles = [hit["title"] for hit in hits]
    except Exception as exc:
        search_hits.append({"query": query, "error": str(exc)})
        print("FAIL", query, flush=True)
        continue
    search_hits.append({"query": query, "titles": titles})
    print("OK", query, "->", titles, flush=True)

(root / "catalog-image-work" / "priority7-search.json").write_text(
    json.dumps(search_hits, indent=2), encoding="utf-8"
)

seen = set()
titles = []
for row in search_hits:
    for title in row.get("titles") or []:
        if title not in seen:
            seen.add(title)
            titles.append(title)

records = []
for title in titles:
    time.sleep(2.8)
    info_url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "titles": title,
        "prop": "imageinfo",
        "iiprop": "url|extmetadata|size",
    })
    raw = curl(info_url)
    try:
        page = next(iter(json.loads(raw.decode("utf-8"))["query"]["pages"].values()))
        image = page["imageinfo"][0]
    except Exception:
        records.append({"title": title, "error": "missing"})
        print("MISS", title, flush=True)
        continue
    meta = image.get("extmetadata") or {}
    license_name = (meta.get("LicenseShortName") or {}).get("value") or ""
    ok = accepted(license_name)
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
        if dest.exists() and dest.stat().st_size > 20000:
            record["localFile"] = dest.name
            record["bytes"] = dest.stat().st_size
        else:
            file_url = (
                "https://commons.wikimedia.org/wiki/Special:FilePath/"
                + urllib.parse.quote(title.removeprefix("File:"))
                + "?width=1400"
            )
            time.sleep(1.0)
            subprocess.run(
                ["curl.exe", "-sL", "-A", ua, "--max-time", "70", "--connect-timeout", "15", "-o", str(dest), file_url],
                capture_output=True,
            )
            record["localFile"] = dest.name
            record["bytes"] = dest.stat().st_size if dest.exists() else 0
    records.append(record)
    print(("OK" if ok else "SKIP"), license_name, title, flush=True)

(root / "catalog-image-work" / "priority7-selected.json").write_text(
    json.dumps(records, indent=2), encoding="utf-8"
)
print("done", sum(1 for item in records if item.get("localFile")), "files", flush=True)
