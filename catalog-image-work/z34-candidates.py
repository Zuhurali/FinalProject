import json
import os
import subprocess
import time
import urllib.parse

from PIL import Image, ImageDraw, ImageFont

TITLES = [
    "File:Nissan 370Z Cockpit.jpg",
    "File:FairladyZ34interior.jpg",
    "File:Nissan FAIRLADY Z (Z34) Version ST, 2022, interior.jpg",
    "File:Nissan FAIRLADY Z Heritage edition (CBA-Z34) interior.jpg",
    "File:Nissan FAIRLADY Z version ST (CBA-Z34) interior.jpg",
    "File:NISSAN 370Z (3106414385).jpg",
    "File:NISSAN 370Z (3106421295).jpg",
    "File:Nissan 370Z Z34 (5215256365).jpg",
]
UA = "AutoVaultCatalogAudit/1.0 (local dev)"
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "z34-candidates")
os.makedirs(OUT, exist_ok=True)

params = {"action": "query", "format": "json", "titles": "|".join(TITLES), "prop": "imageinfo",
          "iiprop": "size|url|extmetadata", "iiurlwidth": "800",
          "iiextmetadatafilter": "LicenseShortName|Artist|LicenseUrl"}
data = None
for attempt in range(6):
    raw = subprocess.check_output(
        ["curl.exe", "-s", "-A", UA, "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode(params)])
    try:
        data = json.loads(raw)
        break
    except json.JSONDecodeError:
        time.sleep(5 + attempt * 5)

info = {}
for page in data["query"]["pages"].values():
    ii = page["imageinfo"][0]
    meta = ii.get("extmetadata", {})
    info[page["title"]] = ii
    print(page["title"], ii["width"], ii["height"], meta.get("LicenseShortName", {}).get("value"),
          meta.get("Artist", {}).get("value", "")[:120])

tiles = []
for index, title in enumerate(TITLES):
    ii = info.get(title)
    if not ii:
        continue
    path = os.path.join(OUT, f"{index}.jpg")
    if not os.path.exists(path):
        subprocess.run(["curl.exe", "-s", "-L", "-A", UA, "-o", path, ii["thumburl"]], check=True)
        time.sleep(2)
    tiles.append((index, title, path))

TW, TH, COLS = 520, 360, 3
rows = (len(tiles) + COLS - 1) // COLS
sheet = Image.new("RGB", (COLS * (TW + 8) + 8, rows * (TH + 24) + 8), "white")
draw = ImageDraw.Draw(sheet)
for slot, (index, title, path) in enumerate(tiles):
    x = 8 + (slot % COLS) * (TW + 8)
    y = 8 + (slot // COLS) * (TH + 24)
    try:
        img = Image.open(path).convert("RGB")
        img.thumbnail((TW, TH))
        sheet.paste(img, (x, y))
    except Exception as exc:
        draw.text((x, y), f"failed: {exc}", fill="red")
    draw.text((x, y + TH + 4), f"{index}: {title[5:60]}", fill="red", font=ImageFont.load_default())
sheet.save(os.path.join(HERE, "car-audit", "z34-candidates.jpg"), quality=88)
