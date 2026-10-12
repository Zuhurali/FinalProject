import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "priority6"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

titles = [
    # Template replacements still missing
    "File:Nissan 350Z -- 03-16-2012.JPG",
    "File:Nissan 350Z -- 03-16-2012 2.JPG",
    "File:2006 Nissan 350Z Grand Touring.jpg",
    "File:Nissan Fairlady Z (Z33).jpg",
    "File:Nissan 350Z (Z33) front.jpg",
    "File:Porsche 911 Carrera S (997) – Frontansicht, 5. März 2012, Düsseldorf.jpg",
    "File:Porsche 997 Carrera 4S Coupé.jpg",
    "File:Porsche 911 (997) Carrera S Coupé – Heckansicht.jpg",
    "File:Porsche 997 Carrera S rear.jpg",
    "File:The interior of Porsche 911 Carrera (997).jpg",
    "File:Porsche 911 (997) interior.jpg",
    "File:Maserati GranTurismo S Automatic – Frontansicht, 5. April 2012, Düsseldorf.jpg",
    "File:Maserati GranTurismo S – Heckansicht.jpg",
    "File:Maserati GranTurismo Sport – Heckansicht, 10. August 2013, Düsseldorf.jpg",
    "File:The interior of Maserati GranTurismo.jpg",
    "File:Maserati GranTurismo interior.jpg",
    "File:2021 Toyota Highlander Limited AWD, rear 3.13.21.jpg",
    "File:2020 Toyota Highlander XLE AWD, rear 11.8.20.jpg",
    "File:2020 Toyota Highlander Limited AWD, interior 11.8.20.jpg",
    "File:Toyota Highlander Hybrid (XU70) interior.jpg",
    "File:The interior of Rolls-Royce Wraith.jpg",
    "File:Rolls-Royce Wraith interior.jpg",
    "File:2014 Rolls-Royce Wraith Interior.jpg",
    # Remaining high-value catalog gaps
    "File:The interior of BMW 3 Series (G20).jpg",
    "File:BMW M340i xDrive (G20) rear.jpg",
    "File:BMW M3 Competition (G80) rear.jpg",
    "File:BMW M3 G80 interior.jpg",
    "File:The interior of BMW M3 Competition (G80).jpg",
    "File:BMW 4 Series (G22) rear.jpg",
    "File:BMW M5 F90 Competition rear.jpg",
    "File:The interior of BMW M5 (F90).jpg",
    "File:BMW X3 (G01) interior.jpg",
    "File:BMW X5 M Competition interior.jpg",
    "File:BMW X6 M Competition (F96) rear.jpg",
    "File:BMW i4 M50 rear.jpg",
    "File:2023 Nissan Z rear.jpg",
    "File:Nissan Z (RZ34) rear.jpg",
    "File:Tesla Model S Plaid interior.jpg",
    "File:The interior of TESLA MODEL S.jpg",
    "File:Porsche 911 GT3 RS (991.2) rear.jpg",
    "File:Mercedes-Maybach S-Class (Z223) interior.jpg",
    "File:Bentley Continental GT W12 interior.jpg",
    "File:Porsche Panamera 971 interior.jpg",
    "File:Lexus LS 500 interior.jpg",
    "File:Ferrari 488 GTB interior.jpg",
    "File:McLaren 720S interior.jpg",
    "File:Honda Odyssey (RL6) interior.jpg",
    "File:Kia Carnival (KA4) interior.jpg",
    "File:Volkswagen Golf R Mk7 interior.jpg",
    "File:Mazda3 BP interior.jpg",
    "File:Mazda CX-5 KF interior.jpg",
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

(root / "catalog-image-work" / "priority6-selected.json").write_text(
    json.dumps(records, indent=2), encoding="utf-8"
)
print("done", sum(1 for item in records if item.get("localFile")), "files", flush=True)
