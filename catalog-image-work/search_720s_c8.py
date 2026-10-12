import json
import subprocess
import time
import urllib.parse
from pathlib import Path

ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
out_dir = Path(__file__).resolve().parent / "c8-720s"
out_dir.mkdir(exist_ok=True)


def curl(url: str) -> bytes:
    return subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "40", "--connect-timeout", "12", url],
        capture_output=True,
    ).stdout


def api(params: dict) -> dict:
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode(params)
    raw = curl(url)
    try:
        return json.loads(raw.decode("utf-8"))
    except Exception:
        print("BAD JSON", params.get("list") or params.get("titles"), raw[:200])
        return {}


cont = ""
titles = []
while True:
    params = {
        "action": "query",
        "format": "json",
        "list": "categorymembers",
        "cmtitle": "Category:McLaren 720S",
        "cmlimit": "500",
        "cmtype": "file",
    }
    if cont:
        params["cmcontinue"] = cont
    data = api(params)
    titles.extend([m["title"] for m in data.get("query", {}).get("categorymembers", [])])
    cont = data.get("continue", {}).get("cmcontinue")
    if not cont:
        break
    time.sleep(0.4)

print("720S files", len(titles))
for t in titles:
    low = t.lower()
    if any(k in low for k in ["int", "cabin", "dash", "cockpit", "inside", "seat", "steer", "door", "open"]):
        print("CAND", t)

(out_dir / "720s-files.txt").write_text("\n".join(titles), encoding="utf-8")

queries = [
    "Corvette C8 interior",
    "intitle:Corvette C8 interior",
    "Chevrolet Corvette C8 IAA 2021 1X7A018",
    "Chevrolet CORVETTE 3LT rear",
    "McLaren 720S Super Hyper",
    "McLaren 720S doors open",
    "Festival automobile international McLaren 720",
]
print("==== searches ====")
for q in queries:
    time.sleep(0.7)
    hits = api({
        "action": "query",
        "format": "json",
        "list": "search",
        "srsearch": q,
        "srnamespace": "6",
        "srlimit": "10",
    }).get("query", {}).get("search", [])
    print("Q", q)
    for h in hits:
        print(" ", h["title"])
