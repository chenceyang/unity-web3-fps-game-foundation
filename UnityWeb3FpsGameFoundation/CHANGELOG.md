# Changelog

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
