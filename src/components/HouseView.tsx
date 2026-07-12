import { useEffect, useRef, useState } from 'react';
import { Box, Eye, LoaderCircle } from 'lucide-react';
import { APP_CONFIG } from '../config';
import type { MaintenanceTask } from '../domain/types';
import { isValidUnitySelection, UnityMaintenanceBridge } from '../unity/bridge';

export function HouseView({ tasks, onSelection }: { tasks: MaintenanceTask[]; onSelection: (selection: { assetId?: string; roomId?: string }) => void }) {
  const frame = useRef<HTMLIFrameElement>(null);
  const [loaded, setLoaded] = useState(false);
  const [enabled, setEnabled] = useState(false);
  useEffect(() => {
    const listener = (event: MessageEvent) => {
      if (event.origin !== window.location.origin || event.data?.source !== 'unity-house' || !isValidUnitySelection(event.data.payload)) return;
      onSelection(event.data.payload);
    };
    window.addEventListener('message', listener); return () => window.removeEventListener('message', listener);
  }, [onSelection]);
  useEffect(() => {
    if (!loaded) return;
    const bridge = new UnityMaintenanceBridge(frame.current);
    bridge.enterMaintenanceMode(); bridge.refreshStatuses(tasks);
    return () => { bridge.exitMaintenanceMode(); };
  }, [loaded, tasks]);
  if (!enabled) return <section className="house-launch"><div className="house-orb"><Box aria-hidden="true" /></div><span className="eyebrow">Spatial view</span><h2>Your home, task-aware</h2><p>Open the 3D house to see maintenance by room and asset. The task dashboard stays available if WebGL is slow on this device.</p><button className="primary" type="button" onClick={() => setEnabled(true)}><Eye aria-hidden="true" /> Load house view</button><small>The 3D build is about 70 MB and is loaded only when requested.</small></section>;
  return <section className="house-frame-shell">{!loaded && <div className="house-loader"><LoaderCircle aria-hidden="true" /> Loading house…</div>}<iframe ref={frame} src={APP_CONFIG.unity.buildPath} title="Interactive 3D house" onLoad={() => setLoaded(true)} allow="fullscreen" /></section>;
}
