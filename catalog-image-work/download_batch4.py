import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "priority4"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

titles = [
    "File:2009 Nissan 370Z interior.jpg",
    "File:The interior of Porsche 718 Cayman (982).jpg",
    "File:Porsche 718 Cayman (982) interior.jpg",
    "File:Nissan 350Z Z33.jpg",
    "File:2003 Nissan 350Z.jpg",
    "File:Nissan 350Z interior.jpg",
    "File:Porsche 997 Carrera.jpg",
    "File:Porsche 911 (997) Carrera S.jpg",
    "File:Maserati GranTurismo 2008.jpg",
    "File:Maserati GranTurismo S.jpg",
    "File:Tesla Model 3 (2018).jpg",
    "File:2018 Tesla Model 3.jpg",
    "File:Tesla Model 3 interior.jpg",
    "File:2020 Toyota Highlander.jpg",
    "File:Toyota Highlander XU70.jpg",
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

(root / "catalog-image-work" / "priority4-selected.json").write_text(
    json.dumps(records, indent=2), encoding="utf-8"
)
print("done", flush=True)
