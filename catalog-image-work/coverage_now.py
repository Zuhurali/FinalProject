import csv
import subprocess
from collections import defaultdict
from pathlib import Path

root = Path(__file__).resolve().parents[1]
raw = subprocess.check_output(
    [
        "sqlcmd", "-E", "-S", r"VICTUS\SQLEXPRESS", "-d", "FinalProjectDb",
        "-f", "65001", "-h", "-1", "-W", "-s", "|",
        "-Q", "SET NOCOUNT ON; SELECT CAST(c.Id AS varchar(10)), c.Model, CAST(c.Year AS varchar(10)), i.ImageUrl, CASE WHEN i.IsMain=1 THEN '1' ELSE '0' END FROM Cars c JOIN CarImages i ON i.CarId=c.Id JOIN Brands b ON b.Id=c.BrandId ORDER BY c.Id, i.DisplayOrder;",
    ],
    text=True,
    encoding="utf-8",
    errors="replace",
)
rows = []
for line in raw.splitlines():
    parts = [part.strip() for part in line.split("|")]
    if len(parts) == 5 and parts[0].isdigit():
        rows.append(parts)

audit = {}
with (root / "catalog-audit-before.csv").open(encoding="utf-8") as handle:
    for item in csv.DictReader(handle):
        audit[item["ImageUrl"]] = item["InferredView"]

known = {}
with (root / "catalog-image-work" / "ids-111-122.csv").open(encoding="utf-8") as handle:
    for item in csv.DictReader(handle):
        url = (item.get("LocalUrl") or "").strip()
        role = (item.get("Role") or "").strip().lower()
        if url and role and "unresolved" not in (item.get("Notes") or "").lower():
            known[url] = role

def view(url: str) -> str:
    lowered = url.lower()
    if any(token in lowered for token in ("portfolio/", "/blog/", "banner-slider")):
        return "template"
    if url in known:
        return known[url]
    if url in audit:
        return audit[url]
    name = url.lower()
    if any(token in name for token in ("interior", "cabin", "dashboard", "console", "cockpit")):
        return "interior"
    if "rear" in name:
        return "rear"
    if any(token in name for token in ("portfolio/", "blog/", "banner-slider")):
        return "template"
    if "front" in name:
        return "front"
    return "unknown"

cars = defaultdict(list)
for car_id, model, year, url, is_main in rows:
    cars[car_id].append((model, year, url, is_main, view(url)))

full = []
missing_rear = []
missing_interior = []
template = []
for car_id, images in cars.items():
    model, year = images[0][0], images[0][1]
    views = {item[4] for item in images}
    label = f"{car_id} {model} {year}"
    has_front = bool(views & {"front", "front/main", "front-quarter"}) or any(
        item[3] == "1" and item[4] not in {"rear", "interior", "template", "side", "detail", "engine"}
        for item in images
    )
    has_rear = bool(views & {"rear", "rear-quarter"})
    has_interior = bool(views & {"interior", "cabin", "dashboard", "cockpit"})
    if any(item[4] == "template" for item in images):
        template.append(label)
        has_front = False
    if has_front and has_rear and has_interior:
        full.append(label)
    if not has_rear:
        missing_rear.append(label)
    if not has_interior:
        missing_interior.append(label)

report = root / "catalog-image-work" / "coverage-now.txt"
report.write_text(
    "\n".join([
        f"cars {len(cars)}",
        f"images {len(rows)}",
        f"full {len(full)}",
        f"missing_rear {len(missing_rear)}",
        f"missing_interior {len(missing_interior)}",
        f"template_mains {len(template)}",
        "---FULL---",
        *full,
        "---NO REAR---",
        *missing_rear,
        "---NO INTERIOR---",
        *missing_interior,
        "---TEMPLATE---",
        *template,
    ]),
    encoding="utf-8",
)
no_front = []
for car_id, images in cars.items():
    label = f"{car_id} {images[0][0]} {images[0][1]}"
    if label in full or label in template:
        continue
    views = {item[4] for item in images}
    has_rear = bool(views & {"rear", "rear-quarter"})
    has_interior = bool(views & {"interior", "cabin", "dashboard", "cockpit"})
    has_front = label not in missing_rear and label not in missing_interior and False
    if has_rear and has_interior and label not in full:
        no_front.append(label)
extra = report.read_text(encoding="utf-8")
report.write_text(extra + "\n---REAR+INTERIOR NO FRONT---\n" + "\n".join(no_front) + f"\ncount {len(no_front)}\n", encoding="utf-8")
print("no_front", len(no_front))
