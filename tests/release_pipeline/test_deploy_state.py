import fcntl, os, subprocess, tempfile, unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; DEPLOY=ROOT/'deploy/mudx-deploy'
class Tests(unittest.TestCase):
 def invoke(self,state,*args,fail=None,ok=True):
  env=os.environ|{'MUDX_TEST_MODE':'1','MUDX_STATE_DIR':str(state),'MUDX_DOCKER_BIN':str(ROOT/'tests/release_pipeline/fake-docker')}
  if fail: env['MUDX_FAIL_AFTER']=fail
  if (Path(state)/'allowlist.json').exists(): env['MUDX_RUNTIME_ALLOWLIST']=str(Path(state)/'allowlist.json')
  r=subprocess.run([str(DEPLOY),*args],env=env,text=True,capture_output=True); self.assertEqual(ok,r.returncode==0,r.stderr); return r
 def test_grammar_stale_duplicate(self):
  with tempfile.TemporaryDirectory() as d:
   self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64); self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64)
   self.invoke(d,'deploy','0','9.9.9','c'*40,'sha256:'+'b'*64,ok=False); self.invoke(d,'deploy','2;id','9.10.2','c'*40,'sha256:'+'b'*64,ok=False)
 def test_concurrent_attempt_is_rejected(self):
  with tempfile.TemporaryDirectory() as d, open(Path(d,'lock'),'a+') as lock:
   fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
   self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64,ok=False)
 def test_dangerous_runtime_configuration_is_rejected(self):
  with tempfile.TemporaryDirectory() as d:
   Path(d,'allowlist.json').write_text('{"ports":["4560:8080"],"restart_policy":"no","privileged":true}')
   self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64,ok=False)
 def test_interruptions(self):
  for i,s in enumerate(['prepared','prior-policy-intent','prior-policy-disabled','prior-stop-intent','prior-stopped','prior-rename-intent','prior-renamed','candidate-created','candidate-start-intent','candidate-started','verified','candidate-promote-intent','candidate-promoted','committed'],1):
   with self.subTest(stage=s),tempfile.TemporaryDirectory() as d:
    self.invoke(d,'deploy',str(i),f'9.10.{i}','c'*40,'sha256:'+format(i,'064x'),fail=s,ok=False); self.invoke(d,'reconcile')
    self.assertEqual(1,len(Path(d,'restart-eligible').read_text().splitlines()))
 def test_failures_restore_prior(self):
  for marker in ('inject-pull-failure','inject-health-failure'):
   with tempfile.TemporaryDirectory() as d:
    Path(d,marker).touch(); self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64,ok=False); self.assertEqual('prior',Path(d,'active').read_text().strip())
if __name__=='__main__': unittest.main()
