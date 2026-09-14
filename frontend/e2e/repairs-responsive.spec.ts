import { expect, test, type Page } from "@playwright/test";

const id = "11111111-1111-4111-8111-111111111111";
const ticket = {
  id,
  number: 1,
  requesterId: id,
  categoryId: id,
  teamCode: "IT",
  title: "เครื่องพิมพ์ไม่สามารถพิมพ์เอกสารได้",
  description: "ทดสอบอาการและขั้นตอนแก้ไข ไม่ใช่ข้อมูลผู้ป่วย",
  location: "อาคารผู้ป่วยนอก ชั้น 2 ห้องทำงาน",
  contact: "ผู้แจ้งทดสอบ โทร 1234",
  status: "InProgress",
  currentRound: 1,
  createdAt: "2026-09-11T08:00:00Z",
  updatedAt: "2026-09-11T09:00:00Z",
  concurrencyToken: id,
};
const category = {
  id,
  name: "เครื่องพิมพ์",
  teamCode: "IT",
  isActive: true,
  concurrencyToken: id,
};
const roles = [
  { name: "Staff", permissions: [] },
  { name: "IT", permissions: ["WorkIT"] },
  { name: "General", permissions: ["WorkGeneral"] },
  { name: "Dual", permissions: ["WorkIT", "WorkGeneral"] },
  { name: "Admin", permissions: ["ViewAll", "Manage"] },
  { name: "SuperAdmin", permissions: ["ViewAll", "Manage"] },
];

async function fixture(page: Page, role: (typeof roles)[number]) {
  const user = {
    id,
    fullname: "ผู้ใช้ทดสอบระบบแจ้งซ่อม",
    username: "repair-qa",
    role: role.name,
    department: "หน่วยงานทดสอบ",
    permissions: ["ViewOwn", "Create", ...role.permissions].map(
      (p) => `RepairManagement.${p}`,
    ),
  };
  await page.addInitScript((user) => {
    localStorage.setItem("hop.accessToken", "local-qa-token");
    localStorage.setItem("hop.user", JSON.stringify(user));
  }, user);
  await page.route(/^https?:\/\/[^/]+\/api\//, async (route) => {
    const path = new URL(route.request().url()).pathname;
    let data: unknown = {};
    if (path === "/api/auth/me") data = user;
    else if (path === "/api/auth/refresh-token")
      data = { accessToken: "local-qa-token", refreshToken: "", user };
    else if (path === "/api/repairs")
      data = { items: [ticket], total: 1, pageSize: 20 };
    else if (path === "/api/repairs/options")
      data = { categories: [category], teams: [{ code: "IT", name: "IT" }] };
    else if (path === "/api/repairs/summary")
      data = {
        generatedAtUtc: new Date().toISOString(),
        counts: [{ status: "InProgress", count: 1 }],
        teamPending: 1,
      };
    else if (path === "/api/repairs/settings")
      data = {
        categories: [
          category,
          {
            ...category,
            id: "general-category",
            name: "ประปา",
            teamCode: "GENERAL",
            isActive: false,
          },
        ],
        groups: [
          {
            id,
            module: "REPAIR_IT",
            displayName: "ทีม IT",
            status: "Disabled",
            hasSecret: false,
            concurrencyToken: id,
          },
          {
            id: "general-group",
            module: "REPAIR_GENERAL",
            displayName: "ทีมช่างทั่วไป",
            status: "Active",
            hasSecret: true,
            concurrencyToken: id,
          },
        ],
        deliveries: [
          {
            id: "delivery-1",
            requestId: id,
            teamCode: "IT",
            status: "Attention",
            attempts: 3,
            errorCode: "TIMEOUT",
          },
        ],
      };
    else if (path.endsWith("/solvers")) data = [{ id, fullName: "ช่างทดสอบ" }];
    else if (path === `/api/repairs/${id}`)
      data = {
        request: {
          ...ticket,
          teamCode: role.name === "General" ? "GENERAL" : "IT",
        },
        events: [
          {
            id,
            action: "start",
            round: 1,
            actorId: id,
            createdAt: ticket.updatedAt,
            note: "กำลังตรวจสอบอาการ",
            fromStatus: "Submitted",
            toStatus: "InProgress",
          },
        ],
        people: [{ id, fullName: user.fullname }],
        rounds: [{ id, number: 1, startedAt: ticket.createdAt }],
        waiting: [
          {
            id,
            round: 1,
            startedAt: ticket.createdAt,
            endedAt: ticket.updatedAt,
          },
        ],
        images: [],
        contributors: [],
        actions: role.permissions.some((x) => x.startsWith("Work"))
          ? ["wait", "return", "priority", "note", "solve"]
          : ["note"],
        canUpload: true,
      };
    else if (path.includes("badge") || path.includes("unread-count")) data = 0;
    else if (path.includes("notifications")) data = [];
    await route.fulfill({ json: { success: true, message: "", data } });
  });
}

