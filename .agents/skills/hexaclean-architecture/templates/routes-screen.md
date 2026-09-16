# Plantilla: ruta y screen

## Ruta

Archivo: `MarketjoyaFront/src/ui/app.routes.ts` (hoy `routes: Routes = []`).

```ts
{
  path: '<feature>',
  canActivate: [/* guards: Pendiente de decisión (authentication-and-permissions.md) */],
  loadComponent: () => import('./<feature>/<feature>.screen').then((m) => m.<Feature>Screen),
}
```

Lazy loading. Lectura y escritura con permisos distintos cuando existan los guards. Con `withComponentInputBinding()` los parámetros de ruta llegan como inputs.

## Screen

```ts
@Component({
  selector: 'mr-<feature>-screen',
  templateUrl: './<feature>.screen.html',
})
export class <Feature>Screen {}
```

Coordina router, diálogo y eventos. Inyecta service/facade, nunca adapter ni cliente HTTP. Template: loading, vacío, error, éxito y permiso insuficiente; `fieldErrors` junto a su control; en `FAILURE`/`NETWORK`/`UNKNOWN`, `correlationId` visible como referencia de soporte; `data-testid` en acciones cubiertas por E2E. Visual: `Mr*` y tokens `--mr-*`; Tailwind solo layout.
