import "@testing-library/jest-dom/vitest";
import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";
afterEach(() => cleanup());
Object.defineProperty(window.URL, "createObjectURL", { value: () => "blob:test", configurable: true });
Object.defineProperty(window.URL, "revokeObjectURL", { value: () => undefined, configurable: true });
