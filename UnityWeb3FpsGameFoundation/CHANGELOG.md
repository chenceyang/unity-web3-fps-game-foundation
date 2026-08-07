# Changelog

## 1.8.0 - 2026-08-07

- Smoothed movement and aim: the input driver now runs before the motor and weapon and applies look rotation in the same frame, removing one frame of input/aim latency, and the motor snaps the controller onto descending slopes instead of bouncing.
- Removed steady per-frame allocation sources that caused GC hitches: the rig guard caches its renderer set, the bot uses non-allocating physics queries, combat tracers/muzzle flashes/impacts are pooled instead of created and destroyed per shot, weapon audio is synthesized up front, and the combat HUD only touches UI Toolkit when a readout actually changes.
- Realigned the `Game.Web3` layer with the current web3-fps-assets unity-sdk and `api/openapi.yaml`: added `GetConfigAsync`/`ChainConfig`, the full reward state machine (earned/held/claimable/processing/pending_chain/confirmed/failed), `Amount` wei+formatted money, and replaced the four tournament intent routes plus transaction polling with the single `/v1/tournaments/{id}/intents/{action}` route, `TournamentDetail` trust fields and post-match `GetMatchAsync` records.
- Tournament transactions no longer poll: the coordinator creates the intent, opens the system browser and the lobby refreshes state from the backend after the transaction lands on-chain.
- Hardened the HTTP boundary: shared `HttpApiClient` with unified auth/error mapping, null-safe handling of blank 2xx bodies in the bind/claim poll loops, and match result publishing now raises a dedicated conflict error on HTTP 409 that fails fast instead of retrying a doomed payload.
- Declared the built-in module dependencies in `package.json`, generated deterministic `.meta` files for every asset (`tools/generate_unity_metas.py`) so cross-machine GUID references stay stable, and removed the stale package-local backend OpenAPI copy in favour of the single upstream contract.
- Static compilation and the EditMode suite have not been re-run on this machine (no Unity/Windows audit host available); 61 test cases are defined and must be verified in the next Unity session.

## 1.7.0 - 2026-08-07

- Added explicit head, torso and leg damage zones, self-hit filtering, spawn protection and damage-only hit confirmation.
- Added deterministic hip/ADS/movement/bloom spread, surface and damage impacts, while retaining the existing recoil, reload and synthesized audio presentation.
- Copied embedded FBX motions into independently editable generated animation assets and created a skeletal Cobalt first-person arms mesh with a guarded procedural fallback.
- Upgraded the Coral bot with sight checks, reaction delay, last-seen memory, preferred range, strafing, bounded aim error and Recruit/Standard/Veteran presets.
- Rebuilt RIFT RELAY into clearer staggered combat lanes, reduced the non-combat relay silhouette and added readable spawn-side shielding.
- Exposed skeletal recovery state and re-validates the character rig after respawn; static Runtime/Editor/Test compilation passes 53 tests.

## 1.6.4 - 2026-08-07

- Fixed the actual moving giant: Generic FBX animation was replaying imported unit/axis transform curves and exploding the Coral skinned hierarchy at runtime.
- Enabled baked axis conversion, preserved hierarchy and removed redundant constant scale curves during character import.
- Added a runtime rig-bounds guard that disables a corrupt Animator and restores the captured rest pose before rendering; a safe humanoid fallback remains available.
- Added normal and exploded rig-bounds coverage; the static audit now passes 47 tests.

## 1.6.3 - 2026-08-07

- Correctly identified the apparent giant as the 7.6-metre central relay architecture, not the 1.9-metre skeletal enemy.
- Moved both opening spawns onto a clear north lane so the Coral operator is centered and visible immediately without relay or cover occlusion.
- Reduced the relay silhouette and glow to a readable map objective instead of a character-like screen-dominating structure.
- Added opening-lane clearance regression coverage; the static audit now passes 45 tests.

## 1.6.2 - 2026-08-07

- Rebalanced the first-person view model so temporary arm blockouts no longer dominate the camera or resemble malformed characters.
- Preserved the skeletal enemy's imported materials while adding a readable Coral team tint, enforced skinned-mesh visibility and full shadow rendering.
- Moved the initial enemy into a clear sightline and replaced the tiny beacon with a high-contrast team mark.

## 1.6.1 - 2026-08-07

- Moved the Coral bot spawn out from behind the solid relay core and added lightweight obstacle steering so it remains visible and can pursue the player.
- Added a coral enemy-identification beacon and reduced the generated first-person arm scale to preserve the combat sightline.
- Added steering coverage; the static audit now passes 43 tests.

## 1.6.0 - 2026-08-07

- Imported two commercial-use CC0 skeletal low-poly soldier FBX assets with embedded idle, locomotion, shooting, jump and death animation clips.
- Added generated Animator controllers and a runtime character animation bridge for bot locomotion, firing and death states.
- Added a staged first-person reload animation, magazine motion, weapon kick and recoverable camera recoil.
- Added original runtime-synthesized gunshot, magazine-release and magazine-seat audio without shipping third-party sound samples.
- Recorded third-party source, license and SHA-256 provenance; the static audit now passes 41 tests.

