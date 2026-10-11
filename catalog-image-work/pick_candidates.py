import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
data = json.loads((root / "catalog-image-work/search-results-gaps.json").read_text(encoding="utf-8"))
reject = ("fender", "detail", "badge", "wheel", "engine", "logo", "emblem", "seat only")
for item in data:
    kept = []
    for candidate in item.get("candidates", []):
        title = (candidate.get("title") or "").lower()
        description = (candidate.get("description") or "").lower()
        if any(word in title or word in description for word in reject):
            continue
        if candidate.get("width", 0) < 1200:
            continue
        kept.append(candidate)
    print(f"car {item.get('carId')} {item.get('role')} query={item.get('query')} kept={len(kept)}/{len(item.get('candidates', []))}")
    for candidate in kept[:2]:
        print(" ", candidate.get("license"), candidate.get("title"))
        print(" ", candidate.get("filePage"))
