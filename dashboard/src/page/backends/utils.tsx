import { Backend, BackendHealth } from "#/lib/store/slice/types";
import { BackendWithHealth } from "./types";

export const getBackendWithHealth = (backends?: Backend[], backendsHealth?: BackendHealth[]) => {
    if (!backends || !backendsHealth) return [];
    return backends.map((backend: Backend   ) => {
    const health = backendsHealth.find(
      (health: BackendHealth) => health.Identifier === backend.Identifier
    );
    return { ...backend, health } as BackendWithHealth;
  });
};
