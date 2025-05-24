import { test, expect } from '@playwright/test';

test('navigate to room, generate new room, try to play with 2 players', async ({ page }) => {
  // Increase timeout for the test
  test.setTimeout(240_000);

  // Should go to $BASE_URL/room
  await page.goto('/room');

  // Click the "generate new room" button.
  await page.getByRole('button', { name: 'Connect' }).click();

  // Click the "generate new room" button.
  await page.getByRole('button', { name: 'Create new Room' }).click();

  // Wait for 20 seconds.
  await page.waitForTimeout(20000);

  // Take a screenshot.
  await page.screenshot();
  
  // Copy the current URL.
  const currentUrl = page.url();

  // Open a new browser tab and go to the copied URL.
  const newPage = await page.context().newPage();
  await newPage.goto(currentUrl);
  
  // Take another screenshot of the second browser session.
  await newPage.screenshot();

  await page.getByRole('button', { name: 'Play' }).click();
  await newPage.getByRole('button', { name: 'Play' }).click();
  
  // Take another screenshot of the second browser session.
  await page.screenshot();

  // Take another screenshot of the second browser session.
  await newPage.screenshot();
});