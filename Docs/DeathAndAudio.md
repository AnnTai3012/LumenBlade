# Death animation completion and existing SFX

Updated 2026-10-09.

## Expanded sound library and adjustable Skill1–3 speeds

New catalog assignments use BlessOpen for the blessing panel, BossAppear for boss entrance, monster attack/monsterdeath for generic enemies, Revive for player resurrection and Skill2.wav for Skill2. Additional cues route Slime Attk and slime take hit to Green/Blue/Red Slime, DemonSlimeAttack to DemonSlime and Minic skill2 to Mimic's secondary attack. MonsterCombat caches routing from existing archetype/components in Awake. BossEncounter plays the appearance cue once on first player entry for DemonSlime/Bringer; Golem retains its authored entrance callback.

GameAudioCatalog now exposes nested Skill1 Timing, Skill2 Timing and Skill3 Timing settings. Match Animation defaults true; Speed Multiplier defaults 1. Automatic playback pitch equals sound length divided by animation duration, multiplied by the chosen factor. Disabling Match Animation uses Speed Multiplier as manual pitch. Unity playback pitch is clamped to 0.01–3. All three skills use isolated reusable timed AudioSources, with normal playback at 1.34557/2.26415/1.65442 respectively. Pitch changes tone as well as speed; larger factors can intentionally finish before the animation. Serialized enum values were preserved by appending new cues.

Unity compiled without errors. Twenty-two focused runtime checks passed: real prefab skill entry, exact automatic durations, manual/automatic overrides, clamping, four enemy attack routes, Mimic secondary/Slime hit routing, new catalog references and music/ordinary-SFX pitch independence. Evidence: expanded-sounds-report.json. The clean Level 3 scene was restored to Edit Mode; saved scenes/prefabs were not changed. The catalog is selected in Unity for customization.

## Game Over waits for the completed player Death pose

PlayerCheckpointRespawn now requires the actual Base Layer.Death state to reach normalizedTime >= 1 before proceeding; an unrelated current state no longer counts as completion. Missing/disabled Animator configuration retains the fallback delay. Game Over then waits one rendered frame plus gameOverDelayAfterDeath (default 0.25 seconds), and checks that the player is still dead before opening the panel. Revive Cross retains its completion wait without the additional Game Over pose hold.

An actual Level 3 Play Mode probe exercised the real player and restart panel at Animator speed 0.25, with Revive Cross counts consumed only in runtime validation. The panel stayed hidden throughout Death and opened after completion and the added hold. Unity compiled successfully. Evidence: gameover-before.json and gameover-after.json in outputs/death-audio-20261009. Original Edit Mode scene and inventory authoring were preserved.

## Skill1 and exit-gate timing synchronization

Skill1's sound lasts 1.368 seconds while Skill_1 lasts 1.01666665 seconds. PlayerCombat now uses GameAudioManager.PlayCueForDuration at successful Skill1 entry, with pitch 1.34557378 at normal Animator speed. The sound finishes with the clip without changing damage, stamina, hit windows or combat duration. Timed voices are reused and own their pitch; concurrent footsteps, UI and music remain at their original speed. Changing AudioSource pitch also changes the sound's tone. Volume changes propagate to the timed voices.

Exit Open is 0.9 seconds, while OpenExitGate lasts 4.752 seconds. LevelExitDoor now reads the assigned sound duration and sets its own Animator speed to 0.189393952 during accepted closed-gate opening. Its wait timeout accounts for the longer opening and uses scaled elapsed time so a paused animation does not exhaust the timeout. Disable/cancellation restores the earlier Animator speed and cancels the coroutine. Already-open travel skips opening playback. The audio catalog and animation assets do not require manual speed edits.

Unity compilation and nine focused Play Mode checks passed. A real Player prefab and cloned scene gate measured Skill1 audio completion at 1.026 seconds and gate animation completion at 4.758 seconds. Concurrent footstep pitch, duplicate-interaction rejection and gate speed restoration passed. Travel was deliberately suppressed in the timing fixture; no full scene-load test or human listening assessment was performed. Evidence: outputs/death-audio-20261009/sound-timing-report.json. The original dirty Level 3 scene was restored to Edit Mode.

## Additional sounds supplied later on 2026-10-09

The catalog now also assigns Theme background.wav as looping background music, Death.ogg to player/monster/boss death, and Assets/Components/Jump.mp3 to the existing successful-jump cue. ExitGateOpen is appended to the cue enum and assigned OpenExitGate.wav; LevelExitDoor plays it once when an accepted interaction starts opening a closed gate, before its animation. The later travel cue remains separate.

Unity compiled without errors. A short Play Mode check confirmed the looping music source and each of the five newly assigned SFX cues report active playback. Evidence: outputs/death-audio-20261009/new-sounds-report.json. Editor returned to Edit Mode. The following original implementation notes describe the earlier sound inventory.

Player and Slime Any State -> Death transitions previously allowed self transitions. While the Death bool stayed true, their clips restarted repeatedly. Both transitions now disable self transitions; other controller settings are preserved.

DeathAnimationPlayback waits for the current death state's normalized time to reach completion and retains the configured minimum delay. PlayerCheckpointRespawn uses this for both the restart panel and Revive Cross. MonsterController uses it before removing grounded corpses and after flying corpses land. Bringer explicitly enters Death and waits before destruction. Golem waits after landing rather than computing a fixed remaining duration that ignores Animator speed. Existing attack completion priority, Bat Death -> Fall, and Golem fall timeout remain in place.

GameAudioCatalog now references all 13 existing audio files in Assets/Components/Audio, covering 19 slots. UI panels reuse Click; gameplay assignments cover attack, jump attack, dash, footstep, fall impact, Skill1/2/3, healing, revival, blessing reward and chest opening. PlayerSkill3 and PlayerRevive enum values are appended to preserve existing cue indices. Skill3 no longer plays Skill1 audio, and PlayerController plays Revieve on revival instead of the heal-item clip. No death, enemy, boss, shield or music clips were supplied; those slots remain unassigned.

Validation: Unity 6000.3.18f1 compiled the runtime changes. A Play Mode fixture exercised real Player, Slime, Bat, Rat, Mimic, DemonSlime, Bringer and Golem prefabs with Animator speed 0.25, checking death entry/completion, corpse cleanup, player revival, catalog resolution and AudioSource playback. The final report passes 47/47 checks with no fixture runtime errors. Bat completion is observed through its authored exit-time transition to Fall. This checks playback state, not a human listening assessment. No standalone build was run.

Evidence and controller/catalog backups: outputs/death-audio-20261009. The first retained report used an overly strict last-frame sampling threshold for Bat; the final fixture checks its authored Fall transition. Earlier fixture attempts hit a reflection null-check error in validation code, subsequently corrected. A Pipeline command timeout is retained in the Console history; it is not a gameplay compile error.

The original dirty Level 3 Edit Mode scene was restored. Saved scenes and prefabs were not edited. Press Play normally to use the changes; no Inspector wiring is required.
