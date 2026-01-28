import React from "react";
import { BackendHealth } from "#/lib/store/slice/types";
import styles from "./backed-health-info.module.scss";
import { formatDate } from "#/utils/utils";

const BackendHealthInfo = ({ health }: { health: BackendHealth }) => {
  return (
    <div className={styles.backendHealthInfo}>
      <div className={styles.backendHealthInfoItem}>
        <div className={styles.key}>Identifier</div>
        <div className={styles.value}>{health.Identifier}</div>
      </div>
      <div className={styles.backendHealthInfoItem}>
        <div className={styles.key}>Name</div>
        <div className={styles.value}>{health.Name}</div>
      </div>
      <div className={styles.backendHealthInfoItem}>
        <div className={styles.key}>Healthy Since</div>
        <div className={styles.value}>
          {health.HealthySinceUtc ? formatDate(health.HealthySinceUtc) : "N/A"}
        </div>
      </div>
      <div className={styles.backendHealthInfoItem}>
        <div className={styles.key}>Unhealthy Since</div>
        <div className={styles.value}>
          {health.UnhealthySinceUtc
            ? formatDate(health.UnhealthySinceUtc)
            : "N/A"}
        </div>
      </div>
      <div className={styles.backendHealthInfoItem}>
        <div className={styles.key}>Uptime</div>
        <div className={styles.value}>
          {health.Uptime ? (
            <span style={{ fontFamily: "monospace" }}>{health.Uptime}</span>
          ) : (
            "N/A"
          )}
        </div>
      </div>
      <div className={styles.backendHealthInfoItem}>
        <div className={styles.key}>Downtime</div>
        <div className={styles.value}>
          {health.Downtime ? (
            <span style={{ fontFamily: "monospace" }}>{health.Downtime}</span>
          ) : (
            "N/A"
          )}
        </div>
      </div>
      <div className={styles.backendHealthInfoItem}>
        <div className={styles.key}>Active Requests</div>
        <div className={styles.value}>
          <span
            style={{
              color:
                health.ActiveRequests && health.ActiveRequests > 0
                  ? "#1890ff"
                  : "#888",
            }}
          >
            {health.ActiveRequests || 0}
          </span>
        </div>
      </div>
    </div>
  );
};

export default BackendHealthInfo;
