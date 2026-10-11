import json
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "catalog-image-work"
for name in ["search-results.json", "search-results-broad.json", "search-results-bmw.json"]:
    path = root / name
    data = json.loads(path.read_text(encoding="utf-8"))
    print("\n==", name, type(data).__name__, "==")
    if isinstance(data, dict):
        print("keys", list(data.keys())[:12])
        continue
    print("items", len(data))
    for item in data[:8]:
        if isinstance(item, dict):
            print(item.get("carId"), item.get("role") or item.get("query"), "cands", len(item.get("candidates", [])))
