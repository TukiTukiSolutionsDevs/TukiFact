# Slice vertical (feature genérica)

Esqueleto para la primera feature y las siguientes. Sustituye `<feature>` por el bounded context real de Market Real. Rutas relativas a `MarketjoyaFront/`.

## Flujo crear

```text
/<feature>
 -> <feature>.screen.ts
 -> <feature>-form.fragment.ts
 -> <feature>.service.ts
 -> <feature>.facade.ts
 -> CREATE_<FEATURE>_TOKEN
 -> Create<Feature>UseCase
 -> <Feature>WriterPort
 -> <Feature>WriterAdapter
 -> HTTP POST /api/v1/<resource>   (operationId <Module>_<UseCase>)
```

Path, verbo y DTO salen de `MarketjoyaBackend/openapi/marketjoya-api-v1.json` ([api-contract.md](api-contract.md)).

## Archivos clave

- Contrato: `src/core/<feature>/domain/type/create-<feature>-command.type.ts`
- Entrada: `src/core/<feature>/port/in/create-<feature>.port.ts`
- Salida: `src/core/<feature>/port/out/<feature>-writer.port.ts`
- Caso de uso: `src/core/<feature>/application/use-case/create-<feature>.use-case.ts` (+ `.spec.ts`)
- Token in: `src/data/<feature>/token/in/create-<feature>.token.ts`
- Token out: `src/data/<feature>/token/out/<feature>-writer.token.ts`
- Wiring: `src/data/<feature>/<feature>.module.ts`
- Provider: `src/infrastructure/<feature>/<feature>.provider.ts`
- Adapter: `src/infrastructure/<feature>/adapter/out/<feature>-writer.adapter.ts` (+ `.spec.ts`)
- Screen / facade / service / store: `src/ui/<feature>/`
- Ruta: `src/ui/app.routes.ts`

## Responsabilidades

| Pieza | Hace |
|---|---|
| Formulario | Forma datos y validación de UX |
| Service | Loading, estado, feedback, store; ramifica errores por `type`/`code` |
| Facade | Llama al caso de uso |
| Use case | Reglas de aplicación |
| Adapter | HTTP/DTO/mapper → `GoResult<T, AppError>`; único traductor de ProblemDetails |

## Puertos específicos

Lectura y escritura son puertos distintos (`<Feature>ByIdReaderPort`, `<Feature>ListReaderPort`), no un `*RepositoryPort` CRUD.
