"""Local-only image reconstruction service; adapters never import GPU code."""
import os, uuid, json, threading
from datetime import datetime
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.responses import FileResponse
from PIL import Image
from adapters import MockGenerator, TripoSRGenerator

ROOT = Path(__file__).resolve().parent
DATA = Path(os.getenv('PHOTOAR_DATA', str(ROOT / 'data')))
REPO = Path(os.getenv('TRIPOSR_REPO', r'E:\PROJECT_my_python\TripoSR'))
PYTHON = os.getenv('TRIPOSR_PYTHON', r'E:\conda-envs\triposr311\python.exe')
MODE = os.getenv('PHOTOAR_MODE', 'real')
app = FastAPI(title='PhotoAR local reconstruction', version='1.0')
pool = ThreadPoolExecutor(max_workers=1)
jobs = {}
lock = threading.Lock()
DATA.mkdir(parents=True, exist_ok=True)
generator = MockGenerator() if MODE == 'mock' else TripoSRGenerator(
    REPO, PYTHON, DATA/'numba-cache', int(os.getenv('PHOTOAR_RESOLUTION','128')))

def write_state(jid, **values):
    with lock:
        jobs[jid].update(values)
        (DATA / jid / 'status.json').write_text(json.dumps(jobs[jid]), encoding='utf-8')

def reconstruct(jid):
    folder = DATA / jid
    try:
        write_state(jid, state='running')
        mesh = generator.generate(folder/'input.png',folder/'output',folder/'inference.log')
        if not mesh.is_file() or mesh.stat().st_size == 0:
            raise RuntimeError('Reconstruction returned no mesh')
        write_state(jid, state='succeeded', model_url=f'/jobs/{jid}/model', error=None)
    except Exception as exc:
        write_state(jid, state='failed', error=str(exc))

def get_job(jid):
    try: uuid.UUID(jid)
    except ValueError: raise HTTPException(404, 'Unknown job')
    with lock:
        value = jobs.get(jid)
        if value: return dict(value)
    saved = DATA / jid / 'status.json'
    if not saved.exists(): raise HTTPException(404, 'Unknown job')
    value = json.loads(saved.read_text(encoding='utf-8'))
    if value['state'] in ('queued','running'):
        value.update(state='failed', error='Service restarted; please upload again')
    return value

@app.get('/health')
def health():
    return {'status':'ok', 'mode':MODE, 'repo_exists':REPO.is_dir()}

@app.post('/jobs', status_code=202)
async def create_job(photo: UploadFile = File(...)):
    raw = await photo.read(12 * 1024 * 1024 + 1)
    if len(raw) > 12 * 1024 * 1024: raise HTTPException(413, 'Image exceeds 12 MB')
    import io
    try:
        image = Image.open(io.BytesIO(raw))
        if image.width * image.height > 24000000: raise ValueError('Image exceeds 24 megapixels')
        from PIL import ImageOps
        image = ImageOps.exif_transpose(image).convert('RGBA')
        image.load()
    except Exception: raise HTTPException(400, 'Invalid or oversized image')
    with lock:
        if sum(j['state'] in ('queued','running') for j in jobs.values()) >= 4:
            raise HTTPException(429, 'Queue full')
        jid = str(uuid.uuid4())
        folder = DATA / jid
        folder.mkdir()
        image.save(folder / 'input.png')
        jobs[jid] = {'id':jid, 'state':'queued', 'model_url':None, 'error':None}
    pool.submit(reconstruct, jid)
    return get_job(jid)

@app.get('/jobs/{jid}')
def status(jid: str): return get_job(jid)

@app.get('/jobs/{jid}/model')
def model(jid: str):
    if get_job(jid)['state'] != 'succeeded': raise HTTPException(409, 'Model not ready')
    return FileResponse(DATA / jid / 'output' / '0' / 'mesh.obj', media_type='text/plain', filename='mesh.obj')

@app.get('/models')
def list_models():
    """Persisted successful results; independent of worker state or service restarts."""
    results=[]
    for folder in DATA.iterdir():
        if not folder.is_dir(): continue
        try:
            uuid.UUID(folder.name)
            state=get_job(folder.name)
            mesh=folder/'output'/'0'/'mesh.obj'
            if state['state']!='succeeded' or not mesh.is_file(): continue
            created=mesh.stat().st_mtime
            results.append({'id':folder.name,'name':'模型 · '+datetime.fromtimestamp(created).strftime('%m-%d %H:%M'),
                'created':created,'bytes':mesh.stat().st_size,'model_url':f'/jobs/{folder.name}/model',
                'thumbnail_url':f'/jobs/{folder.name}/thumbnail'})
        except (ValueError,KeyError,OSError,json.JSONDecodeError,HTTPException): continue
    return {'models':sorted(results,key=lambda m:m['created'],reverse=True)}

@app.get('/jobs/{jid}/thumbnail')
def thumbnail(jid: str):
    if get_job(jid)['state']!='succeeded': raise HTTPException(409,'Model not ready')
    image_path=DATA/jid/'input.png'
    if not image_path.is_file(): raise HTTPException(404,'Thumbnail unavailable')
    import io
    from fastapi.responses import Response
    with Image.open(image_path) as source:
        image=source.convert('RGB');image.thumbnail((240,240))
        buffer=io.BytesIO();image.save(buffer,format='JPEG',quality=82)
    return Response(buffer.getvalue(),media_type='image/jpeg')

if __name__ == '__main__':
    import uvicorn
    uvicorn.run(app, host=os.getenv('PHOTOAR_HOST','127.0.0.1'), port=int(os.getenv('PHOTOAR_PORT','8090')))
