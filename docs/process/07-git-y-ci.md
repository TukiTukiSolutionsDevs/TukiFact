# Git y CI

**Todo commit, rama y PR lleva el ID del feature, CHG, BUG o ADR que lo justifica, y CI bloquea el merge si falla una prueba crítica, falta documentación obligatoria o se rompe un contrato compartido.** El formato exacto está en [traceability-schema.md §9](traceability-schema.md#9-git).

## Camino rápido

```bash
git switch -c feat/PAY-FEAT-001-register-payment
git commit -m "feat(PAY-FEAT-001): registra pago total en caja"
git commit -m "test(PAY-FEAT-001): cubre TEST-PAY-001-07 monto mayor al saldo"
# PR: "feat(PAY-FEAT-001): registrar pago" con la plantilla de PR
```

## Convenciones

| Elemento | Formato | Ejemplos válidos | Inválidos |
|---|---|---|---|
| Commit | `<type>(<ID>): <asunto>` | `feat(PAY-FEAT-001): registra pago`, `fix(BUG-017): evita doble cobro`, `refactor(CHG-004)!: separa pago parcial`, `docs: corrige enlace` | `feat: registra pago` (sin ID), `feat(payments): …` (scope no es ID), `Fix(BUG-017): …` |
| Título de PR | Igual que commit | `feat(PAY-FEAT-001): registrar pago` | `Registrar pago` |
| Rama | `<type>/<ID>-<slug>` | `feat/PAY-FEAT-001-register-payment`, `fix/BUG-017-double-charge` | `feature/pagos` |

- `type` ∈ `feat|fix|test|refactor|perf|docs|chore|build|ci|revert`.
- ID **obligatorio** para `feat|fix|test|refactor|perf`; opcional (o scope kebab-case) para el resto: `chore(deps): …`, `docs(REL-2026.10.1): registrar release`.
- `!` antes de `:` marca cambio incompatible (coincide con un bump `major`).
- Varios features en un PR: título con el CHG que los agrupa; si no hay CHG, divide el PR.

### Repos separados

| Repo | Contiene | Regla adicional |
|---|---|---|
| Padre (`marketjoya`) | `docs/` y tooling | Un PR que toca `docs/features/<mod>/<ID>-*` lleva ese ID o un CHG/BUG que lo liste, en el título o en algún commit (GIT-3, `trace lint-pr-scope` en CI) |
| `MarketjoyaBackend`, `MarketjoyaFront`, `MarketjoyaMobile` | Código y pruebas | Commits y título con el ID; el cuerpo del PR enlaza la carpeta del feature en el repo padre |

Reconstruir qué código implementa un feature: `git log --oneline --grep 'PAY-FEAT-001'` en cada repo de código, más `trace check --code backend=… front=… mobile=…` para las pruebas.

## Checklist de PR

Refleja la [plantilla de PR](../../.github/PULL_REQUEST_TEMPLATE.md):

- [ ] Título y commits con ID válido.
- [ ] Feature en el estado correcto, movido con `trace promote <ID> --to <estado>` (no a mano).
- [ ] Documentación del feature actualizada en el mismo PR (o PR hermano enlazado en el repo padre).
- [ ] TEST nuevos con token y ruta en `Automatización`.
- [ ] CI verde; evidencias enlazadas si el PR cierra validación.
- [ ] Dependientes y regresión revisados (`impact`).
- [ ] Contrato OpenAPI, contrato de errores y migraciones revisados si aplica.

## CI por repositorio

Cada app es un repo Git propio. El repo padre valida documentación; los hijos validan código y lint de mensajes.

| Repo | Workflow | Qué ejecuta | Bloquea si |
|---|---|---|---|
| Padre (`marketjoya`) | `.github/workflows/docs-traceability.yml` | Pruebas del tooling; `trace check`; `trace build --check`; en PRs `trace lint-pr` sobre el título y `trace lint-pr-scope` con `git diff --name-only base...head` y `git log --format=%B base..head` (checkout con `fetch-depth: 0`) | Error de esquema, IDs duplicados o sin resolver, puerta del estado actual incumplida, matriz desactualizada, título inválido, feature tocado sin su ID ni un CHG/BUG que lo liste (GIT-3) |
| `MarketjoyaBackend` | `.github/workflows/backend-ci.yml` | Job `conventions` (lint de título y commits del PR); `dotnet format --verify-no-changes`, `dotnet build -c Release`, `git diff --exit-code -- openapi/`, `dotnet test` con cobertura y `--report-trx`, artefacto `test-evidence` (TRX + Cobertura, 90 días), paquetes vulnerables, imagen Docker | Cualquier paso falla |
| `MarketjoyaFront` | `.github/workflows/front-ci.yml` | Job `conventions`; `npm ci`, `npm run verify` (+ JUnit como artefacto `unit-test-results`); `npm run e2e` con artefacto `e2e-evidence`. Requiere el secret `DESIGN_SYSTEM_REPO_TOKEN` (token fine-grained `contents: read` sobre el repo padre) porque `tokens:check` lee `design-system/` | Cualquier paso falla |
| `MarketjoyaMobile` | `.github/workflows/mobile-ci.yml` | Job `conventions`; checkout parcial de `design-system/` (secret `DESIGN_SYSTEM_REPO_TOKEN`) pasado con `-Pmarketjoya.designSystemDir`; JDK 21 + 25 (daemon); `./gradlew verify` y `assembleDebug` de ambas apps; artefactos `test-evidence` (JUnit, 90 días) y `quality-reports`. Instrumentados y Maestro fuera de CI hasta tener estrategia de emulador | Cualquier paso falla |

### Reglas de bloqueo

| Regla | Cómo se detecta |
|---|---|
| Prueba crítica falla | Suite roja en el repo; TEST con `falla` impide G-APPROVED |
| Falta documentación obligatoria | `trace check` / `trace gate` en el repo padre |
| Contrato OpenAPI roto | `git diff --exit-code -- openapi/` y `CommittedOpenApiDocumentTests` (backend) |
| Contrato de errores roto | Suites E2E de errores (backend) y pruebas de mapper (front, mobile) según §7 del [contrato](../../.agents/skills/backend-architecture/references/api-error-contract.md#7-pruebas-mínimas-por-plataforma) |
| Migración de base de datos | Tests de integración aplican migraciones por colección; cambios de modelo sin migración rompen el build (ver `MarketjoyaBackend/docs/migrations.md`) |
| Arquitectura | NetArchTest, `lint:architecture`, Konsist |
| Mensaje de commit o título inválido | Paso de lint de esta página |

## Lint de títulos y commits en repos hijos

Paso autocontenido en bash, sin depender del tooling del repo padre. Nota: bash no soporta `\d`; la regex usa `[0-9]`.

```yaml
      - name: Lint PR title and commits (traceability-schema §9)
        if: github.event_name == 'pull_request'
        env:
          PR_TITLE: ${{ github.event.pull_request.title }}
          BASE_SHA: ${{ github.event.pull_request.base.sha }}
          HEAD_SHA: ${{ github.event.pull_request.head.sha }}
        shell: bash
        run: |
          export LC_ALL=C
          TYPES='feat|fix|test|refactor|perf|docs|chore|build|ci|revert'
          IDS='[A-Z]{2,8}-FEAT-[0-9]{3}|CHG-[0-9]{3}|BUG-[0-9]{3}|ADR-[0-9]{3}'
          REL='REL-[0-9]{4}\.[0-9]{2}\.[0-9]+'
          FORMAT="^(${TYPES})(\((${IDS}|${REL}|[a-z0-9-]+)\))?!?: .+$"
          NEEDS_ID='^(feat|fix|test|refactor|perf)[(!:]'
          WITH_ID="^(feat|fix|test|refactor|perf)\((${IDS})\)!?: .+$"
          failed=0
          lint() {
            if ! [[ "$1" =~ $FORMAT ]]; then
              echo "::error::Formato inválido: $1"; failed=1; return
            fi
            if [[ "$1" =~ $NEEDS_ID ]] && ! [[ "$1" =~ $WITH_ID ]]; then
              echo "::error::ID obligatorio para feat|fix|test|refactor|perf: $1"; failed=1
            fi
          }
          lint "$PR_TITLE"
          while IFS= read -r subject; do
            lint "$subject"
          done < <(git log --no-merges --format=%s "$BASE_SHA..$HEAD_SHA")
          exit "$failed"
```

Requisito: el paso `Checkout` del workflow usa `fetch-depth: 0` para que existan los commits del rango. Los títulos llegan por variable de entorno, nunca interpolados en el script.

## Publicar resultados como evidencia

Agregar al final del job, con `if: ${{ !cancelled() }}` para publicar también cuando falla:

| Repo | Artefacto | Ruta |
|---|---|---|
| Backend | Cobertura (ya existe) y resultados TRX | `TestResults/**/*.cobertura*.xml`, `TestResults/**/*.trx` |
| Front | Reporte Playwright y resultados | `tmp/playwright/report/`, `tmp/playwright/test-results/` |
| Mobile | Resultados JUnit y reportes | `**/build/test-results/**/*.xml`, `**/build/reports/tests/` |

```yaml
      - name: Upload test results (QA evidence)
        if: ${{ !cancelled() }}
        uses: actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a # v7.0.1
        with:
          name: test-results-${{ github.sha }}
          path: |
            TestResults/**/*.trx
          if-no-files-found: warn
          retention-days: 90
```

- Backend: TRX con Microsoft.Testing.Platform vía `Microsoft.Testing.Extensions.TrxReport` (en los proyectos `*Tests`) y `dotnet test --report-trx --results-directory TestResults` (implementado en `backend-ci.yml`).
- La URL de la ejecución (`https://github.com/<org>/<repo>/actions/runs/<id>`) se registra en la tabla `Evidencias`; lo aprobado se descarga a `evidence/` porque el artefacto caduca.

## Release y `released_in`

1. Se elige el nombre `REL-YYYY.MM.N` (`N` = número correlativo de la release en el mes, desde 1) y se fija `target_release` en los features que viajan juntos (obligatorio para aprobar con una dependencia aún en validación, G-APPROVED 13).
2. QA deja cada feature en `approved` con `trace promote <ID> --to approved`.
3. Se crea el tag anotado con ese nombre en cada repo liberado, sobre el commit desplegado: `git tag -a REL-2026.10.1 -m "REL-2026.10.1"`.
4. En el repo padre, un PR `docs(REL-2026.10.1): registrar release` que:
   - agrega la fila `REL-2026.10.1` a la tabla `Releases` de `docs/product/RELEASES.md` (se crea desde la [plantilla](templates/RELEASES.md) la primera vez) con fecha, repos y tags, features y CHG;
   - `released_in: REL-2026.10.1` en `FEATURE.md` de cada feature incluido;
   - `Liberado en: REL-2026.10.1` en la entrada vigente de `CHANGELOG.md`;
   - `released_in: REL-2026.10.1` en cada `CHG`.
5. `trace promote <ID> --to released` en cada feature (G-RELEASED 15, escribe `status`), `trace promote CHG-### --to released` en cada CHG (exige `released_in` en `RELEASES.md`) y `trace build`; CI del padre valida.
6. Tag `REL-2026.10.1` también en el repo padre, sobre el merge de ese PR.

> El `REL-…` se declara en `RELEASES.md`; el tag Git lo materializa en cada repo. Toda referencia a un `REL` (`released_in`, `target_release`, `Liberado en`) debe existir en esa tabla una vez creado el archivo.
