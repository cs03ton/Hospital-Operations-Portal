import fs from "node:fs";
import path from "node:path";
import { manualImageMappings } from "../helpers/manualMapping";
import { repoRoot, screenshotRoot } from "../helpers/screenshotConfig";

const phase1Dir = path.join(repoRoot, "docs", "manuals", "phase1");

export function updateManualImages() {
  if (!fs.existsSync(phase1Dir)) {
    return;
  }

  const markdownFiles = fs
    .readdirSync(phase1Dir)
    .filter((file) => file.endsWith(".md") && file !== "_combined.md")
    .map((file) => path.join(phase1Dir, file));

  for (const file of markdownFiles) {
    let content = fs.readFileSync(file, "utf8");
    let changed = false;

    for (const mapping of manualImageMappings) {
      const imagePath = path.join(screenshotRoot, mapping.file);
      if (!fs.existsSync(imagePath)) {
        continue;
      }

      if (content.includes(mapping.placeholder)) {
        content = content.split(mapping.placeholder).join(mapping.markdown);
        changed = true;
      }
    }

    if (changed) {
      fs.writeFileSync(file, content, "utf8");
    }
  }
}

if (require.main === module) {
  updateManualImages();
}
