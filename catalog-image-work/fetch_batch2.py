import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "batch2"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

titles = [
    "File:Audi A3 Sportback 8V facelift interior.jpg",
    "File:2012 Hyundai Veloster (FS2) hatchback (2012-10-26) 04.jpg",
    "File:BMW G30 LCI Exclusive Nappa Leather Mocha (8).jpg",
    "File:BMW G29 Z4 M40i (Facelift) 1X7A6948.jpg",
    "File:BMW i4 1X7A6838.jpg",
    "File:BMW 840i Gran Coupé M Sport (G16) rear.jpg",
    "File:Genesis G90 RS4 Bariloche Brown (40).jpg",
    "File:Genesis G90 RS4 Standard Design Obsidian Black Monotone (79).jpg",
    "File:G26 M440GC Dashboard.jpg",
    "File:Porsche 911 GT3 RS (991.2, 2018) (52574909305).jpg",
    "File:2018 BMW X3 xDrive 20d M Sport (Cockpit).jpg",
    "File:Audi A3 Sportback 1.4TFSI sport (DBA-8VCXS) interior.jpg",
    "File:Genesis G90 RS4 Vik Black (85).jpg",
    "File:BMW Z4 (G29) (4).jpg",
]

def curl(url: str) -> bytes:
    return subprocess.run(
        ["curl.exe", "-skL", "-A", ua, "--max-time", "45", url],
        capture_output=True,
    ).stdout

info_url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
    "action": "query",
    "format": "json",
    "titles": "|".join(titles),
    "prop": "imageinfo",
    "iiprop": "url|extmetadata|size|mime",
})
raw = curl(info_url)
payload = json.loads(raw.decode("utf-8"))
pages = payload.get("query", {}).get("pages", {})
records = []
allowed = ("CC0", "CC BY", "CC BY-SA")
for page in pages.values():
    title = page.get("title")
    missing = page.get("missing")
    imageinfo = (page.get("imageinfo") or [None])[0]
    record = {"title": title, "missing": bool(missing)}
    if imageinfo:
        meta = imageinfo.get("extmetadata") or {}
        license_name = (meta.get("LicenseShortName") or {}).get("value")
        record.update({
            "license": license_name,
            "author": (meta.get("Artist") or {}).get("value"),
            "filePage": imageinfo.get("descriptionurl"),
            "width": imageinfo.get("width"),
            "height": imageinfo.get("height"),
            "mime": imageinfo.get("mime"),
        })
        short = (license_name or "").upper()
        ok = short == "CC0" or short.startswith("CC BY-SA") or (short.startswith("CC BY") and "NC" not in short and "ND" not in short)
        record["acceptedLicense"] = ok
        if ok:
            safe = title.removeprefix("File:").replace(" ", "_").replace("/", "-").replace(":", "-")
            dest = incoming / safe
            url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(title.removeprefix("File:")) + "?width=1200"
            if not dest.exists() or dest.stat().st_size < 8000:
                time.sleep(1.1)
                subprocess.run(
                    ["curl.exe", "-skL", "-A", ua, "--max-time", "70", "-o", str(dest), url],
                    capture_output=True,
                )
            record["localFile"] = str(dest.relative_to(root))
            record["bytes"] = dest.stat().st_size if dest.exists() else 0
    records.append(record)
    print(record.get("license"), record.get("bytes"), title, flush=True)

(root / "catalog-image-work" / "batch2-licenses.json").write_text(
    json.dumps(records, indent=2), encoding="utf-8"
)
print("records", len(records))
