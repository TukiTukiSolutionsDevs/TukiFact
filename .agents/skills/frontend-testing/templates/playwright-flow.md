# Plantilla: flujo Playwright

`MarketjoyaFront/e2e/<flujo>/<critical-flow>.spec.ts`. Base real: `e2e/shell/app-shell.spec.ts`.

```ts
import { expect, test } from '@playwright/test';

test('<flow>_<scenario>_<result>', async ({ page }) => {
  // Arrange
  const name = `item-${crypto.randomUUID()}`;
  await page.goto('/<feature>');

  // Act
  await page.getByTestId('<feature>-name').fill(name);
  await page.getByTestId('<feature>-submit').click();

  // Assert
  await expect(page.getByTestId('<feature>-success')).toBeVisible();
});
```

Sin `waitForTimeout`. Datos únicos. `webServer` y `baseURL` en `playwright.config.ts`, no en el test. Spec de login y `storageState`: Pendiente de decisión (sin endpoint de login).
