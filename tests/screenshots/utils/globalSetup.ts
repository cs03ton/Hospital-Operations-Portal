import fs from "node:fs";
import path from "node:path";
import { recordsPath } from "../helpers/captureRegistry";

async function globalSetup() {
  fs.mkdirSync(path.dirname(recordsPath), { recursive: true });
  fs.writeFileSync(recordsPath, "", "utf8");
}

export default globalSetup;
