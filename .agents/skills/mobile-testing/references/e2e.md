# E2E Android (Maestro)

5–10 viajes críticos. No cada feature.

## Existente

| Flujo | appId | Aserta |
|---|---|---|
| `maestro/pos-smoke.yaml` | `pe.marketjoya.pos` | `id: "pos-root"` visible tras `launchApp: clearState: true` |
| `maestro/preventa-smoke.yaml` | `pe.marketjoya.preventa` | `id: "preventa-root"` visible |

Son smoke de arranque, no viajes de negocio.

## Viajes a añadir con sus features

- POS: venta con turno + ítem + confirmación; rechazo sin turno; pago Unknown → reconciliación.
- Preventa: pedido offline → outbox → sync → Synced; conflicto visible. API de test o stub: Pendiente de decisión.

## Cómo

- Maestro no viene instalado: se instala por desarrollador o imagen de CI (`maestro/README.md`).
- Ejecutar: `./gradlew :app-pos:installDebug :app-preventa:installDebug` y luego `maestro test maestro/<flujo>.yaml` con emulador o dispositivo.
- Selectores: `testTag` expuesto como resource id (`testTagsAsResourceId = true` en la raíz de la app).
- Datos únicos por ejecución. Sin periféricos reales. Permisos concedidos por el flujo.
- Separado de `./gradlew verify`. Artefactos (`.maestro/`, capturas, videos) ignorados por git.
- Espresso no se añade para un viaje que ya cubre Maestro.

Auth/caja: fixture de usuario de test, no datos de producción.
