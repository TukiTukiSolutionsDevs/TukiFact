import asyncio
from playwright import async_api
from playwright.async_api import expect

async def run_test():
    pw = None
    browser = None
    context = None

    try:
        # Start a Playwright session in asynchronous mode
        pw = await async_api.async_playwright().start()

        # Launch a Chromium browser in headless mode with custom arguments
        browser = await pw.chromium.launch(
            headless=True,
            args=[
                "--window-size=1280,720",         # Set the browser window size
                "--disable-dev-shm-usage",        # Avoid using /dev/shm which can cause issues in containers
                "--ipc=host",                     # Use host-level IPC for better stability
                "--single-process"                # Run the browser in a single process mode
            ],
        )

        # Create a new browser context (like an incognito window)
        context = await browser.new_context()
        context.set_default_timeout(5000)

        # Open a new page in the browser context
        page = await context.new_page()

        # Interact with the page elements to simulate user flow
        # -> Navigate to http://localhost:3000/
        await page.goto("http://localhost:3000/")
        
        # -> Open the login page (/login) so I can locate the 'ID de Empresa', 'Email', and 'Contrasena' fields and then fill them.
        await page.goto("http://localhost:3000/login")
        
        # -> Fill the tenant ID, email, and password fields with the provided credentials and submit the form by clicking 'Iniciar Sesión' (submit button index 103).
        frame = context.pages[-1]
        # Input text
        elem = frame.locator('xpath=/html/body/div[2]/div/form/div/div/input').nth(0)
        await asyncio.sleep(3); await elem.fill('0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84')
        
        frame = context.pages[-1]
        # Input text
        elem = frame.locator('xpath=/html/body/div[2]/div/form/div/div[2]/input').nth(0)
        await asyncio.sleep(3); await elem.fill('admin@tukitest.pe')
        
        frame = context.pages[-1]
        # Input text
        elem = frame.locator('xpath=/html/body/div[2]/div/form/div/div[3]/input').nth(0)
        await asyncio.sleep(3); await elem.fill('TestSprite2026!')
        
        # -> Click the 'Iniciar Sesión' button (index 103) to submit the login form and load the dashboard.
        frame = context.pages[-1]
        # Click element
        elem = frame.locator('xpath=/html/body/div[2]/div/form/div[2]/button').nth(0)
        await asyncio.sleep(3); await elem.click()
        
        # -> Click the 'Iniciar Sesión' button (index 103) to submit the login form again, then wait for the app to finish loading the dashboard so KPI cards and charts can be checked.
        frame = context.pages[-1]
        # Click element
        elem = frame.locator('xpath=/html/body/div[2]/div/form/div[2]/button').nth(0)
        await asyncio.sleep(3); await elem.click()
        
        # -> Click the 'Iniciar Sesión' button (index 103) to submit the login form, wait for the app to load, then verify KPI cards and the two charts on the dashboard.
        frame = context.pages[-1]
        # Click element
        elem = frame.locator('xpath=/html/body/div[2]/div/form/div[2]/button').nth(0)
        await asyncio.sleep(3); await elem.click()
        
        # --> Assertions to verify final state
        frame = context.pages[-1]
        assert await frame.locator("xpath=//*[contains(., 'Total Ventas')]").nth(0).is_visible(), "The dashboard should show a KPI card labeled Total Ventas after login.",
        assert await frame.locator("xpath=//*[contains(., 'Ventas mensuales')]").nth(0).is_visible(), "The dashboard should display the Ventas mensuales chart after login."]}
        await asyncio.sleep(5)

    finally:
        if context:
            await context.close()
        if browser:
            await browser.close()
        if pw:
            await pw.stop()

asyncio.run(run_test())
    