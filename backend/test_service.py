import os, tempfile, time, unittest, io
from unittest.mock import patch
os.environ['PHOTOAR_MODE']='mock'
os.environ['PHOTOAR_DATA']=tempfile.mkdtemp(prefix='photoar-test-')
from fastapi.testclient import TestClient
from PIL import Image
from service import app
import service

class ServiceTests(unittest.TestCase):
    def setUp(self): self.client=TestClient(app)
    def test_health(self): self.assertEqual(self.client.get('/health').json()['mode'],'mock')
    def test_invalid(self): self.assertEqual(self.client.post('/jobs',files={'photo':('bad.png',b'bad')}).status_code,400)
    def test_missing(self): self.assertEqual(self.client.get('/jobs/../../no').status_code,404)
    def test_too_large(self):
        self.assertEqual(self.client.post('/jobs',files={'photo':('large.png',b'x'*(12*1024*1024+1))}).status_code,413)
    def test_generator_failure(self):
        buf=io.BytesIO();Image.new('RGB',(32,32),'red').save(buf,format='PNG')
        with patch.object(service.generator,'generate',side_effect=RuntimeError('Simulated GPU failure')):
            r=self.client.post('/jobs',files={'photo':('photo.png',buf.getvalue())});jid=r.json()['id']
            for _ in range(100):
                state=self.client.get('/jobs/'+jid).json()
                if state['state']=='failed':break
                time.sleep(.02)
            self.assertEqual(state['state'],'failed');self.assertIn('GPU failure',state['error'])
            self.assertEqual(self.client.get('/jobs/'+jid+'/model').status_code,409)
    def test_pipeline(self):
        buf=io.BytesIO(); Image.new('RGB',(32,32),'red').save(buf,format='PNG')
        r=self.client.post('/jobs',files={'photo':('photo.png',buf.getvalue())})
        self.assertEqual(r.status_code,202); jid=r.json()['id']
        for _ in range(100):
            state=self.client.get('/jobs/'+jid).json()
            if state['state'] in ('failed','succeeded'): break
            time.sleep(.02)
        self.assertEqual(state['state'],'succeeded',state)
        mesh=self.client.get(state['model_url'])
        self.assertEqual(mesh.status_code,200); self.assertIn('f 1 2 3',mesh.text)

    def test_saved_library_survives_restart_and_excludes_broken_results(self):
        import uuid,json
        jid=str(uuid.uuid4());folder=service.DATA/jid;folder.mkdir()
        (folder/'output'/'0').mkdir(parents=True)
        (folder/'output'/'0'/'mesh.obj').write_text('v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3')
        (folder/'status.json').write_text(json.dumps({'id':jid,'state':'succeeded'}))
        Image.new('RGB',(1200,800),'blue').save(folder/'input.png')
        missing=str(uuid.uuid4());other=service.DATA/missing;other.mkdir()
        (other/'status.json').write_text(json.dumps({'id':missing,'state':'succeeded'}))
        items=self.client.get('/models').json()['models']
        self.assertIn(jid,[item['id'] for item in items]);self.assertNotIn(missing,[item['id'] for item in items])
        item=next(item for item in items if item['id']==jid)
        self.assertEqual(self.client.get(item['model_url']).status_code,200)
        thumb=self.client.get(item['thumbnail_url']);self.assertEqual(thumb.status_code,200)
        with Image.open(io.BytesIO(thumb.content)) as image:self.assertLessEqual(max(image.size),240)
        self.assertEqual(self.client.get('/jobs/not-a-job/thumbnail').status_code,404)

if __name__=='__main__': unittest.main()
