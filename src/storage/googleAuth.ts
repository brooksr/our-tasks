import { AUTH_CONFIG } from '../config';

interface CredentialResponse { credential?: string; select_by?: string; }
interface GoogleButtonOptions {
  type?: 'standard' | 'icon';
  theme?: 'outline' | 'filled_blue' | 'filled_black';
  size?: 'large' | 'medium' | 'small';
  text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
  shape?: 'rectangular' | 'pill' | 'circle' | 'square';
  logo_alignment?: 'left' | 'center';
  width?: number;
}
interface GoogleAccounts {
  id: {
    initialize(options: {
      client_id: string;
      callback: (response: CredentialResponse) => void;
      auto_select?: boolean;
      use_fedcm_for_button?: boolean;
      button_auto_select?: boolean;
    }): void;
    renderButton(parent: HTMLElement, options: GoogleButtonOptions): void;
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

export async function renderGoogleSignInButton(
  parent: HTMLElement,
  onSignedIn: () => void | Promise<void>,
  onError: (error: Error) => void
) {
  const accounts = await waitForGoogle();
  accounts.id.initialize({
    client_id: AUTH_CONFIG.googleClientId,
    auto_select: false,
    use_fedcm_for_button: true,
    button_auto_select: false,
    callback: (response) => {
      if (!response.credential) { onError(new Error('Google did not return an identity token.')); return; }
      credential = response.credential;
      sessionStorage.setItem('our-tasks.google-id-token', credential);
      Promise.resolve(onSignedIn()).catch((error: unknown) => {
        onError(error instanceof Error ? error : new Error('Google sign-in failed.'));
      });
    }
  });
  parent.replaceChildren();
  accounts.id.renderButton(parent, {
    type: 'standard',
    theme: 'outline',
    size: 'large',
    text: 'continue_with',
    shape: 'rectangular',
    logo_alignment: 'left',
    width: Math.min(360, Math.max(240, parent.clientWidth || 320))
  });
}

export function getGoogleCredential() { return credential; }
export function signOutGoogle() {
  credential = null;
  sessionStorage.removeItem('our-tasks.google-id-token');
  window.google?.accounts.id.disableAutoSelect();
}
