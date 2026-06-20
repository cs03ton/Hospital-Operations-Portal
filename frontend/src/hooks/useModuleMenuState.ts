import { useEffect, useMemo, useState } from "react";

const moduleMenuStorageKey = "hop.sidebar.openModules";

export function useModuleMenuState(moduleIds: string[], activeModuleId?: string) {
  const initialOpenModuleIds = useMemo(() => readStoredModuleIds(moduleIds, activeModuleId), [activeModuleId, moduleIds]);
  const [openModuleIds, setOpenModuleIds] = useState<string[]>(initialOpenModuleIds);

  useEffect(() => {
    if (!activeModuleId) {
      return;
    }

    setOpenModuleIds((current) => (current.includes(activeModuleId) ? current : [...current, activeModuleId]));
  }, [activeModuleId]);

  useEffect(() => {
    localStorage.setItem(moduleMenuStorageKey, JSON.stringify(openModuleIds));
  }, [openModuleIds]);

  return {
    isModuleOpen: (moduleId: string) => openModuleIds.includes(moduleId),
    toggleModule: (moduleId: string) =>
      setOpenModuleIds((current) =>
        current.includes(moduleId) ? current.filter((item) => item !== moduleId) : [...current, moduleId],
      ),
  };
}

function readStoredModuleIds(moduleIds: string[], activeModuleId?: string) {
  const raw = localStorage.getItem(moduleMenuStorageKey);
  const stored = raw ? (JSON.parse(raw) as string[]) : [];
  const validStored = stored.filter((moduleId) => moduleIds.includes(moduleId));

  if (activeModuleId && !validStored.includes(activeModuleId)) {
    return [...validStored, activeModuleId];
  }

  return validStored.length ? validStored : moduleIds.slice(0, 1);
}
