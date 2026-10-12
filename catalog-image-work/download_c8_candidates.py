import subprocess
import time
import urllib.parse
from pathlib import Path

incoming = Path(__file__).resolve().parent / "c8-720s"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

files = [
    ("Corvette C8 interior.jpg", "corvette-c8-interior-candidate.jpg"),
    ("MIAS 2024 - 2024 Chevrolet Corvette C8 6.2 LT2 V8 interior.jpg", "corvette-c8-mias-interior.jpg"),
    ("Chevrolet Corvette C8 4.jpg", "corvette-c8-4.jpg"),
    ("Chevrolet Corvette C8 5.jpg", "corvette-c8-5.jpg"),
    ("Chevrolet Corvette C8 Stingray 3LT Coupe Z51 Torch Red.jpg", "c8-torch-red.jpg"),
    ("Chevrolet Corvette C8 Stingray 3LT Coupe Z51 Torch Red 01.jpg", "c8-torch-red-01.jpg"),
    ("Chevrolet Corvette C8 Stingray 3LT Coupe Z51 Torch Red 02.jpg", "c8-torch-red-02.jpg"),
    ("2023 Chevrolet Corvette Stingray Coupe 2LT in Red Mist Metallic Tintcoat, Rear Left, 06-03-2023.jpg", "c8-red-mist-rear.jpg"),
    ("Chevrolet CORVETTE 3LT (7BA-Y2XC) rear.jpg", "c8-3lt-rear.jpg"),
    ("Chevrolet Corvette (C8) rear view.jpg", "c8-rear-view.jpg"),
    ("Chevrolet Corvette C8 IAA 2021 1X7A0151.jpg", "c8-iaa-0151.jpg"),
    ("Chevrolet Corvette C8 convertibles IAA 2021 1X7A0184.jpg", "c8-iaa-0184.jpg"),
    ("Tuned Chevrolet CORVETTE COUPE (C8) rear.jpg", "c8-tuned-rear.jpg"),
    ("Chevrolet Corvette C8 State Street Bridge downtown Montpelier VT May 2022 rear.jpg", "c8-montpelier-rear.jpg"),
    ("McLaren 720S, IAA 2017, (1Y7A3405).jpg", "720s-iaa-3405.jpg"),
    ("McLaren 720S, IAA 2017, (1Y7A3406).jpg", "720s-iaa-3406.jpg"),
    ("Geneva International Motor Show 2018, Le Grand-Saconnex (1X7A9902).jpg", "720s-geneva-9902.jpg"),
    ("Geneva International Motor Show 2018, Le Grand-Saconnex (1X7A9906).jpg", "720s-geneva-9906.jpg"),
    ("Geneva International Motor Show 2018, Le Grand-Saconnex (1X7A9910).jpg", "720s-geneva-9910.jpg"),
    ("The interior of a McLaren 720S.jpg", "720s-interior-the.jpg"),
    ("Interior of McLaren 720S.jpg", "720s-interior-of.jpg"),
    ("McLaren 720S Coupé interior.jpg", "720s-coupe-interior.jpg"),
    ("QAT Doha, Hamad International Airport 047.jpg", "720s-doha-047.jpg"),
]

for commons_name, dest_name in files:
    dest = incoming / dest_name
    if dest.exists() and dest.stat().st_size > 40000:
        print("HAVE", dest_name, dest.stat().st_size, flush=True)
        continue
    url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(commons_name) + "?width=1400"
    time.sleep(7.0)
    result = subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "80", "--connect-timeout", "20", "-o", str(dest), url],
        capture_output=True,
    )
    size = dest.stat().st_size if dest.exists() else 0
    # HTML error pages are typically small or start with <
    kind = "OK" if size > 40000 else "FAIL"
    if dest.exists():
        head = dest.read_bytes()[:20]
        if head.startswith(b"<!DOCTYPE") or head.startswith(b"<html") or head.startswith(b"<HTML"):
            kind = "HTML"
            dest.unlink()
            size = 0
    print(kind, size, dest_name, flush=True)

print("done", flush=True)
