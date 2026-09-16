# .agents — skills para cualquier CLI de agentes

`.agents/skills/<name>/SKILL.md` es la **única fuente de verdad** de las skills del repo. La mayoría de las CLIs la leen de forma nativa; para las demás, `scripts/agents.sh` crea adaptadores (symlinks o archivos de instrucciones) según `targets.conf`.

## Ruta rápida

```bash
.agents/scripts/agents.sh link      # instala los adaptadores de proyecto (idempotente)
.agents/scripts/agents.sh doctor    # verifica estado, symlinks colgantes y duplicados
.agents/scripts/agents.sh lint      # valida todas las skills
```

Resultado esperado: `.claude/skills/<name>` son symlinks relativos a `.agents/skills/<name>` y `CLAUDE.md` importa `AGENTS.md`.

## Estructura

| Ruta | Qué es |
|---|---|
| `skills/<name>/` | Skill real: `SKILL.md` + `references/`, `checklists/`, `templates/` |
| `targets.conf` | Adaptadores por CLI (declarativo) |
| `scripts/agents.sh` | Entrada: `link`, `unlink`, `status`, `doctor`, `lint`, `new-skill` |
| `scripts/lib.sh` | Helpers compartidos |
| `tests/run.sh` | Tests de la tooling (bash puro, repo y HOME falsos) |
| `.state/` | Directorios creados por `link` (ignorado por git) |

## Cómo la ve cada CLI

| CLI | Proyecto | Usuario (`--scope user`) | Instrucciones |
|---|---|---|---|
| Claude Code | `.claude/skills/<name>` → symlink relativo | `~/.claude/skills/<name>` → symlink absoluto | `CLAUDE.md` con `@AGENTS.md` |
| OpenCode | nativo (`.agents/skills`) | `~/.agents/skills` | `AGENTS.md` |
| Codex | nativo | `~/.agents/skills` | `AGENTS.md` |
| pi | nativo | `~/.agents/skills` | `AGENTS.md` |
| Cursor CLI | nativo | `~/.agents/skills` | `AGENTS.md` |
| Kimi Code | nativo | `~/.agents/skills` | `AGENTS.md` |
| Gemini CLI | nativo | `~/.agents/skills` | `.gemini/settings.json` → `context.fileName` incluye `AGENTS.md` |

Las skills reales viven físicamente en `.agents/skills`: Cursor descarta symlinks de skills cuyo destino está fuera de su raíz, así que solo Claude recibe symlinks.

## Comandos

| Comando | Comportamiento |
|---|---|
| `link` | Crea lo que falta, corrige symlinks propios obsoletos y elimina los de skills borradas. Nunca toca directorios o archivos reales; sale con código 1 si hubo conflictos. |
| `unlink` | Quita solo symlinks que resuelven dentro de `.agents/skills` de este repo y los directorios vacíos que creó `link`. `CLAUDE.md` se conserva salvo `--purge-instructions` (quita solo la línea exacta `@AGENTS.md`). `settings.json` no se modifica. |
| `status` | Por adaptador: binario instalado y estado `ok` / `missing` / `stale` / `conflict`. |
| `doctor` | `status` + detalle de `stale`, `conflict` y symlinks `dangling`; sondea `opencode debug skill` para detectar duplicados. Código 1 si hay problemas. |
| `lint [skill...]` | Frontmatter, nombre, `description` (`Trigger:`, una línea, ≤250), `license`, `metadata`, sin `type`, enlaces relativos existentes, sin `.opencode/`. Advierte si falta una sección o hay `TODO`. |
| `new-skill <name>` | Crea la estructura de la guía de estilo y ejecuta `lint`. |

Opciones: `--cli claude,gemini` (por defecto `all`), `--scope project|user`, `--dry-run`, `--force` (solo reemplaza symlinks que apuntan a `.agents/skills/<name>` de otro checkout).

## Agregar una skill

1. `.agents/scripts/agents.sh new-skill <name>` (`^[a-z0-9]+(-[a-z0-9]+)*$`).
2. Completa los `TODO`: `description` con `Trigger:` y la plataforma explícita, secciones en orden (Activation Contract, Hard Rules, Decision Gates, Execution Steps, Output Contract, References).
3. Enlaces locales relativos a la skill; rutas del pack como `design-system/...` (raíz del repo).
4. `agents.sh lint && agents.sh link`, y regístrala en `AGENTS.md`.

## Agregar una CLI

1. Agrega líneas en `targets.conf`: `cli scope kind destination binary`.
2. Elige `native` si la CLI ya lee `.agents/skills`; si no, `skills-symlink` hacia su carpeta de skills.
3. Si necesita un archivo de instrucciones, usa `instructions-import` (archivo con `@AGENTS.md`) o crea un kind nuevo en `scripts/` con su test en `tests/`.
4. `agents.sh status --cli <cli>` y `agents.sh doctor`.

## Garantías y límites

- Nunca borra directorios ni archivos reales, ni skills globales ajenas en `~/.claude/skills` o `~/.agents/skills`.
- Descubrimiento duplicado: OpenCode y Cursor también leen `.claude/skills`. `doctor` informa si OpenCode lista duplicados; mitigación: `export OPENCODE_DISABLE_CLAUDE_CODE_SKILLS=1`.
- Con `--scope user`, los enlaces relativos que salen de la skill (`../../..`) no resuelven desde `~/.claude/skills`; por eso las skills referencian el pack como `design-system/...` desde la raíz del repo.
- Requiere bash 4+ y GNU coreutils (`realpath -m`). `jq` es opcional (fusión de `.gemini/settings.json`).

## Tests

```bash
.agents/tests/run.sh
```

Cada test crea un repo y un `HOME` falsos con `mktemp -d`; `AGENTS_SKIP_CLI_PROBES=1` evita invocar CLIs reales.
