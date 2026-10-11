import subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
www = root / "FinalProject.MVC" / "wwwroot"
raw = subprocess.check_output(
    [
        "sqlcmd", "-E", "-S", r"VICTUS\SQLEXPRESS", "-d", "FinalProjectDb",
        "-f", "65001", "-h", "-1", "-W", "-s", "|",
        "-Q", "SET NOCOUNT ON; SELECT ImageUrl FROM CarImages;",
    ],
    text=True,
    encoding="utf-8",
)
missing = []
for line in raw.splitlines():
    url = line.strip()
    if not url.startswith("/assets/"):
        continue
    path = www / url.lstrip("/")
    if not path.exists():
        missing.append(url)
print("missing", len(missing))
print("\n".join(missing))
