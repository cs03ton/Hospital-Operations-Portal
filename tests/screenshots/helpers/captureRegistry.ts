import fs from "node:fs";
import path from "node:path";
import { indexPath, mappingPath, reportPath, screenshotRoot } from "./screenshotConfig";

export type CaptureStatus = "Captured" | "Failed" | "Skipped";

export type CaptureRecord = {
  role: string;
  module: string;
  step: string;
  file: string;
  manual?: string;
  status: CaptureStatus;
  message?: string;
  startedAt: string;
  completedAt: string;
  durationMs: number;
};

const records: CaptureRecord[] = [];
const startedAt = Date.now();
export const recordsPath = path.join(screenshotRoot, ".capture-records.jsonl");

export function recordCapture(record: CaptureRecord) {
  records.push(record);
  fs.mkdirSync(screenshotRoot, { recursive: true });
  fs.appendFileSync(recordsPath, `${JSON.stringify(record)}\n`, "utf8");
}

export function getCaptureRecords() {
  return [...records];
}

export function writeScreenshotIndexAndReport() {
  fs.mkdirSync(screenshotRoot, { recursive: true });
  const persistedRecords = readPersistedRecords();
  writeIndex(persistedRecords);
  writeMapping(persistedRecords);
  writeReport(persistedRecords);
}

function writeIndex(captureRecords: CaptureRecord[]) {
  const byRole = groupBy(captureRecords, (item) => item.role);
  const lines = ["# HOP Screenshot Index", "", "Generated after Playwright screenshot capture.", ""];

  for (const role of ["user", "head", "director", "hr", "superadmin"]) {
    lines.push(`## ${role}`);
    lines.push("");
    lines.push("| Step | File | Status |");
    lines.push("|---|---|---|");
    for (const record of byRole.get(role) ?? []) {
      lines.push(`| ${escapePipe(record.step)} | \`${record.file}\` | ${record.status} |`);
    }
    lines.push("");
  }

  fs.writeFileSync(indexPath, `${lines.join("\n")}\n`, "utf8");
}

function writeMapping(captureRecords: CaptureRecord[]) {
  const lines = ["# Manual Screenshot Mapping", "", "| Manual File | Screenshot | Step | Status |", "|---|---|---|---|"];
  for (const record of captureRecords.filter((item) => item.manual)) {
    lines.push(`| \`${record.manual}\` | \`${record.file}\` | ${escapePipe(record.step)} | ${record.status} |`);
  }
  fs.writeFileSync(mappingPath, `${lines.join("\n")}\n`, "utf8");
}

function writeReport(captureRecords: CaptureRecord[]) {
  const total = captureRecords.length;
  const success = captureRecords.filter((item) => item.status === "Captured").length;
  const failed = captureRecords.filter((item) => item.status === "Failed").length;
  const skipped = captureRecords.filter((item) => item.status === "Skipped").length;
  const elapsedMs = Date.now() - startedAt;

  const lines = [
    "# HOP Screenshot Capture Report",
    "",
    `Generated at: ${new Date().toISOString()}`,
    "",
    "| Metric | Value |",
    "|---|---:|",
    `| Total screenshots | ${total} |`,
    `| Captured | ${success} |`,
    `| Failed | ${failed} |`,
    `| Skipped | ${skipped} |`,
    `| Elapsed seconds | ${(elapsedMs / 1000).toFixed(1)} |`,
    "",
    "## Details",
    "",
    "| Role | Module | Step | File | Status | Message |",
    "|---|---|---|---|---|---|",
  ];

  for (const record of captureRecords) {
    lines.push(
      `| ${record.role} | ${record.module} | ${escapePipe(record.step)} | \`${record.file}\` | ${record.status} | ${escapePipe(record.message ?? "")} |`,
    );
  }

  fs.writeFileSync(reportPath, `${lines.join("\n")}\n`, "utf8");
}

function readPersistedRecords() {
  if (!fs.existsSync(recordsPath)) {
    return records;
  }

  return fs
    .readFileSync(recordsPath, "utf8")
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => JSON.parse(line) as CaptureRecord);
}

function groupBy<T>(items: T[], keySelector: (item: T) => string) {
  const map = new Map<string, T[]>();
  for (const item of items) {
    const key = keySelector(item);
    map.set(key, [...(map.get(key) ?? []), item]);
  }
  return map;
}

function escapePipe(value: string) {
  return value.replace(/\|/g, "\\|").replace(/\r?\n/g, " ");
}
