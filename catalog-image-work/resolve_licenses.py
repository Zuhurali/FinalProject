import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
files = [
    "/assets/images/catalog/convertible/audi-a5-f5-cabriolet-interior.jpg",
    "/assets/images/catalog/convertible/ford-mustang-s650-interior.jpg",
    "/assets/images/catalog/convertible/mercedes-e-class-a238-interior.jpg",
    "/assets/images/catalog/convertible/mercedes-e-class-a238-rear.jpg",
    "/assets/images/catalog/convertible/porsche-911-992-cabriolet-interior.jpg",
    "/assets/images/catalog/coupe/audi-tt-8s-interior.jpg",
    "/assets/images/catalog/coupe/chevrolet-camaro-ss-2018-front.jpg",
    "/assets/images/catalog/coupe/dodge-challenger-srt-hellcat-front.jpg",
    "/assets/images/catalog/coupe/dodge-challenger-srt-hellcat-interior.jpg",
    "/assets/images/catalog/coupe/dodge-challenger-srt-hellcat-rear.jpg",
    "/assets/images/catalog/coupe/lexus-rc350-2018-interior.jpg",
    "/assets/images/catalog/coupe/nissan-370z-z34-interior.jpg",
    "/assets/images/catalog/coupe/porsche-718-cayman-front.jpg",
    "/assets/images/catalog/coupe/porsche-718-cayman-interior.jpg",
    "/assets/images/catalog/coupe/toyota-gr86-interior.jpg",
    "/assets/images/catalog/coupe/toyota-gr86-rear.jpg",
    "/assets/images/catalog/electric/audi-etron-gt-interior.jpg",
    "/assets/images/catalog/electric/audi-etron-gt-rear.jpg",
    "/assets/images/catalog/electric/hyundai-ioniq-5-2023-interior.jpg",
    "/assets/images/catalog/electric/hyundai-ioniq-5-2023-rear.jpg",
    "/assets/images/catalog/electric/porsche-taycan-front.jpg",
    "/assets/images/catalog/electric/porsche-taycan-interior.jpg",
    "/assets/images/catalog/electric/tesla-model-s-refresh-rear.jpg",
]
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
out = []

def curl(url: str) -> bytes:
    result = subprocess.run(
        ["curl.exe", "-skL", "-A", ua, "--max-time", "40", url],
        capture_output=True,
    )
    return result.stdout

for local in files:
    stem = Path(local).stem.replace("-", " ")
    search = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode(
        {
            "action": "query",
            "format": "json",
            "list": "search",
            "srsearch": f'intitle:"{stem}"',
            "srnamespace": "6",
            "srlimit": "4",
        }
    )
    raw = curl(search)
    title = None
    try:
        payload = json.loads(raw.decode("utf-8"))
        hits = payload.get("query", {}).get("search", [])
        title = hits[0]["title"] if hits else None
        error = payload.get("error", {}).get("code")
    except Exception as exc:
        hits = []
        error = str(exc)
        payload = {"raw": raw[:200].decode("utf-8", "replace")}
    info = None
    if title:
        time.sleep(1.2)
        info_url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode(
            {
                "action": "query",
                "format": "json",
                "titles": title,
                "prop": "imageinfo",
                "iiprop": "url|extmetadata|size",
            }
        )
        info_raw = curl(info_url)
        try:
            info_payload = json.loads(info_raw.decode("utf-8"))
            page = next(iter(info_payload["query"]["pages"].values()))
            image = page["imageinfo"][0]
            meta = image.get("extmetadata", {})
            info = {
                "title": title,
                "page": image.get("descriptionurl"),
                "license": meta.get("LicenseShortName", {}).get("value"),
                "author": meta.get("Artist", {}).get("value"),
                "width": image.get("width"),
                "height": image.get("height"),
            }
        except Exception as exc:
            info = {"error": str(exc), "raw": info_raw[:180].decode("utf-8", "replace")}
    out.append({"local": local, "query": stem, "hits": len(hits) if isinstance(hits, list) else 0, "error": error, "info": info})
    print(local, "->", (info or {}).get("license"), (info or {}).get("title"), flush=True)
    time.sleep(1.5)

(root / "catalog-image-work" / "unseeded-licenses.json").write_text(
    json.dumps(out, indent=2), encoding="utf-8"
)
