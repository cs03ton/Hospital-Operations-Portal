import fs from "node:fs";
import path from "node:path";

export type RoleName = "user" | "head" | "director" | "hr" | "superadmin";

export type ScreenshotUser = {
  username: string;
  password: string;
};

export type ScreenshotSeed = {
  leaveDraftId?: string;
  leavePendingHeadId?: string;
  leavePendingDirectorId?: string;
  leaveApprovedId?: string;
  testUserId?: string;
  testDepartmentId?: string;
  testRoleId?: string;
};

export type ScreenshotConfig = {
  baseUrl: string;
  roles: Record<RoleName, ScreenshotUser>;
  seed: ScreenshotSeed;
};

export const repoRoot = path.resolve(__dirname, "..", "..", "..");
export const screenshotRoot = path.join(repoRoot, "docs", "manuals", "assets", "screenshots");
export const reportPath = path.join(screenshotRoot, "Capture-Report.md");
export const indexPath = path.join(screenshotRoot, "Screenshot-Index.md");
export const mappingPath = path.join(screenshotRoot, "Manual-Screenshot-Mapping.md");
export const configPath = path.join(repoRoot, "tests", "screenshots", "config", "screenshot-users.json");
export const exampleConfigPath = path.join(repoRoot, "tests", "screenshots", "config", "screenshot-users.example.json");

export function loadScreenshotConfig(): ScreenshotConfig {
  if (!fs.existsSync(configPath)) {
    throw new Error(`Missing screenshot config: ${configPath}. Copy ${exampleConfigPath} and fill test-only credentials.`);
  }

  const config = JSON.parse(fs.readFileSync(configPath, "utf8")) as ScreenshotConfig;
  for (const [role, user] of Object.entries(config.roles)) {
    if (!user.username || !user.password || user.password === "CHANGE_ME") {
      throw new Error(`Missing test credential for role '${role}'. Use test accounts only and do not commit screenshot-users.json.`);
    }
  }

  return config;
}
