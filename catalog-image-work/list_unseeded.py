import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
text = (root / "FinalProject/Data/VerifiedCatalogSeeder.cs").read_text(encoding="utf-8")
urls = set(re.findall(r"/assets/images/catalog/[^\"\s]+", text))
catalog = root / "FinalProject.MVC/wwwroot"
files = sorted(
    path
    for path in (catalog / "assets/images/catalog").rglob("*")
    if path.suffix.lower() in {".jpg", ".jpeg", ".png", ".webp"} and "_incoming" not in path.parts
)
extra = []
for path in files:
    relative = "/" + path.relative_to(catalog).as_posix()
    if relative not in urls:
        extra.append(relative)
print(f"seeded {len(urls)}")
print(f"files {len(files)}")
print(f"unseeded {len(extra)}")
print("\n".join(extra))
