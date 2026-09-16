# Plantilla: componente de negocio

`design-system/business-components/<name>.json`

```json
{
  "component": "MrMoneyDisplay",
  "rules": {
    "tabularNumbers": true,
    "currencyAlwaysVisible": true,
    "doNotRelyOnColorOnly": true
  }
}
```

Pantallas de venta/preventa consumen este contrato. No un `Text` + color “verde dinero” local.
