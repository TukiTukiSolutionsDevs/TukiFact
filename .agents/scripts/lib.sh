#!/usr/bin/env bash
# Compartido por los scripts de .agents. No ejecutar directamente: source "lib.sh".
# Requisitos: bash 4+ y GNU coreutils (realpath -m, readlink). jq es opcional.

set -euo pipefail

LIB_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
AGENTS_DIR="$(dirname "$LIB_DIR")"
REPO_ROOT="$(dirname "$AGENTS_DIR")"
SKILLS_DIR="$AGENTS_DIR/skills"
SKILLS_REAL="$(realpath -m -- "$SKILLS_DIR")"
TARGETS_FILE="$AGENTS_DIR/targets.conf"
STATE_FILE="$AGENTS_DIR/.state/created-dirs"
IMPORT_LINE="@AGENTS.md"
IMPORT_COMMENT="<!-- Skills del repo en .agents/skills (fuente de verdad). Ver .agents/README.md. -->"
JQ_BIN="${AGENTS_JQ:-jq}"

DRY_RUN=0

# --- Mensajes -------------------------------------------------------------

info() { printf '==> %s\n' "$*"; }
detail() { printf '    %s\n' "$*"; }
warn() { printf 'WARN: %s\n' "$*" >&2; }
error() { printf 'ERROR: %s\n' "$*" >&2; }

# die <mensaje> [código]
die() {
  error "$1"
  exit "${2:-1}"
}

# Muestra rutas cortas: relativas al repo o con ~ para el HOME.
display_path() {
  local path="$1"
  if [[ "$path" == "$REPO_ROOT/"* ]]; then
    printf '%s\n' "${path#"$REPO_ROOT/"}"
  elif [[ "$path" == "$HOME/"* ]]; then
    # shellcheck disable=SC2088 # "~" literal solo para mostrar.
    printf '~/%s\n' "${path#"$HOME/"}"
  else
    printf '%s\n' "$path"
  fi
}

# Ejecuta un comando, o solo lo muestra con --dry-run.
run() {
  if ((DRY_RUN)); then
    detail "[dry-run] $*"
  else
    "$@"
  fi
}

# --- targets.conf ---------------------------------------------------------

T_CLI=()
T_SCOPE=()
T_KIND=()
T_DEST=()
T_BIN=()

load_targets() {
  [[ -f "$TARGETS_FILE" ]] || die "no existe $TARGETS_FILE"
  local line cli scope kind dest bin extra lineno=0
  T_CLI=() T_SCOPE=() T_KIND=() T_DEST=() T_BIN=()
  while IFS= read -r line || [[ -n "$line" ]]; do
    lineno=$((lineno + 1))
    line="${line%%#*}"
    cli="" scope="" kind="" dest="" bin="" extra=""
    read -r cli scope kind dest bin extra <<<"$line" || true
    [[ -z "$cli" ]] && continue
    [[ -n "$dest" && -z "$extra" ]] ||
      die "targets.conf:$lineno: se esperan 4 o 5 columnas (cli scope kind destination [binary])"
    case "$scope" in
      project | user) ;;
      *) die "targets.conf:$lineno: scope inválido '$scope' (project | user)" ;;
    esac
    case "$kind" in
      native | skills-symlink | instructions-import | gemini-settings) ;;
      *) die "targets.conf:$lineno: kind desconocido '$kind'" ;;
    esac
    T_CLI+=("$cli")
    T_SCOPE+=("$scope")
    T_KIND+=("$kind")
    T_DEST+=("$dest")
    T_BIN+=("${bin:--}")
  done <"$TARGETS_FILE"
}

# Convierte el destino de targets.conf en ruta absoluta.
expand_dest() {
  local dest="$1"
  # shellcheck disable=SC2088 # "~" literal de targets.conf; se expande a mano.
  case "$dest" in
    "~") printf '%s\n' "$HOME" ;;
    "~/"*) printf '%s\n' "$HOME/${dest#"~/"}" ;;
    /*) printf '%s\n' "$dest" ;;
    *) printf '%s\n' "$REPO_ROOT/$dest" ;;
  esac
}

# --- Skills fuente --------------------------------------------------------

# Lista los nombres de carpeta de .agents/skills que contienen SKILL.md.
list_skills() {
  local dir
  [[ -d "$SKILLS_DIR" ]] || return 0
  for dir in "$SKILLS_DIR"/*/; do
    [[ -f "${dir}SKILL.md" ]] || continue
    dir="${dir%/}"
    printf '%s\n' "${dir##*/}"
  done
}

