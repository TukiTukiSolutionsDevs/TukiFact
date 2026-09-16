#!/usr/bin/env bash
# Tests de `agents.sh unlink`.

set -euo pipefail
# shellcheck source=helpers.sh
source "$(dirname "${BASH_SOURCE[0]}")/helpers.sh"

test_unlink_removes_only_managed_links() {
  make_skill alpha
  make_skill beta
  run_agents link --cli claude
  mkdir -p "$FIXTURE_ROOT/elsewhere" "$REPO/.claude/skills/realdir"
  printf 'mío\n' >"$REPO/.claude/skills/realdir/SKILL.md"
  ln -s "$FIXTURE_ROOT/elsewhere" "$REPO/.claude/skills/foreign"
  ln -s ../../.agents/skills/ghost "$REPO/.claude/skills/ghost"

  run_agents unlink --cli claude
  assert_rc 0
  assert_not_exists "$REPO/.claude/skills/alpha"
  assert_not_exists "$REPO/.claude/skills/beta"
  assert_not_exists "$REPO/.claude/skills/ghost"
  assert_symlink "$REPO/.claude/skills/foreign"
  assert_real_dir "$REPO/.claude/skills/realdir"
  [[ -f "$REPO/.claude/skills/realdir/SKILL.md" ]] || fail "se borró contenido real"
  [[ -d "$FIXTURE_ROOT/elsewhere" ]] || fail "se borró el destino de un symlink ajeno"
  [[ -f "$REPO/.agents/skills/alpha/SKILL.md" ]] || fail "se borró la fuente"
}

test_unlink_removes_empty_dirs_it_created() {
  make_skill alpha
  run_agents link --cli claude
  run_agents unlink --cli claude
  assert_rc 0
  assert_not_exists "$REPO/.claude/skills"
  assert_not_exists "$REPO/.claude"
}

test_unlink_keeps_preexisting_dirs() {
  make_skill alpha
  mkdir -p "$REPO/.claude/skills"
  run_agents link --cli claude
  run_agents unlink --cli claude
  assert_rc 0
  assert_real_dir "$REPO/.claude/skills"
}

test_unlink_keeps_instructions_without_purge() {
  make_skill alpha
  run_agents link --cli claude
  run_agents unlink --cli claude
  assert_rc 0
  assert_eq "$(count_lines_equal "$REPO/CLAUDE.md" "@AGENTS.md")" "1"
}

test_unlink_purge_removes_only_import_line() {
  make_skill alpha
  printf '# Mío\n\nTexto propio.\n' >"$REPO/CLAUDE.md"
  run_agents link --cli claude
  run_agents unlink --cli claude --purge-instructions
  assert_rc 0
  assert_eq "$(<"$REPO/CLAUDE.md")" $'# Mío\n\nTexto propio.'
}

test_unlink_user_scope_in_fake_home() {
  make_skill alpha
  mkdir -p "$HOME/.claude/skills/personal"
  run_agents link --scope user
  run_agents unlink --scope user
  assert_rc 0
  assert_not_exists "$HOME/.claude/skills/alpha"
  assert_real_dir "$HOME/.claude/skills/personal"
  assert_not_exists "$HOME/.agents"
}

test_unlink_dry_run_changes_nothing() {
  make_skill alpha
  run_agents link --cli claude
  run_agents unlink --cli claude --dry-run
  assert_rc 0
  assert_symlink "$REPO/.claude/skills/alpha"
}

run_tests
