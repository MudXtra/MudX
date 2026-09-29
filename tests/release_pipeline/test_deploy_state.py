import fcntl, json, os, re, subprocess, tempfile, time, unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]; DEPLOY=ROOT/'deploy/mudx-deploy'
class Tests(unittest.TestCase):
 def invoke(self,state,*args,fail=None,ok=True,extra_env=None):
  env=os.environ|{'MUDX_TEST_MODE':'1','MUDX_STATE_DIR':str(state),'MUDX_DOCKER_BIN':str(ROOT/'tests/release_pipeline/fake-docker')}
  if extra_env: env.update(extra_env)
  if fail: env['MUDX_FAIL_AFTER']=fail
  if 'no-prior' in args: env['MUDX_NO_PRIOR']='1'; args=tuple(x for x in args if x!='no-prior')
  if (Path(state)/'allowlist.json').exists(): env['MUDX_RUNTIME_ALLOWLIST']=str(Path(state)/'allowlist.json')
  r=subprocess.run([str(DEPLOY),*args],env=env,text=True,capture_output=True); self.assertEqual(ok,r.returncode==0,r.stderr); return r
 def test_actual_container_name_is_preserved_in_journal(self):
  with tempfile.TemporaryDirectory() as d:
   Path(d,'inject-rollback-failure').touch()
   self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64,fail='prepared',ok=False)
   journal=json.loads(Path(d,'journal.json').read_text())
   self.assertEqual('MudX',journal['prior_name'])
   self.assertEqual('MudX-candidate-1',journal['candidate_name'])
 def test_successful_reconcile_consumes_recovery_journal(self):
  with tempfile.TemporaryDirectory() as d:
   journal={'sequence':2,'version':'9.10.2','source_sha':'d'*40,'digest':'sha256:'+'b'*64,'stage':'candidate-started','prior_name':'MudX-prior-2','prior_restart':'no','candidate_name':'MudX-candidate-2'}
   Path(d,'journal.json').write_text(json.dumps(journal))
   Path(d,'maintenance').write_text('1\n')
   self.invoke(d,'reconcile')
   self.assertFalse(Path(d,'journal.json').exists(),'successful reconciliation must consume its journal')
   self.invoke(d,'reconcile')

 def test_successful_deploy_rollback_consumes_recovery_journal(self):
  with tempfile.TemporaryDirectory() as d:
   Path(d,'inject-health-failure').touch()
   self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64,ok=False)
   self.assertFalse(Path(d,'journal.json').exists(),'completed rollback must not be replayed at boot')
   self.assertFalse(Path(d,'maintenance').exists())

 def test_supervisor_waits_for_deploy_lock_and_planned_stop_is_sticky(self):
  with tempfile.TemporaryDirectory() as d, open(Path(d,'lock'),'a+') as lock:
   env=os.environ|{'MUDX_TEST_MODE':'1','MUDX_STATE_DIR':d,'MUDX_DOCKER_BIN':str(ROOT/'tests/release_pipeline/fake-docker'),'MUDX_SUPERVISOR_ONCE':'1'}
   fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
   process=subprocess.Popen([str(DEPLOY),'supervise'],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
   time.sleep(.2); self.assertIsNone(process.poll(),'supervisor must block behind the deployment lock')
   fcntl.flock(lock,fcntl.LOCK_UN)
   _,stderr=process.communicate(timeout=5); self.assertEqual(0,process.returncode,stderr)
   self.invoke(d,'stop')
   self.assertTrue(Path(d,'stopped').exists())
 def test_planned_stop_cannot_race_a_supervisor_restart(self):
  with tempfile.TemporaryDirectory() as d:
   fake=Path(d,'docker'); fake.write_text("""#!/usr/bin/env bash
set -euo pipefail
printf '%s\n' "$*" >> "$MUDX_STATE_DIR/docker-calls"
if [[ "$1" == wait ]]; then
 touch "$MUDX_STATE_DIR/wait-ready"
 while [[ ! -e "$MUDX_STATE_DIR/wait-release" ]]; do sleep .01; done
fi
"""); fake.chmod(0o755)
   env=os.environ|{'MUDX_TEST_MODE':'1','MUDX_STATE_DIR':d,'MUDX_DOCKER_BIN':str(fake)}
   supervisor=subprocess.Popen([str(DEPLOY),'supervise'],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
   try:
    for _ in range(200):
     if Path(d,'wait-ready').exists(): break
     time.sleep(.01)
    else: self.fail('supervisor never reached docker wait')
    self.invoke(d,'stop',extra_env={'MUDX_DOCKER_BIN':str(fake)})
    Path(d,'wait-release').touch()
    _,stderr=supervisor.communicate(timeout=2)
    self.assertEqual(0,supervisor.returncode,stderr)
    starts=[line for line in Path(d,'docker-calls').read_text().splitlines() if line.startswith('start ')]
    self.assertEqual(1,len(starts),'planned stop must not be followed by a restart')
   finally:
    if supervisor.poll() is None: supervisor.kill(); supervisor.communicate()
 def test_stop_before_supervisor_initialization_remains_dominant(self):
  with tempfile.TemporaryDirectory() as d:
   self.invoke(d,'resume')
   self.invoke(d,'stop')
   self.invoke(d,'supervise',extra_env={'MUDX_SUPERVISOR_ONCE':'1'})
   self.assertTrue(Path(d,'stopped').exists())
   calls=Path(d,'docker.log').read_text().splitlines()
   self.assertFalse(any(call.startswith('start ') for call in calls),'a newer stop intent must prevent supervisor startup')
 def test_explicit_resume_clears_prior_stop_before_supervisor_start(self):
  with tempfile.TemporaryDirectory() as d:
   self.invoke(d,'stop')
   self.invoke(d,'resume')
   self.assertFalse(Path(d,'stopped').exists())
   self.invoke(d,'supervise',extra_env={'MUDX_SUPERVISOR_ONCE':'1'})
   calls=Path(d,'docker.log').read_text().splitlines()
   self.assertEqual(1,sum(call.startswith('start ') for call in calls))
 def test_planned_stop_reports_docker_failure(self):
  with tempfile.TemporaryDirectory() as d:
   fake=Path(d,'docker'); fake.write_text("""#!/usr/bin/env bash
[[ "$1" == inspect ]] && exit 0
[[ "$1" == stop ]] && exit 1
exit 0
"""); fake.chmod(0o755)
   self.invoke(d,'stop',ok=False,extra_env={'MUDX_DOCKER_BIN':str(fake)})
 def test_stop_intent_survives_lock_timeout_before_docker_mutation(self):
  with tempfile.TemporaryDirectory() as d, open(Path(d,'lock'),'a+') as lock:
   env=os.environ|{'MUDX_TEST_MODE':'1','MUDX_STATE_DIR':d,'MUDX_DOCKER_BIN':str(ROOT/'tests/release_pipeline/fake-docker'),'MUDX_STOP_LOCK_TIMEOUT':'0.2'}
   fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
   process=subprocess.Popen([str(DEPLOY),'stop'],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
   try:
    deadline=time.monotonic()+1
    while not Path(d,'stopped').exists() and time.monotonic()<deadline: time.sleep(.01)
    self.assertTrue(Path(d,'stopped').exists(),'stop intent must be durable before waiting for the deployment lock')
    _,stderr=process.communicate(timeout=2)
    self.assertNotEqual(0,process.returncode,stderr)
    self.assertIn('deployment lock is busy',stderr)
    self.assertTrue(Path(d,'stopped').exists(),'lock timeout must not erase stop intent')
    self.assertFalse(Path(d,'docker.log').exists(),'timed-out stop must not race Docker ownership outside the lock')
   finally:
    if process.poll() is None: process.kill()
    process.communicate()
 def test_stop_waits_for_long_lock_after_persisting_intent(self):
  with tempfile.TemporaryDirectory() as d, open(Path(d,'lock'),'a+') as lock:
   env=os.environ|{'MUDX_TEST_MODE':'1','MUDX_STATE_DIR':d,'MUDX_DOCKER_BIN':str(ROOT/'tests/release_pipeline/fake-docker'),'MUDX_STOP_LOCK_TIMEOUT':'2'}
   fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
   process=subprocess.Popen([str(DEPLOY),'stop'],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
   try:
    deadline=time.monotonic()+1
    while not Path(d,'stopped').exists() and time.monotonic()<deadline: time.sleep(.01)
    self.assertTrue(Path(d,'stopped').exists())
    time.sleep(.2); self.assertIsNone(process.poll(),'stop must keep waiting for an in-flight deployment within budget')
    fcntl.flock(lock,fcntl.LOCK_UN)
    _,stderr=process.communicate(timeout=2)
    self.assertEqual(0,process.returncode,stderr)
    self.assertIn('stop --time 30 MudX',Path(d,'docker.log').read_text())
   finally:
    if process.poll() is None: process.kill()
    process.communicate()
 def test_resume_cannot_clear_an_active_stop_intent(self):
  with tempfile.TemporaryDirectory() as d, open(Path(d,'stop-intent.lock'),'a+') as intent:
   Path(d,'stopped').write_text('1\n')
   fcntl.flock(intent,fcntl.LOCK_EX|fcntl.LOCK_NB)
   result=self.invoke(d,'resume',ok=False)
   self.assertIn('stop is in progress',result.stderr)
   self.assertTrue(Path(d,'stopped').exists(),'concurrent resume must not erase active stop ownership')
 def test_stop_docker_budget_is_bounded_and_intent_survives(self):
  with tempfile.TemporaryDirectory() as d:
   fake=Path(d,'docker'); fake.write_text('#!/bin/sh\nsleep 2\n')
   fake.chmod(0o755)
   started=time.monotonic()
   result=self.invoke(d,'stop',ok=False,extra_env={'MUDX_DOCKER_BIN':str(fake),'MUDX_STOP_DOCKER_TIMEOUT':'0.1'})
   self.assertLess(time.monotonic()-started,1)
   self.assertIn('timed out',result.stderr)
   self.assertTrue(Path(d,'stopped').exists())
   self.assertFalse(Path(d,'maintenance').exists())
 def test_inflight_deploy_aborts_after_a_timed_out_stop_intent(self):
  with tempfile.TemporaryDirectory() as d:
   fake=Path(d,'docker'); fake.write_text("""#!/usr/bin/env bash
set -euo pipefail
printf '%s\n' "$*" >> "$MUDX_STATE_DIR/docker-calls"
if [[ "$1" == pull ]]; then
 touch "$MUDX_STATE_DIR/pull-entered"
 while [[ ! -e "$MUDX_STATE_DIR/pull-release" ]]; do sleep .01; done
elif [[ "$1" == inspect && "$2" == --format ]]; then
 printf 'unless-stopped\n'
fi
"""); fake.chmod(0o755)
   env=os.environ|{'MUDX_TEST_MODE':'1','MUDX_STATE_DIR':d,'MUDX_DOCKER_BIN':str(fake),'MUDX_LOCK_TIMEOUT':'2'}
   deploy=subprocess.Popen([str(DEPLOY),'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
   try:
    deadline=time.monotonic()+2
    while not Path(d,'pull-entered').exists() and time.monotonic()<deadline: time.sleep(.01)
    self.assertTrue(Path(d,'pull-entered').exists(),'deploy never entered its blocking pull')
    stop_env=env|{'MUDX_STOP_LOCK_TIMEOUT':'0.1'}
    stopped=subprocess.run([str(DEPLOY),'stop'],env=stop_env,text=True,capture_output=True,timeout=2)
    self.assertNotEqual(0,stopped.returncode,stopped.stderr)
    self.assertIn('deployment lock is busy',stopped.stderr)
    self.assertTrue(Path(d,'stopped').exists(),'timed-out stop intent must remain authoritative')
    Path(d,'pull-release').touch()
    _,stderr=deploy.communicate(timeout=3)
    self.assertNotEqual(0,deploy.returncode,'in-flight deployment must not commit after a newer stopped marker')
    self.assertIn('service stop requested during deployment',stderr)
    self.assertFalse(Path(d,'current.json').exists(),'stopped deployment must not commit release identity')
    self.assertEqual('prior-stopped',Path(d,'active').read_text().strip(),'rollback may restore but must not restart the prior container')
    self.assertEqual('no',Path(d,'rollback-policy').read_text().strip(),'stopped rollback must leave Docker restart disabled')
   finally:
    if deploy.poll() is None: deploy.kill(); deploy.communicate()
 def test_stop_preserves_preexisting_maintenance_marker(self):
  with tempfile.TemporaryDirectory() as d:
   Path(d,'maintenance').write_text('recovery-owned\n')
   self.invoke(d,'stop')
   self.assertEqual('recovery-owned\n',Path(d,'maintenance').read_text())
 def test_recovery_pending_deploy_rejects_even_an_exact_duplicate(self):
  with tempfile.TemporaryDirectory() as d:
   identity={'sequence':1,'version':'9.10.1','source_sha':'c'*40,'digest':'sha256:'+'a'*64,'container':'MudX'}
   Path(d,'current.json').write_text(json.dumps(identity))
   Path(d,'maintenance').write_text('1\n')
   result=self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64,ok=False)
   self.assertIn('deployment recovery is pending',result.stderr)
   self.assertFalse(Path(d,'docker.log').exists(),'recovery-pending rejection must precede Docker calls')
   self.assertTrue(Path(d,'maintenance').exists())
 def test_supervisor_throttles_a_successful_docker_wait(self):
  with tempfile.TemporaryDirectory() as d:
   fake=Path(d,'docker'); fake.write_text("""#!/usr/bin/env bash
set -euo pipefail
printf '%s\n' "$*" >> "$MUDX_STATE_DIR/docker-calls"
if [[ "$1" == inspect ]]; then printf 'false\n'; fi
if [[ "$1" == wait ]]; then touch "$MUDX_STATE_DIR/wait-returned"; printf '137\n'; fi
"""); fake.chmod(0o755)
   env=os.environ|{'MUDX_TEST_MODE':'1','MUDX_STATE_DIR':d,'MUDX_DOCKER_BIN':str(fake),'MUDX_SUPERVISOR_RESTART_DELAY':'0.3'}
   supervisor=subprocess.Popen([str(DEPLOY),'supervise'],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
   try:
    deadline=time.monotonic()+2
    while not Path(d,'wait-returned').exists() and time.monotonic()<deadline: time.sleep(.01)
    self.assertTrue(Path(d,'wait-returned').exists(),'supervisor never observed container exit')
    time.sleep(.1)
    calls=Path(d,'docker-calls').read_text().splitlines()
    self.assertEqual(1,sum(call.startswith('start ') for call in calls),'ordinary container exit must be throttled before restart')
   finally:
    supervisor.kill(); supervisor.communicate()
 def test_marker_transition_table(self):
  cases=(
   ('new-stop',(),('stop',),True,True,False),
   ('borrowed-maintenance-stop',('maintenance',),('stop',),True,True,True),
   ('stopped-deploy',('stopped',),('deploy','1','9.10.1','c'*40,'sha256:'+'a'*64),False,True,False),
   ('recovery-pending-deploy',('maintenance',),('deploy','1','9.10.1','c'*40,'sha256:'+'a'*64),False,False,True),
  )
  for name,markers,args,ok,stopped,maintenance in cases:
   with self.subTest(name=name),tempfile.TemporaryDirectory() as d:
    for item in markers: Path(d,item).write_text('owned\n')
    self.invoke(d,*args,ok=ok)
    self.assertEqual(stopped,Path(d,'stopped').exists())
    self.assertEqual(maintenance,Path(d,'maintenance').exists())
 def test_stop_handoff_budgets_cover_deploy_recovery_and_manager_deadlines(self):
  source=DEPLOY.read_text(); unit=(ROOT/'deploy/systemd/mudx-container.service').read_text()
  values={name:float(re.search(rf'^{name}=([0-9.]+)$',source,re.MULTILINE).group(1)) for name in (
   'STOP_LOCK_TIMEOUT','STOP_DOCKER_TIMEOUT','DEPLOY_DOCKER_TIMEOUT','RECOVERY_DOCKER_TIMEOUT','SUPERVISOR_DOCKER_TIMEOUT')}
  manager=float(re.search(r'^TimeoutStopSec=([0-9.]+)$',unit,re.MULTILINE).group(1))
  self.assertGreaterEqual(values['STOP_LOCK_TIMEOUT'],values['DEPLOY_DOCKER_TIMEOUT']+values['RECOVERY_DOCKER_TIMEOUT']+5)
  self.assertGreaterEqual(values['STOP_LOCK_TIMEOUT'],values['SUPERVISOR_DOCKER_TIMEOUT']+5)
  self.assertGreaterEqual(manager,values['STOP_LOCK_TIMEOUT']+values['STOP_DOCKER_TIMEOUT']+5)
 def test_systemd_stop_timeout_covers_bounded_lock_and_docker_budgets(self):
  source=DEPLOY.read_text(); unit=(ROOT/'deploy/systemd/mudx-container.service').read_text()
  lock=re.search(r'^STOP_LOCK_TIMEOUT=([0-9.]+)$',source,re.MULTILINE)
  docker=re.search(r'^STOP_DOCKER_TIMEOUT=([0-9.]+)$',source,re.MULTILINE)
  timeout=re.search(r'^TimeoutStopSec=([0-9.]+)$',unit,re.MULTILINE)
  self.assertIsNotNone(lock,'stop lock budget must be explicit')
  self.assertIsNotNone(docker,'stop Docker budget must be explicit')
  self.assertIsNotNone(timeout,'systemd stop timeout must be explicit')
  self.assertGreaterEqual(float(timeout.group(1)),float(lock.group(1))+float(docker.group(1))+5)
 def test_systemd_unit_owns_continuous_recovery(self):
  unit=(ROOT/'deploy/systemd/mudx-container.service').read_text()
  self.assertIn('Type=simple',unit)
  self.assertIn('ExecStartPre=/usr/local/libexec/mudx-deploy resume',unit)
  self.assertIn('ExecStart=/usr/local/libexec/mudx-deploy supervise',unit)
  self.assertIn('ExecStop=/usr/local/libexec/mudx-deploy stop',unit)
  self.assertIn('Restart=always',unit)
  self.assertNotIn('docker start mudxdocwebsite',unit)
 def test_grammar_stale_duplicate(self):
  with tempfile.TemporaryDirectory() as d:
   self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64); self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64)
   self.invoke(d,'deploy','0','9.9.9','c'*40,'sha256:'+'b'*64,ok=False); self.invoke(d,'deploy','2;id','9.10.2','c'*40,'sha256:'+'b'*64,ok=False)
 def test_completed_retry_requires_exact_release_identity(self):
  with tempfile.TemporaryDirectory() as d:
   digest='sha256:'+'a'*64; source='c'*40
   self.invoke(d,'deploy','1','9.10.1',source,digest)
   before=Path(d,'docker.log').read_text()
   self.invoke(d,'deploy','1','9.10.1',source,digest)
   self.assertEqual(before,Path(d,'docker.log').read_text(),'an exact retry must be a no-op')
   self.invoke(d,'deploy','1','99.99.99',source,digest,ok=False)
   self.invoke(d,'deploy','1','9.10.1','d'*40,digest,ok=False)
 def test_stopped_deploy_requires_explicit_resume(self):
  with tempfile.TemporaryDirectory() as d:
   self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64)
   self.invoke(d,'stop')
   before_calls=Path(d,'docker.log').read_text()
   before_current=Path(d,'current.json').read_text()
   result=self.invoke(d,'deploy','2','9.10.2','d'*40,'sha256:'+'b'*64,ok=False)
   self.assertIn('service is intentionally stopped',result.stderr)
   self.assertEqual(before_calls,Path(d,'docker.log').read_text(),'rejected deployment must not call Docker')
   self.assertEqual(before_current,Path(d,'current.json').read_text(),'rejected deployment must not change release identity')
   self.invoke(d,'resume')
   self.invoke(d,'deploy','2','9.10.2','d'*40,'sha256:'+'b'*64)
   self.assertEqual(2,json.loads(Path(d,'current.json').read_text())['sequence'])
 def test_deploy_waits_for_a_transient_supervisor_lock(self):
  with tempfile.TemporaryDirectory() as d, open(Path(d,'lock'),'a+') as lock:
   env=os.environ|{'MUDX_TEST_MODE':'1','MUDX_STATE_DIR':d,'MUDX_DOCKER_BIN':str(ROOT/'tests/release_pipeline/fake-docker'),'MUDX_LOCK_TIMEOUT':'2'}
   fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
   process=subprocess.Popen([str(DEPLOY),'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64],env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
   time.sleep(.2); self.assertIsNone(process.poll(),'deploy must wait for a short supervisor lock')
   fcntl.flock(lock,fcntl.LOCK_UN)
   _,stderr=process.communicate(timeout=5); self.assertEqual(0,process.returncode,stderr)
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
 def test_first_install_without_prior_container(self):
  with tempfile.TemporaryDirectory() as d:
   self.invoke(d,'no-prior','deploy','1','9.10.1','c'*40,'sha256:'+'a'*64)
   self.assertEqual('candidate',Path(d,'active').read_text().strip())
  with tempfile.TemporaryDirectory() as d:
   Path(d,'inject-health-failure').touch(); self.invoke(d,'no-prior','deploy','1','9.10.1','c'*40,'sha256:'+'a'*64,ok=False)
   self.assertEqual('none',Path(d,'active').read_text().strip())
 def test_failures_restore_prior(self):
  for marker in ('inject-pull-failure','inject-health-failure'):
   with tempfile.TemporaryDirectory() as d:
    Path(d,marker).touch(); self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64,ok=False); self.assertEqual('prior',Path(d,'active').read_text().strip())
 def test_failed_supervisor_reconciliation_retains_maintenance(self):
  with tempfile.TemporaryDirectory() as d:
   Path(d,'maintenance').touch(); Path(d,'inject-reconcile-failure').touch()
   result=self.invoke(d,'supervise',ok=False,extra_env={'MUDX_SUPERVISOR_ONCE':'1'})
   self.assertIn('injected reconcile failure',result.stderr)
   self.assertTrue(Path(d,'maintenance').exists())
   Path(d,'inject-reconcile-failure').unlink()
   self.invoke(d,'supervise',extra_env={'MUDX_SUPERVISOR_ONCE':'1'})
   self.assertFalse(Path(d,'maintenance').exists())
 def test_rollback_failure_retains_maintenance_until_supervisor_reconciles(self):
  with tempfile.TemporaryDirectory() as d:
   Path(d,'inject-health-failure').touch()
   Path(d,'inject-rollback-failure').touch()
   result=self.invoke(d,'deploy','1','9.10.1','c'*40,'sha256:'+'a'*64,ok=False)
   self.assertIn('injected health failure',result.stderr)
   self.assertIn('injected rollback failure',result.stderr)
   self.assertTrue(Path(d,'maintenance').exists(),'failed rollback must retain reconciliation intent')
   Path(d,'inject-health-failure').unlink()
   Path(d,'inject-rollback-failure').unlink()
   self.invoke(d,'supervise',extra_env={'MUDX_SUPERVISOR_ONCE':'1'})
   self.assertFalse(Path(d,'maintenance').exists(),'successful supervisor reconciliation clears intent')
   self.assertEqual('prior',Path(d,'active').read_text().strip())
if __name__=='__main__': unittest.main()
