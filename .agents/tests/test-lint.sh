#!/usr/bin/env bash
# Tests de `agents.sh lint` y `agents.sh new-skill`.

set -euo pipefail
# shellcheck source=helpers.sh
source "$(dirname "${BASH_SOURCE[0]}")/helpers.sh"

test_lint_passes_valid_skill() {
  make_skill alpha
  run_agents lint
  assert_rc 0
}

test_lint_rejects_name_not_matching_folder() {
  make_skill alpha
  replace_in_file "$REPO/.agents/skills/alpha/SKILL.md" "name: alpha" "name: beta"
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" "alpha"
  assert_contains "$OUT" "name"
}

test_lint_rejects_invalid_name_format() {
  make_skill Bad_Skill
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" "Bad_Skill"
}

test_lint_rejects_description_without_trigger() {
  make_skill alpha '"Skill sin disparador."'
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" "Trigger:"
}

test_lint_rejects_unquoted_description() {
  make_skill alpha 'Trigger: alpha. Sin comillas.'
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" "description"
}

test_lint_rejects_multiline_description() {
  make_skill alpha '>
  Trigger: alpha. En bloque.'
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" "description"
}

test_lint_rejects_description_over_250_chars() {
  local long
  long="Trigger: $(printf 'x%.0s' {1..250})."
  make_skill alpha "\"$long\""
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" "250"
}

test_lint_warns_description_over_160_chars() {
  local long
  long="Trigger: $(printf 'x%.0s' {1..170})."
  make_skill alpha "\"$long\""
  run_agents lint
  assert_rc 0
  assert_contains "$OUT" "WARN"
  assert_contains "$OUT" "160"
}

test_lint_rejects_missing_license_and_metadata() {
  make_skill alpha
  replace_in_file "$REPO/.agents/skills/alpha/SKILL.md" "license: Apache-2.0" ""
  replace_in_file "$REPO/.agents/skills/alpha/SKILL.md" '  version: "1.0"' ""
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" "license"
  assert_contains "$OUT" "metadata.version"
}

test_lint_rejects_type_field() {
  make_skill alpha
  replace_in_file "$REPO/.agents/skills/alpha/SKILL.md" "license: Apache-2.0" $'license: Apache-2.0\ntype: skill'
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" "type"
}

test_lint_rejects_missing_frontmatter() {
  mkdir -p "$REPO/.agents/skills/alpha"
  printf '# Sin frontmatter\n' >"$REPO/.agents/skills/alpha/SKILL.md"
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" "frontmatter"
}

test_lint_rejects_broken_relative_link() {
  make_skill alpha
  printf -- '- Roto: [missing](references/missing.md)\n' >>"$REPO/.agents/skills/alpha/SKILL.md"
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" "references/missing.md"
}

test_lint_rejects_broken_link_in_reference_file() {
  make_skill alpha
  printf 'Ver [otra](../../beta/SKILL.md)\n' >>"$REPO/.agents/skills/alpha/references/guide.md"
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" "guide.md"
}

test_lint_accepts_cross_skill_link_and_ignores_code_fences() {
  make_skill alpha
  make_skill beta
  {
    printf -- '- Beta: [beta](../beta/SKILL.md#references)\n'
    printf -- '- Web: [sitio](https://example.com/x.md)\n'
    # shellcheck disable=SC2016 # backticks literales de Markdown.
    printf '```ts\nconst x = [a](nope.md);\n```\n'
  } >>"$REPO/.agents/skills/alpha/SKILL.md"
  run_agents lint
  assert_rc 0
}

test_lint_rejects_opencode_reference() {
  make_skill alpha
  # shellcheck disable=SC2016 # backticks literales de Markdown.
  printf 'Ver `.opencode/skills/alpha/SKILL.md`.\n' >>"$REPO/.agents/skills/alpha/references/guide.md"
  run_agents lint
  assert_rc_nonzero
  assert_contains "$OUT" ".opencode/"
}

test_lint_only_named_skill() {
  make_skill alpha
  make_skill beta '"sin trigger"'
  run_agents lint alpha
  assert_rc 0
  run_agents lint beta
  assert_rc_nonzero
}

test_new_skill_scaffolds_valid_skill() {
  run_agents new-skill gamma-rules
  assert_rc 0
  [[ -f "$REPO/.agents/skills/gamma-rules/SKILL.md" ]] || fail "no se creó SKILL.md"
  run_agents lint gamma-rules
  assert_rc 0
  local content
  content="$(<"$REPO/.agents/skills/gamma-rules/SKILL.md")"
  assert_contains "$content" "## Activation Contract"
  assert_contains "$content" "## References"
}

test_new_skill_rejects_invalid_name() {
  run_agents new-skill Bad_Name
  assert_rc_nonzero
  assert_not_exists "$REPO/.agents/skills/Bad_Name"
}

test_new_skill_refuses_existing_skill() {
  make_skill alpha
  local before
  before="$(<"$REPO/.agents/skills/alpha/SKILL.md")"
  run_agents new-skill alpha
  assert_rc_nonzero
  assert_eq "$(<"$REPO/.agents/skills/alpha/SKILL.md")" "$before"
}

run_tests
