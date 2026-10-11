import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "batch4"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
titles = [
    "File:Dodge Challenger SRT Hellcat rear (20418562304).jpg",
    "File:Rolls-Royce Phantom VIII Series II Scivaro Grey Peony Pink Iceland Moss (5).jpg",
]

def curl(url: str) -> bytes:
    return subprocess.run(["curl.exe", "-skL", "-A", ua, "--max-time", "45", url], capture_output=True).stdout

records = []
for title in titles:
    time.sleep(2)
    info_url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query", "format": "json", "titles": title,
        "prop": "imageinfo", "iiprop": "url|extmetadata|size",
    })
    raw = curl(info_url)
    page = next(iter(json.loads(raw.decode("utf-8"))["query"]["pages"].values()))
    image = page["imageinfo"][0]
    meta = image.get("extmetadata") or {}
    license_name = (meta.get("LicenseShortName") or {}).get("value") or ""
    short = license_name.upper()
    ok = short in {"CC0", "PUBLIC DOMAIN"} or short.startswith("CC BY-SA") or (
        short.startswith("CC BY") and "NC" not in short and "ND" not in short
    )
    safe = title.removeprefix("File:").replace(" ", "_").replace("/", "-")
    dest = incoming / safe
    if ok and (not dest.exists() or dest.stat().st_size < 8000):
        time.sleep(1)
        file_url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(title.removeprefix("File:")) + "?width=1200"
        subprocess.run(["curl.exe", "-skL", "-A", ua, "--max-time", "70", "-o", str(dest), file_url], capture_output=True)
    record = {
        "title": title,
        "license": license_name,
        "author": (meta.get("Artist") or {}).get("value"),
        "filePage": image.get("descriptionurl"),
        "acceptedLicense": ok,
        "bytes": dest.stat().st_size if dest.exists() else 0,
        "localFile": dest.name,
    }
    records.append(record)
    (root / "catalog-image-work" / "batch4-extra.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
print("ok")
