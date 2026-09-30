import hashlib, json, os, subprocess, tempfile, textwrap, unittest, zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
class PublishResumeTests(unittest.TestCase):
 def package(self,path,payload):
  with zipfile.ZipFile(path,'w') as z: z.writestr('content.bin',payload)
 def run_case(self,*,release='draft',symbol_published=True,main_published=True,prerelease=False,asset_mismatch=False,ok=True):
  with tempfile.TemporaryDirectory() as d:
   work=Path(d); root=work/'release-artifacts'; packages=root/'packages'; packages.mkdir(parents=True); (work/'tools/release_pipeline').mkdir(parents=True); (work/'fakebin').mkdir()
   main=packages/'MudX.MudBlazor.Extension.9.10.1.nupkg'; symbol=packages/'MudX.MudBlazor.Extension.9.10.1.snupkg'; archive=root/'MudX-9.10.1-linux-amd64.tar.gz'; checksums=root/'SHA256SUMS'; manifest=root/'manifest.json'
   self.package(main,b'main'); self.package(symbol,b'symbol'); archive.write_bytes(b'docker-archive')
   checksums.write_text(''.join(f'{hashlib.sha256(p.read_bytes()).hexdigest()}  {p.name}\n' for p in (main,symbol,archive)))
   manifest.write_text(json.dumps({'schema':2,'source_sha':'a'*40,'version':'9.10.1','producer':'123/1','files':[]}))
   (work/'tools/release_pipeline/release_guard.py').write_bytes((ROOT/'tools/release_pipeline/release_guard.py').read_bytes())
   marker='<!-- mudx-symbol-sha256:'+hashlib.sha256(symbol.read_bytes()).hexdigest()+' -->'; expected=[p.name for p in (main,symbol,archive,checksums,manifest)]
   state={'release':release,'prerelease':prerelease,'body':marker if symbol_published else '','published_main':main_published,'dotnet':[],'assets':expected,'tag':release!='missing','asset_mismatch':asset_mismatch}
   state_path=work/'state.json'; state_path.write_text(json.dumps(state))
   fake=work/'fakebin/fake'; fake.write_text(textwrap.dedent(r'''#!/usr/bin/env python3
import json,os,pathlib,shutil,sys
cmd=pathlib.Path(sys.argv[0]).name; a=sys.argv[1:]; state_path=pathlib.Path(os.environ['FAKE_STATE']); s=json.loads(state_path.read_text())
def save(): state_path.write_text(json.dumps(s))
if cmd=='curl':
 out=a[a.index('-o')+1];
 if s['published_main']:
  shutil.copy2(os.environ['MAIN'],out); print('200',end='')
 else: print('404',end='')
 raise SystemExit(0)
if cmd=='dotnet':
 s['dotnet'].append(a); path=next((x for x in a if x.endswith('.nupkg')), '')
 if path and not path.endswith('.snupkg'): s['published_main']=True
 save(); raise SystemExit(0)
if cmd=='gh':
 if a[:2]==['release','view']:
  if s['release']=='missing': raise SystemExit(1)
  fields=a[a.index('--json')+1] if '--json' in a else ''
  if fields=='assets': print('\n'.join(sorted(s['assets'])))
  elif fields=='body': print(s['body'])
  elif fields=='isDraft,isPrerelease': print(json.dumps({'isDraft':s['release']=='draft','isPrerelease':s['prerelease']}))
  else: print(json.dumps({'databaseId':7,'isDraft':s['release']=='draft','isPrerelease':s['prerelease'],'body':s['body']}))
 elif a[:2]==['release','download']:
  dest=pathlib.Path(a[a.index('-D')+1]); dest.mkdir(exist_ok=True)
  for key in ('MAIN','SYMBOL','ARCHIVE','CHECKSUMS','MANIFEST'): shutil.copy2(os.environ[key],dest)
  if s['asset_mismatch']: (dest/pathlib.Path(os.environ['ARCHIVE']).name).write_bytes(b'tampered')
 elif a[:2]==['release','create']:
  s['release']='draft'; s['tag']=True; s['assets']=[pathlib.Path(x).name for x in a if pathlib.Path(x).exists()]; save()
 elif a[:2]==['release','list']:
  print(os.environ['VERSION'] if s['release']=='exact' else '9.10.0')
 elif a[0]=='api':
  endpoint=next((x for x in a if x.startswith('repos/')), '')
  if '/commits/v' in endpoint:
   if '--silent' in a and not s['tag']: raise SystemExit(1)
   if '--jq' in a: print(os.environ['SHA'])
  elif '--method' in a and 'PATCH' in a:
   if '-F' in a: s['release']='exact'
   if '-f' in a: s['body']=a[a.index('-f')+1].removeprefix('body=')
   save()
 raise SystemExit(0)
raise SystemExit('unexpected fake command '+cmd+' '+repr(a))
''')); fake.chmod(0o755)
   for name in ('curl','dotnet','gh'): (work/'fakebin'/name).symlink_to(fake)
   env=os.environ|{'PATH':str(work/'fakebin')+':'+os.environ['PATH'],'FAKE_STATE':str(state_path),'MAIN':str(main),'SYMBOL':str(symbol),'ARCHIVE':str(archive),'CHECKSUMS':str(checksums),'MANIFEST':str(manifest),'SHA':'a'*40,'VERSION':'9.10.1','PRODUCER':'123/1','PACKAGE_ID':'mudx.mudblazor.extension','NUGET_API_KEY':'secret','GITHUB_REPOSITORY':'MudXtra/MudX'}
   r=subprocess.run(['bash',str(ROOT/'tools/release_pipeline/publish_release.sh')],cwd=work,env=env,text=True,capture_output=True)
   self.assertEqual(ok,r.returncode==0,r.stderr); return json.loads(state_path.read_text())
 def test_symbol_checkpoint_finalizes_without_republish(self):
  state=self.run_case(); self.assertEqual([],state['dotnet']); self.assertEqual('exact',state['release'])
 def test_missing_symbol_pushes_only_symbol_with_explicit_key(self):
  state=self.run_case(symbol_published=False); self.assertEqual(1,len(state['dotnet'])); args=state['dotnet'][0]
  self.assertTrue(any(x.endswith('.snupkg') for x in args)); self.assertEqual('secret',args[args.index('--api-key')+1]); self.assertNotIn('--skip-duplicate',args); self.assertEqual('exact',state['release'])
 def test_completed_resume_is_idempotent(self):
  state=self.run_case(release='exact'); self.assertEqual([],state['dotnet']); self.assertEqual('exact',state['release'])
 def test_missing_main_uses_retained_assets_and_creates_release(self):
  state=self.run_case(release='missing',symbol_published=False,main_published=False); self.assertEqual(2,len(state['dotnet']))
  for args in state['dotnet']: self.assertEqual('secret',args[args.index('--api-key')+1]); self.assertNotIn('--skip-duplicate',args)
  self.assertIn('--no-symbols',state['dotnet'][0]); self.assertEqual('exact',state['release']); self.assertEqual(5,len(state['assets']))
 def test_release_asset_and_prerelease_conflicts_fail_before_mutation(self):
  state=self.run_case(asset_mismatch=True,ok=False); self.assertEqual([],state['dotnet'])
  state=self.run_case(prerelease=True,ok=False); self.assertEqual([],state['dotnet'])
if __name__=='__main__': unittest.main()
