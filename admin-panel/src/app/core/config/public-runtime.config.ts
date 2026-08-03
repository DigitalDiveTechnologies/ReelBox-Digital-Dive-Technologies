/**
 * Public runtime configuration only (PDF §14.2 / Appendix B).
 * No secrets.
 *
 * Values are loaded at startup from `/config.json` (deploy-time injectable).
 * Defaults stay empty so production builds never bake a localhost API URL.
 */
export const publicRuntimeConfig: {
  apiBaseUrl: string;
  signalRUrl: string;
} = {
  apiBaseUrl: '',
  signalRUrl: '',
};

/** Fetches `/config.json` once before auth restore. Missing file keeps defaults. */
export async function loadPublicRuntimeConfig(): Promise<void> {
  try {
    const response = await fetch('/config.json', { cache: 'no-store' });
    if (!response.ok) {
      return;
    }
    const json = (await response.json()) as {
      apiBaseUrl?: unknown;
      signalRUrl?: unknown;
    };
    if (typeof json.apiBaseUrl === 'string') {
      publicRuntimeConfig.apiBaseUrl = json.apiBaseUrl.trim();
    }
    if (typeof json.signalRUrl === 'string') {
      publicRuntimeConfig.signalRUrl = json.signalRUrl.trim();
    }
  } catch {
    // Offline / missing asset — leave defaults; AuthService surfaces a clear error.
  }
}
