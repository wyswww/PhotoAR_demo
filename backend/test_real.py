"""Explicit real GPU integration test, independent of Android and Unity."""
import sys, time, json
from pathlib import Path
from fastapi.testclient import TestClient
from service import app, REPO, ROOT

def main():
    client=TestClient(app)
    photo=Path(sys.argv[1]) if len(sys.argv)>1 else REPO/'examples/chair.png'
    r=client.post('/jobs',files={'photo':(photo.name,photo.read_bytes(),'image/png')})
    r.raise_for_status();state=r.json();print(state,flush=True);started=time.monotonic()
    while state['state'] not in ('failed','succeeded') and time.monotonic()-started<630:
        time.sleep(1);state=client.get('/jobs/'+state['id']).json()
    reports=ROOT.parent/'reports';reports.mkdir(exist_ok=True)
    (reports/'real-inference.json').write_text(json.dumps(dict(state,elapsed_seconds=round(time.monotonic()-started,2)),indent=2))
    assert state['state']=='succeeded',state
    result=client.get(state['model_url']);result.raise_for_status()
    (reports/'chair.obj').write_bytes(result.content)
    print('PASS real photo -> GPU inference -> status -> downloaded OBJ',len(result.content),flush=True)

if __name__=='__main__':main()
