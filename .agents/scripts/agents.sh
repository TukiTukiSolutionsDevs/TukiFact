#!/usr/bin/env bash
# Instalador CLI-agnóstico de skills: .agents/skills es la fuente de verdad y
# targets.conf declara cómo la ve cada CLI (nativo, symlinks o archivos de instrucciones).
#
# Uso: .agents/scripts/agents.sh <comando> [opciones]
# Ver .agents/README.md o `agents.sh --help`.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$(realpath -- "${BASH_SOURCE[0]}")")" && pwd -P)"
# shellcheck source=lib.sh
source "$SCRIPT_DIR/lib.sh"

SKILL_NAME_RE='^[a-z0-9]+(-[a-z0-9]+)*$'
REQUIRED_SECTIONS=("Activation Contract" "Hard Rules" "Decision Gates" "Execution Steps" "Output Contract" "References")

COMMAND=""
FORCE=0
CLI_FILTER="all"
SCOPE="project"
PURGE_INSTRUCTIONS=0
ARGS=()
SKILLS=()
declare -A SOURCE_SET=()

usage() {
  cat <<'EOF'
Uso: agents.sh <comando> [opciones]

Comandos:
  link               Instala los adaptadores de targets.conf (idempotente).
  unlink             Quita solo los symlinks que apuntan a .agents/skills de este repo.
  status             Estado por CLI: binario instalado y adaptador (ok/missing/stale/conflict).
  doctor             status + symlinks colgantes, skills obsoletas y duplicados de descubrimiento.
  lint [skill...]    Valida frontmatter, enlaces relativos y referencias prohibidas.
  new-skill <name>   Crea una skill con la estructura de la guía de estilo y la valida.

Opciones:
  --cli <lista|all>      CLIs separadas por coma (por defecto: all).
  --scope project|user   Ámbito de targets.conf (por defecto: project).
  --dry-run              Muestra lo que haría sin modificar nada.
  --force                Permite reemplazar un symlink de otro checkout de .agents/skills.
                         Nunca borra directorios ni archivos reales.
  --purge-instructions   Con unlink: quita la línea exacta @AGENTS.md de los archivos de instrucciones.
  -h, --help             Muestra esta ayuda.
EOF
}

parse_args() {
  while (($#)); do
    case "$1" in
      --cli)
        (($# >= 2)) || die "--cli requiere un valor" 2
        CLI_FILTER="$2"
        shift 2
        continue
        ;;
      --cli=*) CLI_FILTER="${1#*=}" ;;
      --scope)
        (($# >= 2)) || die "--scope requiere un valor" 2
        SCOPE="$2"
        shift 2
        continue
        ;;
      --scope=*) SCOPE="${1#*=}" ;;
      --dry-run) DRY_RUN=1 ;;
      --force) FORCE=1 ;;
      --purge-instructions) PURGE_INSTRUCTIONS=1 ;;
      -h | --help)
        usage
        exit 0
        ;;
      -*) die "opción desconocida: $1" 2 ;;
      *)
        if [[ -z "$COMMAND" ]]; then
          COMMAND="$1"
        else
          ARGS+=("$1")
        fi
        ;;
    esac
    shift
  done
  case "$SCOPE" in
    project | user) ;;
    *) die "--scope inválido '$SCOPE' (project | user)" 2 ;;
  esac
}

# --- Selección de adaptadores ---------------------------------------------

