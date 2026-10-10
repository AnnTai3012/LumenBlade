# Skill status HUD

Implemented 2026-10-09.

The three supplied Player/Ui/Skill_1.png, Skill_2.png and Skill_3.png images are assigned through GameResourceSettings.Skill HUD icons. Their complete 307x272 sprites replace the fallback skill-name text. Images preserve aspect ratio inside each frame, dim with availability and retain a visible hotkey badge and stamina cost. The original frame/state behavior remains. Nineteen runtime regressions passed after this addition, and outputs/skill-hud-20261009/icons.png shows the final layout. Missing icon references fall back to the skill-name text.

PlayerResourceHud creates and binds one PlayerSkillHud on its existing Canvas. All scenes using that HUD receive three 88x88 skill cells, anchored to the bottom-right with 36px horizontal and 28px vertical inset in Canvas reference units. The cells copy the same UiSpriteFrame texture, borders and scale used by CryHeal/CryShield. No scene or prefab rewiring is required.

Cells show Skill1/2/3, the actual configured K/I/U keys and stamina cost. Cyan highlights the skill being executed. Gold indicates readiness; dimmed frames report unavailable configuration, chapter lock, insufficient stamina, another action, pause or death. State is read from PlayerCombat, PlayerStamina, Health and PlayerMotor2D, without consuming stamina or triggering skills. The existing Skill2 unlock rule is preserved. The cells are status indicators and do not intercept pointer input.

PlayerStamina exposes read-only cost/affordability/unlock helpers and reuses the same cost calculation for spending. PlayerCombat exposes execution/configuration queries. PlayerInputReader exposes configured skill keys. HUD text/colors update only when state or cost changes. UI references survive Editor script reloads; no animation/cooldown timer is duplicated.

Validation: Unity compiled; 19 focused Play Mode checks passed for all three skills' ready/executing/insufficient-stamina states, busy alternatives, the actual chapter rule, pause, anchoring, frame texture, key bindings and pointer pass-through. Game View screenshots confirm ready and active layouts. Evidence: outputs/skill-hud-20261009/report.json, ready.png and executing.png. An initial readonly-reference issue during Play Mode script reload was repaired by preserving the generated UI references across reloads.
