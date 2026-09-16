#!/usr/bin/env bash
# Tests de `agents.sh status` y `agents.sh doctor`.

set -euo pipefail
# shellcheck source=helpers.sh
source "$(dirname "${BASH_SOURCE[0]}")/helpers.sh"

test_status_reports_missing_then_ok() {
  make_skill alpha
  run_agents status --cli claude
  assert_rc 0
  assert_contains "$OUT" "missing"
  run_agents link --cli claude
  run_agents status --cli claude
  assert_rc 0
  assert_contains "$OUT" "ok"
  assert_not_contains "$OUT" "sí"
  # La columna ESTADO no debe decir missing (el detalle "missing=0" sí puede aparecer).
  [[ ! "$OUT" =~ [[:space:]]missing[[:space:]] ]] || fail "algún adaptador sigue en missing"
}

test_status_reports_conflict_for_real_dir() {
  make_skill alpha
  mkdir -p "$REPO/.claude/skills/alpha"
  run_agents status --cli claude
  assert_rc 0
  assert_contains "$OUT" "conflict"
}

test_doctor_ok_after_link() {
  make_skill alpha
  run_agents link
  run_agents doctor
  assert_rc 0
}

test_doctor_detects_stale_link() {
  make_skill alpha
  make_skill beta
  run_agents link --cli claude
  rm -rf "$REPO/.agents/skills/beta"
  run_agents doctor --cli claude
  assert_rc_nonzero
  assert_contains "$OUT" "stale"
  assert_contains "$OUT" "beta"
}

test_doctor_detects_dangling_foreign_symlink() {
  make_skill alpha
  run_agents link --cli claude
  ln -s ../nowhere/ghost "$REPO/.claude/skills/ghost"
  run_agents doctor --cli claude
  assert_rc_nonzero
  assert_contains "$OUT" "dangling"
  assert_contains "$OUT" "ghost"
  assert_symlink "$REPO/.claude/skills/ghost"
}

test_doctor_user_scope_uses_fake_home() {
  make_skill alpha
  run_agents link --scope user
  run_agents doctor --scope user
  assert_rc 0
  assert_contains "$OUT" "ok"
}

run_tests
