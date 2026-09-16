# Preventa — reglas de negocio

- Offline-first.
- Cartera, productos, precios y rutas en local.
- Pedido persistido (Room + `LocalId`) antes de sync.
- GPS solo vía puerto de ubicación (API del puerto: Pendiente de decisión, `references/hardware.md`).
- Stock offline es informativo, no autoridad.
- Conflictos explícitos en UI.
- Sync = Command del módulo correspondiente + outbox (`PendingSyncWriter`); no HTTP ad-hoc.
