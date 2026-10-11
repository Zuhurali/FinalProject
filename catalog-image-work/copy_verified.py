import shutil
from pathlib import Path

root = Path(__file__).resolve().parents[1]
inc = root / "catalog-image-work" / "incoming"
www = root / "FinalProject.MVC" / "wwwroot" / "assets" / "images" / "catalog"
pairs = [
    (inc / "2021_BMW_M4_Competition_G82_rear.jpg", www / "bmw" / "bmw-m4-g82-2021-rear.jpg"),
    (inc / "BMW_G82_M4_Competition_Extended_Merino_Leather_Kyalami_Orange_Black_(12).jpg", www / "bmw" / "bmw-m4-g82-interior.jpg"),
    (inc / "2019_BMW_530i_M_Sport_(40).jpg", www / "bmw" / "bmw-530i-g30-2019-rear.jpg"),
    (inc / "BMW_G70's_front_Interior.jpg", www / "bmw" / "bmw-g70-interior.jpg"),
    (inc / "2019_BMW_M850i_xDrive_Automatic_4.4_Rear.jpg", www / "bmw" / "bmw-m850i-g14-2019-rear.jpg"),
    (inc / "2019_BMW_M850i_xDrive_4.4_Interior.jpg", www / "bmw" / "bmw-m850i-2019-interior.jpg"),
    (inc / "BMW_X5_M_Competition_(G05)_rear.jpg", www / "bmw" / "bmw-x5-m-competition-g05-rear.jpg"),
    (inc / "2020_BMW_X5_(G05)_interior.jpg", www / "bmw" / "bmw-x5-g05-2020-interior.jpg"),
    (inc / "BMW_G29_Z4_M40i_Vernasca_Leather_Ivory_White_Deco_(2).jpg", www / "bmw" / "bmw-z4-m40i-g29-interior.jpg"),
    (inc / "BMW_G26E_i4_Mocha_Vernasca_Leather_(3).jpg", www / "bmw" / "bmw-i4-g26-interior.jpg"),
    (inc / "2023_Tesla_Model_3_Highland_Long_Range_AWD_(Rear).jpg", www / "electric" / "tesla-model-3-highland-2023-rear.jpg"),
    (inc / "Dodge_Challenger_SRT_Hellcat_front_(20418607854).jpg", www / "coupe" / "dodge-challenger-srt-hellcat-front.jpg"),
    (inc / "Dodge_Challenger_SRT_Hellcat_interior_(16320162432).jpg", www / "coupe" / "dodge-challenger-srt-hellcat-interior.jpg"),
    (inc / "Honda_CIVIC_TYPE_R_(FK8)_interior.jpg", www / "hatchback" / "honda-civic-type-r-fk8-interior.jpg"),
    (inc / "2012_Hyundai_Veloster,_rear_left,_10-29-2023.jpg", www / "hatchback" / "hyundai-veloster-2012-rear.jpg"),
    (root / "catalog-image-work" / "selected" / "76-interior-2023-volkswagen-golf-gti-interior.jpg", www / "hatchback" / "volkswagen-golf-gti-2023-interior.jpg"),
]
for src, dest in pairs:
    if not src.exists():
        raise SystemExit(f"missing {src}")
    shutil.copy2(src, dest)
    print(dest.name, dest.stat().st_size)
