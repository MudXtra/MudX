import hashlib, json, os, subprocess, tempfile, textwrap, unittest, zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
class PublishResumeTests(unittest.TestCase):
 def package(self,path,payload):
  with zipfile.ZipFile(path,'w') as z: z.writestr('content.bin',payload)
 def run_case(self,symbol_published,deployed,*,finalized=False,prerelease=False,inspect_digest=None,ok=True):
  with tempfile.TemporaryDirectory() as d:
   work=Path(d); (work/'release-artifacts/packages').mkdir(parents=True); (work/'tools/release_pipeline').mkdir(parents=True); (work/'fakebin').mkdir()
   main=work/'release-artifacts/packages/MudX.MudBlazor.Extension.9.10.1.nupkg'; symbol=work/'release-artifacts/packages/MudX.MudBlazor.Extension.9.10.1.snupkg'
   self.package(main,b'main'); self.package(symbol,b'symbol'); digest='sha256:'+'b'*64; sha='a'*40
   (work/'release-artifacts/manifest.json').write_text(json.dumps({'image_digest':digest})); (work/'release-artifacts/image.oci.tar').write_bytes(b'oci')
   (work/'tools/release_pipeline/release_guard.py').write_bytes((ROOT/'tools/release_pipeline/release_guard.py').read_bytes())
   marker='<!-- mudx-symbol-sha256:'+hashlib.sha256(symbol.read_bytes()).hexdigest()+' -->'
   state={'draft':not finalized,'prerelease':prerelease,'body':marker if symbol_published else '', 'deployed':deployed, 'dotnet':[], 'ssh':0, 'copies':[]}
   state_path=work/'state.json'; state_path.write_text(json.dumps(state))
   fake=work/'fakebin/fake'; fake.write_text(textwrap.dedent(r'''#!/usr/bin/env python3
import json,os,pathlib,shutil,sys
cmd=pathlib.Path(sys.argv[0]).name; a=sys.argv[1:]; state_path=pathlib.Path(os.environ['FAKE_STATE']); s=json.loads(state_path.read_text())
def save(): state_path.write_text(json.dumps(s))
if cmd=='sudo': raise SystemExit(0)
if cmd=='skopeo':
 if a[0]=='inspect': print(os.environ['INSPECT_DIGEST'])
 elif a[0]=='copy': s['copies'].append(a[1:]); save()
 raise SystemExit(0)
if cmd=='curl':
 out=a[a.index('-o')+1] if '-o' in a else None; url=a[-1]
 if 'flatcontainer' in url:
  shutil.copy2(os.environ['MAIN'],out); print('200',end='')
 else: print(os.environ['SHA'] if s['deployed'] else 'prior',end='')
 raise SystemExit(0)
if cmd=='dotnet':
 s['dotnet'].append(a[2] if len(a)>2 else ''); save(); raise SystemExit(0)
if cmd=='ssh': s['ssh']+=1; s['deployed']=True; save(); raise SystemExit(0)
if cmd=='gh':
 if a[:2]==['release','view']:
  fields=a[a.index('--json')+1] if '--json' in a else ''
  if fields=='assets': print('\n'.join(sorted((pathlib.Path(os.environ['MAIN']).name,pathlib.Path(os.environ['SYMBOL']).name))))
  elif fields=='body': print(s['body'])
  elif fields=='isDraft,isPrerelease': print(json.dumps({'isDraft':s['draft'],'isPrerelease':s['prerelease']}))
  else: print(json.dumps({'databaseId':7,'isDraft':s['draft'],'isPrerelease':s['prerelease'],'body':s['body']}))
 elif a[:2]==['release','download']:
  dest=pathlib.Path(a[a.index('-D')+1]); dest.mkdir(exist_ok=True); shutil.copy2(os.environ['MAIN'],dest); shutil.copy2(os.environ['SYMBOL'],dest)
 elif a[:2]==['release','list']:
  print(os.environ['VERSION'] if not s['draft'] else '9.10.0')
 elif a[0]=='api':
  if '--method' in a and 'PATCH' in a:
   if '-F' in a: s['draft']=False
   if '-f' in a:
    value=a[a.index('-f')+1]; s['body']=value.removeprefix('body=')
   save()
  elif '--jq' in a: print(os.environ['SHA'])
 raise SystemExit(0)
raise SystemExit('unexpected fake command '+cmd+' '+repr(a))
''')); fake.chmod(0o755)
   for name in ('sudo','skopeo','curl','dotnet','ssh','gh'): (work/'fakebin'/name).symlink_to(fake)
   env=os.environ|{'PATH':str(work/'fakebin')+':'+os.environ['PATH'],'FAKE_STATE':str(state_path),'MAIN':str(main),'SYMBOL':str(symbol),'DIGEST':digest,'INSPECT_DIGEST':inspect_digest or digest,'SHA':sha,'VERSION':'9.10.1','PRODUCER':'123/1','IMAGE':'ghcr.io/mudxtra/mudx/mudxdocwebsite','PACKAGE_ID':'mudx.mudblazor.extension','PUBLIC_URL':'https://example.invalid','NUGET_API_KEY':'secret','GHCR_TOKEN':'token','GITHUB_ACTOR':'actor','GITHUB_REPOSITORY':'mudxtra/MudX','GITHUB_RUN_ID':'456','GITHUB_RUN_ATTEMPT':'1','HOST':'host','USER':'user','PORT':'22','KNOWN_HOSTS':'host key','DEPLOY_KEY':'key'}
   r=subprocess.run(['bash','-x',str(ROOT/'tools/release_pipeline/publish_release.sh')],cwd=work,env=env,text=True,capture_output=True)
   self.assertEqual(ok,r.returncode==0,r.stderr); return json.loads(state_path.read_text()),str(symbol)
 def test_symbol_checkpoint_allows_deployment_retry_without_republish(self):
  state,_=self.run_case(True,False); self.assertEqual([],state['dotnet']); self.assertEqual(1,state['ssh']); self.assertFalse(state['draft']); self.assertTrue(any('@sha256:' in part for copy in state['copies'] for part in copy))
 def test_main_published_symbol_missing_publishes_only_symbols(self):
  state,symbol=self.run_case(False,True); self.assertEqual(1,len(state['dotnet'])); self.assertEqual(Path(symbol).name,Path(state['dotnet'][0]).name); self.assertEqual(0,state['ssh']); self.assertFalse(state['draft'])
 def test_completed_resume_is_idempotent(self):
  state,_=self.run_case(True,True,finalized=True); self.assertEqual([],state['dotnet']); self.assertEqual(0,state['ssh'])
 def test_registry_and_prerelease_conflicts_fail_before_mutation(self):
  state,_=self.run_case(True,True,inspect_digest='sha256:'+'c'*64,ok=False); self.assertEqual([],state['dotnet']); self.assertEqual(0,state['ssh'])
  state,_=self.run_case(True,True,prerelease=True,ok=False); self.assertEqual([],state['dotnet']); self.assertEqual(0,state['ssh'])
if __name__=='__main__': unittest.main()
