import subprocess, tempfile, unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; GUARD=ROOT/'tools/release_pipeline/release_guard.py'
class Tests(unittest.TestCase):
 def run_guard(self,*args,ok=True):
  r=subprocess.run(['python3',str(GUARD),*args],text=True,capture_output=True); self.assertEqual(ok,r.returncode==0,r.stderr); return r
 def test_versions_and_actor(self):
  self.assertEqual('9.10.1',self.run_guard('calculate','--current','9.10.0','--bump','patch').stdout.strip())
  self.assertEqual('9.12.0',self.run_guard('calculate','--current','9.10.0','--known','9.11.7','--bump','minor').stdout.strip())
  self.run_guard('calculate','--current','9.10.0','--custom','09.11.0',ok=False)
  self.run_guard('actor','--actor','versile2','--triggering-actor','versile2','--allowed','versile2')
  self.run_guard('actor','--actor','versile2','--triggering-actor','mallory','--allowed','versile2',ok=False)
 def test_version_diff(self):
  with tempfile.TemporaryDirectory() as d:
   b,a=Path(d,'before'),Path(d,'after'); b.write_text('<Version>9.10.0</Version>\n'); a.write_text('<Version>9.10.1</Version>\n')
   self.run_guard('version-files','--before',str(b),'--after',str(a),'--expected','9.10.1')
   a.write_text('<Version>9.10.1</Version>\n<Unsafe>true</Unsafe>\n'); self.run_guard('version-files','--before',str(b),'--after',str(a),'--expected','9.10.1',ok=False)
 def test_manifest_tampering(self):
  with tempfile.TemporaryDirectory() as d:
   a,m=Path(d,'package.nupkg'),Path(d,'manifest.json'); a.write_bytes(b'candidate')
   self.run_guard('manifest-create','--output',str(m),'--sha','a'*40,'--version','9.10.1','--image-digest','sha256:'+'b'*64,'--producer','123/1',str(a))
   self.run_guard('manifest-verify','--manifest',str(m),'--sha','a'*40,'--version','9.10.1','--producer','123/1'); a.write_bytes(b'tampered')
   self.run_guard('manifest-verify','--manifest',str(m),'--sha','a'*40,'--version','9.10.1','--producer','123/1',ok=False)
if __name__=='__main__': unittest.main()
