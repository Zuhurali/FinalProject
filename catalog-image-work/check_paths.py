import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
text = (root / "FinalProject" / "Data" / "VerifiedCatalogSeeder.cs").read_text(encoding="utf-8")
www = root / "FinalProject.MVC" / "wwwroot"
missing = []
for url in re.findall(r'"(/assets/images/[^"]+)"', text):
    path = www.joinpath(*url.lstrip("/").split("/"))
    if not path.exists():
        missing.append(url)
print("missing", len(missing))
for url in missing:
    print(url)
