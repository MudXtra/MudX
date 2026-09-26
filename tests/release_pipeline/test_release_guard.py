import json, re, subprocess, tempfile, unittest, zipfile
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
 def test_manifest_rejects_escape_and_requires_release_set(self):
  with tempfile.TemporaryDirectory() as d:
   root=Path(d,'release-artifacts'); root.mkdir(); outside=Path(d,'outside.nupkg'); outside.write_bytes(b'x')
   manifest=Path(root,'manifest.json'); manifest.write_text(json.dumps({'schema':1,'source_sha':'a'*40,'version':'9.10.1','image_digest':'sha256:'+'b'*64,'producer':'123/1','files':[{'path':'../outside.nupkg','sha256':'2d711642b726b04401627ca9fbac32f5c8530fb1903cc4db02258717921a4881'}]}))
   self.run_guard('manifest-verify','--manifest',str(manifest),'--root',str(root),'--sha','a'*40,'--version','9.10.1','--producer','123/1',ok=False)
 def test_repository_signature_is_only_allowed_package_difference(self):
  with tempfile.TemporaryDirectory() as d:
   candidate,published=Path(d,'candidate.nupkg'),Path(d,'published.nupkg')
   for path,signature,payload in ((candidate,b'author',b'payload'),(published,b'repository',b'payload')):
    with zipfile.ZipFile(path,'w') as z: z.writestr('lib/net8.0/a.dll',payload); z.writestr('.signature.p7s',signature)
   self.run_guard('package-equivalent','--candidate',str(candidate),'--published',str(published))
   with zipfile.ZipFile(published,'w') as z: z.writestr('lib/net8.0/a.dll',b'tampered'); z.writestr('.signature.p7s',b'repository')
   self.run_guard('package-equivalent','--candidate',str(candidate),'--published',str(published),ok=False)
 def test_resume_plans_partial_mismatch_deploy_retry_and_complete(self):
  partial=self.run_guard('resume-plan','--image','match','--main','equivalent','--symbols','missing','--deploy','missing','--release','missing')
  self.assertEqual({'image':'complete','main':'complete','symbols':'publish','deploy':'deploy','release':'create-draft','latest':'update'},json.loads(partial.stdout))
  self.run_guard('resume-plan','--image','match','--main','mismatch','--symbols','missing','--deploy','missing','--release','missing',ok=False)
  checkpoint=self.run_guard('resume-plan','--image','match','--main','equivalent','--symbols','published','--deploy','missing','--release','draft')
  self.assertEqual('complete',json.loads(checkpoint.stdout)['symbols']); self.assertEqual('deploy',json.loads(checkpoint.stdout)['deploy'])
  blocked=self.run_guard('resume-plan','--image','match','--main','equivalent','--symbols','unknown','--deploy','match','--release','missing',ok=False)
  self.assertIn('manual reconciliation required',blocked.stderr)
  complete=self.run_guard('resume-plan','--image','match','--main','equivalent','--symbols','unknown','--deploy','match','--release','exact')
  self.assertTrue(all(v=='complete' for v in json.loads(complete.stdout).values()))
 def test_retained_artifact_missing_expired_and_exact_selection(self):
  with tempfile.TemporaryDirectory() as d:
   data=Path(d,'artifacts.json'); data.write_text(json.dumps({'artifacts':[{'id':7,'name':'candidate','expired':False,'workflow_run':{'id':123}}]}))
   self.assertEqual('7',self.run_guard('artifact-select','--json',str(data),'--name','candidate','--run','123').stdout.strip())
   self.run_guard('artifact-select','--json',str(data),'--name','candidate','--run','124',ok=False)
   data.write_text(json.dumps({'artifacts':[{'id':7,'name':'candidate','expired':True,'workflow_run':{'id':123}}]})); self.run_guard('artifact-select','--json',str(data),'--name','candidate','--run','123',ok=False)
 def test_head_binding_detects_movement_at_any_boundary(self):
  sha='a'*40; self.run_guard('head-binding','--expected',sha,'--observed',sha,'--observed',sha,'--observed',sha)
  self.run_guard('head-binding','--expected',sha,'--observed',sha,'--observed','b'*40,'--observed',sha,ok=False)
 def test_workflow_resume_and_action_pins(self):
  text=(ROOT/'.github/workflows/Release_MudX.yml').read_text()
  self.assertIn('producer_run_id',text); self.assertIn('producer_attempt',text)
  self.assertIn('artifact-select --json',text)
  for use in re.findall(r'uses:\s*([^\s#]+)',text): self.assertRegex(use,r'^[^@]+@[0-9a-f]{40}$')
  publisher=(ROOT/'tools/release_pipeline/publish_release.sh').read_text()
  self.assertIn('--no-symbols',publisher); self.assertIn('package-equivalent',publisher)
  self.assertNotRegex(publisher,r'(^|\s)--skip-duplicate(\s|$)')
if __name__=='__main__': unittest.main()
