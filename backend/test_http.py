"""Socket-level smoke test: runs its own mock server and shuts it down."""
import os,sys,time,json,tempfile,subprocess,socket,urllib.request
from pathlib import Path
from PIL import Image
import io

def main():
    with socket.socket() as sock:
        sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
    with tempfile.TemporaryDirectory(prefix='photoar-http-') as temp:
        env=dict(os.environ,PHOTOAR_MODE='mock',PHOTOAR_DATA=temp,PHOTOAR_PORT=str(port),PHOTOAR_HOST='127.0.0.1')
        with open(Path(temp)/'server.log','wb') as log:
            process=subprocess.Popen([sys.executable,str(Path(__file__).with_name('service.py'))],env=env,stdout=log,stderr=log,
                                     creationflags=subprocess.CREATE_NO_WINDOW if os.name=='nt' else 0)
            base=f'http://127.0.0.1:{port}'
            try:
                for _ in range(100):
                    try:
                        with urllib.request.urlopen(base+'/health',timeout=1) as response:
                            assert json.load(response)['mode']=='mock';break
                    except OSError:time.sleep(.1)
                else:raise RuntimeError('Server did not become ready')
                image=io.BytesIO();Image.new('RGB',(32,32),'blue').save(image,format='PNG')
                boundary='PhotoARHttpTestBoundary'
                body=(f'--{boundary}\r\nContent-Disposition: form-data; name="photo"; filename="photo.png"\r\nContent-Type: image/png\r\n\r\n'.encode()+image.getvalue()+f'\r\n--{boundary}--\r\n'.encode())
                request=urllib.request.Request(base+'/jobs',data=body,headers={'Content-Type':f'multipart/form-data; boundary={boundary}'})
                with urllib.request.urlopen(request,timeout=5) as response:
                    assert response.status==202;job=json.load(response)
                for _ in range(100):
                    with urllib.request.urlopen(base+'/jobs/'+job['id'],timeout=5) as response:job=json.load(response)
                    if job['state'] in ('succeeded','failed'):break
                    time.sleep(.05)
                assert job['state']=='succeeded',job
                with urllib.request.urlopen(base+job['model_url'],timeout=5) as response:assert b'f 1 2 3' in response.read()
                print('PASS loopback HTTP: health, multipart upload, polling, OBJ download')
            finally:
                process.terminate()
                try:process.wait(timeout=5)
                except subprocess.TimeoutExpired:process.kill();process.wait()

if __name__=='__main__':main()
