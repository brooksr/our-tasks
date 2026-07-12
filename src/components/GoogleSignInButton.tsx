import { useEffect, useRef } from 'react';
import { renderGoogleSignInButton } from '../storage/googleAuth';

export function GoogleSignInButton({ disabled, onError, onSignedIn }: {
  disabled: boolean;
  onError: (error: Error) => void;
  onSignedIn: () => void | Promise<void>;
}) {
  const target = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!target.current) return;
    renderGoogleSignInButton(target.current, onSignedIn, onError).catch((error: unknown) => {
      onError(error instanceof Error ? error : new Error('Google Sign-In did not load.'));
    });
  }, [onError, onSignedIn]);

  return <div className={`google-signin-container ${disabled ? 'disabled' : ''}`} ref={target} aria-busy={disabled} />;
}