validate_cli_filter() {
  [[ "$CLI_FILTER" == all ]] && return 0
  local cli known i
  local -a wanted=()
  IFS=, read -ra wanted <<<"$CLI_FILTER"
  ((${#wanted[@]})) || die "--cli vacío" 2
  for cli in "${wanted[@]}"; do
    known=0
    for i in "${!T_CLI[@]}"; do
      [[ "${T_CLI[i]}" == "$cli" ]] && known=1
    done
    ((known)) || die "CLI desconocida en targets.conf: '$cli'" 2
  done
}

cli_selected() {
  [[ "$CLI_FILTER" == all ]] && return 0
  local cli
  local -a wanted=()
  IFS=, read -ra wanted <<<"$CLI_FILTER"
  for cli in "${wanted[@]}"; do
    [[ "$cli" == "$1" ]] && return 0
  done
  return 1
}

target_selected() {
  [[ "${T_SCOPE[$1]}" == "$SCOPE" ]] && cli_selected "${T_CLI[$1]}"
}

prepare() {
  load_targets
  validate_cli_filter
  mapfile -t SKILLS < <(list_skills)
  SOURCE_SET=()
  local name
  for name in "${SKILLS[@]}"; do SOURCE_SET["$name"]=1; done
  ((DRY_RUN)) && info "modo dry-run: no se modifica nada"
  return 0
}

binary_state() {
  local bin="$1"
  if [[ "$bin" == - ]]; then
    echo "n/a"
  elif command -v "$bin" >/dev/null 2>&1; then
    # Sin tilde: printf alinea por bytes y un carácter multibyte descuadra la tabla.
    echo "si"
  else
    echo "no"
  fi
}

# --- link -----------------------------------------------------------------

LINK_CONFLICTS=0

link_skills() {
  local dest="$1" scope="$2" name path state target entry
  if ((${#SKILLS[@]} == 0)); then
    warn "no hay skills en $(display_path "$SKILLS_DIR")"
    return 0
  fi
  ensure_dir "$dest"
  for name in "${SKILLS[@]}"; do
    path="$dest/$name"
    target="$(link_target "$dest" "$name" "$scope")"
    state="$(classify_entry "$path" "$name")"
    case "$state" in
      ok) detail "ok       $name" ;;
      missing)
        run ln -s -- "$target" "$path"
        ((DRY_RUN)) || detail "enlazado $name -> $target"
        ;;
      stale)
        run rm -f -- "$path"
        run ln -s -- "$target" "$path"
        ((DRY_RUN)) || detail "corregido $name -> $target"
        ;;
      conflict-managed)
        if ((FORCE)); then
          run rm -f -- "$path"
          run ln -s -- "$target" "$path"
          ((DRY_RUN)) || detail "reemplazado (--force) $name -> $target"
        else
          warn "conflict: $(display_path "$path") apunta a otro checkout ($(readlink -- "$path")); usa --force"
          LINK_CONFLICTS=$((LINK_CONFLICTS + 1))
        fi
        ;;
      conflict-symlink)
        warn "conflict: $(display_path "$path") es un symlink ajeno ($(readlink -- "$path")); no se toca"
        LINK_CONFLICTS=$((LINK_CONFLICTS + 1))
        ;;
      conflict-real)
        warn "conflict: $(display_path "$path") es un directorio/archivo real; no se toca"
        LINK_CONFLICTS=$((LINK_CONFLICTS + 1))
        ;;
    esac
  done
  # Symlinks propios cuya skill ya no existe en la fuente.
  for entry in "$dest"/*; do
    name="${entry##*/}"
    [[ -z "${SOURCE_SET[$name]:-}" ]] || continue
    if is_managed_link "$entry"; then
      run rm -f -- "$entry"
      ((DRY_RUN)) || detail "eliminado stale $name (ya no existe en .agents/skills)"
    fi
  done
}

cmd_link() {
  prepare
  local i dest matched=0
  for i in "${!T_CLI[@]}"; do
    target_selected "$i" || continue
    matched=$((matched + 1))
    dest="$(expand_dest "${T_DEST[i]}")"
    info "${T_CLI[i]} (${T_SCOPE[i]}) ${T_KIND[i]}: $(display_path "$dest")"
    case "${T_KIND[i]}" in
      native) detail "nativo: la CLI lee .agents/skills; nada que instalar" ;;
      skills-symlink) link_skills "$dest" "${T_SCOPE[i]}" ;;
      instructions-import) ensure_import_line "$dest" ;;
      gemini-settings) ensure_gemini_settings "$dest" ;;
    esac
  done
  ((matched)) || warn "ninguna entrada de targets.conf coincide (cli=$CLI_FILTER, scope=$SCOPE)"
  if ((LINK_CONFLICTS)); then
    error "$LINK_CONFLICTS conflicto(s) sin resolver; revisa los WARN anteriores"
    return 1
  fi
  info "link completado"
}

