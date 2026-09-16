# Mapa de arquitectura

Proyecto: `MarketjoyaFront/` (Angular 22.1, TypeScript 6, componentes standalone; `strict`, `strictTemplates`, `strictStandalone`). Rutas relativas a esa carpeta.

## Estructura actual

```text
src/
├── base/               GoResult, goOk/goErr, AppError + ErrorType + FieldError
├── core/               dominio + aplicación + puertos, una carpeta por bounded context
│   └── auth/           contrato de refresh de sesión (sin cablear)
├── data/               tokens y composición DI (sin features aún)
├── environment/        configuración pública sin secretos (vacío)
├── infrastructure/     adapters, DTOs, mappers, providers
│   └── http/           toAppError (ProblemDetails -> AppError), correlationIdInterceptor
├── ui/                 rutas, layouts, screens, facades, services, stores
│   ├── app.routes.ts   rutas lazy (vacías)
│   └── shell/          AppShellLayout (selector mr-root, data-testid app-shell)
├── styles/             tailwind-layout-only.css + generated/ (tokens del design-system)
├── styles.css          estilos globales (CSS, no SCSS)
├── main.ts             bootstrap
└── app.config.ts       composición raíz
```

Cada capa tiene su `README.md`. No hay features de negocio todavía: la primera sigue [vertical-slice.md](vertical-slice.md).

## Composición raíz

- `src/main.ts`: `bootstrapApplication(AppShellLayout, appConfig)`.
- `src/app.config.ts`: `provideBrowserGlobalErrorListeners()`, `provideHttpClient(withFetch(), withInterceptors([correlationIdInterceptor]))`, `provideRouter(routes, withComponentInputBinding())`.
- Es el único punto que puede importar todas las capas (elemento `app` del lint).

## Aliases (`tsconfig.json` → `paths`)

`@base/*`, `@core/*`, `@data/*`, `@environment/*`, `@infrastructure/*`, `@ui/*` → `./src/<capa>/*`. Entre archivos vecinos del mismo slice se usan imports relativos.

## Angular

- Prefijo de selector `mr` (`angular.json`); ESLint exige componentes `mr-kebab-case` y directivas `mrCamelCase`.
- Estilos de componente en CSS (`inlineStyleLanguage: css`).

## Capas

| Área | Responsabilidad | Prohibido |
|---|---|---|
| `base` | Result, códigos, utils transversales | Lógica de feature, Angular |
| `core` | tipos, puertos, casos de uso | Framework UI, HTTP, storage, router, globals |
| `data` | tokens, factories, módulos de composición | Reglas de negocio, IO directo, cliente HTTP |
| `infrastructure` | adapters, HTTP, DTOs, mappers | UI, router, toast |
| `ui` | entrada, presentación, estado, feedback | adapters, tokens `out`, cliente HTTP |

`core` define contratos; `infrastructure` los implementa; `data` conecta; `ui` orquesta. `ui` consume `design-system/` (`../../design-system/SKILL.md`); Angular es un renderer del mismo contrato que Compose.

## Flujo de una operación

```text
route -> screen/form -> feature service -> facade -> input token
     -> use case -> output port -> adapter -> backend /api/v1
     <- GoResult<T, AppError> <- facade <- service/store <- template
```

Errores: [errors-and-results.md](errors-and-results.md).

## Nombres (feature genérica)

- `<operation>.use-case.ts`, `<operation>.port.ts`, `<capability>-writer.port.ts`
- `<operation>.token.ts`, `<capability>-writer.adapter.ts`, `<feature>.provider.ts`
- `<feature>.facade.ts`, `<feature>.service.ts`, `<feature>.store.ts`, `<feature>.screen.ts`
- Spec junto al archivo: `<archivo>.spec.ts`.
