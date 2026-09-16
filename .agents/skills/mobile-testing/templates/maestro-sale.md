# Plantilla: Maestro POS

Flujo existente de referencia: `maestro/pos-smoke.yaml`. Flujo de venta (a crear con la feature): `maestro/pos-complete-sale.yaml`.

```yaml
# Venta con turno abierto. Ids = testTag de las Screens.
appId: pe.marketjoya.pos
---
- launchApp:
    clearState: true
- assertVisible:
    id: "pos-root"
- tapOn:
    id: "open-shift"
- tapOn:
    id: "scan-or-add"
- tapOn:
    id: "confirm-sale"
- assertVisible:
    id: "sale-success"
```

- Solo `pos-root` existe hoy; los demás `testTag` se crean con la feature.
- Requiere `testTagsAsResourceId = true` en la raíz (ya presente en `PosAppRoot`).
- Datos de catálogo del entorno de CI, no producción. Maestro se instala aparte (`maestro/README.md`).
