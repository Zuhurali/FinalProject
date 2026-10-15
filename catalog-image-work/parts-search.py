import json, subprocess, sys, urllib.parse

QUERIES2 = {
    "bore-gauge-3": ["inside micrometer", "Innenmessschraube", "telescoping gauge", "Dreipunkt-Innenmessschraube", "Bohrungsmessgerät"],
    "engine-assembly-3": ["small-block V8 engine", "BMW engine exhibit", "Mercedes-AMG engine", "car engine exhibit museum", "LS3 engine"],
}
QUERIES_OLD2 = {
    "air-filter-2": ["Luftfilter Auto", "air filter automobile paper element", "K&N air filter"],
    "bore-gauge-2": ["Innenmessgerät", "Innenfeinmessgerät", "cylinder bore gauge dial indicator", "Messuhr Innenmessung"],
    "check-ring-2": ["Einstellring", "Lehrring", "ring gauge"],
    "engine-assembly-2": ["engine on engine stand", "V8 engine museum exhibit", "crate engine", "Motor Ausstellung V8"],
    "coilover-2": ["Gewindefahrwerk", "shock absorber", "coil spring strut"],
    "gear-shaft-2": ["Getriebewelle", "gearbox shaft gears", "Zahnradwelle"],
}
QUERIES_ALL = {
    "engine-assembly": ["car engine assembly isolated", "automobile engine block display"],
    "brake-caliper": ["brake caliper disc rotor", "brake caliper red"],
    "coilover-shock": ["coilover", "shock absorber spring car"],
    "air-filter": ["engine air filter element", "car air filter panel"],
    "oil-filter": ["oil filter spin-on", "car oil filter"],
    "timing-belt": ["timing belt", "toothed belt engine"],
    "ball-bearing": ["ball bearing", "deep groove ball bearing"],
    "bore-gauge": ["bore gauge", "dial bore gauge"],
    "check-ring": ["setting ring gauge", "ring gauge calibration"],
    "turbocharger": ["turbocharger", "turbocharger car cutaway"],
    "exhaust-muffler": ["exhaust muffler", "car silencer muffler"],
    "exhaust-tip": ["exhaust tip", "exhaust tailpipe chrome"],
    "splined-shaft": ["drive shaft spline", "cardan shaft", "propeller shaft car"],
    "gear-shaft": ["gear shaft", "transmission gear shaft", "bolts nuts fasteners"],
    "outside-micrometer": ["micrometer caliper", "outside micrometer"],
    "performance-tire": ["car tyre tread", "performance tire"],
}

def search(term, limit=25):
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query", "format": "json", "generator": "search",
        "gsrsearch": f"filetype:bitmap {term}", "gsrnamespace": 6, "gsrlimit": limit,
        "prop": "imageinfo", "iiprop": "url|size|mime|extmetadata", "iiurlwidth": 480,
    })
    raw = subprocess.run(["curl.exe", "-sL", "-A", "AutoVaultCatalog/1.0 (student project)", "--max-time", "60", url],
                         capture_output=True, check=True).stdout
    data = json.loads(raw)
    out = []
    for page in (data.get("query", {}).get("pages", {}) or {}).values():
        info = (page.get("imageinfo") or [{}])[0]
        if info.get("mime") not in ("image/jpeg", "image/png", "image/webp"):
            continue
        if max(info.get("width", 0), info.get("height", 0)) < 1200:
            continue
        meta = info.get("extmetadata", {})
        out.append({
            "title": page["title"], "w": info["width"], "h": info["height"],
            "url": info["url"], "thumb": info.get("thumburl"),
            "license": meta.get("LicenseShortName", {}).get("value", ""),
            "artist": meta.get("Artist", {}).get("value", "")[:200],
            "page": info.get("descriptionurl"),
        })
    return out

import os, time
OUT = r"C:\Users\HP\OneDrive\Desktop\codeACA\codeACA\FinalProject\catalog-image-work\parts-search.json"
results = json.load(open(OUT, encoding="utf-8")) if os.path.exists(OUT) else {}
for key, terms in QUERIES2.items():
    if results.get(key):
        continue
    seen, items = set(), []
    for t in terms:
        for attempt in range(4):
            try:
                for item in search(t):
                    if item["title"] not in seen:
                        seen.add(item["title"]); items.append(item)
                break
            except Exception as e:
                print("retry", key, t, attempt, e, file=sys.stderr)
                time.sleep(8 * (attempt + 1))
        time.sleep(3)
    results[key] = items
    print(key, len(items))
    json.dump(results, open(OUT, "w", encoding="utf-8"), indent=1)
