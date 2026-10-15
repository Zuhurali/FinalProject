import json
import re
import subprocess
import time
import urllib.parse

API = "https://commons.wikimedia.org/w/api.php?"
ROOTS = ["Category:Nissan 370Z", "Category:Nissan Fairlady Z (Z34)"]
KEYWORDS = re.compile(r"interior|cockpit|dash|shift|inside|innen|interieur|int\b|cabin|gear|console|steering|seat", re.I)


def get(params):
    url = API + urllib.parse.urlencode(params)
    for attempt in range(5):
        raw = subprocess.check_output(["curl.exe", "-s", "-A", "AutoVaultCatalogAudit/1.0 (local dev)", url])
        try:
            return json.loads(raw)
        except json.JSONDecodeError:
            time.sleep(3 + attempt * 3)
    return {}


seen, queue, hits = set(), list(ROOTS), []
while queue and len(seen) < 60:
    cat = queue.pop(0)
    if cat in seen:
        continue
    seen.add(cat)
    cont = {}
    while True:
        data = get({"action": "query", "format": "json", "list": "categorymembers", "cmtitle": cat,
                    "cmlimit": "500", "cmtype": "file|subcat", **cont})
        for member in data.get("query", {}).get("categorymembers", []):
            title = member["title"]
            if title.startswith("Category:"):
                if "370Z" in title or "Z34" in title:
                    queue.append(title)
            elif KEYWORDS.search(title):
                hits.append((cat, title))
        if "continue" not in data:
            break
        cont = data["continue"]

for cat, title in sorted(set(hits), key=lambda item: item[1]):
    print(title)
print(len(seen), "categories")
