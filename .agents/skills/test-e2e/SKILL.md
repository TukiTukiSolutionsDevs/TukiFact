---
name: test-e2e
description: "Trigger: E2E sin plataforma clara, end-to-end, flujo crítico punta a punta. Enruta a backend-testing, frontend-testing o mobile-testing."
license: Apache-2.0
metadata:
  author: marketjoya
  version: "1.0"
---

# Router E2E — Market Real

## Activation Contract

Activa esta skill cuando la solicitud pida pruebas E2E o end-to-end y no quede claro si son del backend, del frontend o de mobile. Si la plataforma es explícita, carga directamente la skill de testing de esa plataforma.

## Hard Rules

- Esta skill solo enruta. No define reglas de testing propias.
- Las reglas E2E viven en la skill de testing de cada plataforma; esa skill manda.
- Un flujo que cruza plataformas se prueba por plataforma, cada una con su skill y su runner.
- Si la plataforma no se puede inferir de la solicitud ni del código tocado, pregunta antes de escribir tests.

## Decision Gates

| Señal | Plataforma | Skill | Reference E2E |
|---|---|---|---|
| Endpoint HTTP, `WebApplicationFactory`, `E2ETests`, ProblemDetails | Backend .NET | [backend-testing](../backend-testing/SKILL.md) | [presentation.md](../backend-testing/references/presentation.md) |
| Browser, Angular, Playwright, `data-testid` | Frontend | [frontend-testing](../frontend-testing/SKILL.md) | [e2e.md](../frontend-testing/references/e2e.md) |
| Android, POS/preventa, Maestro, emulador | Mobile | [mobile-testing](../mobile-testing/SKILL.md) | [e2e.md](../mobile-testing/references/e2e.md) |
| Ninguna señal | — | Preguntar la plataforma | — |

## Execution Steps

1. Busca señales de plataforma en la solicitud y en los archivos tocados.
2. Elige la fila de la tabla; si aplican varias plataformas, repite por cada una.
3. Carga la `SKILL.md` de la plataforma y su reference E2E antes de escribir o revisar tests.
4. Sigue los Execution Steps, el checklist E2E y el Output Contract de esa skill.

## Output Contract

- Plataforma(s) elegida(s) y la señal que la determinó.
- Skill(s) y reference(s) E2E cargadas.
- El resto de la salida la define la skill de la plataforma.

## References

- Backend: [../backend-testing/SKILL.md](../backend-testing/SKILL.md), checklist [presentation.md](../backend-testing/checklists/presentation.md), plantilla [presentation-endpoint.md](../backend-testing/templates/presentation-endpoint.md)
- Frontend: [../frontend-testing/SKILL.md](../frontend-testing/SKILL.md), checklist [e2e.md](../frontend-testing/checklists/e2e.md), plantilla [playwright-flow.md](../frontend-testing/templates/playwright-flow.md)
- Mobile: [../mobile-testing/SKILL.md](../mobile-testing/SKILL.md), checklist [e2e.md](../mobile-testing/checklists/e2e.md), plantilla [maestro-sale.md](../mobile-testing/templates/maestro-sale.md)
