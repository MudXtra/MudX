#!/usr/bin/env python3
import argparse, hashlib, json, re, sys
from pathlib import Path
SEMVER=re.compile(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\Z')
SHA=re.compile(r'[0-9a-f]{40}\Z'); DIGEST=re.compile(r'sha256:[0-9a-f]{64}\Z')
def version(v):
 m=SEMVER.fullmatch(v or '')
 if not m: raise ValueError(f'non-canonical stable SemVer: {v!r}')
 return tuple(map(int,m.groups()))
def calculate(a):
 known=[version(a.current),*(version(v) for v in a.known)]
 cur=max(known)
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
def manifest_create(a):
 if not SHA.fullmatch(a.sha) or not DIGEST.fullmatch(a.image_digest): raise ValueError('invalid artifact identity')
 version(a.version); files=[{'path':str(Path(x)),'sha256':file_digest(x)} for x in a.files]
 Path(a.output).write_text(json.dumps({'schema':1,'source_sha':a.sha,'version':a.version,'image_digest':a.image_digest,'producer':a.producer,'files':files},sort_keys=True,indent=2)+'\n')
def manifest_verify(a):
 d=json.loads(Path(a.manifest).read_text())
 if (d.get('source_sha'),d.get('version'),d.get('producer'))!=(a.sha,a.version,a.producer): raise ValueError('manifest producer/source/version mismatch')
 if not DIGEST.fullmatch(d.get('image_digest','')): raise ValueError('invalid image digest')
 for f in d.get('files',[]):
  if file_digest(f['path'])!=f['sha256']: raise ValueError('checksum mismatch: '+f['path'])
def main():
 p=argparse.ArgumentParser(); s=p.add_subparsers(dest='cmd',required=True)
 x=s.add_parser('calculate'); x.add_argument('--current',required=True); x.add_argument('--known',action='append',default=[]); x.add_argument('--bump',choices=['patch','minor','major'],default='patch'); x.add_argument('--custom'); x.set_defaults(fn=calculate)
 x=s.add_parser('actor'); x.add_argument('--actor',required=True); x.add_argument('--triggering-actor',required=True); x.add_argument('--allowed',required=True); x.set_defaults(fn=actor)
 x=s.add_parser('version-files'); x.add_argument('--before',required=True); x.add_argument('--after',required=True); x.add_argument('--expected',required=True); x.set_defaults(fn=version_files)
 x=s.add_parser('manifest-create'); x.add_argument('--output',required=True); x.add_argument('--sha',required=True); x.add_argument('--version',required=True); x.add_argument('--image-digest',required=True); x.add_argument('--producer',required=True); x.add_argument('files',nargs='+'); x.set_defaults(fn=manifest_create)
 x=s.add_parser('manifest-verify'); x.add_argument('--manifest',required=True); x.add_argument('--sha',required=True); x.add_argument('--version',required=True); x.add_argument('--producer',required=True); x.set_defaults(fn=manifest_verify)
 a=p.parse_args()
 try: a.fn(a)
 except (ValueError,OSError,json.JSONDecodeError) as e: print('release guard: '+str(e),file=sys.stderr); return 2
 return 0
if __name__=='__main__': raise SystemExit(main())
