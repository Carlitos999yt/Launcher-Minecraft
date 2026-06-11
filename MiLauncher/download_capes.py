import urllib.request
import json
import os

req = urllib.request.Request('https://api.github.com/repos/lukynkaCZE/minecraft-capes/contents/capes', headers={'User-Agent': 'Mozilla/5.0'})
try:
    response = urllib.request.urlopen(req).read().decode('utf-8')
    files = json.loads(response)
    
    out_dir = os.path.join('Resources', 'Capes')
    os.makedirs(out_dir, exist_ok=True)
    
    downloaded = 0
    for f in files:
        if f['name'].endswith('.png'):
            url = f['download_url']
            print(f"Downloading {f['name']}...")
            urllib.request.urlretrieve(url, os.path.join(out_dir, f['name']))
            downloaded += 1
            
    print(f"Successfully downloaded {downloaded} capes!")
except Exception as e:
    print(f"Error: {e}")
