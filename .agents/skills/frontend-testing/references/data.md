# Capa: data

Solo el **wiring**: factory + `deps`. No reglas de negocio.

## Qué probar

- El token `in` resuelve el use case con el puerto `out` correcto.
- Un cambio de adapter no exige tocar el test del use case (solo el provider).

## Cómo

TestBed mínimo: `provide` del módulo data + fake del token de salida. No importes la app entera.

Si el factory es trivial (`(w) => new UseCase(w)`), un test por feature basta. No dupliques por operación.
