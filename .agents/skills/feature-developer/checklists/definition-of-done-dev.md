# Checklist: definición de terminado (desarrollo)

Todas las casillas antes de `trace promote <ID> --to ready-for-qa`.

## Alcance

- [ ] `trace promote <ID> --to in-progress` pasó antes del primer cambio de código.
- [ ] Todo `Archivos a crear o modificar` de `TECHNICAL-SPEC.md` hecho; nada fuera de la lista (o spec actualizada por el arquitecto).
- [ ] Cada AC de `FEATURE.md` implementado; ningún comportamiento extra.
- [ ] Desviaciones registradas como `Q-*` o `CHG-###`, no resueltas en silencio.

## Pruebas

- [ ] Cada `TEST-*` de su repo en `QA-PLAN.md` existe y contiene el token literal (schema §8):
  - .NET: `[Trait("TestId", "TEST-<MOD>-###-##")]`
  - Vitest/Playwright: `it('TEST-<MOD>-###-## …')`
  - Kotlin: nombre con backticks o `// TEST-<MOD>-###-##` en la línea anterior
  - Maestro: `# TEST-<MOD>-###-##` en la cabecera
- [ ] Tests escritos primero (rojo) y en verde ahora.
- [ ] Nivel y proyecto de test según la skill de testing de la plataforma.
- [ ] Casos negativos, límites y permisos del QA-PLAN automatizados en su nivel.
- [ ] Ningún test con esperas fijas, orden dependiente, `skip` o reintentos para esconder inestabilidad.
- [ ] `Automatización` en `QA-PLAN.md` con la ruta relativa al repo de cada test.

## Calidad técnica

- [ ] Capas y dependencias según la skill de arquitectura (architecture tests en verde).
- [ ] Errores con `code` del contrato de errores; ProblemDetails en HTTP.
- [ ] Autorización en backend; sin secretos en clientes.
- [ ] Endpoints presentes en `MarketjoyaBackend/openapi/marketjoya-api-v1.json` (regenerado si cambió).
- [ ] Migraciones (EF / Room) incluidas y reversibles cuando `impact.migrations: true`.
- [ ] `X-Correlation-Id` propagado en flujos nuevos.
- [ ] UI solo con componentes y tokens del design system.

## Verificación

- [ ] Backend: `dotnet format --verify-no-changes && dotnet build -c Release && dotnet test`
- [ ] Front: `npm run verify`
- [ ] Mobile: `./gradlew verify`
- [ ] `trace check --code backend=MarketjoyaBackend front=MarketjoyaFront mobile=MarketjoyaMobile` sin errores.
- [ ] `trace build` sin diferencias pendientes de commitear.

## Documentación y git

- [ ] `CHANGELOG.md`: entrada vigente con `Archivos técnicos`, `Pruebas`, `Riesgos`; `Validación` sigue `pendiente`.
- [ ] Commits `<type>(<ID>): <asunto>` validados con `trace lint-commit`.
- [ ] Rama `<type>/<ID>-<slug>`; título de PR validado con `trace lint-pr`.
- [ ] PR con IDs, puertas evaluadas y resultado de verificación.
- [ ] G-QA: toda fila no manual de `Casos de prueba` con ruta en `Automatización`; luego `trace promote <ID> --to ready-for-qa`.