## 1.5.0 - 2026-08-07

- Added a 30-round magazine, reserve ammunition, timed reload on R and respawn refill with isolated EditMode coverage.
- Reworked the combat HUD around a low-interference survival-FPS layout with live ammo, reserve, reload progress, empty-state warning, hit marker and compact loadout strip.
- Added visual-only player and bot tracers plus muzzle flashes while preserving authoritative hitscan as the sole damage result.
- Upgraded the generated low-poly operator preview, first-person hands, armor details and view-model alignment; the static audit now passes 39 tests.

## 1.4.6 - 2026-08-07

- Added a runtime PanelSettings fallback so the UI Toolkit lobby and combat HUD recover when Unity 6 clears UIDocument's scene reference.
- Stores the generated PanelSettings on the formal view components, preserving the default theme during runtime recovery.
- Added formal panel default coverage; the static audit now passes 36 tests.

## 1.4.5 - 2026-08-07

- Reloads PanelSettings after synchronous asset import and writes the UIDocument reference after Unity 6 OnEnable completes.
- Adds a generation-time assertion so a lobby with a missing PanelSettings reference can no longer be saved silently.
- Generates reverse-facing orbital backdrop geometry for pipelines whose unlit shader has fixed back-face culling.

## 1.4.4 - 2026-08-07

- Selects Built-in, URP or HDRP shaders from the project's active render pipeline instead of installed shader availability.
- Prevents the scene generator from running in Play Mode or while scripts are compiling.
- Eliminates the magenta-material failure in Built-in Render Pipeline projects.

## 1.4.3 - 2026-08-07

- Fixed Unity 6 dropping the generated UIDocument PanelSettings reference, which prevented the lobby and combat UI from rendering.
- Persisted UI Toolkit panel, UXML and sorting-order references explicitly in generated scenes.
- Made the orbital backdrop material double-sided so it remains visible from the gameplay camera.

## 1.4.2 - 2026-08-06

- Removed the formal scene's dependency on legacy generated Prototype prefabs, preventing missing-script actor instances.
- Added a layered low-poly Coral operator with textured ceramic armor, helmet, visor, equipment, limbs and weapon.
- Added visible first-person Cobalt arms, gloves, armor and wrist signal around the KESTREL-7 view model.

## 1.4.1 - 2026-08-06

- Replaced the enclosed color-block arena presentation with an open orbital-industrial RIFT RELAY visual slice.
- Added a project-owned broken-ring orbital backdrop, fog, emissive navigation, industrial towers, relay lighting and architectural framing.
- Improved the three weapon blockouts and fixed view-model instantiation so the combat scene is saved reliably.

## 1.4.0 - 2026-08-06

- Added a one-click ASH//LEDGER vertical-slice generator with a formal UI Toolkit lobby and combat HUD.
- Added the editable RIFT RELAY three-lane arena blockout and KESTREL-7, PULSE-9 and RELAY-3 weapon prefabs.
- Connected mock wallet, NFT, reward and tournament state to the formal lobby without placing Web3 work in combat.
- Added formal content catalog tests; the static Unity audit now passes 35 tests.

## 1.3.1 - 2026-08-06

- Added the formal UI, art direction, launch-map, weapon-content and Web3 creativity specification.
- Added the ASH//LEDGER vertical-slice concept board for production alignment.
- Defined the three cosmetic loadout slots and explicit competitive-readability constraints for NFT content.

## 1.3.0 - 2026-08-06

- Added a dedicated-server entitlement adapter and a validated, default-cosmetic loadout snapshot resolver.
- Added an SDK-neutral authoritative match session that freezes player cosmetics before combat starts.
- Added a publish-once match result guard: failed attempts remain retryable and conflicting results are rejected.
- Added the internal entitlement-check OpenAPI contract and EditMode coverage for server boundaries.

## 1.2.0 - 2026-08-06

- Added a testable lobby session for NFT inventory, loadout, wallet, reward and tournament operations.
- Added a functional IMGUI Web3 lobby and a Unity menu command that generates its mock-backed scene.
- Added a mock URL recorder so editor demos never open real wallet or transaction pages.
- Added EditMode coverage for refresh, string token IDs, wallet/reward flow and tournament registration.

## 1.1.0 - 2026-08-06

- Added a Unity editor command that generates an editable local FPS scene and player/bot prefabs.
- Added offline deathmatch rules, bot combat, respawn, score, timer, result screen and restart flow.
- Added a dependency-free IMGUI HUD and playable prototype guide.
- Added EditMode coverage for target-kill, timeout, draw and reset rules.

## 1.0.0 - 2026-08-05

- Initial game-side foundation for gameplay, NFT assets, rewards, tournaments and match attestations.
