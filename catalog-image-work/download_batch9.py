import subprocess
import time
import urllib.parse
from pathlib import Path

root = Path(__file__).resolve().parents[1]
incoming = root / "catalog-image-work" / "verified9"
incoming.mkdir(exist_ok=True)
ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"

# Confirmed Commons titles (reuse-friendly licenses) plus well-known File: names.
# Special:FilePath avoids the search API rate limit.
files = [
    # BMW G20 LCI cabin for M340i 2022
    ("BMW G20 LCI Sensatec Perforated Cognac (2).jpg", "bmw-g20-lci-interior.jpg"),
    ("BMW G20 LCI Sensatec Perforated Cognac (10).jpg", "bmw-g20-lci-interior-alt.jpg"),
    # Same-generation rears / cabins
    ("2019 BMW 330i M Sport Automatic 2.0 Rear.jpg", "bmw-g20-330i-rear.jpg"),
    ("BMW 3 Series (G20) IMG 2706.jpg", "bmw-g20-rear-guess.jpg"),
    ("2021 BMW M3 Competition G80.jpg", "bmw-m3-g80-guess.jpg"),
    ("BMW M3 Competition (G80) rear.jpg", "bmw-m3-g80-rear-guess.jpg"),
    ("2021 BMW M4 Competition G82 rear.jpg", "bmw-m4-g82-rear-already.jpg"),
    ("BMW M5 F90 rear.jpg", "bmw-m5-f90-rear-guess.jpg"),
    ("2018 BMW M5 Automatic 4.4 Rear.jpg", "bmw-m5-f90-rear.jpg"),
    ("2018 BMW M5 Interior.jpg", "bmw-m5-f90-interior-guess.jpg"),
    ("BMW X5 (G05) rear.jpg", "bmw-x5-g05-rear-guess.jpg"),
    ("2020 BMW X5 xDrive40i rear.jpg", "bmw-x5-g05-rear.jpg"),
    ("BMW X6 M Competition (F96) rear.jpg", "bmw-x6m-rear-guess.jpg"),
    ("BMW 4 Series Coupe (G22) rear.jpg", "bmw-g22-rear-guess.jpg"),
    ("2021 BMW 430i Coupe Automatic 2.0 Interior.jpg", "bmw-g22-interior-guess.jpg"),
    # Minivans — only use if generation matches after visual check
    ("2021 Toyota Sienna Limited interior.jpg", "toyota-sienna-xl40-interior-guess.jpg"),
    ("2022 Toyota Sienna interior.jpg", "toyota-sienna-interior-guess.jpg"),
    ("2018 Honda Odyssey EX-L interior.jpg", "honda-odyssey-rl6-interior-guess.jpg"),
    ("2019 Honda Odyssey interior.jpg", "honda-odyssey-2019-interior-guess.jpg"),
    ("2022 Kia Carnival SX interior.jpg", "kia-carnival-ka4-interior-guess.jpg"),
    ("2020 Kia Carnival interior.jpg", "kia-carnival-interior-guess.jpg"),
    ("2017 Chrysler Pacifica interior.jpg", "chrysler-pacifica-interior-guess.jpg"),
    ("2021 Chrysler Pacifica interior.jpg", "chrysler-pacifica-2021-interior-guess.jpg"),
    ("Volkswagen T7 Multivan interior.jpg", "vw-t7-multivan-interior-guess.jpg"),
    ("Hyundai Staria interior.jpg", "hyundai-staria-interior-guess.jpg"),
    # Sports / luxury cabins
    ("Ferrari 488 GTB interior.jpg", "ferrari-488-interior-guess.jpg"),
    ("Ferrari 488 GTB dashboard.jpg", "ferrari-488-dash-guess.jpg"),
    ("McLaren 720S interior.jpg", "mclaren-720s-interior-guess.jpg"),
    ("Lamborghini Huracan LP610-4 interior.jpg", "huracan-interior-guess.jpg"),
    ("Lamborghini Aventador LP700-4 interior.jpg", "aventador-interior-guess.jpg"),
    ("Lamborghini Diablo VT interior.jpg", "diablo-interior-guess.jpg"),
    ("Porsche 991 GT3 RS interior.jpg", "gt3rs-interior-guess.jpg"),
    ("Porsche 911 Turbo S 992 interior.jpg", "992-turbos-interior-guess.jpg"),
    ("Tesla Model S Plaid interior.jpg", "tesla-models-plaid-interior-guess.jpg"),
    ("2021 Tesla Model S interior.jpg", "tesla-models-2021-interior-guess.jpg"),
    ("Bentley Continental GT 2018 interior.jpg", "continental-gt-interior-guess.jpg"),
    ("Porsche Panamera 971 interior.jpg", "panamera-971-interior-guess.jpg"),
    ("2018 Lexus LS 500 interior.jpg", "lexus-ls500-interior-guess.jpg"),
    ("Mercedes-Maybach S-Class Z223 interior.jpg", "maybach-z223-interior-guess.jpg"),
    ("Nissan Fairlady Z RZ34 rear.jpg", "nissan-z-rz34-rear-guess.jpg"),
    ("2023 Nissan Z rear.jpg", "nissan-z-2023-rear-guess.jpg"),
    ("Nissan Leaf ZE1 interior.jpg", "nissan-leaf-ze1-interior-guess.jpg"),
    ("2019 Mazda3 sedan interior.jpg", "mazda3-bp-interior-guess.jpg"),
    ("Mazda CX-5 KF interior.jpg", "mazda-cx5-kf-interior-guess.jpg"),
    ("Volkswagen Golf 8 R interior.jpg", "golf-8-r-interior-guess.jpg"),
    ("2018 Volkswagen Golf R interior.jpg", "golf-7-r-interior-guess.jpg"),
    ("Maserati GranTurismo MC interior.jpg", "granturismo-interior-guess.jpg"),
    ("2008 Maserati GranTurismo Interior.jpg", "granturismo-2008-interior-guess.jpg"),
    ("Rolls-Royce Wraith Series I interior.jpg", "wraith-interior-guess.jpg"),
    ("2014 Rolls-Royce Wraith interior.jpg", "wraith-2014-interior-guess.jpg"),
    ("BMW iX rear.jpg", "bmw-ix-rear-guess.jpg"),
    ("Porsche Taycan rear.jpg", "porsche-taycan-rear-guess.jpg"),
    ("Mercedes-Benz S-Class W223 rear.jpg", "sclass-w223-rear-guess.jpg"),
    ("Genesis G90 RS4 interior.jpg", "g90-interior-guess.jpg"),
]

for commons_name, dest_name in files:
    dest = incoming / dest_name
    if dest.exists() and dest.stat().st_size > 40000:
        print("HAVE", dest_name, dest.stat().st_size, flush=True)
        continue
    url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(commons_name) + "?width=1400"
    time.sleep(6.5)
    subprocess.run(
        ["curl.exe", "-sL", "-A", ua, "--max-time", "80", "--connect-timeout", "20", "-o", str(dest), url],
        check=False,
    )
    size = dest.stat().st_size if dest.exists() else 0
    # Commons missing files often return a small HTML/error payload
    if size < 40000:
        if dest.exists():
            dest.unlink()
        print("MISS", dest_name, size, commons_name, flush=True)
    else:
        print("OK", dest_name, size, commons_name, flush=True)

print("done", flush=True)
