#!/usr/bin/env bash
# Utilidades compartidas por los tests de .agents. No ejecutar directamente: source "helpers.sh".
# Cada test corre en un subshell con un repo falso y un HOME falso bajo mktemp -d.

set -euo pipefail

TESTS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
SOURCE_AGENTS_DIR="$(dirname "$TESTS_DIR")"

# Crea el repo y el HOME falsos; nunca toca el HOME real.
setup_fixture() {
  FIXTURE_ROOT="$(mktemp -d)"
  REPO="$FIXTURE_ROOT/repo"
  FAKE_HOME="$FIXTURE_ROOT/home"
  mkdir -p "$REPO/.agents/skills" "$FAKE_HOME"
  if [[ -d "$SOURCE_AGENTS_DIR/scripts" ]]; then
    cp -R "$SOURCE_AGENTS_DIR/scripts" "$REPO/.agents/"
  fi
  if [[ -f "$SOURCE_AGENTS_DIR/targets.conf" ]]; then
    cp "$SOURCE_AGENTS_DIR/targets.conf" "$REPO/.agents/"
  fi
  printf '# AGENTS\n' >"$REPO/AGENTS.md"
  export HOME="$FAKE_HOME"
  export AGENTS_SKIP_CLI_PROBES=1
  AGENTS="$REPO/.agents/scripts/agents.sh"
}

teardown_fixture() {
  if [[ -n "${FIXTURE_ROOT:-}" && -d "$FIXTURE_ROOT" ]]; then
    rm -rf -- "$FIXTURE_ROOT"
  fi
}

# Crea una skill válida con una reference local enlazada.
make_skill() {
  local name="$1" desc dir
  if (($# >= 2)); then
    desc="$2"
  else
    desc="\"Trigger: prueba de $name. Skill de prueba.\""
  fi
  dir="$REPO/.agents/skills/$name"
  mkdir -p "$dir/references"
  printf '# Guía\n' >"$dir/references/guide.md"
  cat >"$dir/SKILL.md" <<EOF
---
name: $name
description: $desc
license: Apache-2.0
metadata:
  author: test
  version: "1.0"
---

# $name

## Activation Contract

Activa en pruebas.

## Hard Rules

- Regla.

## Decision Gates

| Situación | Acción |
|---|---|
| A | B |

## Execution Steps

1. Paso.

## Output Contract

- Salida.

## References

- Guía: [references/guide.md](references/guide.md)
EOF
}

# Reemplaza la primera aparición de un texto en un archivo.
replace_in_file() {
  local file="$1" old="$2" new="$3" content
  content="$(<"$file")"
  printf '%s\n' "${content/"$old"/"$new"}" >"$file"
}

# Ejecuta agents.sh dentro del repo falso; deja la salida en OUT y el código en RC.
run_agents() {
  set +e
  OUT="$(cd "$REPO" && bash "$AGENTS" "$@" 2>&1)"
  RC=$?
  set -e
}

fail() {
  printf '      %s\n' "$@" >&2
  local line
  if [[ -n "${OUT:-}" ]]; then
    printf '      --- salida ---\n' >&2
    while IFS= read -r line; do
      printf '      | %s\n' "$line" >&2
    done <<<"$OUT"
  fi
  exit 1
}

assert_rc() {
  [[ "${RC:-}" == "$1" ]] || fail "código esperado $1, obtenido ${RC:-<vacío>}"
}

assert_rc_nonzero() {
  [[ "${RC:-0}" != 0 ]] || fail "se esperaba un código distinto de 0"
}

assert_eq() {
  [[ "$1" == "$2" ]] || fail "esperado: [$2]" "obtenido: [$1]"
}

assert_contains() {
  [[ "$1" == *"$2"* ]] || fail "no contiene: [$2]"
}

assert_not_contains() {
  [[ "$1" != *"$2"* ]] || fail "no debería contener: [$2]"
}

assert_symlink() {
  [[ -L "$1" ]] || fail "no es symlink: $1"
}

assert_not_exists() {
  [[ ! -e "$1" && ! -L "$1" ]] || fail "no debería existir: $1"
}

assert_real_dir() {
  [[ -d "$1" && ! -L "$1" ]] || fail "no es un directorio real: $1"
}

# Cuenta las líneas de un archivo iguales a un texto exacto.
count_lines_equal() {
  local file="$1" expected="$2" line count=0
  while IFS= read -r line || [[ -n "$line" ]]; do
    [[ "$line" == "$expected" ]] && count=$((count + 1))
  done <"$file"
  printf '%s\n' "$count"
}

# Descubre y ejecuta las funciones test_* del archivo que hace source.
run_tests() {
  local fn rc pass=0 failed=0 _decl _flag
  local -a tests=()
  while read -r _decl _flag fn; do
    [[ "$fn" == test_* ]] && tests+=("$fn")
  done < <(declare -F)
  for fn in "${tests[@]}"; do
    set +e
    (
      set -euo pipefail
      trap teardown_fixture EXIT
      setup_fixture
      "$fn"
    )
    rc=$?
    set -e
    if ((rc == 0)); then
      pass=$((pass + 1))
      printf '  ok    %s\n' "$fn"
    else
      failed=$((failed + 1))
      printf '  FAIL  %s\n' "$fn"
    fi
  done
  printf 'RESULT pass=%d fail=%d\n' "$pass" "$failed"
  ((failed == 0))
}
