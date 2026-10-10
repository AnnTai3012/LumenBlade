# Revive image visibility fix

Updated 2026-10-09.

Confirmed in the real Level 1 scene: Health.OnRevived fires, the Revive Effect Frame is active, the sprite is valid and CanvasGroup alpha is 1, but its Animation child GameObject is inactive. The old playback path activates only the parent frame, leaving the image invisible. This is independent of the audio mix and camera projection.

PlayerRecoveryEffectUI.Play now explicitly activates the authored Image GameObject when starting a valid effect. The common playback path also restores a disabled healing image. Existing frame sequence, timing, body/feet anchoring, size, inventory and death wait are retained. No scene/prefab/animation assets were modified.

The real Revive Cross reproduction consumed exactly one cross and emitted one revive event. Before/after Game View captures confirm that the same authored frame was invisible before and visible after the fix. Evidence is retained under outputs/revive-fix-20261009. The regression probe covers repeated revival, disabled image children, pause, full-duration cleanup, HUD disable/re-enable and healing.

Unity 6000.3.18f1 compiled successfully; 20/20 focused Play Mode checks passed with no captured runtime errors. The original clean Level 1 Edit Mode scene was restored.

This fix concerns the existing OnRevived effect used by Revive Cross. Restart reloads a living player and does not emit OnRevived; that existing separate flow was observed during diagnosis.
