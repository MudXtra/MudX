#!/usr/bin/env python3
import argparse, hashlib, json, re, sys, zipfile
from pathlib import Path

SEMVER=re.compile(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\Z')
SHA=re.compile(r'[0-9a-f]{40}\Z')
SYMBOL_MARKER=re.compile(r'<!-- mudx-symbol-sha256:[0-9a-f]{64} -->\Z')
def version(v):
 m=SEMVER.fullmatch(v or '')
 if not m: raise ValueError(f'non-canonical stable SemVer: {v!r}')
 return tuple(map(int,m.groups()))
def calculate(a):
 current=version(a.current); known=[version(v) for v in a.known]
 if a.custom:
  target=version(a.custom)
  if target<current: raise ValueError('custom version must equal or exceed the project version')
  if known and target<=max(known): raise ValueError('custom version must exceed every published and reserved version')
 else:
  cur=max([current,*known]); i={'major':0,'minor':1,'patch':2}[a.bump]; target=list(cur); target[i]+=1
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
def ci_run(a):
 if not SHA.fullmatch(a.sha): raise ValueError('invalid CI source SHA')
 if a.event=='pull_request' and (a.pr is None or a.pr<1): raise ValueError('pull request CI requires a valid expected PR number')
 data=json.loads(Path(a.json).read_text()); runs=data.get('workflow_runs')
 if not isinstance(runs,list): raise ValueError('workflow run response is malformed')
 matches=[]
 for run in runs:
  if not isinstance(run,dict): continue
  repository=run.get('repository') or {}
  if (run.get('head_sha'),repository.get('full_name'),run.get('path'),run.get('event'))!=(a.sha,a.repository,a.workflow,a.event): continue
  if a.event=='pull_request':
   associations=run.get('pull_requests')
   if not isinstance(associations,list): continue
   associated={item.get('number') for item in associations if isinstance(item,dict)}
   if associated and a.pr not in associated: continue
   if run.get('display_title')!=f'MudX CI PR #{a.pr}': continue
  matches.append(run)
 if not matches: raise ValueError('exact trusted CI run is missing')
 def order(run):
  return tuple(value if isinstance(value,int) else 0 for value in (run.get('run_number'),run.get('run_attempt'),run.get('id')))
 latest=max(matches,key=order)
 if latest.get('status')!='completed' or latest.get('conclusion')!='success': raise ValueError('latest exact trusted CI run is not successful')
 if not isinstance(latest.get('id'),int): raise ValueError('trusted CI run ID is invalid')
 print(latest['id'])
def symbol_marker(a):
 if not SYMBOL_MARKER.fullmatch(a.expected): raise ValueError('expected symbol marker is malformed')
 occurrences=[line for line in a.body.splitlines() if 'mudx-symbol-sha256:' in line]
 if not occurrences:
  if a.allow_missing: print('missing'); return
  raise ValueError('expected symbol marker is missing')
 if occurrences!=[a.expected]: raise ValueError('symbol marker history is ambiguous or conflicting')
 print('published')
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
 if not SHA.fullmatch(a.sha): raise ValueError('invalid artifact source identity')
 version(a.version); root=Path(a.root).resolve() if a.root else None; files=[]
 for item in a.files:
  path=Path(item).resolve(); name=str(path.relative_to(root)) if root else str(Path(item))
  if root: path=safe_file(root,name)
  files.append({'path':name,'sha256':file_digest(path)})
 Path(a.output).write_text(json.dumps({'schema':2,'source_sha':a.sha,'version':a.version,'producer':a.producer,'files':files},sort_keys=True,indent=2)+'\n')
def manifest_verify(a):
 d=json.loads(Path(a.manifest).read_text())
 if d.get('schema')!=2: raise ValueError('unsupported manifest schema')
 if (d.get('source_sha'),d.get('version'),d.get('producer'))!=(a.sha,a.version,a.producer): raise ValueError('manifest producer/source/version mismatch')
 version(a.version); entries=d.get('files')
 if not isinstance(entries,list) or not entries: raise ValueError('manifest files must be a nonempty list')
 root=Path(a.root) if a.root else None; names=[]
 for item in entries:
  if not isinstance(item,dict) or set(item)!= {'path','sha256'} or not isinstance(item.get('path'),str) or not re.fullmatch(r'[0-9a-f]{64}',item.get('sha256','')): raise ValueError('invalid manifest file entry')
  name=item['path']; path=safe_file(root,name) if root else Path(name)
  if name in names: raise ValueError('duplicate manifest path: '+name)
  names.append(name)
  if file_digest(path)!=item['sha256']: raise ValueError('checksum mismatch: '+name)
 if root:
  main=[n for n in names if n.endswith('.nupkg') and not n.endswith('.snupkg')]
  symbols=[n for n in names if n.endswith('.snupkg')]
  archives=[n for n in names if n==f'MudX-{a.version}-linux-amd64.tar.gz']
  checksums=[n for n in names if n=='SHA256SUMS']
  if len(main)!=1 or len(symbols)!=1 or len(archives)!=1 or len(checksums)!=1 or len(names)!=4: raise ValueError('manifest must contain one package, one symbol package, the versioned linux/amd64 Docker archive, and SHA256SUMS')
def zip_payload(path):
 with zipfile.ZipFile(path) as archive:
  names=[name for name in archive.namelist() if name.lower()!='.signature.p7s']
  if len(names)!=len(set(names)): raise ValueError('duplicate ZIP entry')
  return {name:hashlib.sha256(archive.read(name)).hexdigest() for name in sorted(names)}
def package_equivalent(a):
 if zip_payload(a.candidate)!=zip_payload(a.published): raise ValueError('published package content differs beyond .signature.p7s repository signing')
def head_binding(a):
 if not SHA.fullmatch(a.expected) or not a.observed or any(value!=a.expected for value in a.observed): raise ValueError('version PR head changed during authorization')
def artifact_select(a):
 data=json.loads(Path(a.json).read_text()); matches=[]
 for item in data.get('artifacts',[]):
  if item.get('name')==a.name and item.get('expired') is False and (item.get('workflow_run') or {}).get('id')==a.run: matches.append(item.get('id'))
 if len(matches)!=1 or not isinstance(matches[0],int): raise ValueError('Retained producer artifact is missing, expired, or ambiguous; do not rebuild or allocate a version.')
 print(matches[0])
def resume_plan(a):
 if a.main=='mismatch' or a.release=='mismatch': raise ValueError('existing immutable publication does not match retained candidate')
 if a.release=='exact':
  if a.main!='equivalent' or a.symbols!='published': raise ValueError('finalized release conflicts with external state')
  print(json.dumps({key:'complete' for key in ('main','symbols','release')},sort_keys=True)); return
 if a.symbols=='unknown': raise ValueError('symbol publication cannot be proven; manual reconciliation required before retry')
 plan={'main':'complete' if a.main=='equivalent' else 'publish','symbols':'complete' if a.symbols=='published' else 'publish','release':'finalize' if a.release=='draft' else 'create-draft'}
 print(json.dumps(plan,sort_keys=True))
def main():
 p=argparse.ArgumentParser(); s=p.add_subparsers(dest='cmd',required=True)
 x=s.add_parser('calculate'); x.add_argument('--current',required=True); x.add_argument('--known',action='append',default=[]); x.add_argument('--bump',choices=['patch','minor','major'],default='patch'); x.add_argument('--custom'); x.set_defaults(fn=calculate)
 x=s.add_parser('actor'); x.add_argument('--actor',required=True); x.add_argument('--triggering-actor',required=True); x.add_argument('--allowed',required=True); x.set_defaults(fn=actor)
 x=s.add_parser('version-files'); x.add_argument('--before',required=True); x.add_argument('--after',required=True); x.add_argument('--expected',required=True); x.set_defaults(fn=version_files)
 x=s.add_parser('ci-run'); x.add_argument('--json',required=True); x.add_argument('--sha',required=True); x.add_argument('--repository',required=True); x.add_argument('--workflow',required=True); x.add_argument('--event',choices=['pull_request','push'],required=True); x.add_argument('--pr',type=int); x.set_defaults(fn=ci_run)
 x=s.add_parser('symbol-marker'); x.add_argument('--body',required=True); x.add_argument('--expected',required=True); x.add_argument('--allow-missing',action='store_true'); x.set_defaults(fn=symbol_marker)
 x=s.add_parser('manifest-create'); x.add_argument('--output',required=True); x.add_argument('--root'); x.add_argument('--sha',required=True); x.add_argument('--version',required=True); x.add_argument('--producer',required=True); x.add_argument('files',nargs='+'); x.set_defaults(fn=manifest_create)
 x=s.add_parser('manifest-verify'); x.add_argument('--manifest',required=True); x.add_argument('--root'); x.add_argument('--sha',required=True); x.add_argument('--version',required=True); x.add_argument('--producer',required=True); x.set_defaults(fn=manifest_verify)
 x=s.add_parser('package-equivalent'); x.add_argument('--candidate',required=True); x.add_argument('--published',required=True); x.set_defaults(fn=package_equivalent)
 x=s.add_parser('head-binding'); x.add_argument('--expected',required=True); x.add_argument('--observed',action='append',required=True); x.set_defaults(fn=head_binding)
 x=s.add_parser('artifact-select'); x.add_argument('--json',required=True); x.add_argument('--name',required=True); x.add_argument('--run',required=True,type=int); x.set_defaults(fn=artifact_select)
 x=s.add_parser('resume-plan'); x.add_argument('--main',choices=['missing','equivalent','mismatch'],required=True); x.add_argument('--symbols',choices=['missing','published','unknown'],required=True); x.add_argument('--release',choices=['missing','draft','exact','mismatch'],required=True); x.set_defaults(fn=resume_plan)
 a=p.parse_args()
 try: a.fn(a)
 except (ValueError,OSError,json.JSONDecodeError,zipfile.BadZipFile) as error: print('release guard: '+str(error),file=sys.stderr); return 2
 return 0
if __name__=='__main__': raise SystemExit(main())
