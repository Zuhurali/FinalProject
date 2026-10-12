from collections import defaultdict
from pathlib import Path

rows = []
for line in Path("catalog-image-work/live-images.tsv").read_text(encoding="utf-8", errors="replace").splitlines():
    if not line.strip() or line.startswith("Id|") or set(line) <= {"-", "|"}:
        continue
    parts = [part.strip() for part in line.split("|")]
    if len(parts) < 7 or not parts[0].isdigit():
        continue
    rows.append({
        "id": int(parts[0]),
        "brand": parts[1],
        "model": parts[2],
        "year": parts[3],
        "main": parts[4] in {"1", "True"},
        "url": parts[6],
    })

def classify(url: str) -> str:
    name = url.lower()
    if any(token in name for token in ("portfolio/", "banner/", "blog/")):
        return "template"
    if any(token in name for token in ("interior", "cabin", "cockpit", "dashboard", "console", "rear-seats", "rearseat")):
        return "interior"
    if any(token in name for token in ("rear", "heck", "back")):
        return "rear"
    if any(token in name for token in ("front", "quarter")):
        return "front"
    if "side" in name:
        return "side"
    return "unknown"

cars = defaultdict(lambda: {"views": set(), "urls": [], "main": None, "brand": "", "model": "", "year": ""})
for row in rows:
    car = cars[row["id"]]
    car["brand"] = row["brand"]
    car["model"] = row["model"]
    car["year"] = row["year"]
    view = classify(row["url"])
    car["views"].add(view)
    car["urls"].append((view, row["url"]))
    if row["main"]:
        car["main"] = view

total_cars = len(cars)
total_images = len(rows)
with_rear = sum(1 for car in cars.values() if "rear" in car["views"])
with_interior = sum(1 for car in cars.values() if "interior" in car["views"])
missing_rear = [cid for cid, car in cars.items() if "rear" not in car["views"]]
missing_interior = [cid for cid, car in cars.items() if "interior" not in car["views"]]
missing_either = sorted(set(missing_rear) | set(missing_interior))
template_mains = [cid for cid, car in cars.items() if car["main"] == "template"]

lines = [
    f"cars {total_cars}",
    f"images {total_images}",
    f"verified_rear {with_rear}",
    f"verified_interior {with_interior}",
    f"missing_rear {len(missing_rear)}",
    f"missing_interior {len(missing_interior)}",
    f"missing_either {len(missing_either)}",
    f"template_mains {len(template_mains)}",
    "---MISSING REAR---",
]
for cid in missing_rear:
    car = cars[cid]
    lines.append(f"{cid} {car['brand']} {car['model']} {car['year']}")
lines.append("---MISSING INTERIOR---")
for cid in missing_interior:
    car = cars[cid]
    lines.append(f"{cid} {car['brand']} {car['model']} {car['year']}")
Path("catalog-image-work/live-coverage-now.txt").write_text("\n".join(lines), encoding="utf-8")
print("\n".join(lines[:12]))
print("wrote catalog-image-work/live-coverage-now.txt")
