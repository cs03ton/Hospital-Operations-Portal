export const repairActionOrder = [
  "start",
  "resume",
  "solve",
  "accept",
  "resubmit",
  "reopen",
  "wait",
  "priority",
  "note",
  "return",
  "reject-solution",
  "cancel",
] as const;

const primaryActions = new Set([
  "start",
  "resume",
  "solve",
  "accept",
  "resubmit",
  "reopen",
]);

export function orderRepairActions(actions: string[]) {
  return [
    ...repairActionOrder.filter((item) => actions.includes(item)),
    ...actions.filter(
      (item) =>
        !repairActionOrder.includes(item as (typeof repairActionOrder)[number]),
    ),
  ];
}

export function primaryRepairAction(actions: string[]) {
  return orderRepairActions(actions).find((item) => primaryActions.has(item));
}
