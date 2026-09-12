import { apiFetch } from './client';

export interface WindowsAppConfig {
  launchOnStartup: boolean;
  minimizeToTray: boolean;
  closeToTray: boolean;
  globalMediaKeysEnabled: boolean;
  notificationsEnabled: boolean;
}

export interface WindowsNotification {
  title: string;
  message: string;
  timestampUtc: string;
}

export function fetchWindowsConfig(): Promise<WindowsAppConfig> {
  return apiFetch<WindowsAppConfig>('/platform/windows/config');
}

export function updateWindowsConfig(config: WindowsAppConfig): Promise<WindowsAppConfig> {
  return apiFetch<WindowsAppConfig>('/platform/windows/config', {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(config)
  });
}

export function fetchTrayState(): Promise<{ visible: boolean }> {
  return apiFetch<{ visible: boolean }>('/platform/windows/tray');
}

export function setTrayVisibility(visible: boolean): Promise<{ visible: boolean }> {
  return apiFetch<{ visible: boolean }>('/platform/windows/tray/visibility', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ visible })
  });
}

export function sendWindowsNotification(title: string, message: string): Promise<void> {
  return apiFetch<void>('/platform/windows/notifications', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ title, message })
  });
}

export function fetchNotificationHistory(): Promise<WindowsNotification[]> {
  return apiFetch<WindowsNotification[]>('/platform/windows/notifications/history');
}

export function fetchFileAssociations(): Promise<{ extensions: string[] }> {
  return apiFetch<{ extensions: string[] }>('/platform/windows/associations');
}

export function registerFileAssociations(extensions: string[]): Promise<{ extensions: string[] }> {
  return apiFetch<{ extensions: string[] }>('/platform/windows/associations/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ extensions })
  });
}

export function triggerMediaKey(action: 'play' | 'pause' | 'next' | 'previous' | 'stop'): Promise<{ handled: boolean; action: string }> {
  return apiFetch<{ handled: boolean; action: string }>('/platform/windows/mediakeys', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ action })
  });
}

export function exportBackupUrl(): string {
  return '/api/platform/windows/backup/export';
}
