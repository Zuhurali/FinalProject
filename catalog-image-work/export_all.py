import csv
import subprocess
from collections import defaultdict
from pathlib import Path

root = Path(__file__).resolve().parents[1]
raw = subprocess.check_output(
    [
        "sqlcmd", "-E", "-S", r"VICTUS\SQLEXPRESS", "-d", "FinalProjectDb",
        "-f", "65001", "-h", "-1", "-W", "-s", "|",
        "-Q",
        "SET NOCOUNT ON; SELECT CAST(c.Id AS varchar(10)), b.Name, c.Model, CAST(c.Year AS varchar(10)), c.Color, CAST(i.Id AS varchar(10)), CASE WHEN i.IsMain=1 THEN '1' ELSE '0' END, CAST(i.DisplayOrder AS varchar(10)), i.ImageUrl FROM Cars c JOIN Brands b ON b.Id=c.BrandId JOIN CarImages i ON i.CarId=c.Id ORDER BY c.Id, i.DisplayOrder;",
    ],
    text=True,
    encoding="utf-8",
    errors="replace",
)

rows = []
for line in raw.splitlines():
    parts = [part.strip() for part in line.split("|")]
    if len(parts) == 9 and parts[0].isdigit():
        rows.append(parts)

def classify(url: str) -> str:
    lowered = url.lower()
    if any(token in lowered for token in ("portfolio/", "/blog/", "banner-slider")):
        return "template"
    if any(token in lowered for token in ("interior", "cabin", "dashboard", "console", "cockpit")):
        return "interior"
    if "rear" in lowered:
        return "rear"
    if "front" in lowered:
        return "front"
    return "unknown"

out = root / "catalog-image-work" / "live-catalog.csv"
with out.open("w", encoding="utf-8", newline="") as handle:
    writer = csv.writer(handle)
    writer.writerow(["CarId", "Brand", "Model", "Year", "Color", "ImageId", "IsMain", "DisplayOrder", "ImageUrl", "GuessedView"])
    for row in rows:
        writer.writerow(row + [classify(row[8])])

cars = defaultdict(list)
for row in rows:
    cars[row[0]].append(row)

summary = []
for car_id, images in cars.items():
    brand, model, year = images[0][1], images[0][2], images[0][3]
    views = {classify(item[8]) for item in images}
    mains = [item for item in images if item[6] == "1"]
    main_view = classify(mains[0][8]) if mains else "none"
    summary.append(
        f"{car_id}|{brand}|{model}|{year}|images={len(images)}|main={main_view}|views={','.join(sorted(views))}|main_url={mains[0][8] if mains else ''}"
    )

(root / "catalog-image-work" / "live-summary.txt").write_text("\n".join(summary), encoding="utf-8")
print("cars", len(cars), "images", len(rows))