# --- Symlinks -------------------------------------------------------------

# Verdadero si el symlink resuelve (aunque esté colgante) dentro de .agents/skills de este repo.
is_managed_link() {
  [[ -L "$1" ]] || return 1
  [[ "$(realpath -m -- "$1")" == "$SKILLS_REAL/"* ]]
}

# Verdadero si el symlink apunta a <algo>/.agents/skills/<name> de otro checkout.
is_managed_elsewhere() {
  local path="$1" name="$2" target
  [[ -L "$path" ]] || return 1
  target="$(readlink -- "$path")"
  target="${target%/}"
  [[ "$target" == */.agents/skills/"$name" || "$target" == .agents/skills/"$name" ]]
}

# Estado de destination/<name> respecto a la skill fuente <name>:
# ok | missing | stale | conflict-managed | conflict-symlink | conflict-real
classify_entry() {
  local path="$1" name="$2" resolved
  if [[ ! -e "$path" && ! -L "$path" ]]; then
    echo missing
  elif [[ -L "$path" ]]; then
    resolved="$(realpath -m -- "$path")"
    if [[ "$resolved" == "$SKILLS_REAL/$name" && -f "$resolved/SKILL.md" ]]; then
      echo ok
    elif [[ "$resolved" == "$SKILLS_REAL/"* ]]; then
      echo stale
    elif is_managed_elsewhere "$path" "$name"; then
      echo conflict-managed
    else
      echo conflict-symlink
    fi
  else
    echo conflict-real
  fi
}

# Target del symlink: relativo en project, absoluto en user.
link_target() {
  local dest_dir="$1" name="$2" scope="$3"
  if [[ "$scope" == project ]]; then
    realpath -m --relative-to="$(realpath -m -- "$dest_dir")" -- "$SKILLS_REAL/$name"
  else
    printf '%s\n' "$SKILLS_REAL/$name"
  fi
}

# --- Directorios creados por la tooling -----------------------------------

is_recorded_dir() {
  local dir="$1" line
  [[ -f "$STATE_FILE" ]] || return 1
  while IFS= read -r line; do
    [[ "$line" == "$dir" ]] && return 0
  done <"$STATE_FILE"
  return 1
}

record_created_dir() {
  is_recorded_dir "$1" && return 0
  mkdir -p -- "$(dirname "$STATE_FILE")"
  printf '%s\n' "$1" >>"$STATE_FILE"
}

forget_created_dir() {
  local dir="$1" line tmp
  [[ -f "$STATE_FILE" ]] || return 0
  tmp="$(mktemp)"
  while IFS= read -r line; do
    [[ "$line" == "$dir" ]] || printf '%s\n' "$line"
  done <"$STATE_FILE" >"$tmp"
  cat -- "$tmp" >"$STATE_FILE"
  rm -f -- "$tmp"
}

