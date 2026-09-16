# EVID-CASH-001-04 — Rendimiento de apertura en POS físico (manual)

> **Evidencia ilustrativa.** Medición ficticia; muestra cómo se documenta un TEST `manual`.

| Campo | Valor |
|---|---|
| TEST | TEST-CASH-001-15 |
| Entorno | POS Android de QA (API 30), red 4G, QA backend |
| Build / commit | app-pos `c90d3a4`, backend `b7e41c2` |
| Fecha | 2026-08-31 |
| Ejecutado por | qa (ejemplo) |
| Resultado | pasa |

Pasos:

1. Iniciar sesión con `cajero01`.
2. Abrir la caja con S/ 200.00 y medir desde el toque en "Abrir caja" hasta ver "Caja abierta" (cronómetro de la app de medición de QA).
3. Cerrar la sesión de prueba desde la herramienta de datos de QA y repetir hasta 30 aperturas.

Resultado: p50 0.9 s · p95 1.4 s · máximo 1.8 s (objetivo REQ-006: p95 < 2 s).
