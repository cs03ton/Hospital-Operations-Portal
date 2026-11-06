import { useEffect, useMemo, useState } from "react";

const moduleMenuStorageKey = "hop.sidebar.openModules";

export function useModuleMenuState(moduleIds: string[], activeModuleId?: string, storageKey = moduleMenuStorageKey, activePath?: string) {
  const initialOpenModuleIds = useMemo(() => readStoredModuleIds(moduleIds, activeModuleId, storageKey), [activeModuleId, moduleIds, storageKey]);
  const [openModuleIds, setOpenModuleIds] = useState<string[]>(initialOpenModuleIds);

  useEffect(() => {
    if (!activeModuleId) {
      return;
    }

    setOpenModuleIds((current) => (current.includes(activeModuleId) ? current : [...current, activeModuleId]));
  }, [activeModuleId, activePath]);

  useEffect(() => {
    localStorage.setItem(storageKey, JSON.stringify(openModuleIds));
  }, [openModuleIds, storageKey]);

  return {
    isModuleOpen: (moduleId: string) => openModuleIds.includes(moduleId),
    toggleModule: (moduleId: string) =>
      setOpenModuleIds((current) =>
        current.includes(moduleId) ? current.filter((item) => item !== moduleId) : [...current, moduleId],
      ),
  };
}

function readStoredModuleIds(moduleIds: string[], activeModuleId: string | undefined, storageKey: string) {
  const raw = localStorage.getItem(storageKey);
  const stored = raw ? (JSON.parse(raw) as string[]) : [];
  const validStored = stored.filter((moduleId) => moduleIds.includes(moduleId));

  if (activeModuleId && !validStored.includes(activeModuleId)) {
    return [...validStored, activeModuleId];
  }

  return validStored.length ? validStored : moduleIds.slice(0, 1);
}
