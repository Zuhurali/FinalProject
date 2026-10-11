import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "incoming"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

titles = [
    "File:2021 BMW M4 Competition G82 rear.jpg",
    "File:BMW G82 M4 Competition Extended Merino Leather Kyalami Orange Black (12).jpg",
    "File:2019 BMW 530i M Sport (40).jpg",
    "File:BMW 740d xDrive Excellence (G70) rear.jpg",
    "File:BMW G70's front Interior.jpg",
    "File:2019 BMW M850i xDrive Automatic 4.4 Rear.jpg",
    "File:2019 BMW M850i xDrive 4.4 Interior.jpg",
    "File:2023 BMW G01 X3 M40i DSC 7394.jpg",
    "File:BMW X5 M Competition (G05) rear.jpg",
    "File:2020 BMW X5 (G05) interior.jpg",
    "File:BMW G29 Z4 M40i Vernasca Leather Ivory White Deco (2).jpg",
    "File:BMW G26E i4 Mocha Vernasca Leather (3).jpg",
    "File:2023 Tesla Model 3 Highland Long Range AWD (Rear).jpg",
    "File:Dodge Challenger SRT Hellcat front (20418607854).jpg",
    "File:Dodge Challenger SRT Hellcat interior (16320162432).jpg",
    "File:Honda CIVIC TYPE R (FK8) interior.jpg",
    "File:2012 Hyundai Veloster, rear left, 10-29-2023.jpg",
]

def load_meta():
    found = {}
    for name in ["search-results-bmw.json", "search-results-gaps.json", "unseeded-licenses.json"]:
        data = json.loads((root / "catalog-image-work" / name).read_text(encoding="utf-8"))
        items = data if isinstance(data, list) else []
        for item in items:
            candidates = item.get("candidates") or []
            if item.get("info"):
                candidates = [item["info"]]
            for candidate in candidates:
                title = candidate.get("title")
                if title:
                    found[title] = candidate
    return found

meta = load_meta()
records = []
for title in titles:
    name = title.removeprefix("File:").replace(" ", "_").replace("/", "-")
    dest = incoming / name
    url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(title.removeprefix("File:")) + "?width=1600"
    if not dest.exists() or dest.stat().st_size < 10000:
        result = subprocess.run(
            ["curl.exe", "-skL", "-A", ua, "--max-time", "60", "-o", str(dest), url],
            capture_output=True,
        )
        print(title, dest.stat().st_size if dest.exists() else 0, result.returncode, flush=True)
        time.sleep(1.2)
    else:
        print("exists", title, dest.stat().st_size, flush=True)
    info = meta.get(title, {})
    records.append({
        "title": title,
        "localFile": str(dest.relative_to(root)),
        "bytes": dest.stat().st_size if dest.exists() else 0,
        "license": info.get("license"),
        "author": info.get("author"),
        "filePage": info.get("filePage") or info.get("page"),
        "sourceUrl": url,
    })

for record in records:
    if record["license"]:
        continue
    time.sleep(3)
    info_url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "titles": record["title"],
        "prop": "imageinfo",
        "iiprop": "url|extmetadata",
    })
    raw = subprocess.run(
        ["curl.exe", "-skL", "-A", ua, "--max-time", "40", info_url],
        capture_output=True,
    ).stdout
    try:
        page = next(iter(json.loads(raw.decode("utf-8"))["query"]["pages"].values()))
        image = page["imageinfo"][0]
        meta_info = image.get("extmetadata", {})
        record["license"] = meta_info.get("LicenseShortName", {}).get("value")
        record["author"] = meta_info.get("Artist", {}).get("value")
        record["filePage"] = image.get("descriptionurl")
        print("licensed", record["title"], record["license"], flush=True)
    except Exception as exc:
        record["licenseError"] = str(exc)
        record["raw"] = raw[:160].decode("utf-8", "replace")
        print("license failed", record["title"], record["raw"], flush=True)

(root / "catalog-image-work" / "batch-download.json").write_text(json.dumps(records, indent=2), encoding="utf-8")
print("wrote", len(records))
