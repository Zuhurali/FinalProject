import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "priority"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

titles = [
    "File:Rolls-Royce Phantom VIII Series I IMG 9101.jpg",
    "File:Rolls-Royce Phantom, Geneva International Motor Show 2018, Le Grand-Saconnex (1X7A1220).jpg",
    "File:Geneva International Motor Show 2018, Le Grand-Saconnex (1X7A1231).jpg",
    "File:2014 Rolls-Royce Wraith, diamond black rR.jpg",
    "File:Rolls-Royce Wraith rear - Tokyo Auto Salon 2015.jpg",
    "File:Rolls-Royce Wraith (2019) (53627615714).jpg",
    "File:Geneva MotorShow 2013 - Rolls-Royce Wraith back.jpg",
    "File:BMW 7-Series (G11) 745e (2019) (53332492790).jpg",
    "File:BMW 7-Series (G11) 730d (2022) (53327092300).jpg",
    "File:2018 Audi A8 50 TDi Quattro Automatic 3.0 Front.jpg",
    "File:Audi A8 D5 (2021) 1X7A6342.jpg",
    "File:Audi A8 L D5 Black Valcona Interior with Piano Black Inlay (35).jpg",
    "File:Audi A8 L D5 Black Valcona Interior with Piano Black Inlay (5).jpg",
    "File:Mercedes-AMG GT V8 Biturbo 2.jpg",
    "File:Mercedes-AMG C190 GT R IMG 7306.jpg",
    "File:The interior of Mercedes-AMG GT S (C190).jpg",
    "File:Mercedes-AMG GT (C190).jpg",
    "File:Mercedes-AMG GT (C190) front.jpg",
]


def curl(url: str) -> bytes:
    completed = subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "25", "--connect-timeout", "10", url],
        capture_output=True,
    )
    return completed.stdout


def accepted(license_name: str) -> bool:
    short = (license_name or "").upper()
    return short in {"CC0", "PUBLIC DOMAIN"} or short.startswith("CC BY-SA") or (
        short.startswith("CC BY") and "NC" not in short and "ND" not in short
    )


records = []
for title in titles:
    time.sleep(2.2)
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
    except Exception as exc:
        records.append({"title": title, "error": str(exc), "raw": raw[:160].decode("utf-8", "replace")})
        print("FAIL", title, flush=True)
        continue
    meta = image.get("extmetadata") or {}
    license_name = (meta.get("LicenseShortName") or {}).get("value") or ""
    ok = accepted(license_name)
    record = {
        "title": title,
        "license": license_name,
        "author": (meta.get("Artist") or {}).get("value"),
        "filePage": image.get("descriptionurl"),
        "width": image.get("width"),
        "height": image.get("height"),
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

(root / "catalog-image-work" / "priority-selected.json").write_text(
    json.dumps(records, indent=2), encoding="utf-8"
)
print("done", len(records), flush=True)
