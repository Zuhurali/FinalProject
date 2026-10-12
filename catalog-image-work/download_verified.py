import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "verified8"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

files = [
    "Nissan 350Z Premium Pack in sunset orange, 2003, front view.jpg",
    "2021 Toyota Highlander interior.jpg",
    "2021 Toyota Highlander XLE AWD, rear 12.12.20.jpg",
    "2023 Toyota Highlander interior.jpg",
    "2018 Maserati GranTurismo Sport Automatic 4.7 Front.jpg",
    "2007 Maserati GranTurismo V8 Automatic 4.2 Rear.jpg",
    "Maserati GranTurismo rear 20071104.jpg",
    "Porsche 911 Carrera S Coupé (997, Facelift) – Frontansicht, 17. April 2011, Düsseldorf.jpg",
    "Porsche 911 Carrera S Coupé (997, Facelift) – Heckansicht (1), 17. April 2011, Düsseldorf.jpg",
    "TOYOTA HIGHLANDER (XU70) CHINA VERSION INTERIOR (2).jpg",
]

for name in files:
    safe = name.replace(" ", "_").replace("/", "-").replace(":", "-").replace(",", "")
    dest = incoming / safe
    if dest.exists() and dest.stat().st_size > 30000:
        print("HAVE", dest.name, dest.stat().st_size, flush=True)
        continue
    url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(name) + "?width=1400"
    time.sleep(8.0)
    result = subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "80", "--connect-timeout", "20", "-o", str(dest), url],
        capture_output=True,
    )
    size = dest.stat().st_size if dest.exists() else 0
    print(("OK" if size > 30000 else "FAIL"), size, name, flush=True)

print("done", flush=True)
