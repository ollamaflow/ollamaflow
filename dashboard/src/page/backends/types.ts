import { Backend, BackendHealth } from "#/lib/store/slice/types";

export type BackendWithHealth = Backend & {
    health: BackendHealth;
};