import os
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CATALOG = os.path.join(ROOT, "FinalProject.MVC", "wwwroot", "assets", "images", "catalog")
OUT = os.path.join(ROOT, "catalog-image-work", "car-audit")

name = sys.argv[1]
files = sys.argv[2:]
TW, TH, COLS = 480, 330, 3
rows = (len(files) + COLS - 1) // COLS
sheet = Image.new("RGB", (COLS * (TW + 8) + 8, rows * (TH + 24) + 8), "white")
draw = ImageDraw.Draw(sheet)
font = ImageFont.load_default()
for index, rel in enumerate(files):
    x = 8 + (index % COLS) * (TW + 8)
    y = 8 + (index // COLS) * (TH + 24)
    img = Image.open(os.path.join(CATALOG, rel.replace("/", os.sep))).convert("RGB")
    img.thumbnail((TW, TH))
    sheet.paste(img, (x, y))
    draw.text((x, y + TH + 4), rel, fill="red", font=font)
sheet.save(os.path.join(OUT, name + ".jpg"), quality=88)
