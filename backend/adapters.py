"""Replaceable generators. The HTTP layer has no TripoSR dependency."""
import os
import subprocess
from pathlib import Path
from typing import Protocol

class MeshGenerator(Protocol):
    def generate(self, photo: Path, output: Path, log: Path) -> Path: ...

class MockGenerator:
    def generate(self, photo, output, log):
        mesh=output/'0'/'mesh.obj';mesh.parent.mkdir(parents=True,exist_ok=True)
        mesh.write_text('v -1 0 -1 1 0 0\nv 1 0 -1 0 1 0\nv 0 2 0 0 0 1\nf 1 2 3\nf 3 2 1\n')
        return mesh

class TripoSRGenerator:
    def __init__(self, repo: Path, python: str, cache: Path, resolution=128):
        self.repo,self.python,self.cache,self.resolution=repo,python,cache,resolution
    def generate(self, photo, output, log):
        self.cache.mkdir(parents=True,exist_ok=True)
        env=dict(os.environ,HF_HUB_OFFLINE='1',HF_HUB_DISABLE_SYMLINKS_WARNING='1',
                 NUMBA_CACHE_DIR=str(self.cache),PYTHONDONTWRITEBYTECODE='1')
        with log.open('w',encoding='utf-8') as stream:
            subprocess.run([self.python,str(self.repo/'run.py'),str(photo),
                '--output-dir',str(output),'--model-save-format','obj',
                '--mc-resolution',str(self.resolution)],cwd=self.repo,env=env,
                stdout=stream,stderr=subprocess.STDOUT,check=True,timeout=600)
        return output/'0'/'mesh.obj'
