# Automatización de pruebas

**Cada `AC-…` se demuestra con al menos un `TEST-…` automatizado en el nivel más bajo que prueba la regla de negocio, más un e2e si el flujo es crítico.** La cobertura se mide por reglas y riesgos (dinero, stock, caja, SUNAT, sync), no por porcentaje. Cómo escribir cada prueba lo define la skill de testing de cada repo; este documento define qué debe probar y cómo se traza.

## Camino rápido

1. Durante `analysis`, QA declara el TEST en `QA-PLAN.md` → `Casos de prueba` (criterios, tipo, nivel, repo) y deja el plan en `status: ready` (G-READY 5).
2. El desarrollador escribe la prueba siguiendo la skill del repo e incluye el token `TEST-…` (tabla más abajo).
3. Rellena la columna `Automatización` con la ruta relativa al repo.
4. CI verde en el repo; `trace check --code backend=… front=… mobile=…` confirma que la ruta contiene el token.

## Niveles × repositorios

| Nivel (`Nivel`) | Backend (`backend-testing`) | Frontend (`frontend-testing`) | Mobile (`mobile-testing`) | Qué debe demostrar |
|---|---|---|---|---|
| `domain` / `unit` | xUnit v3 (MTP) sobre agregados y VO, sin IO | Vitest sobre `base`/`core` con fakes de puerto | JUnit4 JVM sobre dominio y UseCase con fakes de `:core:testing` | Cada regla `BR-…`, límites, invariantes, cálculos |
| `integration` | Caso de uso vía `ISender` + Postgres en Testcontainers | Service/store con facade fake | ViewModel con Turbine + `MainDispatcherRule` | El caso de uso completo produce el efecto y los errores (`type` + `code`) |
| `database` / `migration` | Integración de persistencia; migraciones aplicadas por colección | — | Room in-memory, contrato de DAO (`androidTest`) | Persistencia, restricciones, compatibilidad expand/contract |
| `contract` | Drift de OpenAPI (`CommittedOpenApiDocumentTests`), ProblemDetails | Adapter HTTP con `HttpClient` real + `HttpTestingController` | Adapter HTTP con MockWebServer | Forma del contrato y tabla del [contrato de errores §7](../../.agents/skills/backend-architecture/references/api-error-contract.md#7-pruebas-mínimas-por-plataforma) |
| `messaging` / `cross-module` | Consumers, outbox/inbox Wolverine + RabbitMQ en Testcontainers | — | Outbox y `client_mutation_id` (mismo ID ×2 = un efecto) | Eventos producidos/consumidos, idempotencia, reintentos |
| `component` | — | Componentes y guards con TestBed mínimo | Compose UI con `createComposeRule` + `testTag` | Estados visibles, habilitado/deshabilitado, mensajes |
| `e2e` | `WebApplicationFactory` en `Marketjoya.Api.E2ETests` (auth, status, ProblemDetails) | Playwright en `e2e/<flujo>/` con `getByTestId` | Maestro en `maestro/` | Recorrido de usuario del flujo crítico |
| Arquitectura (sin TEST por AC) | NetArchTest en `Marketjoya.ArchitectureTests` | `npm run lint:architecture` | Konsist en `:architecture-tests` | Fronteras de capa y módulo |

`Tipo` (`positivo`, `negativo`, `limite`, `permisos`, `concurrencia`, etc.; valores ASCII sin tilde) es independiente del nivel: un AC de pago parcial suele tener TEST `positivo` y `limite` en `domain`, `negativo` en `integration` y un `e2e` del camino feliz.

### Qué exige cada tipo de AC

| El AC trata de… | Mínimo exigido |
|---|---|
| Cálculo o regla de negocio | `domain` `positivo` + `limite` + `negativo` |
| Validación de entrada | `integration` con `FieldError` (`field` + `code`) |
| Permiso o rol | `integration` o `e2e` con tipo `permisos` por rol permitido y denegado |
| Estado o transición | `domain` por transición permitida y prohibida |
| Mensaje de error al usuario | `component` o `e2e` mostrando el mensaje por `code` |
| Evento o notificación | `messaging` |
| Offline / sincronización | `integration` mobile con outbox + `messaging` backend idempotente |
| Integración externa (SUNAT, OCR, GPS) | `integration` con el puerto sustituido (NSubstitute solo aquí) + `manual` en entorno de QA |

## Comportamientos que un e2e debe cubrir

Por cada flujo crítico, marca lo que aplica y declara un TEST por ítem o agrupado por AC:

- [ ] Navegación: entrada al flujo, retroceso, deep link si existe.
- [ ] Formularios: carga inicial, edición, envío.
- [ ] Validaciones: mensaje en el campo, nunca solo color.
- [ ] Cambios de estado visibles tras la acción.
- [ ] Mensajes de error por `code` y referencia `correlationId` en fallas genéricas.
- [ ] Roles: usuario autorizado completa; no autorizado no ve o recibe `FORBIDDEN`.
- [ ] Confirmaciones antes de acciones irreversibles.
- [ ] Cancelaciones sin efecto persistido.
- [ ] Reintentos: doble clic o reenvío no duplican (idempotencia).
- [ ] Actualización de datos en otras vistas afectadas.
- [ ] Recarga: el estado persiste o se recupera correctamente.

## Token del TEST por stack

| Stack | Forma | Ejemplo |
|---|---|---|
| .NET xUnit | `[Trait("TestId", "TEST-…")]` y/o `DisplayName` | `[Fact(DisplayName = "TEST-PAY-001-01 registra pago total")]` + `[Trait("TestId", "TEST-PAY-001-01")]` |
| Vitest / Playwright | Nombre del `it`/`test` | `it('TEST-PAY-001-01 registra pago total', …)` |
| Kotlin JUnit / Compose | Nombre con backticks o comentario en la línea anterior | `` fun `TEST-PAY-001-01 registra pago total`() `` |
| Maestro | Comentario en la cabecera del flujo | `# TEST-PAY-001-01` |

- Un TEST puede estar en un solo test o en varios (parametrizados), pero la ruta de `Automatización` apunta al archivo principal.
- El nombre del test sigue además la convención de la skill del repo.
- Código de producción: sin tokens.

## Independencia de QA

- [ ] QA diseña los casos a partir de `FEATURE.md`, no del código.
- [ ] El desarrollador puede implementar los TEST, pero QA revisa que prueben el AC y no la implementación.
- [ ] QA ejecuta la validación en un entorno distinto al del desarrollador.
- [ ] Un TEST que falla se reporta como `falla` + `BUG`, nunca se ajusta el AC ([05-gestion-de-cambios.md](05-gestion-de-cambios.md#independencia-de-qa)).
- [ ] Assertions de error por `type` + `code`, nunca por `description`.

## Política de pruebas inestables (flaky)

| Regla | Detalle |
|---|---|
| Inestable = defecto | Se abre `BUG-###` con `source: ci` y el TEST pasa a `bloqueado` |
| Cuarentena | Solo con BUG abierto; el TEST sigue declarado y el feature no aprueba (G-APPROVED 9) |
| Prohibido | Reintentos automáticos para llegar a verde, `Thread.Sleep`, `waitForTimeout`, `delay` real, orden entre tests |
| Causas a revisar primero | Datos compartidos entre tests, reloj real, esperas fijas, contenedores recreados por test |
| Salida | Arreglo verificado con 3 ejecuciones consecutivas verdes en CI; BUG `verified` |

## Evidencias

| Regla | Detalle |
|---|---|
| Obligatoria | Por cada TEST `e2e` automatizado y por cada TEST `manual` (G-APPROVED 11) |
| Declaración | Fila en `Evidencias`: `\| EVID-… \| TEST-… \| archivo o enlace \| fecha \|` |
| Formatos | `evidence/EVID-<MOD>-###-##.md\|png\|json\|txt` o URL del artefacto de CI |
| Contenido mínimo | Entorno, build/commit, fecha, resultado, pasos (manual), captura o reporte |
| Datos sensibles | Sin credenciales, tokens, PAN/CVV ni datos personales reales; enmascarar |
| Enlaces de CI | Deben apuntar a una ejecución concreta (no a "último build"); los artefactos de CI caducan, por eso lo aprobado se descarga a `evidence/` |

Artefactos de CI que sirven como evidencia: [07-git-y-ci.md](07-git-y-ci.md#publicar-resultados-como-evidencia).
