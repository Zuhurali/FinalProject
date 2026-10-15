import json, os, subprocess, sys, time
from PIL import Image, ImageDraw

ROOT = r"C:\Users\HP\OneDrive\Desktop\codeACA\codeACA\FinalProject\catalog-image-work"
THUMBS = os.path.join(ROOT, "parts-thumbs")
os.makedirs(THUMBS, exist_ok=True)
results = json.load(open(os.path.join(ROOT, "parts-search.json"), encoding="utf-8"))
keys = sys.argv[1:] or list(results)

for key in keys:
    items = results[key][:16]
    cells = []
    for i, item in enumerate(items):
        dest = os.path.join(THUMBS, f"{key}-{i:02d}.jpg")
        if not os.path.exists(dest) or os.path.getsize(dest) < 2000:
            subprocess.run(["curl.exe", "-sL", "-A", "AutoVaultCatalog/1.0 (student project)",
                            "--max-time", "60", "-o", dest, item["thumb"]])
            time.sleep(1.2)
        try:
            im = Image.open(dest).convert("RGB")
        except Exception:
            im = Image.new("RGB", (320, 240), "grey")
        im.thumbnail((320, 240))
        cell = Image.new("RGB", (320, 262), "white")
        cell.paste(im, ((320 - im.width) // 2, 22 + (240 - im.height) // 2))
        ImageDraw.Draw(cell).text((4, 4), f"{i} {item['w']}x{item['h']} {item['license'][:14]}", fill="black")
        cells.append(cell)
    if not cells:
        continue
    cols = 4
    rows = (len(cells) + cols - 1) // cols
    sheet = Image.new("RGB", (320 * cols, 262 * rows), "white")
    for i, c in enumerate(cells):
        sheet.paste(c, ((i % cols) * 320, (i // cols) * 262))
    sheet.save(os.path.join(ROOT, f"sheet-{key}.jpg"), quality=85)
    print("sheet", key, len(cells))