async function noOverflow(page: Page) {
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth + 1,
    ),
  ).toBeTruthy();
}

test("start action confirms without requiring a reason", async ({ page }) => {
  await fixture(page, roles[1]);
  let submitted: Record<string, unknown> | undefined;
  await page.route(`**/api/repairs/${id}/actions/start`, async (route) => {
    submitted = route.request().postDataJSON();
    await route.fulfill({
      json: {
        success: true,
        data: { ...ticket, status: "InProgress" },
      },
    });
  });
  await page.route(`**/api/repairs/${id}`, async (route) => {
    await route.fulfill({
      json: {
        success: true,
        data: {
          request: { ...ticket, status: "Submitted" },
          people: [{ id, fullName: "ช่างทดสอบ" }],
          events: [],
          rounds: [{ id, number: 1, startedAt: ticket.createdAt }],
          waiting: [],
          images: [],
          contributors: [],
          actions: ["start"],
          canUpload: true,
        },
      },
    });
  });

  await page.goto(`/repairs/${id}`);
  await page.getByRole("button", { name: "เริ่มดำเนินการ", exact: true }).first().click();
  await expect(page.getByText("ยืนยันเริ่มดำเนินการงานแจ้งซ่อมนี้")).toBeVisible();
  await expect(page.getByRole("textbox", { name: "รายละเอียด / เหตุผล" })).toHaveCount(0);
  await expect(page.getByRole("button", { name: "ยืนยัน", exact: true })).toBeEnabled();
  await page.getByRole("button", { name: "ยืนยัน", exact: true }).click();

  expect(submitted?.concurrencyToken).toBe(id);
  expect(submitted?.note).toBe("");
});

