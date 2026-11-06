import { useContext, useEffect } from "react";
import { UNSAFE_NavigationContext } from "react-router-dom";
import { confirmSystem } from "../components/common/SystemDialogHost";
const message = "มีข้อมูลที่ยังไม่ได้ดาวน์โหลด หากออกจากหน้านี้ข้อมูลจะถูกล้าง ต้องการออกหรือไม่?";
// Register before BrowserRouter's window listener, so POP navigation can be restored
// before the router unmounts the form while our asynchronous dialog is open.
const popGuards = new Set<(event: PopStateEvent) => void>();
if (typeof window !== "undefined") window.addEventListener("popstate", event => {
  for (const guard of popGuards) guard(event);
}, true);
export function useMemoLeaveGuard(dirty: boolean) {
  const { navigator } = useContext(UNSAFE_NavigationContext);
  useEffect(() => {
    if (!dirty) return;
    const controller = new AbortController();
    const push = navigator.push, replace = navigator.replace;
    let index = window.history.state?.idx ?? 0, pending = false, allowPop = false;
    let afterRestore: (() => void) | null = null;
    const approve = async (action: () => void) => {
      if (pending) return; pending = true;
      const accepted = await confirmSystem(message, controller.signal); pending = false;
      if (accepted && !controller.signal.aborted) action();
    };
    const wrappedPush: typeof push = (...args) => { void approve(() => { push.apply(navigator, args); index = window.history.state?.idx ?? index; }); };
    const wrappedReplace: typeof replace = (...args) => { void approve(() => replace.apply(navigator, args)); };
    navigator.push = wrappedPush; navigator.replace = wrappedReplace;
    // Closing/reloading the browser requires its native beforeunload warning.
    const unload = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ""; };
    const pop = (event: PopStateEvent) => {
      const next = event.state?.idx;
      if (typeof next !== "number") return;
      if (allowPop) { allowPop = false; index = next; return; }
      event.stopImmediatePropagation();
      if (next === index) { const restored = afterRestore; afterRestore = null; restored?.(); return; }
      if (!pending) {
        afterRestore = () => { pending = false; void approve(() => { allowPop = true; window.history.go(next - index); }); };
        pending = true;
      }
      window.history.go(index - next);
    };
    window.addEventListener("beforeunload", unload); popGuards.add(pop);
    return () => {
      controller.abort();
      if (navigator.push === wrappedPush) navigator.push = push;
      if (navigator.replace === wrappedReplace) navigator.replace = replace;
      window.removeEventListener("beforeunload", unload); popGuards.delete(pop);
    };
  }, [dirty, navigator]);
}
