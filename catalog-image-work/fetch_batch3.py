import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "batch3"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
queries = [
    "Hyundai Ioniq 5 rear",
    "Hyundai IONIQ 5 interior",
    "Toyota GR86 rear",
    "Toyota GR86 interior",
    "Nissan Leaf ZE1 rear",
    "Toyota Sienna 2021 rear",
    "Audi e-tron GT interior",
    "Porsche Taycan interior",
    "Mercedes-AMG GT C190 rear",
    "Dodge Challenger rear",
    "Rolls-Royce Phantom VIII interior",
    "Ford Mustang S650 convertible rear",
]

def curl(url: str) -> bytes:
    return subprocess.run(
        ["curl.exe", "-skL", "-A", ua, "--max-time", "45", url],
        capture_output=True,
    ).stdout

titles = []
search_log = []
for query in queries:
    time.sleep(1.3)
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "list": "search",
        "srsearch": query,
        "srnamespace": "6",
        "srlimit": "4",
    })
    raw = curl(url)
    try:
        hits = json.loads(raw.decode("utf-8")).get("query", {}).get("search", [])
    except Exception as exc:
        search_log.append({"query": query, "error": str(exc), "raw": raw[:180].decode("utf-8", "replace")})
        print("search fail", query, flush=True)
        continue
    picked = [hit["title"] for hit in hits[:2]]
    search_log.append({"query": query, "titles": picked})
    titles.extend(picked)
    print(query, "->", picked, flush=True)

titles = list(dict.fromkeys(titles))
records = []
if titles:
    time.sleep(1.3)
    info_url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "titles": "|".join(titles),
        "prop": "imageinfo",
        "iiprop": "url|extmetadata|size|mime",
    })
    payload = json.loads(curl(info_url).decode("utf-8"))
    for page in payload.get("query", {}).get("pages", {}).values():
        title = page.get("title")
        imageinfo = (page.get("imageinfo") or [None])[0]
        record = {"title": title, "missing": bool(page.get("missing"))}
        if not imageinfo:
            records.append(record)
            continue
        meta = imageinfo.get("extmetadata") or {}
        license_name = (meta.get("LicenseShortName") or {}).get("value") or ""
        short = license_name.upper()
        ok = short in {"CC0", "PUBLIC DOMAIN"} or short.startswith("CC BY-SA") or (
            short.startswith("CC BY") and "NC" not in short and "ND" not in short
        )
        record.update({
            "license": license_name,
            "author": (meta.get("Artist") or {}).get("value"),
            "filePage": imageinfo.get("descriptionurl"),
            "acceptedLicense": ok,
        })
        if ok:
            safe = title.removeprefix("File:").replace(" ", "_").replace("/", "-").replace(":", "-")
            dest = incoming / safe
            file_url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(title.removeprefix("File:")) + "?width=1100"
            if not dest.exists() or dest.stat().st_size < 8000:
                time.sleep(1.0)
                subprocess.run(
                    ["curl.exe", "-skL", "-A", ua, "--max-time", "70", "-o", str(dest), file_url],
                    capture_output=True,
                )
            record["localFile"] = str(dest)
            record["bytes"] = dest.stat().st_size if dest.exists() else 0
        records.append(record)
        print(license_name, record.get("bytes"), title, flush=True)

out = {"searches": search_log, "records": records}
(root / "catalog-image-work" / "batch3-licenses.json").write_text(json.dumps(out, indent=2), encoding="utf-8")
print("done", len(records))
