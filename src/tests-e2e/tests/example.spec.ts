import { test, expect } from '@playwright/test';

test('has title', async ({ page }) => {
  await page.goto('https://playwright.dev/');

  // Expect a title "to contain" a substring.
  await expect(page).toHaveTitle(/Playwright/);
});

test('get started link', async ({ page }) => {
  await page.goto('https://playwright.dev/');

  // Click the get started link.
  await page.getByRole('link', { name: 'Get started' }).click();

  // Expects page to have a heading with the name of Installation.
  await expect(page.getByRole('heading', { name: 'Installation' })).toBeVisible();
});

test('navigate to room and generate new room', async ({ page }) => {
  await page.goto('http://localhost:6350/room');

  // Click the "generate new room" button.
  await page.getByRole('button', { name: 'Create new Room' }).click();

  // Wait for 20 seconds.
  await page.waitForTimeout(20000);

  // Take a screenshot.
  await page.screenshot({ path: 'screenshot1.png' });

  // Copy the current URL.
  const currentUrl = page.url();

  // Open a new browser tab and go to the copied URL.
  const newPage = await page.context().newPage();
  await newPage.goto(currentUrl);

  // Take another screenshot of the second browser session.
  await newPage.screenshot({ path: 'screenshot2.png' });
});