# mkdir -p que recuerda qué componentes creó, para que unlink pueda limpiarlos.
ensure_dir() {
  local dir="$1" cur="$1" created
  local -a missing=()
  while [[ ! -d "$cur" ]]; do
    [[ ! -e "$cur" ]] || die "$(display_path "$cur") existe y no es un directorio"
    missing+=("$cur")
    cur="$(dirname "$cur")"
  done
  ((${#missing[@]})) || return 0
  run mkdir -p -- "$dir"
  if ((!DRY_RUN)); then
    for created in "${missing[@]}"; do record_created_dir "$created"; done
  fi
}

dir_is_empty() {
  [[ -d "$1" && -z "$(ls -A -- "$1")" ]]
}

# Borra hacia arriba los directorios vacíos que creó la tooling.
prune_created_dirs() {
  local dir="$1"
  while is_recorded_dir "$dir"; do
    if [[ ! -e "$dir" ]]; then
      forget_created_dir "$dir"
    elif [[ -L "$dir" ]] || ! dir_is_empty "$dir"; then
      return 0
    else
      run rmdir -- "$dir"
      ((DRY_RUN)) || forget_created_dir "$dir"
    fi
    dir="$(dirname "$dir")"
  done
}

# --- Archivo de instrucciones (@AGENTS.md) --------------------------------

has_import_line() {
  local file="$1" line
  [[ -f "$file" ]] || return 1
  while IFS= read -r line || [[ -n "$line" ]]; do
    [[ "${line%$'\r'}" == "$IMPORT_LINE" ]] && return 0
  done <"$file"
  return 1
}

ensure_import_line() {
  local file="$1"
  if [[ ! -e "$file" ]]; then
    ensure_dir "$(dirname "$file")"
    if ((DRY_RUN)); then
      detail "[dry-run] crear $(display_path "$file") con $IMPORT_LINE"
    else
      printf '%s\n%s\n' "$IMPORT_COMMENT" "$IMPORT_LINE" >"$file"
      detail "creado $(display_path "$file")"
    fi
  elif has_import_line "$file"; then
    detail "ok: $(display_path "$file") ya importa $IMPORT_LINE"
  elif ((DRY_RUN)); then
    detail "[dry-run] añadir $IMPORT_LINE al final de $(display_path "$file")"
  else
    if [[ -s "$file" && "$(tail -c1 -- "$file" | wc -l)" -eq 0 ]]; then
      printf '\n' >>"$file"
    fi
    printf '%s\n' "$IMPORT_LINE" >>"$file"
    detail "añadido $IMPORT_LINE a $(display_path "$file") (contenido previo intacto)"
  fi
}

# Quita solo las líneas exactas "@AGENTS.md"; el resto del archivo no cambia.
remove_import_line() {
  local file="$1" line tmp
  has_import_line "$file" || { detail "sin $IMPORT_LINE en $(display_path "$file")"; return 0; }
  if ((DRY_RUN)); then
    detail "[dry-run] quitar $IMPORT_LINE de $(display_path "$file")"
    return 0
  fi
  tmp="$(mktemp)"
  while IFS= read -r line || [[ -n "$line" ]]; do
    [[ "${line%$'\r'}" == "$IMPORT_LINE" ]] || printf '%s\n' "$line"
  done <"$file" >"$tmp"
  cat -- "$tmp" >"$file"
  rm -f -- "$tmp"
  detail "quitado $IMPORT_LINE de $(display_path "$file")"
}

# --- Gemini settings.json -------------------------------------------------

have_jq() {
  command -v "$JQ_BIN" >/dev/null 2>&1
}

gemini_has_agents() {
  local file="$1" content
  [[ -f "$file" ]] || return 1
  if have_jq; then
    "$JQ_BIN" -e '(.context.fileName // []) | if type == "array" then index("AGENTS.md") != null else . == "AGENTS.md" end' \
      "$file" >/dev/null 2>&1
  else
    content="$(<"$file")"
    [[ "$content" == *'"AGENTS.md"'* ]]
  fi
}

ensure_gemini_settings() {
  local file="$1" tmp
  if [[ ! -e "$file" ]]; then
    ensure_dir "$(dirname "$file")"
    if ((DRY_RUN)); then
      detail "[dry-run] crear $(display_path "$file") con context.fileName [\"AGENTS.md\"]"
    else
      printf '{\n  "context": {\n    "fileName": ["AGENTS.md"]\n  }\n}\n' >"$file"
      detail "creado $(display_path "$file")"
    fi
  elif gemini_has_agents "$file"; then
    detail "ok: $(display_path "$file") ya incluye AGENTS.md"
  elif ! have_jq; then
    warn "jq no disponible: añade \"AGENTS.md\" a context.fileName en $(display_path "$file") (sin cambios)"
  elif ! "$JQ_BIN" empty "$file" >/dev/null 2>&1; then
    warn "$(display_path "$file") no es JSON válido (sin cambios)"
  elif ((DRY_RUN)); then
    detail "[dry-run] añadir AGENTS.md a context.fileName en $(display_path "$file")"
  else
    tmp="$(mktemp)"
    "$JQ_BIN" '.context = ((.context // {}) | .fileName = (
        (.fileName // []) | (if type == "array" then . else [.] end)
        | if index("AGENTS.md") then . else . + ["AGENTS.md"] end))' "$file" >"$tmp"
    cat -- "$tmp" >"$file"
    rm -f -- "$tmp"
    detail "añadido AGENTS.md a context.fileName en $(display_path "$file") (resto intacto)"
  fi
}
