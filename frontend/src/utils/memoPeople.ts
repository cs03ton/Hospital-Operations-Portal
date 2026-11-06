import type { MemoOptions } from "../api/correspondenceApi";

export function memoApprovers(options: MemoOptions) {
  return options.approvers ?? [...new Map([...options.heads, ...options.directors].map(person => [person.id, person])).values()];
}

export function defaultMemoHead(options: MemoOptions, preferredId?: string | null): string {
  const allowed = new Set(memoApprovers(options).map(person => person.id));
  const ownHeads = options.heads.filter(person => !!options.proposer.departmentId && person.departmentId === options.proposer.departmentId && allowed.has(person.id));
  return ownHeads.find(person => person.id === preferredId)?.id ?? (ownHeads.length === 1 ? ownHeads[0].id : "");
}
