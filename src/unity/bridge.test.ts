import { describe, expect, it } from 'vitest';
import { isValidUnitySelection } from './bridge';

describe('Unity bridge payload validation', () => {
  it('accepts string IDs and rejects executable/object payloads', () => {
    expect(isValidUnitySelection({ assetId: 'asset-hot-tub', roomId: 'room-backyard' })).toBe(true);
    expect(isValidUnitySelection({ assetId: { dangerous: true } })).toBe(false);
    expect(isValidUnitySelection(null)).toBe(false);
  });
});
