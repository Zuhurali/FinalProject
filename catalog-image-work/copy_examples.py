import shutil
from pathlib import Path

root = Path(__file__).resolve().parents[1]
src = root / "catalog-image-work" / "priority"
luxury = root / "FinalProject.MVC" / "wwwroot" / "assets" / "images" / "catalog" / "luxury"
coupe = root / "FinalProject.MVC" / "wwwroot" / "assets" / "images" / "catalog" / "coupe"

copies = [
    (src / "phantom-front__Rolls-Royce_Phantom_VIII_Series_I_IMG_9101.jpg", luxury / "rolls-royce-phantom-viii-front.jpg"),
    (src / "2014_Rolls-Royce_Wraith_diamond_black_rR.jpg", luxury / "rolls-royce-wraith-rear.jpg"),
    (src / "Mercedes-AMG_GT_(C190)_front.jpg", coupe / "mercedes-amg-gt-c190-front.jpg"),
    (src / "The_interior_of_Mercedes-AMG_GT_S_(C190).jpg", coupe / "mercedes-amg-gt-c190-interior.jpg"),
    (src / "2018_Audi_A8_50_TDi_Quattro_Automatic_3.0_Front.jpg", luxury / "audi-a8-d5-front.jpg"),
    (src / "a8-interior__Audi_A8_L_D5_Black_Valcona_Interior_with_Piano_Black_Inlay_(4).jpg", luxury / "audi-a8-d5-interior.jpg"),
    (src / "Audi_A8_L_D5_Black_Valcona_Interior_with_Piano_Black_Inlay_(35).jpg", luxury / "audi-a8-d5-rear-seats.jpg"),
]

for source, dest in copies:
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, dest)
    print(dest.name, dest.stat().st_size)
