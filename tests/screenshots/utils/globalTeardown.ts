import { writeScreenshotIndexAndReport } from "../helpers/captureRegistry";
import { updateManualImages } from "./updateManualImages";

async function globalTeardown() {
  writeScreenshotIndexAndReport();
  updateManualImages();
}

export default globalTeardown;