test("409 keeps the note and requires explicitly loading the latest token", async ({
  page,
}) => {
  await fixture(page, roles[1]);
  let conflict = false;
  let submitted: Record<string, unknown> | undefined;
  await page.route(`**/api/repairs/${id}/actions/note`, async (route) => {
    submitted = route.request().postDataJSON();
    if (!conflict) {
      conflict = true;
      await route.fulfill({
        status: 409,
        json: { success: false, message: "Conflict" },
      });
    } else await route.fulfill({ json: { success: true, data: ticket } });
  });
  await page.route(`**/api/repairs/${id}`, async (route) => {
    if (!conflict) return route.fallback();
    await route.fulfill({
      json: {
        success: true,
        data: {
          request: { ...ticket, concurrencyToken: "latest-token" },
          people: [],
          events: [],
          rounds: [],
          waiting: [],
          images: [],
          contributors: [],
          actions: ["note"],
          canUpload: false,
        },
      },
    });
  });
  await page.goto(`/repairs/${id}`);
  await page.getByRole("button", { name: "เพิ่มบันทึก", exact: true }).click();
  await page
    .getByRole("textbox", { name: "รายละเอียด / เหตุผล" })
    .fill("บันทึกต้องไม่หาย");
  await page.getByRole("button", { name: "ยืนยัน", exact: true }).click();
  await expect(
    page.getByRole("textbox", { name: "รายละเอียด / เหตุผล" }),
  ).toHaveValue("บันทึกต้องไม่หาย");
  await page
    .getByRole("button", { name: "โหลดข้อมูลล่าสุดโดยคงข้อความ" })
    .click();
  await expect(
    page.getByRole("textbox", { name: "รายละเอียด / เหตุผล" }),
  ).toHaveValue("บันทึกต้องไม่หาย");
  await expect(
    page.getByRole("button", { name: "ยืนยัน", exact: true }),
  ).toBeEnabled();
  await page.getByRole("button", { name: "ยืนยัน", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  expect(submitted?.concurrencyToken).toBe("latest-token");
  expect(submitted?.note).toBe("บันทึกต้องไม่หาย");
});

for (const width of [375, 430, 768, 1366, 1920]) {
  test(`department dropdown at ${width}px`, async ({ page }, info) => {
    await page.setViewportSize({ width, height: 932 });
    await fixture(page, roles[0]);
    await page.route("**/api/repairs/options", (route) =>
      route.fulfill({
        json: {
          success: true,
          data: {
            teams: [],
            categories: [
              category,
              ...Array.from({ length: 10 }, (_, i) => ({
                ...category,
                id: `category-${i}`,
                name: `ประเภทงานเพิ่มเติม ${i + 1}`,
              })),
            ],
          },
        },
      }),
    );
    await page.goto("/repairs/new");
    await expect(page.getByLabel("หน่วยงานผู้แจ้ง")).toHaveValue(
      "หน่วยงานทดสอบ",
    );
    await expect(page.getByLabel("หน่วยงานผู้แจ้ง")).toBeDisabled();
    await page.getByRole("combobox", { name: "ประเภทงาน" }).click();
    await expect(page.getByRole("listbox")).toBeVisible();
    const popup = await page.getByRole("listbox").boundingBox();
    expect(popup!.x).toBeGreaterThanOrEqual(0);
    expect(popup!.x + popup!.width).toBeLessThanOrEqual(width);
    await page.getByRole("option", { name: "เครื่องพิมพ์ · IT" }).click();
    await page.getByRole("combobox", { name: "ประเภทงาน" }).click();
    await expect(page.getByRole("option", { selected: true })).toHaveText(
      "เครื่องพิมพ์ · IT",
    );
    await page.screenshot({
      path: info.outputPath("repair-dropdown.png"),
      animations: "disabled",
    });
    await page.keyboard.press("Escape");
    await noOverflow(page);
  });
  for (const role of roles) {
    test(`${role.name} at ${width}px`, async ({ page }, info) => {
      await page.setViewportSize({ width, height: width < 768 ? 812 : 1024 });
      page.on("pageerror", (error) =>
        console.error("BROWSER ERROR:", error.message),
      );
      await fixture(page, role);
      await page.goto("/dashboard/repair");
      await expect(
        page.getByRole("heading", {
          name: "Dashboard แจ้งซ่อม",
          exact: true,
          level: 4,
        }),
      ).toBeVisible();
      await expect(
        page.getByText(ticket.title, { exact: false }).last(),
      ).toBeVisible();
      await noOverflow(page);
      await page.screenshot({
        path: info.outputPath("repair-dashboard.png"),
        fullPage: true,
        animations: "disabled",
      });
      await page.goto("/repairs");
      await expect(
        page.getByText(ticket.title, { exact: false }).last(),
      ).toBeVisible();
      await noOverflow(page);
      if (width >= 900)
        await expect(
          page.getByRole("columnheader", { name: "ความเร่งด่วน" }),
        ).toBeVisible();
      else await expect(page.getByRole("table")).toHaveCount(0);
      await page.screenshot({
        path: info.outputPath("repair-list.png"),
        fullPage: true,
        animations: "disabled",
      });
      await page
        .getByRole("link", { name: "ดูรายละเอียด", exact: true })
        .click();
      await expect(
        page.getByText("ประวัติการดำเนินการ", { exact: true }),
      ).toBeVisible();
      for (const heading of [
        "ขั้นตอนถัดไป",
        "ข้อมูลใบงาน",
        "รอบการซ่อมและผลตรวจรับ",
        "รูปภาพประกอบ",
      ])
        await expect(page.getByText(heading, { exact: true })).toBeVisible();
      await noOverflow(page);
      await page.screenshot({
        path: info.outputPath("repair-detail.png"),
        fullPage: true,
        animations: "disabled",
      });
      if (role.permissions.some((x) => x.startsWith("Work"))) {
        const mobileActions = page.getByLabel("การดำเนินการหลักบนมือถือ");
        if (width < 900) {
          await expect(mobileActions).not.toBeVisible();
          await page
            .getByText("รอบการซ่อมและผลตรวจรับ", { exact: true })
            .scrollIntoViewIfNeeded();
          await expect(mobileActions).toBeVisible();
          const actionBox = await mobileActions.boundingBox();
          expect(actionBox!.x).toBeGreaterThanOrEqual(0);
          expect(actionBox!.x + actionBox!.width).toBeLessThanOrEqual(width);
          expect(actionBox!.y + actionBox!.height).toBeLessThanOrEqual(
            await page.evaluate(() => window.innerHeight),
          );
          await page.screenshot({
            path: info.outputPath("repair-detail-sticky.png"),
            animations: "disabled",
          });
        } else await expect(mobileActions).not.toBeVisible();
        await page
          .getByRole("button", { name: "แก้ไขเสร็จ", exact: true })
          .click();
        await expect(page.getByRole("dialog")).toBeVisible();
        await expect(
          page.getByRole("button", { name: "ยืนยัน", exact: true }),
        ).toBeDisabled();
        const box = await page.getByRole("dialog").boundingBox();
        expect(box!.x).toBeGreaterThanOrEqual(0);
        expect(box!.x + box!.width).toBeLessThanOrEqual(width);
        await page.screenshot({
          path: info.outputPath("repair-solve.png"),
          animations: "disabled",
        });
        await page.getByRole("button", { name: "กลับ", exact: true }).click();
      }
      await page.goto("/repairs/new");
      await expect(
        page.getByRole("button", { name: "ส่งแจ้งซ่อม", exact: true }),
      ).toBeVisible();
      await noOverflow(page);
      await page.screenshot({
        path: info.outputPath("repair-create.png"),
        fullPage: true,
        animations: "disabled",
      });
      await page.getByLabel("เลือกรูปภาพแนบ").setInputFiles({
        name: "preview.png",
        mimeType: "image/png",
        buffer: Buffer.from(
          "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aL1cAAAAASUVORK5CYII=",
          "base64",
        ),
      });
      await expect(page.getByAltText("ภาพตัวอย่าง preview.png")).toBeVisible();
      await page.getByRole("button", { name: "นำรูป preview.png ออก" }).click();
      await expect(page.getByAltText("ภาพตัวอย่าง preview.png")).toHaveCount(0);
      if (role.permissions.includes("Manage")) {
        await page.goto("/repairs/settings");
        await noOverflow(page);
        await page.screenshot({
          path: info.outputPath("repair-settings-categories.png"),
          fullPage: true,
        });
        await page.getByRole("tab", { name: "กลุ่มแจ้งเตือน" }).click();
        await page.screenshot({
          path: info.outputPath("repair-settings-groups.png"),
          fullPage: true,
        });
        await page
          .getByRole("button", { name: "ตั้งค่าปลายทาง" })
          .first()
          .click();
        await expect(page.getByRole("dialog")).toBeVisible();
        await noOverflow(page);
        await page.screenshot({
          path: info.outputPath("repair-settings-dialog.png"),
          fullPage: true,
        });
        await page.getByRole("button", { name: "กลับ", exact: true }).click();
        await page.getByRole("tab", { name: "ผลส่งแจ้งเตือน" }).click();
        await expect(page.getByText("TIMEOUT")).toBeVisible();
        await noOverflow(page);
        await page.screenshot({
          path: info.outputPath("repair-settings-deliveries.png"),
          fullPage: true,
        });
      }
    });
  }
}