# --- unlink ---------------------------------------------------------------

unlink_skills() {
  local dest="$1" entry removed=0
  if [[ ! -d "$dest" ]]; then
    detail "no existe; nada que quitar"
    return 0
  fi
  for entry in "$dest"/*; do
    if is_managed_link "$entry"; then
      run rm -f -- "$entry"
      removed=$((removed + 1))
    fi
  done
  detail "$removed symlink(s) propios quitados"
  prune_created_dirs "$dest"
}

cmd_unlink() {
  prepare
  local i dest
  for i in "${!T_CLI[@]}"; do
    target_selected "$i" || continue
    dest="$(expand_dest "${T_DEST[i]}")"
    info "${T_CLI[i]} (${T_SCOPE[i]}) ${T_KIND[i]}: $(display_path "$dest")"
    case "${T_KIND[i]}" in
      native) detail "nativo: nada que quitar" ;;
      skills-symlink) unlink_skills "$dest" ;;
      instructions-import)
        if ((PURGE_INSTRUCTIONS)); then
          remove_import_line "$dest"
        else
          detail "se conserva (usa --purge-instructions para quitar $IMPORT_LINE)"
        fi
        ;;
      gemini-settings) detail "se conserva; unlink no modifica settings.json" ;;
    esac
  done
  info "unlink completado"
}

# --- status / doctor ------------------------------------------------------

# Resultado de scan_symlink_dest.
S_OK=0 S_MISSING=0 S_STALE=0 S_CONFLICT=0 S_DANGLING=0

# Revisa destination contra la fuente; con verbose=1 lista cada problema.
scan_symlink_dest() {
  local dest="$1" verbose="$2" name path state entry
  S_OK=0 S_MISSING=0 S_STALE=0 S_CONFLICT=0 S_DANGLING=0
  for name in "${SKILLS[@]}"; do
    path="$dest/$name"
    state="$(classify_entry "$path" "$name")"
    case "$state" in
      ok) S_OK=$((S_OK + 1)) ;;
      missing)
        S_MISSING=$((S_MISSING + 1))
        ((verbose)) && detail "missing   $name"
        ;;
      stale)
        S_STALE=$((S_STALE + 1))
        ((verbose)) && detail "stale     $name -> $(readlink -- "$path")"
        ;;
      conflict-*)
        S_CONFLICT=$((S_CONFLICT + 1))
        ((verbose)) && detail "conflict  $name (${state#conflict-})"
        ;;
    esac
  done
  [[ -d "$dest" ]] || return 0
  for entry in "$dest"/*; do
    name="${entry##*/}"
    [[ -L "$entry" ]] || continue
    if [[ -z "${SOURCE_SET[$name]:-}" ]] && is_managed_link "$entry"; then
      S_STALE=$((S_STALE + 1))
      ((verbose)) && detail "stale     $name (no existe en .agents/skills)"
    elif [[ ! -e "$entry" ]]; then
      S_DANGLING=$((S_DANGLING + 1))
      ((verbose)) && detail "dangling  $name -> $(readlink -- "$entry")"
    fi
  done
  return 0
}

