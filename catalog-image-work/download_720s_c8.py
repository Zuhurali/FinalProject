import json
import subprocess
import time
import urllib.parse
from pathlib import Path

ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
incoming = Path(__file__).resolve().parent / "c8-720s"
incoming.mkdir(exist_ok=True)

files = [
    "McLaren 720S '1001HP Super Hyper' (2018) (55311108634).jpg",
    "McLaren 720S-doors open.jpg",
    "McLaren 720S, IAA 2017, (1Y7A3409).jpg",
    "McLaren 720S, IAA 2017, (1Y7A3410).jpg",
    "McLaren 720S, IAA 2017, (1Y7A3411).jpg",
    "McLaren 720S, IAA 2017, (1Y7A3412).jpg",
    "McLaren 720S, IAA 2017, (1Y7A3413).jpg",
    "The interior of McLaren 720S.jpg",
    "McLaren 720S interior dashboard.jpg",
    "McLaren 720S Spider interior.jpg",
    "Corvette C8 interior.jpg",
    "MIAS 2024 - 2024 Chevrolet Corvette C8 6.2 LT2 V8 interior.jpg",
    "Chevrolet Corvette C8 4.jpg",
    "Chevrolet Corvette C8 5.jpg",
    "Chevrolet Corvette C8 Stingray 3LT Coupe Z51 Torch Red.jpg",
    "Chevrolet Corvette C8 Stingray 3LT Coupe Z51 Torch Red 01.jpg",
    "Chevrolet Corvette C8 Stingray 3LT Coupe Z51 Torch Red 02.jpg",
    "2023 Chevrolet Corvette Stingray Coupe 2LT in Red Mist Metallic Tintcoat, Rear Left, 06-03-2023.jpg",
    "Chevrolet CORVETTE 3LT (7BA-Y2XC) rear.jpg",
    "Chevrolet Corvette (C8) rear view.jpg",
    "Chevrolet Corvette C8 IAA 2021 1X7A0151.jpg",
    "Chevrolet Corvette C8 convertibles IAA 2021 1X7A0184.jpg",
    "Tuned Chevrolet CORVETTE COUPE (C8) rear.jpg",
    "Chevrolet Corvette C8 State Street Bridge downtown Montpelier VT May 2022 rear.jpg",
]


def accepted(license_name: str) -> bool:
    short = (license_name or "").upper()
    return short in {"CC0", "PUBLIC DOMAIN"} or short.startswith("CC BY-SA") or (
        short.startswith("CC BY") and "NC" not in short and "ND" not in short
    )


records = []
for name in files:
    safe = "".join(ch if ch.isalnum() or ch in "-_." else "_" for ch in name)[:120]
    dest = incoming / safe
    title = "File:" + name
    info_url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "titles": title,
        "prop": "imageinfo",
        "iiprop": "url|size|extmetadata",
        "iiurlwidth": "1400",
    })
    time.sleep(1.0)
    raw = subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "30", "--connect-timeout", "12", info_url],
        capture_output=True,
    ).stdout
    try:
        pages = json.loads(raw.decode("utf-8")).get("query", {}).get("pages", {})
        page = next(iter(pages.values()))
        info = (page.get("imageinfo") or [None])[0]
    except Exception as exc:
        print("METAFAIL", name, exc, flush=True)
        continue
    if not info:
        print("MISSING", name, flush=True)
        records.append({"title": title, "error": "missing"})
        continue
    meta = info.get("extmetadata") or {}
    license_name = (meta.get("LicenseShortName", {}) or {}).get("value", "")
    author = (meta.get("Artist", {}) or {}).get("value", "")
    records.append({
        "title": title,
        "license": license_name,
        "accepted": accepted(license_name),
        "author": author,
        "filePage": "https://commons.wikimedia.org/wiki/" + urllib.parse.quote(title.replace(" ", "_")),
        "thumburl": info.get("thumburl") or info.get("url"),
        "width": info.get("thumbwidth") or info.get("width"),
        "height": info.get("thumbheight") or info.get("height"),
    })
    print(("OK" if accepted(license_name) else "LICENSE"), license_name, name, flush=True)
    if dest.exists() and dest.stat().st_size > 30000:
        print("HAVE", dest.name, dest.stat().st_size, flush=True)
        continue
    url = info.get("thumburl") or info.get("url")
    time.sleep(1.2)
    subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "80", "--connect-timeout", "20", "-o", str(dest), url],
    )
    size = dest.stat().st_size if dest.exists() else 0
    print(("DL" if size > 30000 else "FAIL"), size, dest.name, flush=True)

(incoming / "licenses.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
print("done", len(records), flush=True)
