# Checklist: nueva feature

- [ ] Bounded context y scope DI definidos.
- [ ] Operaciones localizadas en el OpenAPI (`operationId`); si faltan, bloqueado y pedido al backend.
- [ ] Tipo/entidad y commands en `src/core/<feature>`.
- [ ] Puertos de entrada y salida definidos.
- [ ] Casos de uso contienen reglas, no solo delegación accidental.
- [ ] Tokens `in`/`out` y módulo en `src/data/<feature>`.
- [ ] Adapter y provider en `src/infrastructure/<feature>`; adapter único traductor de errores (`toAppError` → `AppError`).
- [ ] Service ramifica por `type`/`code`, no por `description`; `correlationId` visible en fallos.
- [ ] DTOs/mappers del contrato, solo en infrastructure.
- [ ] Facade, service y store separados en `src/ui/<feature>`.
- [ ] Layout y providers tienen lifecycle intencional.
- [ ] Ruta lazy en `src/ui/app.routes.ts`; guards según estado de auth.
- [ ] Permisos de lectura y mutación alineados con botones.
- [ ] Estados loading, empty, error y success cubiertos.
- [ ] UI con `Mr*` / tokens `--mr-*`; Tailwind solo layout; selector `mr-`.
- [ ] `npm run lint` sin errores de fronteras.
- [ ] Tests del slice según `../../frontend-testing/SKILL.md`; `npm run verify` en verde.
