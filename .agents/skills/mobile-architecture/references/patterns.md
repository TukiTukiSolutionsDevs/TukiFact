# Patrones Android

Un patrón entra con 2–3 variaciones reales (mismo criterio que `backend-architecture`).

| Situación | Patrón | Dónde |
|---|---|---|
| Algoritmos intercambiables (pago, pesaje) | Strategy | Domain o Application |
| SDK/protocolo externo | Adapter sobre Port | Infrastructure (`UsbScaleAdapter`) |
| Retry/log/métrica en un puerto | Decorator | Infrastructure |
| Recibo / documento con partes | Builder | Domain o Infrastructure |
| Elegir impl por config | Factory | Infrastructure / DI |
| Ciclo de un pago | State | Domain |
| Room, OkHttp, logger | Singleton **por DI** | Nunca Activity/View |
| Estado UI | `StateFlow` / `Flow` | Presentation |

No uses patrones por moda. Specification en memoria si es predicado de dominio; no `repo.find(spec)` como query.
