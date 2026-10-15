import os
import subprocess
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WWW = os.path.join(ROOT, "FinalProject.MVC", "wwwroot")
OUT = os.path.join(ROOT, "catalog-image-work", "car-audit")
os.makedirs(OUT, exist_ok=True)

query = (
    "SET NOCOUNT ON; SELECT c.Id, b.Name, c.Model, c.Color, c.Transmission, i.ImageUrl "
    "FROM Cars c JOIN Brands b ON b.Id=c.BrandId JOIN CarImages i ON i.CarId=c.Id "
    "ORDER BY c.Id, i.IsMain DESC, i.DisplayOrder"
)
raw = subprocess.check_output(
    ["sqlcmd", "-S", "VICTUS\\SQLEXPRESS", "-d", "FinalProjectDb", "-E", "-W", "-h", "-1", "-s", "|", "-Q", query],
    text=True, encoding="utf-8", errors="replace")

cars = {}
for line in raw.splitlines():
    parts = line.strip().split("|")
    if len(parts) != 6 or not parts[0].isdigit():
        continue
    cid = int(parts[0])
    cars.setdefault(cid, {"label": f"#{cid} {parts[1]} {parts[2]} | {parts[3]} | {parts[4]}", "images": []})
    cars[cid]["images"].append(parts[5])

font = ImageFont.load_default()
TW, TH, COLS = 260, 170, 6
ids = sorted(cars)
per_sheet = 8
for sheet_no in range(0, len(ids), per_sheet):
    chunk = ids[sheet_no:sheet_no + per_sheet]
    sheet = Image.new("RGB", (COLS * (TW + 6) + 6, len(chunk) * (TH + 40) + 6), "white")
    draw = ImageDraw.Draw(sheet)
    for row, cid in enumerate(chunk):
        y = 6 + row * (TH + 40)
        draw.text((8, y), cars[cid]["label"], fill="black", font=font)
        for col, url in enumerate(cars[cid]["images"][:COLS]):
            path = os.path.join(WWW, url.lstrip("/").replace("/", os.sep))
            x = 6 + col * (TW + 6)
            try:
                img = Image.open(path).convert("RGB")
                size = f"{img.width}x{img.height}"
                img.thumbnail((TW, TH))
                sheet.paste(img, (x, y + 14))
            except Exception:
                size = "MISSING"
            draw.text((x, y + 16 + TH), f"{col + 1}: {size}", fill="red", font=font)
    sheet.save(os.path.join(OUT, f"sheet-{sheet_no // per_sheet + 1:02d}.jpg"), quality=85)
print(len(ids), "cars")
