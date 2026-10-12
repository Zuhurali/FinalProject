import json
import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
queries = [
    "Rolls-Royce Phantom VIII",
    "Rolls-Royce Phantom VIII rear",
    "Rolls-Royce Wraith interior",
    "Rolls-Royce Wraith rear",
    "BMW 7 Series G12 interior",
    "BMW 7 Series G11 rear",
    "Audi A8 D5 front",
    "Audi A8 D5 interior",
    "Mercedes-AMG GT C190",
    "Mercedes-AMG GT interior",
    "Nissan 350Z Z33",
    "Nissan 350Z interior",
    "Porsche 911 997",
    "Maserati GranTurismo",
    "Tesla Model 3",
    "Tesla Model 3 interior",
    "Toyota Highlander XU70",
    "Toyota Highlander interior",
]

def curl(url: str) -> bytes:
    completed = subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "20", "--connect-timeout", "10", url],
        capture_output=True,
    )
    return completed.stdout


records = []
for query in queries:
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "list": "search",
        "srsearch": query,
        "srnamespace": "6",
        "srlimit": "5",
    })
    raw = curl(url)
    try:
        hits = json.loads(raw.decode("utf-8")).get("query", {}).get("search", [])
        titles = [hit["title"] for hit in hits]
    except Exception as exc:
        titles = []
        records.append({"query": query, "error": str(exc), "raw": raw[:180].decode("utf-8", "replace")})
        print(f"FAIL {query}", flush=True)
        continue
    records.append({"query": query, "titles": titles})
    print(f"OK {query} -> {len(titles)}", flush=True)
    time.sleep(0.6)

(root / "catalog-image-work" / "priority-search.json").write_text(
    json.dumps(records, indent=2), encoding="utf-8"
)
print("done", flush=True)
