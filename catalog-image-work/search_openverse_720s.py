import json
import subprocess
from pathlib import Path

ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
out = Path(__file__).resolve().parent / "c8-720s" / "openverse-720s.json"
url = (
    "https://api.openverse.org/v1/images/"
    "?q=McLaren%20720S%20interior%20dashboard"
    "&license=cc0,by,by-sa"
    "&page_size=20"
)
raw = subprocess.run(
    ["curl.exe", "-sL", "-A", ua, "--max-time", "40", "--connect-timeout", "15", url],
    capture_output=True,
).stdout
try:
    data = json.loads(raw.decode("utf-8"))
except Exception as exc:
    print("FAIL", exc, raw[:300])
    raise
results = []
for item in data.get("results", []):
    results.append({
        "title": item.get("title"),
        "url": item.get("url"),
        "foreign": item.get("foreign_landing_url"),
        "license": item.get("license"),
        "creator": item.get("creator"),
        "source": item.get("source"),
    })
    print(item.get("license"), item.get("title"), item.get("foreign_landing_url"), flush=True)
out.write_text(json.dumps(results, indent=2), encoding="utf-8")
print("count", len(results))
