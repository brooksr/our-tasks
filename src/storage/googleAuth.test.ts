import { beforeEach, describe, expect, it, vi } from 'vitest';
import { getGoogleCredential, renderGoogleSignInButton } from './googleAuth';

describe('Google Sign-In button', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  it('uses the explicit FedCM button flow without invoking One Tap', async () => {
    let callback: ((response: { credential?: string }) => void) | undefined;
    const initialize = vi.fn((options: { callback: typeof callback }) => { callback = options.callback; });
    const renderButton = vi.fn();
    Object.defineProperty(window, 'google', {
      configurable: true,
      value: { accounts: { id: { initialize, renderButton, disableAutoSelect: vi.fn() } } }
    });
    const signedIn = vi.fn();
    const failed = vi.fn();
    const parent = document.createElement('div');

    await renderGoogleSignInButton(parent, signedIn, failed);

    expect(initialize).toHaveBeenCalledWith(expect.objectContaining({
      auto_select: false,
      use_fedcm_for_button: true,
      button_auto_select: false
    }));
    expect(renderButton).toHaveBeenCalledOnce();
    callback?.({ credential: 'test-id-token' });
    await Promise.resolve();
    expect(getGoogleCredential()).toBe('test-id-token');
    expect(signedIn).toHaveBeenCalledOnce();
    expect(failed).not.toHaveBeenCalled();
  });
});
