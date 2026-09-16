# Checklist: E2E Playwright

- [ ] El flujo está en la lista crítica.
- [ ] Spec en `e2e/<flujo>/`; nombre `flujo_escenario_resultado`.
- [ ] `getByTestId`; sin `waitForTimeout`.
- [ ] `webServer`/API compartidos; datos únicos.
- [ ] Auth reutiliza sesión cuando exista login (Pendiente de decisión).
- [ ] Un fallo de negocio visible.
- [ ] `npm run e2e` ejecutado.
