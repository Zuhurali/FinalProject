import json
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "catalog-image-work"
out = []
for name in ["search-results.json", "search-results-broad.json"]:
    data = json.loads((root / name).read_text(encoding="utf-8"))
    out.append(f"== {name}")
    for key, value in data.items():
        candidates = value if isinstance(value, list) else value.get("candidates") or value.get("hits") or []
        if isinstance(value, dict) and not candidates:
            candidates = value.get("results") or []
        out.append(f"{key} type={type(value).__name__} n={len(candidates) if isinstance(candidates, list) else '?'}")
        if isinstance(value, dict) and not isinstance(candidates, list):
            out.append("  keys " + ",".join(value.keys()))
        items = candidates if isinstance(candidates, list) else []
        if isinstance(value, dict) and "title" in value:
            items = [value]
        for candidate in items[:3]:
            if not isinstance(candidate, dict):
                out.append("  " + str(candidate)[:160])
                continue
            title = candidate.get("title") or candidate.get("file") or ""
            lic = candidate.get("license") or candidate.get("License") or ""
            out.append(f"  {lic} | {title}")

bmw = json.loads((root / "search-results-bmw.json").read_text(encoding="utf-8"))
out.append("== bmw")
for group in bmw:
    cands = group.get("candidates") or []
    out.append(f"id={group.get('carId')} {group.get('role')} n={len(cands)} q={group.get('query')}")
    for candidate in cands[:2]:
        out.append(f"  {candidate.get('license')} | {candidate.get('title')}")

(root / "search-summary.txt").write_text("\n".join(out), encoding="utf-8")
print("wrote", len(out))
