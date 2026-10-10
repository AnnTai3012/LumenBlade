# Recovery anchors and DemonSlime attack pairs

Updated 2026-10-09.

Heal now anchors its image center to the player's body collider center. Revive anchors the image's bottom center to the body collider's feet. UI positions are converted using the effect root's actual parent RectTransform. Recovery initializes sprite-size caches and subscriptions on enable, including after script reload, and renders a skipped final frame once before hiding.

Level BossC2 currently contains two enabled MainCamera-tagged cameras. The old recovery code cached Camera.main, selecting the camera that did not render the player-centered view. Recovery now prefers an enabled scene camera with an active Cinemachine brain and excludes off-screen render targets; it refreshes selection every 0.5 seconds. Camera objects and scenes are not modified. Before/after Game View screenshots document the confirmed camera mismatch and corrected body/feet anchors.

DemonSlime's shared MonsterController uses an opt-in pair mode, configured only on its prefab. It starts two consecutive primary swings, bypassing the normal cooldown for the second swing while retaining independent damage windows and animation completion. New pairs start at least five seconds apart. In the interval and outside melee range, the boss follows the player; it does not start a swing out of range. Random counterattacks are suppressed in pair mode so they cannot add extra attacks. The boss may follow a player throughout its arena rather than reverting to patrol beyond its old detection range.

DemonSlime's melee initiation range is now 3 units. The earlier 1.25-unit center range was shorter than the contact distance of its scaled solid body collider, allowing it to chase without reaching attack eligibility. The authored attack collider and damage values are retained. Other monster prefab settings and their ordinary attack behavior are unchanged. Inspector options are Use Attack Pairs and Attack Pair Interval on MonsterController.

Unity compilation and 46 focused Play Mode checks passed. Tests cover actual Heal/Revive events, precise body-center and feet alignment before/after movement, final-frame retention, cache rebuilding, two consecutive boss swings, pursuit and damage at 2.6-unit separation, and a measured five-second pair interval. Final screenshots visually confirm alignment in the real BossC2 camera arrangement. Original BossC2 Edit Mode was restored; saved scenes were not changed. Evidence and DemonSlime backup: outputs/recovery-demonslime-20261009.
