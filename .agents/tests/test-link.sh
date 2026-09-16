#!/usr/bin/env bash
# Tests de `agents.sh link`.

set -euo pipefail
# shellcheck source=helpers.sh
source "$(dirname "${BASH_SOURCE[0]}")/helpers.sh"

test_link_project_creates_relative_symlinks() {
  make_skill alpha
  make_skill beta
  run_agents link --cli claude
  assert_rc 0
  local name
  for name in alpha beta; do
    assert_symlink "$REPO/.claude/skills/$name"
    assert_eq "$(readlink "$REPO/.claude/skills/$name")" "../../.agents/skills/$name"
    [[ -f "$REPO/.claude/skills/$name/SKILL.md" ]] || fail "el symlink $name no resuelve"
  done
}

test_link_is_idempotent() {
  make_skill alpha
  make_skill beta
  run_agents link
  assert_rc 0
  run_agents link
  assert_rc 0
  assert_eq "$(readlink "$REPO/.claude/skills/alpha")" "../../.agents/skills/alpha"
  local entries=("$REPO/.claude/skills"/*)
  assert_eq "${#entries[@]}" "2"
  assert_eq "$(count_lines_equal "$REPO/CLAUDE.md" "@AGENTS.md")" "1"
}

test_link_refuses_to_clobber_real_directory() {
  make_skill alpha
  make_skill beta
  mkdir -p "$REPO/.claude/skills/alpha"
  printf 'mío\n' >"$REPO/.claude/skills/alpha/keep.txt"
  run_agents link --cli claude --force
  assert_rc_nonzero
  assert_contains "$OUT" "conflict"
  assert_real_dir "$REPO/.claude/skills/alpha"
  [[ -f "$REPO/.claude/skills/alpha/keep.txt" ]] || fail "se borró contenido real"
  assert_symlink "$REPO/.claude/skills/beta"
}

test_link_dry_run_changes_nothing() {
  make_skill alpha
  run_agents link --dry-run
  assert_rc 0
  assert_contains "$OUT" "dry-run"
  assert_not_contains "$OUT" "enlazado"
  assert_not_exists "$REPO/.claude"
  assert_not_exists "$REPO/CLAUDE.md"
  assert_not_exists "$REPO/.gemini"
}

test_link_force_only_replaces_managed_symlink_from_other_checkout() {
  make_skill alpha
  make_skill beta
  mkdir -p "$REPO/.claude/skills" "$FIXTURE_ROOT/foreign/beta"
  ln -s "$FIXTURE_ROOT/old-clone/.agents/skills/alpha" "$REPO/.claude/skills/alpha"
  ln -s "$FIXTURE_ROOT/foreign/beta" "$REPO/.claude/skills/beta"

  run_agents link --cli claude
  assert_rc_nonzero
  assert_eq "$(readlink "$REPO/.claude/skills/alpha")" "$FIXTURE_ROOT/old-clone/.agents/skills/alpha"

  run_agents link --cli claude --force
  assert_rc_nonzero
  assert_eq "$(readlink "$REPO/.claude/skills/alpha")" "../../.agents/skills/alpha"
  assert_eq "$(readlink "$REPO/.claude/skills/beta")" "$FIXTURE_ROOT/foreign/beta"
}

test_link_prunes_stale_managed_links() {
  make_skill alpha
  make_skill beta
  run_agents link --cli claude
  rm -rf "$REPO/.agents/skills/beta"
  run_agents link --cli claude
  assert_rc 0
  assert_not_exists "$REPO/.claude/skills/beta"
  assert_symlink "$REPO/.claude/skills/alpha"
}

test_link_user_scope_creates_absolute_links_in_fake_home() {
  make_skill alpha
  mkdir -p "$HOME/.claude/skills/personal"
  printf 'global\n' >"$HOME/.claude/skills/personal/SKILL.md"
  run_agents link --scope user
  assert_rc 0
  local expected
  expected="$(realpath "$REPO")/.agents/skills/alpha"
  assert_eq "$(readlink "$HOME/.claude/skills/alpha")" "$expected"
  assert_eq "$(readlink "$HOME/.agents/skills/alpha")" "$expected"
  assert_real_dir "$HOME/.claude/skills/personal"
  [[ -f "$HOME/.claude/skills/personal/SKILL.md" ]] || fail "se tocó una skill global ajena"
  assert_not_exists "$REPO/.claude"
  assert_not_exists "$REPO/CLAUDE.md"
}

test_link_cli_filter_limits_adapters() {
  make_skill alpha
  run_agents link --cli gemini
  assert_rc 0
  [[ -f "$REPO/.gemini/settings.json" ]] || fail "no se creó .gemini/settings.json"
  assert_not_exists "$REPO/.claude"
  assert_not_exists "$REPO/CLAUDE.md"
}

test_link_rejects_unknown_cli_and_scope() {
  make_skill alpha
  run_agents link --cli nope
  assert_rc 2
  run_agents link --scope galaxy
  assert_rc 2
  assert_not_exists "$REPO/.claude"
}

run_tests
