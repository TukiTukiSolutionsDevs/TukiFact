# Búsqueda de reutilización

Correr desde la raíz del repo padre. Siempre `rg`/`fd` con `-HI` y excluyendo artefactos de build. Registrar en `Reutilización` el comando, lo encontrado (ruta) y la decisión.

## Procedimiento

1. Nombrar la necesidad en términos de negocio y en términos técnicos (sustantivo + verbo: `Payment`, `Register`, `CashShift`, `Close`).
2. Buscar por nombre en los tres repos y en `docs/features` (¿otro feature ya lo resolvió?).
3. Buscar por mecanismo: handler, endpoint, evento, tabla, componente, puerto.
4. Leer lo encontrado; decidir reutilizar, extender o crear.
5. Crear solo con las 9 preguntas y ADR.

## Features y documentación

```bash
rg -HI -n "<Término>" docs/features docs/product docs/adr
rg -HI -n "<MOD>-FEAT-\d{3}" docs/traceability/dependency-map.md
```

## Backend (`MarketjoyaBackend/`)

```bash
B=MarketjoyaBackend
X="-g !**/bin/** -g !**/obj/**"
# Módulos existentes
fd -HI -t d -d 1 . $B/src/Modules
# Commands / queries y sus handlers (mediador propio: IRequest, IRequestHandler)
rg -HI -n $X "record \w*<Término>\w*(Command|Query)\b|IRequestHandler<\w*<Término>" $B/src
# Endpoints (IEndpoint + WithName = operationId) y permisos
rg -HI -n $X ": IEndpoint|\.WithName\(\"\w*<Término>|RequireAuthorization\(\"" $B/src/Modules
rg -HI -n "\"operationId\": \"\w*<Término>" $B/openapi/marketjoya-api-v1.json
# Integration events (contratos) y consumers
rg -HI -n $X "record \w*IntegrationEvent\b" $B/src
rg -HI -n $X "IIntegrationEventConsumer<\w*<Término>" $B/src
# Domain events y handlers
rg -HI -n $X "IDomainEventHandler<|record \w*DomainEvent\b" $B/src/Modules
# Tablas, schemas y configuraciones EF
rg -HI -n $X "HasDefaultSchema|ToTable\(|IEntityTypeConfiguration<\w*<Término>" $B/src
fd -HI -t f -e cs . $B/src -E bin -E obj | rg -i "migration"
# Repositorios y agregados
rg -HI -n $X "interface I\w*<Término>\w*Repository|class \w*<Término>\w* : (AggregateRoot|Entity)" $B/src
# Pruebas existentes (regresión)
rg -HI -n $X "Trait\(\"TestId\"" $B/tests
```

## Frontend (`MarketjoyaFront/`)

```bash
F=MarketjoyaFront/src
# Features hexagonales
fd -HI -t d -d 1 . $F/core $F/infrastructure $F/ui
# Puertos de entrada y salida
fd -HI -t f "\.port\.ts$" $F/core | rg -i "<término>"
# Use cases, executors, adapters, facades
fd -HI -t f "\.(use-case|executor|adapter|facade|store)\.ts$" $F | rg -i "<término>"
# Uso de un endpoint o operationId del backend
rg -HI -n "/api/v1/<recurso>" $F
# Componentes Mr* del design system
rg -HI -n "selector: 'mr-" $F
# Pruebas existentes
rg -HI -n "it\('TEST-" $F MarketjoyaFront/e2e
```

## Mobile (`MarketjoyaMobile/`)

```bash
M=MarketjoyaMobile
X="-E build"
# Módulos de feature y core
fd -HI -t d -d 1 . $M/feature $M/core $M/hardware $X
# Use cases y puertos
fd -HI -t f "UseCase\.kt$" $M $X | rg -i "<término>"
rg -HI -n -g '*.kt' -g '!**/build/**' "interface \w*<Término>\w*(Repository|Port|Gateway)" $M
# Room: entidades, DAOs, schemas exportados
rg -HI -n -g '*.kt' -g '!**/build/**' "@Entity\(|@Dao" $M/core/database
fd -HI . $M/core/database/schemas
# Red y sync offline
rg -HI -n -g '*.kt' -g '!**/build/**' "client_mutation_id|clientMutationId|PendingSyncOperation" $M
# Hardware (impresora, lector, balanza)
fd -HI -t f -e kt . $M/hardware $X | rg -i "<término>"
# Componentes Mr* y flujos Maestro
rg -HI -n -g '*.kt' -g '!**/build/**' "fun Mr\w+\(" $M/core/designsystem
rg -HI -n "# TEST-" $M/maestro
```

## Design system

```bash
fd -HI -t f . design-system | rg -i "<término>"
```

## Criterios de decisión

| Resultado | Decisión |
|---|---|
| Existe y cubre el caso | Reutilizar; citar ruta |
| Existe y cubre parcialmente | Extender; evaluar impacto en consumidores actuales |
| Existe en otro módulo | Consumir su contrato (endpoint/evento); nunca su persistencia |
| No existe | Crear; si es servicio, patrón, cola, evento o integración → 9 preguntas + ADR |