# Imprime "estado|detalle" de un adaptador.
target_state() {
  local kind="$1" dest="$2" verbose="${3:-0}"
  case "$kind" in
    native)
      if ((${#SKILLS[@]})); then
        echo "ok|${#SKILLS[@]} skills en .agents/skills"
      else
        echo "missing|no hay skills en .agents/skills"
      fi
      ;;
    skills-symlink)
      scan_symlink_dest "$dest" "$verbose" >&2
      local summary="ok=$S_OK missing=$S_MISSING stale=$S_STALE conflict=$S_CONFLICT"
      if ((S_CONFLICT)); then
        echo "conflict|$summary"
      elif ((S_STALE)); then
        echo "stale|$summary"
      elif ((S_MISSING)); then
        echo "missing|$summary"
      else
        echo "ok|$summary"
      fi
      ;;
    instructions-import)
      if has_import_line "$dest"; then echo "ok|importa $IMPORT_LINE"; else echo "missing|sin $IMPORT_LINE"; fi
      ;;
    gemini-settings)
      if gemini_has_agents "$dest"; then echo "ok|context.fileName incluye AGENTS.md"; else echo "missing|sin AGENTS.md en context.fileName"; fi
      ;;
  esac
}

STATUS_PROBLEMS=0

print_status() {
  local verbose="$1" i dest result state info_text matched=0
  STATUS_PROBLEMS=0
  info "Estado (scope: $SCOPE, skills fuente: ${#SKILLS[@]})"
  printf '    %-9s %-4s %-20s %-9s %-24s %s\n' CLI BIN ADAPTADOR ESTADO DESTINO DETALLE
  for i in "${!T_CLI[@]}"; do
    target_selected "$i" || continue
    matched=$((matched + 1))
    dest="$(expand_dest "${T_DEST[i]}")"
    result="$(target_state "${T_KIND[i]}" "$dest" "$verbose" 2>/dev/null)"
    state="${result%%|*}"
    info_text="${result#*|}"
    printf '    %-9s %-4s %-20s %-9s %-24s %s\n' "${T_CLI[i]}" "$(binary_state "${T_BIN[i]}")" \
      "${T_KIND[i]}" "$state" "$(display_path "$dest")" "$info_text"
    case "$state" in
      conflict | stale) STATUS_PROBLEMS=$((STATUS_PROBLEMS + 1)) ;;
    esac
  done
  ((matched)) || warn "ninguna entrada de targets.conf coincide (cli=$CLI_FILTER, scope=$SCOPE)"
  return 0
}

cmd_status() {
  prepare
  print_status 0
}

# Ejecuta con timeout si está disponible.
with_timeout() {
  local seconds="$1"
  shift
  if command -v timeout >/dev/null 2>&1; then
    timeout "$seconds" "$@"
  else
    "$@"
  fi
}

