import "@testing-library/jest-dom/vitest";
import { afterEach } from "vitest";
import { cleanup } from "@testing-library/react";
const storedValues = new Map<string, string>();
const testLocalStorage: Storage = {
  get length() { return storedValues.size; },
  clear: () => storedValues.clear(),
  getItem: (key) => storedValues.get(key) ?? null,
  key: (index) => [...storedValues.keys()][index] ?? null,
  removeItem: (key) => { storedValues.delete(key); },
  setItem: (key, value) => { storedValues.set(key, String(value)); },
};
Object.defineProperty(globalThis, "localStorage", { value: testLocalStorage, configurable: true });
Object.defineProperty(window, "localStorage", { value: testLocalStorage, configurable: true });
afterEach(() => { cleanup(); testLocalStorage.clear(); });
Object.defineProperty(window.URL, "createObjectURL", { value: () => "blob:test", configurable: true });
Object.defineProperty(window.URL, "revokeObjectURL", { value: () => undefined, configurable: true });
