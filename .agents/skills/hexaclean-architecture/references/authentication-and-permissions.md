# Auth y permisos

Guards y directivas son UX y defensa de entrada, no autorización suficiente. Cada mutación se valida en el backend de Market Real.

## Estado actual

- Frontend: solo `src/core/auth` (refresh de sesión, [refresh-session.md](refresh-session.md)). No hay adapter, storage de credenciales, guards, directivas ni catálogo de permisos.
- Backend: JWT Bearer con claims `sub`, `company`, `warehouse`, `cash_register`, `permissions`. Los endpoints exigen permisos: 401 sin sesión válida (`UNAUTHORIZED`; `Auth.Unauthorized` si llega sin `code`), 403 sin permiso (`FORBIDDEN`; `Auth.Forbidden` si llega sin `code`). En el OpenAPI los permisos requeridos figuran como scopes del esquema `Bearer`.
- Backend: no existen endpoints de login ni de refresh.

## Rutas nuevas

- Permiso de lectura en la ruta.
- Permiso de mutación en crear/editar.
- Acciones ocultas si no hay permiso.
- La misma regla aplicada en el API.

Probar: autenticado, anónimo, lectura sin escritura, permiso insuficiente.

## Cliente

- No confíes en storage del navegador como frontera de seguridad.
- No pongas secretos OAuth/AI en el frontend.
- Nombres de permiso: los del claim `permissions` del backend; no inventes un catálogo propio.
- `UNAUTHORIZED`: refresh single-flight una vez; si falla, cierre de sesión ([refresh-session.md](refresh-session.md)).
- `FORBIDDEN`: estado de permiso insuficiente; no refresca ni reintenta. Ramifica por `type`/`code`, nunca por `description` ([errors-and-results.md](errors-and-results.md)).

## Pendiente de decisión

- Endpoints de login y refresh, y contrato de tokens (payload, duración, rotación): backend.
- Almacenamiento de sesión en el cliente.
- Guards (`authGuard`, `permissionGuard`) y catálogo de permisos del frontend.
