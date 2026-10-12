import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "priority3"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

titles = [
    "File:BMW 740i (G11) interior.jpg",
    "File:BMW 740i (G11) rear.jpg",
    "File:Rolls-Royce Phantom VIII Back Genf 2018.jpg",
    "File:Rolls-Royce Phantom VIII Heck.jpg",
    "File:The rear of Rolls-Royce Phantom VIII.jpg",
    "File:Rolls-Royce Phantom VIII rear.jpg",
    "File:Porsche 718 Cayman interior.jpg",
    "File:The interior of Porsche 718 Cayman.jpg",
    "File:Nissan 370Z interior.jpg",
    "File:The interior of Nissan 370Z.jpg",
    "File:Audi A5 Cabriolet F5 interior.jpg",
    "File:The interior of Audi A5 Cabriolet (F5).jpg",
    "File:Lexus RC 350 interior.jpg",
    "File:The interior of Lexus RC.jpg",
    "File:Mercedes-Benz E-Class Cabriolet A238 interior.jpg",
    "File:The interior of Mercedes-Benz E-Class Cabriolet (A238).jpg",
]

queries = [
    "Rolls-Royce Phantom VIII Back Genf",
    "The rear of Rolls-Royce Phantom VIII",
    "The interior of Porsche 718 Cayman",
    "The interior of Nissan 370Z",
    "The interior of Audi A5 Cabriolet",
    "The interior of Lexus RC",
    "The interior of Mercedes-Benz E-Class Cabriolet",
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


found = []
for query in queries:
    time.sleep(2.8)
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "list": "search",
        "srsearch": query,
        "srnamespace": "6",
        "srlimit": "3",
    })
    raw = curl(url)
    try:
        hits = json.loads(raw.decode("utf-8")).get("query", {}).get("search", [])
        titles.extend(hit["title"] for hit in hits)
        print("OK", query, [hit["title"] for hit in hits], flush=True)
    except Exception as exc:
        print("FAIL", query, exc, flush=True)

seen = set()
records = []
for title in titles:
    if title in seen:
        continue
    seen.add(title)
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
        time.sleep(0.8)
        subprocess.run(
            ["curl.exe", "-sL", "-A", ua, "--max-time", "70", "--connect-timeout", "15", "-o", str(dest), file_url],
            capture_output=True,
        )
        record["localFile"] = dest.name
        record["bytes"] = dest.stat().st_size if dest.exists() else 0
    records.append(record)
    print(("OK" if ok else "SKIP"), license_name, title, flush=True)

(root / "catalog-image-work" / "priority3-selected.json").write_text(
    json.dumps(records, indent=2), encoding="utf-8"
)
print("done", flush=True)
