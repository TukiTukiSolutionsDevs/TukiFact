# Redacción de criterios de aceptación

Formato de declaración: `docs/process/traceability-schema.md` §4.2 (`#### AC-<MOD>-###-## — <título>` bajo su historia, líneas `Given`, `When`, `Then`, `And` opcional).

## Reglas de verificabilidad

1. **Un comportamiento por AC.** Si el `Then` tiene dos resultados independientes, son dos AC.
2. **Datos concretos.** Montos, cantidades, roles y estados con valores, no adjetivos ("rápido", "correcto", "adecuado").
3. **Resultado observable.** El `Then` describe algo que una prueba puede comprobar: estado, mensaje, dato persistido, evento visible, permiso denegado.
4. **Sin solución técnica.** No nombrar endpoints, tablas, colas ni componentes; eso es de `TECHNICAL-SPEC.md`.
5. **Contexto completo en `Given`.** Rol, estado previo y datos relevantes; nada implícito.
6. **Una acción en `When`.** Si hay varias, el flujo se describe en `Flujo principal` y el AC prueba la última.
7. **Casos negativos explícitos.** Cada regla con límite o permiso tiene al menos un AC de rechazo.
8. **Trazable a regla.** Si el AC depende de una `BR-*` o `ASM-*`, citarla en el título o en una línea `And`.
9. **Estable ante cambios de UI.** Evitar "hace clic en el botón azul"; usar la acción de negocio.

## Ejemplos

### Malo

```markdown
#### AC-PAY-001-01 — Pago correcto
Given un cajero
When registra un pago
Then el sistema funciona correctamente y se actualiza todo
```

Problemas: sin datos, `Then` no observable, varios efectos mezclados.

### Bueno

```markdown
#### AC-PAY-001-01 — Pago total en efectivo cierra la venta
Given un cajero con caja abierta y una venta pendiente por S/ 45.50
When registra un pago en efectivo de S/ 50.00
Then la venta queda en estado pagada
And el vuelto mostrado es S/ 4.50
```

### Malo (técnico y ambiguo)

```markdown
#### AC-PAY-001-02 — Validación
Given el endpoint POST /payments
When se envía un monto inválido
Then devuelve 400
```

### Bueno (negativo, con regla)

```markdown
#### AC-PAY-001-02 — Rechaza pago menor al saldo (BR-PAY-001-01)
Given un cajero con una venta pendiente por S/ 45.50
When registra un pago en efectivo de S/ 40.00 sin marcar pago parcial
Then el pago no se registra
And se muestra el mensaje "El monto no cubre el total de la venta"
```

### Bueno (permiso)

```markdown
#### AC-PAY-001-05 — Anulación exige supervisor
Given un cajero sin permiso de anulación y un pago registrado hoy
When solicita anular el pago
Then la anulación no se ejecuta
And se solicita la autorización de un supervisor
```

### Bueno (límite)

```markdown
#### AC-PAY-001-03 — Pago exacto no genera vuelto
Given una venta pendiente por S/ 45.50
When el cajero registra un pago en efectivo de S/ 45.50
Then la venta queda en estado pagada
And el vuelto mostrado es S/ 0.00
```

## Señales de AC a reescribir

- Palabras vagas: "correctamente", "adecuado", "rápido", "etc.", "y/o".
- `Then` sin valor esperado o con "debería poder".
- AC que solo repite la historia.
- Regla sin AC negativo o sin límite probado.
- Dato del maquetado sin `REQ-###` ni `ASM-*` que lo respalde.
