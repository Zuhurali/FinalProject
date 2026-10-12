import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "priority2"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

queries = [
    "BMW G11 Innenraum",
    "BMW 7er G12 Interieur",
    "BMW 750Li dashboard",
    "BMW 7 Series G11 rear view",
    "Rolls-Royce Phantom VIII Heck",
    "Rolls-Royce Phantom VIII rear view",
    "Rolls-Royce Wraith dashboard",
    "Rolls-Royce Wraith Innenraum",
    "File:Audi A8 L D5 Black Valcona Interior with Piano Black Inlay (4).jpg",
]

def curl(url: str) -> bytes:
    return subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "25", "--connect-timeout", "10", url],
        capture_output=True,
    ).stdout


records = []
for query in queries:
    time.sleep(3.0)
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "list": "search",
        "srsearch": query,
        "srnamespace": "6",
        "srlimit": "4",
    })
    raw = curl(url)
    try:
        hits = json.loads(raw.decode("utf-8")).get("query", {}).get("search", [])
        titles = [hit["title"] for hit in hits]
    except Exception as exc:
        records.append({"query": query, "error": str(exc)})
        print("FAIL", query, flush=True)
        continue
    records.append({"query": query, "titles": titles})
    print("OK", query, "->", titles, flush=True)

(root / "catalog-image-work" / "priority2-search.json").write_text(
    json.dumps(records, indent=2), encoding="utf-8"
)
print("done", flush=True)
