import shutil
from pathlib import Path

root = Path(__file__).resolve().parents[1]
batch = root / "catalog-image-work" / "batch2"
www = root / "FinalProject.MVC" / "wwwroot" / "assets" / "images" / "catalog"
pairs = [
    (batch / "Audi_A3_Sportback_8V_facelift_interior.jpg", www / "hatchback" / "audi-a3-sportback-8v-facelift-interior.jpg"),
    (batch / "BMW_G30_LCI_Exclusive_Nappa_Leather_Mocha_(8).jpg", www / "bmw" / "bmw-g30-lci-interior.jpg"),
    (batch / "BMW_Z4_(G29)_(4).jpg", www / "bmw" / "bmw-z4-g29-m40i-rear.jpg"),
    (batch / "BMW_840i_Gran_Coupé_M_Sport_(G16)_rear.jpg", www / "luxury" / "bmw-840i-g16-rear.jpg"),
    (batch / "Porsche_911_GT3_RS_(991.2,_2018)_(52574909305).jpg", www / "sports-car" / "porsche-911-gt3-rs-991-2-front.jpg"),
]
for src, dest in pairs:
    if not src.exists():
        raise SystemExit(f"missing {src}")
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dest)
    print(dest.name, dest.stat().st_size)
