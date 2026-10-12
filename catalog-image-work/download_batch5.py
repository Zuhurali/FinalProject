import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "priority5"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

titles = [
    "File:2008 Nissan Fairlady Z Interior.JPG",
    "File:Nissan 350Z Premium Pack sunset orange, 2003, Interior.jpg",
    "File:Nissan 350Z rear 20070914.jpg",
    "File:Tesla Model 3 (2023), long range, Japan, interior.jpg",
    "File:Tesla Model 3 (2023), long range, Japan, right-rear.jpg",
    "File:The interior of TESLA MODEL 3.jpg",
    "File:Maserati Gran Turismo (2).jpg",
    "File:Tesla Model 3 (2023), long range, Japan, front.jpg",
    "File:Tesla Model 3 (2023), long range, Japan, right-front.jpg",
    "File:2023 Manila Auto Salon - Tesla Model 3 Long Range AWD 2023 by EV Solutions (1).jpg",
    "File:2021 Tesla Model 3, front 11.10.21.jpg",
    "File:Toyota Highlander Hybrid (XU70) 1X7A6356.jpg",
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


records = []
for title in titles:
    time.sleep(3.0)
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

(root / "catalog-image-work" / "priority5-selected.json").write_text(
    json.dumps(records, indent=2), encoding="utf-8"
)
print("done", flush=True)
