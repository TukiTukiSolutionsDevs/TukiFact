# Plantilla: tipo de dominio

Ruta: `src/core/<feature>/domain/type/<name>.type.ts`

```ts
export interface I<Name> {
  id: string;
}
```

Reglas: un tipo por archivo, sin Angular/DTO, campos explícitos e invariantes documentadas. Usa aliases solo cuando aporten semántica real.
