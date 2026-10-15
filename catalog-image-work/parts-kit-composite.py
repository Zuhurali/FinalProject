import json, os, subprocess, time
from PIL import Image, ImageOps

ROOT = r"C:\Users\HP\OneDrive\Desktop\codeACA\codeACA\FinalProject"
WORK = os.path.join(ROOT, "catalog-image-work")
ORIG = os.path.join(WORK, "parts-hd-originals")
OUT = os.path.join(ROOT, r"FinalProject.MVC\wwwroot\assets\images\portfolio\parts\hd\gear-shaft-fastener-kit.webp")

FASTENER = ("https://upload.wikimedia.org/wikipedia/commons/thumb/d/d6/"
            "Late_1900s_zinc_plated_M12_8_G_hex_head_machine_bolt_and_nut_by_ESKA_GmbH_Automotive_Germany_view_1.jpg/"
            "1920px-Late_1900s_zinc_plated_M12_8_G_hex_head_machine_bolt_and_nut_by_ESKA_GmbH_Automotive_Germany_view_1.jpg")
SHAFTS = ("https://upload.wikimedia.org/wikipedia/commons/thumb/c/c0/Cambio_innesti_frontali.jpg/"
          "1920px-Cambio_innesti_frontali.jpg")
os.makedirs(ORIG, exist_ok=True)


def fetch(url, path):
    for attempt in range(5):
        if os.path.exists(path) and os.path.getsize(path) > 50_000:
            try:
                return ImageOps.exif_transpose(Image.open(path)).convert("RGB")
            except Exception:
                pass
        subprocess.run(["curl.exe", "-sL", "-A", "AutoVaultCatalog/1.0 (student project)",
                        "--max-time", "120", "-o", path, url], check=True)
        time.sleep(3 + attempt * 5)
    raise SystemExit(f"could not download {url}")


fastener = fetch(FASTENER, os.path.join(ORIG, "fastener-bolt-nut.jpg"))
shafts = fetch(SHAFTS, os.path.join(ORIG, "gear-shaft.jpg"))


def cover(im, w, h, fy=0.5):
    scale = max(w / im.width, h / im.height)
    if scale > 1:
        raise SystemExit(f"panel {w}x{h} would upscale a {im.width}x{im.height} source")
    rw, rh = round(im.width * scale), round(im.height * scale)
    im = im.resize((rw, rh), Image.LANCZOS)
    left = (rw - w) // 2
    top = min(max(0, round(fy * rh - h / 2)), rh - h)
    return im.crop((left, top, left + w, top + h))


W, H, GAP = 1600, 1200, 8
top_h = 700
bottom_h = H - top_h - GAP
canvas = Image.new("RGB", (W, H), "white")
canvas.paste(cover(shafts, W, top_h), (0, 0))
canvas.paste(cover(fastener, W, bottom_h, 0.55), (0, top_h + GAP))
canvas.save(OUT, "WEBP", quality=86, method=6)
print("shafts", shafts.size, "fastener", fastener.size, "->", canvas.size, os.path.getsize(OUT))

lic_path = os.path.join(WORK, "parts-hd-licenses.json")
credits = json.load(open(lic_path, encoding="utf-8"))
credits["gear-shaft-fastener-kit"] = {
    "file": "~/assets/images/portfolio/parts/hd/gear-shaft-fastener-kit.webp",
    "size": f"{W}x{H}",
    "source": ["https://commons.wikimedia.org/wiki/File:Cambio_innesti_frontali.jpg",
               "https://commons.wikimedia.org/wiki/File:Late_1900s_zinc_plated_M12_8_G_hex_head_machine_bolt_and_nut_by_ESKA_GmbH_Automotive_Germany_view_1.jpg"],
    "license": ["CC BY-SA 4.0", "CC BY 4.0"],
    "author": ["A7N8X", "R. Henrik Nilsson"],
}
json.dump(credits, open(lic_path, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