# OpenCode y Cursor también leen .claude/skills: verifica si OpenCode lista duplicados.
probe_duplicate_discovery() {
  local claude_dest="$REPO_ROOT/.claude/skills" out line name count_total=0 dups=0 from_claude=0 from_agents=0
  local -A seen=()
  info "Descubrimiento duplicado (.agents/skills + .claude/skills)"
  if [[ ! -d "$claude_dest" ]]; then
    detail "sin .claude/skills en el proyecto: no aplica"
    return 0
  fi
  detail "Cursor también lee .claude/skills (gated); no verificable sin la CLI interactiva."
  if [[ -n "${AGENTS_SKIP_CLI_PROBES:-}" ]]; then
    detail "sondeo de CLIs omitido (AGENTS_SKIP_CLI_PROBES)"
    return 0
  fi
  if ! command -v opencode >/dev/null 2>&1; then
    detail "opencode no instalado: sondeo omitido"
    return 0
  fi
  out="$(mktemp)"
  # A archivo, no a pipe: opencode puede truncar la salida grande en un pipe.
  if ! (cd "$REPO_ROOT" && with_timeout 120 opencode debug skill >"$out" 2>/dev/null); then
    warn "opencode debug skill falló; sondeo omitido"
    rm -f -- "$out"
    return 0
  fi
  while IFS= read -r line; do
    if [[ "$line" =~ \"name\":\ \"([a-z0-9-]+)\" ]]; then
      name="${BASH_REMATCH[1]}"
      [[ -n "${SOURCE_SET[$name]:-}" ]] || continue
      seen["$name"]=$((${seen[$name]:-0} + 1))
      count_total=$((count_total + 1))
    elif [[ "$line" == *'"location":'*"$REPO_ROOT/.claude/skills/"* ]]; then
      from_claude=$((from_claude + 1))
    elif [[ "$line" == *'"location":'*"$REPO_ROOT/.agents/skills/"* ]]; then
      from_agents=$((from_agents + 1))
    fi
  done <"$out"
  rm -f -- "$out"
  for name in "${!seen[@]}"; do
    if ((seen[$name] > 1)); then
      dups=$((dups + 1))
      warn "OpenCode lista '$name' ${seen[$name]} veces"
    fi
  done
  detail "opencode: $count_total skills del repo (desde .agents/skills: $from_agents, desde .claude/skills: $from_claude)"
  if ((dups)); then
    warn "duplicados en OpenCode; mitigación: export OPENCODE_DISABLE_CLAUDE_CODE_SKILLS=1"
  else
    detail "opencode: sin duplicados (deduplica por nombre)"
  fi
  return 0
}

cmd_doctor() {
  prepare
  local i dest problems=0
  print_status 0
  problems=$STATUS_PROBLEMS
  for i in "${!T_CLI[@]}"; do
    target_selected "$i" || continue
    [[ "${T_KIND[i]}" == skills-symlink ]] || continue
    dest="$(expand_dest "${T_DEST[i]}")"
    info "Detalle ${T_CLI[i]}: $(display_path "$dest")"
    scan_symlink_dest "$dest" 1
    if ((S_DANGLING)); then
      problems=$((problems + 1))
    fi
    if ((S_OK + S_MISSING + S_STALE + S_CONFLICT + S_DANGLING == S_OK)); then
      detail "sin problemas"
    fi
  done
  if [[ "$SCOPE" == project ]] && cli_selected claude; then
    probe_duplicate_discovery
  fi
  if ((problems)); then
    error "doctor: $problems adaptador(es) con problemas (stale/conflict/dangling)"
    return 1
  fi
  info "doctor: sin problemas"
}

# --- lint -----------------------------------------------------------------

LINT_ERRORS=0
LINT_WARNINGS=0

lint_error() {
  error "$1: $2"
  LINT_ERRORS=$((LINT_ERRORS + 1))
}

lint_warn() {
  warn "$1: $2"
  LINT_WARNINGS=$((LINT_WARNINGS + 1))
}

trim() {
  local value="$1"
  value="${value#"${value%%[![:space:]]*}"}"
  value="${value%"${value##*[![:space:]]}"}"
  printf '%s' "$value"
}

lint_frontmatter() {
  local skill="$1" file="$2" i end=-1 line key="" in_meta=0
  local name="" desc="" license="" author="" version="" desc_multiline=0
  local -a lines=()
  mapfile -t lines <"$file"
  if [[ "${lines[0]:-}" != "---" ]]; then
    lint_error "$skill" "falta frontmatter YAML (--- al inicio de SKILL.md)"
    return 0
  fi
  for ((i = 1; i < ${#lines[@]}; i++)); do
    if [[ "${lines[i]%$'\r'}" == "---" ]]; then
      end=$i
      break
    fi
  done
  if ((end < 0)); then
    lint_error "$skill" "frontmatter sin cierre (---)"
    return 0
  fi

  for ((i = 1; i < end; i++)); do
    line="${lines[i]%$'\r'}"
    case "$line" in
      "" | "#"*) ;;
      " "* | $'\t'*)
        if ((in_meta)); then
          case "$(trim "$line")" in
            author:*) author="$(trim "${line#*author:}")" ;;
            version:*) version="$(trim "${line#*version:}")" ;;
          esac
        elif [[ "$key" == description ]]; then
          desc_multiline=1
        fi
        ;;
      name:*) key=name in_meta=0 name="$(trim "${line#name:}")" ;;
      description:*) key=description in_meta=0 desc="$(trim "${line#description:}")" ;;
      license:*) key=license in_meta=0 license="$(trim "${line#license:}")" ;;
      metadata:*) key=metadata in_meta=1 ;;
      type:*)
        key=type in_meta=0
        lint_error "$skill" "campo 'type' no permitido (Kimi solo acepta prompt/inline/flow)"
        ;;
      *) key="${line%%:*}" in_meta=0 ;;
    esac
  done

  if [[ -z "$name" ]]; then
    lint_error "$skill" "falta name"
  else
    [[ "$name" =~ $SKILL_NAME_RE && ${#name} -le 64 ]] ||
      lint_error "$skill" "name '$name' no cumple ^[a-z0-9]+(-[a-z0-9]+)*\$ (1-64)"
    [[ "$name" == "$skill" ]] || lint_error "$skill" "name '$name' distinto de la carpeta '$skill'"
  fi

  if [[ -z "$desc" ]]; then
    lint_error "$skill" "falta description"
  elif ((desc_multiline)) || [[ ! "$desc" =~ ^\"(.*)\"$ ]]; then
    lint_error "$skill" "description debe ir entre comillas dobles en una sola línea"
  else
    desc="${BASH_REMATCH[1]}"
    [[ "$desc" == "Trigger:"* ]] || lint_error "$skill" "description debe empezar con \"Trigger:\""
    if ((${#desc} > 250)); then
      lint_error "$skill" "description de ${#desc} caracteres (máximo 250)"
    elif ((${#desc} > 160)); then
      lint_warn "$skill" "description de ${#desc} caracteres (recomendado <= 160)"
    fi
  fi

  [[ -n "$license" ]] || lint_error "$skill" "falta license"
  [[ -n "$author" ]] || lint_error "$skill" "falta metadata.author"
  [[ -n "$version" ]] || lint_error "$skill" "falta metadata.version"
  return 0
}

lint_sections() {
  local skill="$1" file="$2" line section idx last=-1
  local -A position=()
  local n=0
  while IFS= read -r line || [[ -n "$line" ]]; do
    n=$((n + 1))
    [[ "$line" == "## "* ]] && position["$(trim "${line#"## "}")"]=$n
  done <"$file"
  for section in "${REQUIRED_SECTIONS[@]}"; do
    idx="${position[$section]:-}"
    if [[ -z "$idx" ]]; then
      lint_warn "$skill" "falta la sección '## $section'"
    elif ((idx < last)); then
      lint_warn "$skill" "sección '## $section' fuera de orden"
    else
      last=$idx
    fi
  done
}

lint_links() {
  local skill="$1" dir="$2" file line rest target rel in_fence content base
  local re='\]\(([^)[:space:]]+)([[:space:]]+"[^"]*")?\)'
  local -a files=()
  shopt -s globstar nullglob
  files=("$dir"/**/*.md)
  for file in "${files[@]}"; do
    rel="${file#"$dir/"}"
    base="$(dirname "$file")"
    in_fence=0
    while IFS= read -r line || [[ -n "$line" ]]; do
      if [[ "$line" =~ ^[[:space:]]*(\`\`\`|~~~) ]]; then
        in_fence=$((1 - in_fence))
        continue
      fi
      ((in_fence)) && continue
      rest="$line"
      while [[ "$rest" =~ $re ]]; do
        target="${BASH_REMATCH[1]}"
        rest="${rest#*"${BASH_REMATCH[0]}"}"
        target="${target#<}"
        target="${target%>}"
        case "$target" in
          http://* | https://* | mailto:* | "#"*) continue ;;
          /* | "~"*)
            lint_error "$skill" "$rel: enlace no relativo '$target' (usa rutas relativas locales)"
            continue
            ;;
        esac
        target="${target%%#*}"
        [[ -e "$base/$target" ]] ||
          lint_error "$skill" "$rel: enlace roto '$target'"
      done
    done <"$file"
  done
  for file in "$dir"/**; do
    [[ -f "$file" ]] || continue
    content="$(<"$file")"
    [[ "$content" != *".opencode/"* ]] ||
      lint_error "$skill" "${file#"$dir/"}: referencia a .opencode/ (usa rutas neutrales a la CLI)"
    [[ "$content" != *TODO* ]] || lint_warn "$skill" "${file#"$dir/"}: contiene TODO"
  done
  shopt -u globstar nullglob
}

lint_skill() {
  local skill="$1" dir="$SKILLS_DIR/$1"
  if [[ ! -d "$dir" ]]; then
    lint_error "$skill" "no existe $(display_path "$dir")"
    return 0
  fi
  [[ "$skill" =~ $SKILL_NAME_RE && ${#skill} -le 64 ]] ||
    lint_error "$skill" "nombre de carpeta inválido (^[a-z0-9]+(-[a-z0-9]+)*\$, 1-64)"
  if [[ ! -f "$dir/SKILL.md" ]]; then
    lint_error "$skill" "falta SKILL.md"
    return 0
  fi
  lint_frontmatter "$skill" "$dir/SKILL.md"
  lint_sections "$skill" "$dir/SKILL.md"
  lint_links "$skill" "$dir"
}

cmd_lint() {
  local skill dir
  local -a names=("$@")
  LINT_ERRORS=0 LINT_WARNINGS=0
  if ((${#names[@]} == 0)); then
    for dir in "$SKILLS_DIR"/*/; do
      [[ -d "$dir" ]] || continue
      dir="${dir%/}"
      names+=("${dir##*/}")
    done
  fi
  ((${#names[@]})) || warn "no hay skills en $(display_path "$SKILLS_DIR")"
  for skill in "${names[@]}"; do
    lint_skill "$skill"
  done
  info "lint: ${#names[@]} skill(s), $LINT_ERRORS error(es), $LINT_WARNINGS advertencia(s)"
  ((LINT_ERRORS == 0))
}

# --- new-skill ------------------------------------------------------------

cmd_new_skill() {
  local name="${1:-}" dir author
  [[ -n "$name" ]] || die "uso: agents.sh new-skill <name>" 2
  [[ "$name" =~ $SKILL_NAME_RE && ${#name} -le 64 ]] ||
    die "nombre inválido '$name': usa ^[a-z0-9]+(-[a-z0-9]+)*\$ (1-64 caracteres)" 2
  dir="$SKILLS_DIR/$name"
  [[ ! -e "$dir" ]] || die "ya existe $(display_path "$dir")"
  author="${AGENTS_SKILL_AUTHOR:-$(basename "$REPO_ROOT")}"
  if ((DRY_RUN)); then
    info "[dry-run] crear $(display_path "$dir/SKILL.md")"
    return 0
  fi
  mkdir -p -- "$dir"
  cat >"$dir/SKILL.md" <<EOF
---
name: $name
description: "Trigger: TODO palabras clave con plataforma. TODO qué hace la skill."
license: Apache-2.0
metadata:
  author: $author
  version: "1.0"
---

# TODO título

## Activation Contract

TODO: situaciones exactas que cargan esta skill.

## Hard Rules

- TODO: restricción verificable.

## Decision Gates

| Situación | Acción |
|---|---|
| TODO | TODO |

## Execution Steps

1. TODO: paso operativo.

## Output Contract

- TODO: qué entrega el agente.

## References

- TODO: enlaces relativos locales (references/, checklists/, templates/ o ../<skill>/SKILL.md).
EOF
  info "creada $(display_path "$dir/SKILL.md")"
  cmd_lint "$name"
  info "siguiente paso: completa los TODO y ejecuta agents.sh link"
}

main() {
  parse_args "$@"
  case "$COMMAND" in
    link) cmd_link ;;
    unlink) cmd_unlink ;;
    status) cmd_status ;;
    doctor) cmd_doctor ;;
    lint) cmd_lint "${ARGS[@]}" ;;
    new-skill) cmd_new_skill "${ARGS[@]}" ;;
    "")
      usage >&2
      exit 2
      ;;
    *) die "comando desconocido: $COMMAND (ver --help)" 2 ;;
  esac
}

main "$@"
