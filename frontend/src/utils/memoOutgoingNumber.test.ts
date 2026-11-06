import { expect, it } from "vitest";
import { isMemoOutgoingNumber } from "./memoOutgoingNumber";
it.each(["29", "29.1", "029.01", "12345678901234567890"])("accepts %s unchanged", value => {
  expect(isMemoOutgoingNumber(value)).toBe(true);
});
it.each(["", ".1", "29.", "29..1", "29.1.2", "29/1", "-29", "29a", "123456789012345678901"])("rejects %s", value => {
  expect(isMemoOutgoingNumber(value)).toBe(false);
});
