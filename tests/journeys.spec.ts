import { test, expect, type Page } from "@playwright/test";
async function create(page: Page, reference = "E2E-100") {
  await page.goto("/#/payments/new");
  await page.getByLabel("Merchant reference").fill(reference);
  await page
    .getByRole("button", { name: "Create payment intent", exact: true })
    .click();
  await page
    .getByRole("button", { name: "Issue instructions", exact: true })
    .click();
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(reference);
}
async function scenario(page: Page, value: string) {
  await page.getByLabel("Scenario", { exact: true }).selectOption(value);
  await page.getByRole("button", { name: "Run scenario", exact: true }).click();
}
test("create, detect, deduplicate, finalize, reconcile and persist", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (e) => errors.push(e.message));
  await create(page);
  await scenario(page, "detect");
  await scenario(page, "duplicate");
  await scenario(page, "finalize");
  await expect(page.getByText("succeeded", { exact: true })).toBeVisible();
  await page.getByRole("tab", { name: "Transfers" }).click();
  await expect(
    page.getByRole("table", { name: "Blockchain transfers" }).getByRole("row"),
  ).toHaveCount(2);
  await page.reload();
  await expect(page.getByText("succeeded", { exact: true })).toBeVisible();
  await page.goto("/#/settlements");
  const row = page.getByRole("row").filter({ hasText: "E2E-100" });
  await row.getByRole("button", { name: "Record statement" }).click();
  await page
    .getByRole("button", { name: "Record matched statement", exact: true })
    .click();
  await expect(row.getByText("Settled", { exact: true })).toBeVisible();
  expect(errors).toEqual([]);
});
test("filter empty and error states recover, tenant routes stay isolated", async ({
  page,
}) => {
  await page.goto("/#/payments");
  await page
    .getByRole("searchbox", { name: "Search payments" })
    .fill("does-not-exist");
  await expect(page.getByText("No payments found")).toBeVisible();
  await page.getByRole("button", { name: "Reset filters" }).click();
  await page.getByLabel("Sandbox list state").selectOption("error");
  await expect(page.getByText("Payments could not be loaded")).toBeVisible();
  await page.getByRole("button", { name: "Retry loading" }).click();
  await page.getByRole("link", { name: "ORD-8414", exact: true }).click();
  await page.getByLabel("Active tenant").selectOption("harbor");
  await expect(page.getByText("Access denied")).toBeVisible();
});
test("provider outage routes to alternative and mandatory screening fails closed", async ({
  page,
}) => {
  await page.goto("/#/providers");
  await page.getByLabel("Available: Bridgeway").uncheck();
  await create(page, "E2E-FALLBACK");
  await expect(page.getByText("Orbit Payments", { exact: true })).toBeVisible();
  await page.goto("/#/routing");
  await page.getByLabel("Unavailable / blocked").check();
  await page.getByRole("button", { name: "Evaluate routes" }).click();
  await expect(page.getByText("No eligible route · fail closed")).toBeVisible();
});
test("webhook replay preserves stable ID and reports delivery history", async ({
  page,
}) => {
  await page.goto("/#/webhooks");
  const row = page
    .getByRole("table", { name: "Webhook deliveries" })
    .getByRole("row")
    .nth(1);
  const id = await row.getByRole("button").first().innerText();
  await row.getByRole("button", { name: "Replay event" }).click();
  await page
    .getByRole("button", { name: "Replay webhook", exact: true })
    .click();
  await expect(row.getByText("Approved", { exact: true })).toBeVisible();
  await row.getByRole("button", { name: id, exact: true }).click();
  await expect(
    page.getByText("Replay delivered", { exact: true }),
  ).toBeVisible();
});
test("theme, keyboard navigation and responsive shell", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { level: 1 })).toHaveCount(1);
  await page.getByRole("button", { name: "Toggle theme" }).click();
  await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
  await page.keyboard.press("Tab");
  await expect(page.locator(":focus")).toHaveCount(1);
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBe(true);
});

test("statement import surfaces mismatch and preserves the original observation after correction", async ({
  page,
}) => {
  await create(page, "E2E-STATEMENT");
  await scenario(page, "detect");
  await scenario(page, "finalize");
  const id = new URL(page.url()).hash.split("/")[2];
  await page.goto("/#/settlements");
  await page
    .getByLabel("Statement CSV")
    .fill(`intent_id,amount_usdc,provider_reference\n${id},249.00,E2E-STMT-1`);
  await page
    .getByRole("button", { name: "Import statement", exact: true })
    .click();
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Import statement", exact: true })
    .click();
  const row = page.getByRole("row").filter({ hasText: "E2E-STATEMENT" });
  await expect(row.getByText("In review", { exact: true })).toBeVisible();
  await page
    .getByLabel("Statement CSV")
    .fill(`intent_id,amount_usdc,provider_reference\n${id},250.00,E2E-STMT-2`);
  await page
    .getByRole("button", { name: "Import statement", exact: true })
    .click();
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Import statement", exact: true })
    .click();
  await expect(row.getByText("Settled", { exact: true })).toBeVisible();
  await row.getByRole("link").click();
  await page.getByRole("tab", { name: "Event timeline", exact: true }).click();
  await expect(
    page.getByText("Settlement mismatch detected", { exact: true }),
  ).toBeVisible();
});

test("all operations screens render in both themes without runtime errors", async ({
  page,
}) => {
  const errors: string[] = [];
  page.on("pageerror", (e) => errors.push(e.message));
  for (const dark of [false, true]) {
    await page.goto("/#/overview");
    if (dark) await page.getByRole("button", { name: "Toggle theme" }).click();
    for (const route of [
      "overview",
      "payments",
      "routing",
      "providers",
      "reconciliation",
      "settlements",
      "webhooks",
      "administration",
      "audit",
    ]) {
      await page.goto(`/#/${route}`);
      await expect(page.getByRole("heading", { level: 1 })).toHaveCount(1);
      await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
    }
  }
  expect(errors).toEqual([]);
});

test("confirmation dialog supports keyboard dismissal and returns focus", async ({
  page,
}) => {
  await page.goto("/#/providers");
  const trigger = page
    .getByRole("button", { name: "Emergency stop", exact: true })
    .first();
  await trigger.click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).not.toBeVisible();
  await expect(trigger).toBeFocused();
});

test("capture validated desktop previews", async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto("/#/overview");
  await page.screenshot({
    path: "docs/screenshots/overview.png",
    fullPage: true,
  });
  await create(page, "PREVIEW-100");
  await scenario(page, "detect");
  await page.reload();
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "PREVIEW-100",
  );
  await page.screenshot({
    path: "docs/screenshots/payment-details.png",
    fullPage: true,
  });
});
