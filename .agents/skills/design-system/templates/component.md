# Plantilla: componente primitivo

`design-system/components/<name>.json`

```json
{
  "component": "MrThing",
  "variants": ["primary", "secondary"],
  "states": ["default", "pressed", "disabled", "loading"],
  "sizes": {
    "medium": {
      "heightWeb": 44,
      "heightAndroid": 48,
      "paddingInline": "{spacing.4}",
      "radius": "{radius.md}",
      "typography": "{typography.labelLarge}"
    }
  }
}
```

Luego el renderer Angular y el Compose. No uses el componente solo en una feature sin contrato.
