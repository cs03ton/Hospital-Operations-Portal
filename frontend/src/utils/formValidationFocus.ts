export function focusInvalidFormField(root: HTMLElement | null, field: string) {
  const wrapper = root?.querySelector<HTMLElement>(`[data-validation-field="${field}"]`);
  const target = wrapper?.querySelector<HTMLElement>('input:not([disabled]):not([hidden]):not([aria-hidden="true"]):not([type="hidden"]), textarea:not([disabled]), [role="combobox"], button:not([disabled]), a[href]') ?? wrapper;
  target?.scrollIntoView?.({ behavior: "smooth", block: "center" });
  target?.focus({ preventScroll: true });
}
