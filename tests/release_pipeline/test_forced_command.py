import json, os, subprocess, tempfile, unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
FORCED=ROOT/'deploy/mudx-deploy-ssh'

class ForcedCommandTests(unittest.TestCase):
 def invoke(self,command,ok=True):
  with tempfile.TemporaryDirectory() as d:
   output=Path(d,'argv.json'); target=Path(d,'deploy')
   target.write_text('#!/usr/bin/env python3\nimport json,os,sys\nopen(os.environ["OUTPUT"],"w").write(json.dumps(sys.argv))\n'); target.chmod(0o755)
   env=os.environ|{'SSH_ORIGINAL_COMMAND':command,'MUDX_TEST_MODE':'1','MUDX_DEPLOY_BIN':str(target),'OUTPUT':str(output)}
   result=subprocess.run([str(FORCED)],env=env,text=True,capture_output=True)
   self.assertEqual(ok,result.returncode==0,result.stderr)
   return json.loads(output.read_text()) if output.exists() else None
 def test_preflight_proves_transport_without_deployment(self):
  result=subprocess.run([str(FORCED)],env=os.environ|{'SSH_ORIGINAL_COMMAND':'preflight'},text=True,capture_output=True)
  self.assertEqual(0,result.returncode,result.stderr)
  self.assertEqual('mudx-deploy-ready',result.stdout.strip())
 def test_fixed_system_interpreter_ignores_caller_path(self):
  with tempfile.TemporaryDirectory() as d:
   marker=Path(d,'hijacked'); python=Path(d,'python3')
   python.write_text('#!/bin/sh\ntouch "$HIJACK_MARKER"\necho hijacked\n')
   python.chmod(0o755)
   env=os.environ|{'SSH_ORIGINAL_COMMAND':'preflight','PATH':d,'HIJACK_MARKER':str(marker)}
   result=subprocess.run([str(FORCED)],env=env,text=True,capture_output=True)
   self.assertEqual(0,result.returncode,result.stderr)
   self.assertEqual('mudx-deploy-ready',result.stdout.strip())
   self.assertFalse(marker.exists(),'privileged wrapper must not resolve its interpreter through caller PATH')
   self.assertEqual('#!/usr/bin/python3',FORCED.read_text().splitlines()[0])
 def test_exact_deploy_grammar_is_forwarded_without_a_shell(self):
  argv=self.invoke('deploy 42 9.10.1 '+'a'*40+' sha256:'+'b'*64)
  self.assertEqual(['deploy','deploy','42','9.10.1','a'*40,'sha256:'+'b'*64],[Path(argv[0]).name,*argv[1:]])
 def test_arbitrary_and_ambiguous_commands_are_rejected(self):
  for command in ('bash','deploy 1 9.10.1 '+'a'*40+' sha256:'+'b'*64+';id','deploy 01 9.10.1 '+'a'*40+' sha256:'+'b'*64,'deploy 1 9.10.1 '+'a'*39+' sha256:'+'b'*64,''):
   with self.subTest(command=command): self.invoke(command,ok=False)

if __name__=='__main__': unittest.main()
