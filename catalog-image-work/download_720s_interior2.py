import json
import subprocess
import time
import urllib.parse
from pathlib import Path

ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
incoming = Path(__file__).resolve().parent / "c8-720s"
incoming.mkdir(exist_ok=True)


def curl(url: str, dest=None) -> bytes:
    args = ["curl.exe", "-sL", "-A", ua, "--max-time", "40", "--connect-timeout", "12"]
    if dest:
        args += ["-o", str(dest), url]
        subprocess.run(args)
        return b""
    return subprocess.run(args + [url], capture_output=True).stdout


print("=== category festival ===", flush=True)
for cat in [
    "Category:Festival automobile international 2018 - McLaren 720 S coupé",
    "Category:McLaren 720S interiors",
    "Category:McLaren 720S Spider",
]:
    url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
        "action": "query",
        "format": "json",
        "list": "categorymembers",
        "cmtitle": cat,
        "cmlimit": "50",
    })
    time.sleep(1.5)
    raw = curl(url)
    try:
        members = json.loads(raw.decode("utf-8")).get("query", {}).get("categorymembers", [])
        print(cat, len(members), flush=True)
        for m in members:
            print(" ", m.get("title"), flush=True)
    except Exception as exc:
        print("FAIL", cat, exc, raw[:120], flush=True)

print("=== openverse ===", flush=True)
ov = curl(
    "https://api.openverse.org/v1/images/?q=McLaren%20720S%20interior&license=cc0,by,by-sa&page_size=20"
)
try:
    items = json.loads(ov.decode("utf-8")).get("results", [])
    for item in items:
        print(item.get("license"), item.get("title"), item.get("foreign_landing_url"), flush=True)
except Exception as exc:
    print("OV FAIL", exc, ov[:200], flush=True)

print("=== extra files ===", flush=True)
files = [
    ("McLaren 720S IR.jpg", "720s-ir.jpg"),
    ("The interior of McLaren 720S COUPE.JPG", "720s-coupe-int-jpg.jpg"),
    ("McLaren 720S 140204.jpg", "720s-140204.jpg"),
    ("McLaren 720S - 04.jpg", "720s-04.jpg"),
    ("2018 McLaren 720S 5.jpg", "2018-720s-5.jpg"),
    ("2018 McLaren 720S 6.jpg", "2018-720s-6.jpg"),
    ("McLaren 720S (2018) (52228246608).jpg", "720s-52228246608.jpg"),
    ("McLaren 720S (2018) (52228246633).jpg", "720s-52228246633.jpg"),
    ("Interior of a McLaren 750S (Japanese specification).jpg", "750s-interior-ref-only.jpg"),
    ("Festival automobile international 2018 - McLaren 720 S coupé - 001.jpg", "festival-720s-001.jpg"),
    ("Festival automobile international 2018 - McLaren 720 S coupé - 008.jpg", "festival-720s-008.jpg"),
    ("Festival automobile international 2018 - McLaren 720 S coupé - 012.jpg", "festival-720s-012.jpg"),
]
for commons_name, dest_name in files:
    dest = incoming / dest_name
    if dest.exists() and dest.stat().st_size > 40000:
        print("HAVE", dest_name, flush=True)
        continue
    url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(commons_name) + "?width=1400"
    time.sleep(6.5)
    curl(url, dest)
    size = dest.stat().st_size if dest.exists() else 0
    kind = "OK" if size > 40000 else "FAIL"
    if dest.exists():
        head = dest.read_bytes()[:15]
        if head.startswith(b"<!DOCTYPE") or head.startswith(b"<html") or head.startswith(b"<HTML"):
            kind = "HTML"
            dest.unlink()
            size = 0
    print(kind, size, dest_name, flush=True)

print("done", flush=True)
