import json
import subprocess
import time
import urllib.parse
from pathlib import Path

ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
out = Path(__file__).resolve().parent / "c8-720s" / "interior-search.json"


def curl(url: str) -> bytes:
    return subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "40", "--connect-timeout", "12", url],
        capture_output=True,
    ).stdout


queries = [
    "McLaren 720S interior",
    "McLaren 720S dashboard",
    "McLaren 720S cockpit",
    "McLaren 720S steering wheel",
    "720S Innenraum",
    "720S habitacle",
    "720S interne",
    "Geneva 2018 McLaren 720S",
    "IAA 2017 McLaren 720S",
    "1X7A99 McLaren",
    "McLaren 720S Super Series interior",
    "insource:720S interior",
]
results = []
for q in queries:
    time.sleep(2.5)
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "list": "search",
        "srsearch": q,
        "srnamespace": "6",
        "srlimit": "12",
    })
    raw = curl(url)
    try:
        hits = json.loads(raw.decode("utf-8")).get("query", {}).get("search", [])
        titles = [h["title"] for h in hits]
    except Exception as exc:
        titles = [f"ERROR {exc}"]
    results.append({"query": q, "titles": titles})
    print(q, "->", titles[:6], flush=True)

out.write_text(json.dumps(results, indent=2), encoding="utf-8")
print("wrote", out)
