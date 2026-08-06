# Changelog

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
