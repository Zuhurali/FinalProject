import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "verified8"
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
files = [
    "P997 carrera s innenraum.jpg",
    "P997 carrera s innenraum P1010060.JPG",
    "P997 carrera s armaturen.jpg",
]
for name in files:
    dest = incoming / name.replace(" ", "_")
    if dest.exists() and dest.stat().st_size > 20000:
        print("HAVE", dest.name, dest.stat().st_size, flush=True)
        continue
    time.sleep(8.0)
    url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(name) + "?width=1400"
    subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "80", "--connect-timeout", "20", "-o", str(dest), url],
        capture_output=True,
    )
    size = dest.stat().st_size if dest.exists() else 0
    print(("OK" if size > 20000 else "FAIL"), size, name, flush=True)
print("done", flush=True)
