import { AUTH_CONFIG } from '../config';

interface CredentialResponse { credential?: string; select_by?: string; }
interface GoogleAccounts {
  id: {
    initialize(options: { client_id: string; callback: (response: CredentialResponse) => void; auto_select?: boolean }): void;
    prompt(callback?: (notification: { isNotDisplayed(): boolean; getNotDisplayedReason(): string }) => void): void;
    disableAutoSelect(): void;
  };
}

declare global { interface Window { google?: { accounts: GoogleAccounts }; } }

let credential = sessionStorage.getItem('our-tasks.google-id-token');

function waitForGoogle() {
  return new Promise<GoogleAccounts>((resolve, reject) => {
    const started = Date.now();
    const timer = window.setInterval(() => {
      if (window.google?.accounts) { clearInterval(timer); resolve(window.google.accounts); }
      else if (Date.now() - started > 8_000) { clearInterval(timer); reject(new Error('Google Sign-In did not load.')); }
    }, 50);
  });
}

export async function signInWithGoogle() {
  const accounts = await waitForGoogle();
  return new Promise<string>((resolve, reject) => {
    accounts.id.initialize({
      client_id: AUTH_CONFIG.googleClientId,
      auto_select: true,
      callback: (response) => {
        if (!response.credential) { reject(new Error('Google did not return an identity token.')); return; }
        credential = response.credential;
        sessionStorage.setItem('our-tasks.google-id-token', credential);
        resolve(credential);
      }
    });
    accounts.id.prompt((notice) => {
      if (notice.isNotDisplayed()) reject(new Error(`Google Sign-In unavailable: ${notice.getNotDisplayedReason()}`));
    });
  });
}

export function getGoogleCredential() { return credential; }
export function signOutGoogle() {
  credential = null;
  sessionStorage.removeItem('our-tasks.google-id-token');
  window.google?.accounts.id.disableAutoSelect();
}
