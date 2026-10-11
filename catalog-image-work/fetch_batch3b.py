import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "batch3"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
groups = [
    [
        "File:Hyundai Ioniq 5 (rear).jpg",
        "File:Hyundai Ioniq 5 NE Interior (1).jpg",
        "File:Toyota GR86 SZ rear.jpg",
        "File:The interior of Toyota GR86 RZ 6AT (3BA-ZN8-A2F7).jpg",
        "File:2019 Nissan Leaf rear.jpg",
    ],
    [
        "File:2021 Toyota Sienna XLE Hybrid, rear 12.21.21.jpg",
        "File:2023 Audi e-tron GT interior.jpg",
        "File:Porsche Taycan GTS (interior) (taken in 2022) (Kyoto, Japan).jpg",
        "File:Mercedes-AMG GT (C190) rear.jpg",
        "File:Dodge Challenger SRT in Nevinnomyssk (rear).jpg",
    ],
]

def curl(url: str) -> bytes:
    return subprocess.run(
        ["curl.exe", "-skL", "-A", ua, "--max-time", "45", url],
        capture_output=True,
    ).stdout

records = []
for index, titles in enumerate(groups):
    if index:
        time.sleep(3)
    info_url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "titles": "|".join(titles),
        "prop": "imageinfo",
        "iiprop": "url|extmetadata|size|mime",
    })
    raw = curl(info_url)
    try:
        payload = json.loads(raw.decode("utf-8"))
    except Exception:
        print("info fail", raw[:200], flush=True)
        continue
    for page in payload.get("query", {}).get("pages", {}).values():
        title = page.get("title")
        imageinfo = (page.get("imageinfo") or [None])[0]
        record = {"title": title, "missing": bool(page.get("missing"))}
        if not imageinfo:
            records.append(record)
            print("no info", title, flush=True)
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
            safe = title.removeprefix("File:").replace(" ", "_").replace("/", "-").replace(":", "-").replace(",", "")
            dest = incoming / safe
            file_url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(title.removeprefix("File:")) + "?width=1100"
            if not dest.exists() or dest.stat().st_size < 8000:
                time.sleep(1.1)
                subprocess.run(
                    ["curl.exe", "-skL", "-A", ua, "--max-time", "70", "-o", str(dest), file_url],
                    capture_output=True,
                )
            record["localFile"] = str(dest.name)
            record["bytes"] = dest.stat().st_size if dest.exists() else 0
        records.append(record)
        print(license_name, record.get("bytes"), title, flush=True)

(root / "catalog-image-work" / "batch3-licenses.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
print("done", len(records))
