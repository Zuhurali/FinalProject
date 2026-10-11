import json
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "catalog-image-work"

def brief(candidate):
    if not isinstance(candidate, dict):
        return str(candidate)[:120]
    title = candidate.get("title") or candidate.get("page")
    license_name = candidate.get("license") or candidate.get("License")
    return f"{license_name} | {title}"

data = json.loads((root / "search-results.json").read_text(encoding="utf-8"))
for key, value in data.items():
    print("\n", key)
    items = value if isinstance(value, list) else value.get("candidates", [value])
    if isinstance(value, dict) and "title" in value:
        items = [value]
    for item in items[:3]:
        print(" ", brief(item))

bmw = json.loads((root / "search-results-bmw.json").read_text(encoding="utf-8"))
print("\n== BMW ==")
for item in bmw:
    cands = item.get("candidates") or []
    print(f"\ncar {item.get('carId')} {item.get('role')} n={len(cands)}")
    for candidate in cands[:2]:
        print(" ", brief(candidate))
