export const SECURITY_ALERT_EVENT_TYPES = {
  suspiciousLogin: 'Security.Alert.SuspiciousLogin',
  criticalAction: 'Security.Alert.CriticalAction'
} as const;

export const SECURITY_ALERT_EVENT_TYPE_OPTIONS: readonly string[] = [
  SECURITY_ALERT_EVENT_TYPES.suspiciousLogin,
  SECURITY_ALERT_EVENT_TYPES.criticalAction
];