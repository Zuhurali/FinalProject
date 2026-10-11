import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
text = (root / "FinalProject/Data/VerifiedCatalogSeeder.cs").read_text(encoding="utf-8")
pattern = re.compile(
    r'new\("([^"]+)", "([^"]+)", "([^"]+)", (\d+), .*?"(/assets/images/[^"]+)"(?:, new\[\] \{([^}]*)\})?\)'
)
records = json.loads((root / "catalog-image-work/download-records.json").read_text(encoding="utf-8"))
licensed = {}
if isinstance(records, list):
    for item in records:
        path = item.get("local") or item.get("path") or item.get("file") or ""
        licensed[path.replace("\\", "/")] = item.get("license")
elif isinstance(records, dict):
    for key, item in records.items():
        path = ""
        if isinstance(item, dict):
            path = item.get("local") or item.get("path") or item.get("file") or ""
            licensed[str(path).replace("\\", "/")] = item.get("license")

def role(url: str) -> str:
    name = url.lower()
    if any(token in name for token in ("interior", "cabin", "dashboard", "console", "wheel")):
        return "interior"
    if "rear" in name:
        return "rear"
    if any(token in name for token in ("side", "profile")):
        return "side"
    return "front"

rows = []
for match in pattern.finditer(text):
    brand, model, category, year, main, gallery = match.groups()
    urls = [main]
    if gallery:
        urls.extend(re.findall(r'"(/assets/images/[^"]+)"', gallery))
    roles = {role(url) for url in urls}
    rows.append((brand, model, year, category, roles, urls))

print(f"vehicles {len(rows)}")
full = [row for row in rows if {"front", "rear", "interior"} <= row[4]]
missing_rear = [row for row in rows if "rear" not in row[4]]
missing_interior = [row for row in rows if "interior" not in row[4]]
template = [row for row in rows if any("/portfolio/" in url or "/blog/" in url or "banner-slider" in url for url in row[5])]
print(f"full {len(full)}")
print(f"missing_rear {len(missing_rear)}")
print(f"missing_interior {len(missing_interior)}")
print(f"template {len(template)}")
print("---MISSING REAR---")
for row in missing_rear:
    print(f"{row[0]} {row[1]} {row[2]} {row[3]}")
print("---MISSING INTERIOR---")
for row in missing_interior:
    print(f"{row[0]} {row[1]} {row[2]} {row[3]}")
print("---TEMPLATE---")
for row in template:
    print(f"{row[0]} {row[1]} {row[2]} {row[5][0]}")
simple = re.findall(r'new\("([^"]+)", "([^"]+)", "([^"]+)", (\d+)', text)
parsed = {(row[0], row[1], row[2]) for row in rows}
print("---UNPARSED---")
for item in simple:
    key = (item[0], item[1], item[3])
    if key not in parsed:
        print(item)
