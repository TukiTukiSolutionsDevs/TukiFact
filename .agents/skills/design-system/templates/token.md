# Plantilla: token semántico

Añadir en `design-system/tokens/colors.semantic.json` (o el archivo que corresponda), referenciando un primitivo existente:

```json
{
  "color": {
    "semantic": {
      "status": {
        "info": "{color.primitive.blue.500}"
      }
    }
  }
}
```

Tras regenerar ([checklists/new-token.md](../checklists/new-token.md)):

- Web: `var(--mr-color-status-info)`.
- Android: `MrTheme.colors.statusInfo`.

Nunca el primitivo ni `#RRGGBB` en la feature.
