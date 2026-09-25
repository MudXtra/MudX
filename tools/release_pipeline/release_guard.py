#!/usr/bin/env python3
import argparse, hashlib, json, re, sys, zipfile
from pathlib import Path
SEMVER=re.compile(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\Z')
SHA=re.compile(r'[0-9a-f]{40}\Z'); DIGEST=re.compile(r'sha256:[0-9a-f]{64}\Z')
def version(v):
 m=SEMVER.fullmatch(v or '')
 if not m: raise ValueError(f'non-canonical stable SemVer: {v!r}')
 return tuple(map(int,m.groups()))
def calculate(a):
 known=[version(a.current),*(version(v) for v in a.known)]; cur=max(known)
 if a.custom:
  target=version(a.custom)
  if target<=cur: raise ValueError('custom version must be greater than every project, published, and reserved version')
 else:
  i={'major':0,'minor':1,'patch':2}[a.bump]; target=list(cur); target[i]+=1
  for n in range(i+1,3): target[n]=0
 print('.'.join(map(str,target)))
def actor(a):
 allowed={x.strip() for x in a.allowed.split(',') if x.strip()}
 if not allowed or a.actor not in allowed or a.triggering_actor not in allowed: raise ValueError('dispatch and rerun actors must both be authorized')
def version_files(a):
 before=Path(a.before).read_text(); after=Path(a.after).read_text(); version(a.expected)
 old=re.findall(r'<Version>([^<]+)</Version>',before); new=re.findall(r'<Version>([^<]+)</Version>',after)
 if len(old)!=1 or new!=[a.expected]: raise ValueError('exactly one Version property is required')
 if after!=before.replace(f'<Version>{old[0]}</Version>',f'<Version>{a.expected}</Version>',1): raise ValueError('diff contains changes beyond calculated Version XML value')
def file_digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def safe_file(root,name):
 if Path(name).is_absolute(): raise ValueError('manifest path must be relative to artifact root')
 root=root.resolve(); raw=root/name
 if raw.is_symlink(): raise ValueError('manifest path must not be a symlink')
 path=raw.resolve()
 if path.parent!=root and root not in path.parents: raise ValueError('manifest path escapes artifact root')
 if not path.is_file(): raise ValueError('manifest file missing: '+name)
 return path
def manifest_create(a):
 if not SHA.fullmatch(a.sha) or not DIGEST.fullmatch(a.image_digest): raise ValueError('invalid artifact identity')
 version(a.version); root=Path(a.root).resolve() if a.root else None; files=[]
 for item in a.files:
  path=Path(item).resolve()
  name=str(path.relative_to(root)) if root else str(Path(item))
  if root: path=safe_file(root,name)
  files.append({'path':name,'sha256':file_digest(path)})
 Path(a.output).write_text(json.dumps({'schema':1,'source_sha':a.sha,'version':a.version,'image_digest':a.image_digest,'producer':a.producer,'files':files},sort_keys=True,indent=2)+'\n')
def manifest_verify(a):
 manifest=Path(a.manifest); d=json.loads(manifest.read_text())
 if d.get('schema')!=1: raise ValueError('unsupported manifest schema')
 if (d.get('source_sha'),d.get('version'),d.get('producer'))!=(a.sha,a.version,a.producer): raise ValueError('manifest producer/source/version mismatch')
 version(a.version)
 if not DIGEST.fullmatch(d.get('image_digest','')): raise ValueError('invalid image digest')
 entries=d.get('files')
 if not isinstance(entries,list) or not entries: raise ValueError('manifest files must be a nonempty list')
 root=Path(a.root) if a.root else None; names=[]
 for f in entries:
  if not isinstance(f,dict) or set(f)!= {'path','sha256'} or not isinstance(f.get('path'),str) or not re.fullmatch(r'[0-9a-f]{64}',f.get('sha256','')): raise ValueError('invalid manifest file entry')
  name=f['path']; path=safe_file(root,name) if root else Path(name)
  if name in names: raise ValueError('duplicate manifest path: '+name)
  names.append(name)
  if file_digest(path)!=f['sha256']: raise ValueError('checksum mismatch: '+name)
 if root:
  main=[n for n in names if n.endswith('.nupkg') and not n.endswith('.snupkg')]
  symbols=[n for n in names if n.endswith('.snupkg')]
  images=[n for n in names if n=='image.oci.tar']
  if len(main)!=1 or len(symbols)!=1 or len(images)!=1 or len(names)!=3: raise ValueError('manifest must contain exactly one main package, one symbol package, and image.oci.tar')
def zip_payload(path):
 with zipfile.ZipFile(path) as z:
  names=[n for n in z.namelist() if n.lower()!='.signature.p7s']
  if len(names)!=len(set(names)): raise ValueError('duplicate ZIP entry')
  return {n:hashlib.sha256(z.read(n)).hexdigest() for n in sorted(names)}
def package_equivalent(a):
 if zip_payload(a.candidate)!=zip_payload(a.published): raise ValueError('published package content differs beyond .signature.p7s repository signing')
def head_binding(a):
 if not SHA.fullmatch(a.expected) or not a.observed or any(v!=a.expected for v in a.observed): raise ValueError('version PR head changed during authorization')
def artifact_select(a):
 data=json.loads(Path(a.json).read_text()); matches=[]
 for item in data.get('artifacts',[]):
  if item.get('name')==a.name and item.get('expired') is False and (item.get('workflow_run') or {}).get('id')==a.run: matches.append(item.get('id'))
 if len(matches)!=1 or not isinstance(matches[0],int): raise ValueError('Retained producer artifact is missing, expired, or ambiguous; do not rebuild or allocate a version.')
 print(matches[0])
def resume_plan(a):
 if a.image=='mismatch' or a.main=='mismatch' or a.release=='mismatch': raise ValueError('existing immutable publication does not match retained candidate')
 if a.release=='exact':
  if a.image!='match' or a.main!='equivalent' or a.deploy!='match': raise ValueError('finalized release conflicts with external state')
  print(json.dumps({k:'complete' for k in ('image','main','symbols','deploy','release','latest')},sort_keys=True)); return
 if a.symbols=='unknown': raise ValueError('symbol publication cannot be proven; manual reconciliation required before retry')
 plan={'image':'complete' if a.image=='match' else 'publish','main':'complete' if a.main=='equivalent' else 'publish','symbols':'publish' if a.symbols=='missing' else 'complete','deploy':'complete' if a.deploy=='match' else 'deploy','release':'finalize' if a.release=='draft' else 'create-draft','latest':'update'}
 print(json.dumps(plan,sort_keys=True))
def main():
 p=argparse.ArgumentParser(); s=p.add_subparsers(dest='cmd',required=True)
 x=s.add_parser('calculate'); x.add_argument('--current',required=True); x.add_argument('--known',action='append',default=[]); x.add_argument('--bump',choices=['patch','minor','major'],default='patch'); x.add_argument('--custom'); x.set_defaults(fn=calculate)
 x=s.add_parser('actor'); x.add_argument('--actor',required=True); x.add_argument('--triggering-actor',required=True); x.add_argument('--allowed',required=True); x.set_defaults(fn=actor)
 x=s.add_parser('version-files'); x.add_argument('--before',required=True); x.add_argument('--after',required=True); x.add_argument('--expected',required=True); x.set_defaults(fn=version_files)
 x=s.add_parser('manifest-create'); x.add_argument('--output',required=True); x.add_argument('--root'); x.add_argument('--sha',required=True); x.add_argument('--version',required=True); x.add_argument('--image-digest',required=True); x.add_argument('--producer',required=True); x.add_argument('files',nargs='+'); x.set_defaults(fn=manifest_create)
 x=s.add_parser('manifest-verify'); x.add_argument('--manifest',required=True); x.add_argument('--root'); x.add_argument('--sha',required=True); x.add_argument('--version',required=True); x.add_argument('--producer',required=True); x.set_defaults(fn=manifest_verify)
 x=s.add_parser('package-equivalent'); x.add_argument('--candidate',required=True); x.add_argument('--published',required=True); x.set_defaults(fn=package_equivalent)
 x=s.add_parser('head-binding'); x.add_argument('--expected',required=True); x.add_argument('--observed',action='append',required=True); x.set_defaults(fn=head_binding)
 x=s.add_parser('artifact-select'); x.add_argument('--json',required=True); x.add_argument('--name',required=True); x.add_argument('--run',required=True,type=int); x.set_defaults(fn=artifact_select)
 x=s.add_parser('resume-plan'); x.add_argument('--image',choices=['absent','match','mismatch'],required=True); x.add_argument('--main',choices=['missing','equivalent','mismatch'],required=True); x.add_argument('--symbols',choices=['missing','published','unknown'],required=True); x.add_argument('--deploy',choices=['missing','match'],required=True); x.add_argument('--release',choices=['missing','draft','exact','mismatch'],required=True); x.set_defaults(fn=resume_plan)
 a=p.parse_args()
 try: a.fn(a)
 except (ValueError,OSError,json.JSONDecodeError,zipfile.BadZipFile) as e: print('release guard: '+str(e),file=sys.stderr); return 2
 return 0
if __name__=='__main__': raise SystemExit(main())
