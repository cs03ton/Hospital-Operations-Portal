import { describe, expect, it } from "vitest";
import {
  orderRepairActions,
  primaryRepairAction,
} from "../utils/repairActions";

describe("repair detail action hierarchy", () => {
  it.each([
    ["Submitted", ["note", "cancel", "priority", "return", "start"], "start"],
    ["InProgress", ["return", "wait", "solve", "note"], "solve"],
    ["WaitingParts", ["priority", "resume", "return"], "resume"],
    ["Resolved", ["reject-solution", "accept"], "accept"],
    ["Returned", ["cancel", "resubmit"], "resubmit"],
    ["Closed", ["reopen"], "reopen"],
    ["Cancelled", [], undefined],
  ])("selects the primary next action for %s", (_status, actions, expected) => {
    expect(primaryRepairAction(actions)).toBe(expected);
  });

  it("orders actions by workflow importance and retains unknown server actions", () => {
    expect(
      orderRepairActions(["cancel", "note", "future-action", "solve", "wait"]),
    ).toEqual(["solve", "wait", "note", "cancel", "future-action"]);
  });
});
