#!/usr/bin/env bash
# Ejecuta todos los tests de la tooling de .agents (bash puro, sin dependencias externas).
# Uso: .agents/tests/run.sh [test-archivo.sh ...]

set -euo pipefail

TESTS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"

main() {
  local file output rc total_pass=0 total_fail=0 broken=0 pass fail
  local -a files=()
  if (($#)); then
    for file in "$@"; do files+=("$TESTS_DIR/${file##*/}"); done
  else
    files=("$TESTS_DIR"/test-*.sh)
  fi

  for file in "${files[@]}"; do
    [[ -f "$file" ]] || { echo "ERROR: no existe $file" >&2; exit 2; }
    echo "==> ${file##*/}"
    set +e
    output="$(bash "$file" 2>&1)"
    rc=$?
    set -e
    printf '%s\n' "$output"
    if [[ "$output" =~ RESULT\ pass=([0-9]+)\ fail=([0-9]+) ]]; then
      pass="${BASH_REMATCH[1]}"
      fail="${BASH_REMATCH[2]}"
      total_pass=$((total_pass + pass))
      total_fail=$((total_fail + fail))
    elif ((rc != 0)); then
      broken=$((broken + 1))
    fi
  done

  echo "==> Total: $total_pass ok, $total_fail fallidos, $broken archivos rotos"
  ((total_fail == 0 && broken == 0))
}

main "$@"
