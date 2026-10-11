import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
data = json.loads((root / "catalog-image-work" / "search-results-gaps.json").read_text(encoding="utf-8"))
print("groups", len(data))
for group in data:
    candidates = group.get("candidates") or []
    print(f"id={group.get('carId')} role={group.get('role')} q={group.get('query')} n={len(candidates)}")
    for candidate in candidates[:4]:
        title = (candidate.get("title") or "")[:120]
        print("   ", candidate.get("license"), "|", title)
