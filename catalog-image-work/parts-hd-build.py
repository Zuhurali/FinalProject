import json, os, re, subprocess, time
from PIL import Image, ImageOps

ROOT = r"C:\Users\HP\OneDrive\Desktop\codeACA\codeACA\FinalProject"
WORK = os.path.join(ROOT, "catalog-image-work")
ORIG = os.path.join(WORK, "parts-hd-originals")
OUT = os.path.join(ROOT, r"FinalProject.MVC\wwwroot\assets\images\portfolio\parts\hd")
os.makedirs(ORIG, exist_ok=True)
os.makedirs(OUT, exist_ok=True)
results = json.load(open(os.path.join(WORK, "parts-search.json"), encoding="utf-8"))

# product file name -> (search key, candidate index, horizontal focus 0..1, vertical focus 0..1)
PICKS = {
    "engine-assembly": ("engine-assembly-3", 2, 0.5, 0.45),
    "brake-caliper": ("brake-caliper", 7, 0.5, 0.42),
    "coilover-shock": ("coilover-2", 3, 0.5, 0.42),
    "air-filter": ("air-filter-2", 7, 0.5, 0.5),
    "oil-filter": ("oil-filter", 7, 0.5, 0.5),
    "timing-belt": ("timing-belt", 1, 0.5, 0.5),
    "ball-bearing": ("ball-bearing", 1, 0.5, 0.5),
    "bore-gauge": ("bore-gauge-3", 11, 0.5, 0.5),
    "check-ring": ("check-ring-2", 8, 0.5, 0.5),
    "turbocharger": ("turbocharger", 4, 0.5, 0.45),
    "exhaust-muffler": ("exhaust-muffler", 2, 0.5, 0.5),
    "exhaust-tip": ("exhaust-tip", 6, 0.5, 0.5),
    "splined-shaft": ("splined-shaft", 4, 0.5, 0.5),
    "outside-micrometer": ("outside-micrometer", 3, 0.5, 0.5),
}

def strip_html(s):
    return re.sub(r"<[^>]+>", "", s or "").strip()

credits = {}
for name, (key, idx, fx, fy) in PICKS.items():
    item = results[key][idx]
    ext = os.path.splitext(item["url"].split("?")[0])[1].lower() or ".jpg"
    src = os.path.join(ORIG, name + ext)
    im = None
    original_url = item["url"].split("?")[0]
    file_part = original_url.split("/wikipedia/commons/")[1]
    rendition_url = ("https://upload.wikimedia.org/wikipedia/commons/thumb/" + file_part +
                     "/1920px-" + file_part.rsplit("/", 1)[1])
    for attempt in range(6):
        if os.path.exists(src) and os.path.getsize(src) > 50_000:
            try:
                im = ImageOps.exif_transpose(Image.open(src)).convert("RGB")
                break
            except Exception:
                pass
        url = rendition_url if attempt % 2 == 0 else original_url
        subprocess.run(["curl.exe", "-sL", "-A", "AutoVaultCatalog/1.0 (student project)",
                        "--max-time", "180", "-o", src, url], check=True)
        time.sleep(3 + attempt * 5)
    if im is None:
        raise SystemExit(f"could not download {name}")
    w, h = im.size
    if w / h > 4 / 3:
        cw, ch = round(h * 4 / 3), h
    else:
        cw, ch = w, round(w * 3 / 4)
    left = min(max(0, round(fx * w - cw / 2)), w - cw)
    top = min(max(0, round(fy * h - ch / 2)), h - ch)
    crop = im.crop((left, top, left + cw, top + ch))
    if crop.width > 1600:
        crop = crop.resize((1600, 1200), Image.LANCZOS)
    dest = os.path.join(OUT, name + ".webp")
    crop.save(dest, "WEBP", quality=86, method=6)
    credits[name] = {
        "file": f"~/assets/images/portfolio/parts/hd/{name}.webp",
        "size": f"{crop.width}x{crop.height}",
        "source": item["page"], "original": f"{w}x{h}",
        "license": item["license"], "author": strip_html(item["artist"]),
    }
    print(name, f"{w}x{h}", "->", f"{crop.width}x{crop.height}", os.path.getsize(dest))

json.dump(credits, open(os.path.join(WORK, "parts-hd-licenses.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
