import json, re, subprocess, tempfile, unittest, zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]; GUARD=ROOT/'tools/release_pipeline/release_guard.py'
class Tests(unittest.TestCase):
 def run_guard(self,*args,ok=True):
  r=subprocess.run(['python3',str(GUARD),*args],text=True,capture_output=True)
  self.assertEqual(ok,r.returncode==0,r.stderr); return r
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
 def test_ci_run_requires_exact_successful_identity(self):
  sha='a'*40; workflow='.github/workflows/Build_And_Test.yml'
  exact={'id':17,'run_attempt':2,'head_sha':sha,'repository':{'full_name':'MudXtra/MudX'},'path':workflow,'event':'pull_request','status':'completed','conclusion':'success'}
  def check(runs,ok):
   with tempfile.TemporaryDirectory() as d:
    p=Path(d,'runs.json'); p.write_text(json.dumps({'workflow_runs':runs}))
    return self.run_guard('ci-run','--json',str(p),'--sha',sha,'--repository','MudXtra/MudX','--workflow',workflow,'--event','pull_request',ok=ok)
  self.assertEqual('17',check([exact],True).stdout.strip()); check([],False)
  newer_failure=exact|{'id':18,'run_attempt':3,'conclusion':'failure'}
  check([exact,newer_failure],False)
  for field,value in (('head_sha','b'*40),('repository',{'full_name':'mallory/MudX'}),('path','.github/workflows/Other.yml'),('event','push'),('status','in_progress'),('conclusion','failure'),('conclusion','cancelled'),('conclusion','skipped')):
   with self.subTest(field=field,value=value): check([exact|{field:value}],False)
 def test_manifest_tampering(self):
  with tempfile.TemporaryDirectory() as d:
   a,m=Path(d,'package.nupkg'),Path(d,'manifest.json'); a.write_bytes(b'candidate')
   self.run_guard('manifest-create','--output',str(m),'--sha','a'*40,'--version','9.10.1','--producer','123/1',str(a))
   self.run_guard('manifest-verify','--manifest',str(m),'--sha','a'*40,'--version','9.10.1','--producer','123/1'); a.write_bytes(b'tampered')
   self.run_guard('manifest-verify','--manifest',str(m),'--sha','a'*40,'--version','9.10.1','--producer','123/1',ok=False)
 def test_manifest_requires_release_only_asset_set(self):
  with tempfile.TemporaryDirectory() as d:
   root=Path(d,'release-artifacts'); packages=root/'packages'; packages.mkdir(parents=True)
   files=[packages/'MudX.MudBlazor.Extension.9.10.1.nupkg',packages/'MudX.MudBlazor.Extension.9.10.1.snupkg',root/'MudX-9.10.1-linux-amd64.tar.gz',root/'SHA256SUMS']
   for i,path in enumerate(files): path.write_bytes(f'asset-{i}'.encode())
   m=root/'manifest.json'; base=('manifest-create','--output',str(m),'--root',str(root),'--sha','a'*40,'--version','9.10.1','--producer','123/1')
   self.run_guard(*base,*(str(path) for path in files))
   verify=('manifest-verify','--manifest',str(m),'--root',str(root),'--sha','a'*40,'--version','9.10.1','--producer','123/1')
   self.run_guard(*verify); data=json.loads(m.read_text()); data['files']=data['files'][:-1]; m.write_text(json.dumps(data)); self.run_guard(*verify,ok=False)
 def test_manifest_rejects_escape(self):
  with tempfile.TemporaryDirectory() as d:
   root=Path(d,'release-artifacts'); root.mkdir(); Path(d,'outside.nupkg').write_bytes(b'x'); m=root/'manifest.json'
   m.write_text(json.dumps({'schema':2,'source_sha':'a'*40,'version':'9.10.1','producer':'123/1','files':[{'path':'../outside.nupkg','sha256':'2d711642b726b04401627ca9fbac32f5c8530fb1903cc4db02258717921a4881'}]}))
   self.run_guard('manifest-verify','--manifest',str(m),'--root',str(root),'--sha','a'*40,'--version','9.10.1','--producer','123/1',ok=False)
 def test_repository_signature_is_only_allowed_package_difference(self):
  with tempfile.TemporaryDirectory() as d:
   candidate,published=Path(d,'candidate.nupkg'),Path(d,'published.nupkg')
   for path,signature,payload in ((candidate,b'author',b'payload'),(published,b'repository',b'payload')):
    with zipfile.ZipFile(path,'w') as z: z.writestr('lib/net8.0/a.dll',payload); z.writestr('.signature.p7s',signature)
   self.run_guard('package-equivalent','--candidate',str(candidate),'--published',str(published))
   with zipfile.ZipFile(published,'w') as z: z.writestr('lib/net8.0/a.dll',b'tampered'); z.writestr('.signature.p7s',b'repository')
   self.run_guard('package-equivalent','--candidate',str(candidate),'--published',str(published),ok=False)
 def test_resume_plans_release_only_partial_and_complete_states(self):
  partial=self.run_guard('resume-plan','--main','equivalent','--symbols','missing','--release','missing')
  self.assertEqual({'main':'complete','symbols':'publish','release':'create-draft'},json.loads(partial.stdout))
  self.run_guard('resume-plan','--main','mismatch','--symbols','missing','--release','missing',ok=False)
  self.run_guard('resume-plan','--main','equivalent','--symbols','unknown','--release','draft',ok=False)
  complete=self.run_guard('resume-plan','--main','equivalent','--symbols','published','--release','exact')
  self.assertTrue(all(v=='complete' for v in json.loads(complete.stdout).values()))
 def test_retained_artifact_missing_expired_and_exact_selection(self):
  with tempfile.TemporaryDirectory() as d:
   data=Path(d,'artifacts.json'); data.write_text(json.dumps({'artifacts':[{'id':7,'name':'candidate','expired':False,'workflow_run':{'id':123}}]}))
   self.assertEqual('7',self.run_guard('artifact-select','--json',str(data),'--name','candidate','--run','123').stdout.strip())
   self.run_guard('artifact-select','--json',str(data),'--name','candidate','--run','124',ok=False)
   payload=json.loads(data.read_text()); payload['artifacts'][0]['expired']=True; data.write_text(json.dumps(payload)); self.run_guard('artifact-select','--json',str(data),'--name','candidate','--run','123',ok=False)
 def test_head_binding_detects_movement_at_any_boundary(self):
  sha='a'*40; self.run_guard('head-binding','--expected',sha,'--observed',sha,'--observed',sha,'--observed',sha)
  self.run_guard('head-binding','--expected',sha,'--observed',sha,'--observed','b'*40,'--observed',sha,ok=False)
 def test_release_workflow_is_fork_owned_ci_gated_and_deployment_free(self):
  text=(ROOT/'.github/workflows/Release_MudX.yml').read_text()
  for expected in ('MUDX_RELEASE_FORK','ci-run','--event pull_request','--event push','docker save','MudX-$VERSION-linux-amd64.tar.gz'): self.assertIn(expected,text)
  self.assertNotIn('environment:',text); self.assertNotRegex(text,r'(?i)ghcr|ssh|docker push'); self.assertNotIn('dotnet test',text)
  for use in re.findall(r'uses:\s*([^\s#]+)',text): self.assertRegex(use,r'^[^@]+@[0-9a-f]{40}$')
  build=(ROOT/'.github/workflows/Build_And_Test.yml').read_text(); self.assertIn("python3 -m unittest discover -s tests/release_pipeline",build)
  self.assertTrue((ROOT/'.github/workflows/auto-assign.yml').is_file())
  for obsolete in ('Update_MudX_Version.yml','deploy-mudx-nuget.yml','Build_And_Deploy.yml','Deploy.yml'): self.assertFalse((ROOT/'.github/workflows'/obsolete).exists())
  publisher=(ROOT/'tools/release_pipeline/publish_release.sh').read_text(); self.assertIn('--api-key "$NUGET_API_KEY"',publisher)
  self.assertNotRegex(publisher,r'(?i)ghcr|skopeo|ssh|public_url|docker'); self.assertNotRegex(publisher,r'(^|\s)--skip-duplicate(\s|$)')
if __name__=='__main__': unittest.main()
