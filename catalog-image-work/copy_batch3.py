import shutil
from pathlib import Path

root = Path(__file__).resolve().parents[1]
src = root / "catalog-image-work" / "batch3"
www = root / "FinalProject.MVC" / "wwwroot" / "assets" / "images" / "catalog"
pairs = [
    (src / "Hyundai_Ioniq_5_(rear).jpg", www / "electric" / "hyundai-ioniq-5-rear.jpg"),
    (src / "Hyundai_Ioniq_5_NE_Interior_(1).jpg", www / "electric" / "hyundai-ioniq-5-ne-interior.jpg"),
    (src / "Toyota_GR86_SZ_rear.jpg", www / "coupe" / "toyota-gr86-sz-rear.jpg"),
    (src / "The_interior_of_Toyota_GR86_RZ_6AT_(3BA-ZN8-A2F7).jpg", www / "coupe" / "toyota-gr86-rz-interior.jpg"),
    (src / "2019_Nissan_Leaf_rear.jpg", www / "hatchback" / "nissan-leaf-ze1-2019-rear.jpg"),
    (src / "2021_Toyota_Sienna_XLE_Hybrid_rear_12.21.21.jpg", www / "minivans" / "toyota-sienna-xl40-2021-rear.jpg"),
    (src / "2023_Audi_e-tron_GT_interior.jpg", www / "electric" / "audi-etron-gt-2023-interior.jpg"),
    (src / "Porsche_Taycan_GTS_(interior)_(taken_in_2022)_(Kyoto_Japan).jpg", www / "electric" / "porsche-taycan-gts-interior.jpg"),
    (src / "Mercedes-AMG_GT_(C190)_rear.jpg", www / "coupe" / "mercedes-amg-gt-c190-rear.jpg"),
]
for source, dest in pairs:
    if not source.exists():
        raise SystemExit(f"missing {source}")
    shutil.copy2(source, dest)
    print(dest.name, dest.stat().st_size)
