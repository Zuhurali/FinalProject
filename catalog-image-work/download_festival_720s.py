import json
import subprocess
import time
import urllib.parse
from pathlib import Path

ua = "AutoVaultCatalog/1.0 (educational project; local catalog seed)"
incoming = Path(__file__).resolve().parent / "c8-720s"
incoming.mkdir(exist_ok=True)


def curl(url: str, dest=None) -> bytes:
    args = ["curl.exe", "-sL", "-A", ua, "--max-time", "50", "--connect-timeout", "12"]
    if dest:
        args += ["-o", str(dest), url]
        subprocess.run(args)
        return b""
    return subprocess.run(args + [url], capture_output=True).stdout


url = "https://commons.wikimedia.org/w/api.php?" + urllib.parse.urlencode({
    "action": "query",
    "format": "json",
    "list": "categorymembers",
    "cmtitle": "Category:Festival automobile international 2018 - McLaren 720 S coupé",
    "cmlimit": "50",
    "cmtype": "file",
})
members = json.loads(curl(url).decode("utf-8")).get("query", {}).get("categorymembers", [])
print("members", len(members), flush=True)
for member in members:
    title = member["title"]
    name = title.replace("File:", "")
    dest = incoming / ("festival-" + "".join(ch if ch.isalnum() else "-" for ch in name[-8:]).strip("-") + ".jpg")
    # unique by last digits
    digits = "".join(ch for ch in name if ch.isdigit())[-3:]
    dest = incoming / f"festival-{digits}.jpg"
    if dest.exists() and dest.stat().st_size > 40000:
        print("HAVE", dest.name, title, flush=True)
        continue
    file_url = "https://commons.wikimedia.org/wiki/Special:FilePath/" + urllib.parse.quote(name) + "?width=1400"
    time.sleep(6.5)
    curl(file_url, dest)
    size = dest.stat().st_size if dest.exists() else 0
    kind = "OK" if size > 40000 else "FAIL"
    if dest.exists():
        head = dest.read_bytes()[:15]
        if head.startswith(b"<!DOCTYPE") or head.startswith(b"<html") or head.startswith(b"<HTML"):
            kind = "HTML"
            dest.unlink()
            size = 0
    print(kind, size, dest.name, title, flush=True)

print("done", flush=True)
