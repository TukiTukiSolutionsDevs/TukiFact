#!/usr/bin/env bash
# Tests de los adaptadores instructions-import (CLAUDE.md) y gemini-settings.

set -euo pipefail
# shellcheck source=helpers.sh
source "$(dirname "${BASH_SOURCE[0]}")/helpers.sh"

test_instructions_import_created_when_absent() {
  make_skill alpha
  run_agents link --cli claude
  assert_rc 0
  [[ -f "$REPO/CLAUDE.md" ]] || fail "no se creó CLAUDE.md"
  assert_eq "$(count_lines_equal "$REPO/CLAUDE.md" "@AGENTS.md")" "1"
}

test_instructions_import_appends_once_and_preserves_content() {
  make_skill alpha
  printf '# Proyecto\n\nReglas propias.' >"$REPO/CLAUDE.md"
  run_agents link --cli claude
  assert_rc 0
  run_agents link --cli claude
  assert_rc 0
  assert_eq "$(<"$REPO/CLAUDE.md")" $'# Proyecto\n\nReglas propias.\n@AGENTS.md'
}

test_instructions_import_leaves_file_untouched_when_present() {
  make_skill alpha
  printf '# Proyecto\n@AGENTS.md\nMás reglas.\n' >"$REPO/CLAUDE.md"
  local before
  before="$(<"$REPO/CLAUDE.md")"
  run_agents link --cli claude
  assert_rc 0
  assert_eq "$(<"$REPO/CLAUDE.md")" "$before"
}

test_gemini_settings_created_when_absent() {
  make_skill alpha
  run_agents link --cli gemini
  assert_rc 0
  assert_contains "$(<"$REPO/.gemini/settings.json")" '"AGENTS.md"'
  if command -v jq >/dev/null 2>&1; then
    jq -e '.context.fileName | index("AGENTS.md") != null' "$REPO/.gemini/settings.json" >/dev/null ||
      fail "context.fileName no incluye AGENTS.md"
  fi
}

test_gemini_settings_merge_preserves_existing_keys() {
  command -v jq >/dev/null 2>&1 || return 0
  make_skill alpha
  mkdir -p "$REPO/.gemini"
  printf '{"theme":"dark","context":{"fileName":"GEMINI.md"}}\n' >"$REPO/.gemini/settings.json"
  run_agents link --cli gemini
  assert_rc 0
  run_agents link --cli gemini
  assert_rc 0
  assert_eq "$(jq -r '.theme' "$REPO/.gemini/settings.json")" "dark"
  assert_eq "$(jq -c '.context.fileName' "$REPO/.gemini/settings.json")" '["GEMINI.md","AGENTS.md"]'
}

test_gemini_settings_without_jq_warns_and_skips() {
  make_skill alpha
  mkdir -p "$REPO/.gemini"
  printf '{"theme":"dark"}\n' >"$REPO/.gemini/settings.json"
  AGENTS_JQ=jq-inexistente-para-test run_agents link --cli gemini
  assert_rc 0
  assert_contains "$OUT" "WARN"
  assert_eq "$(<"$REPO/.gemini/settings.json")" '{"theme":"dark"}'
}

run_tests
