const DEFAULT_STATS_POLLING_INTERVAL = 3000;

export function getStatsPollingInterval () {
  const interval = Number(window.pollingInterval);

  return Number.isFinite(interval) && interval > 0
    ? interval
    : DEFAULT_STATS_POLLING_INTERVAL;
}

export function getStatsPollingIntervalSeconds () {
  return getStatsPollingInterval() / 1000;
}
