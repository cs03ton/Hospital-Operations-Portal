// A registration identifier, not a decimal quantity: retain zeros and the suffix.
export function isMemoOutgoingNumber(value: string) {
  return value.length <= 20 && /^\d+(?:\.\d+)?$/.test(value);
}
