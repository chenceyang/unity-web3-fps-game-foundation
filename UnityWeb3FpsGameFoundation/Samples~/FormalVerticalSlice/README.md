# ASH//LEDGER Vertical Slice

This sample is generated from the package so that its scenes, materials and prefabs remain editable in the host Unity project.

1. Import this sample or leave it unimported; the generator itself is already available from the package.
2. Run `Tools > Web3 FPS > Create ASH LEDGER Vertical Slice`.
3. Open `Assets/AshLedgerVerticalSlice/Scenes/AshLedgerLobby.unity` and press Play.
4. Enter through the demo login/guest overlay, then select `DEPLOY TO RIFT RELAY`; the
   matchmaking-confirm page freezes the entitlement snapshot before loading combat, and the
   standalone post-match page offers REMATCH or BACK TO LOBBY afterwards.

The command creates:

- a UI Toolkit lobby connected to the mock NFT, wallet, reward and tournament services, with login, matchmaking-confirm and asset-detail overlays plus a standalone post-match page;
- a local authoritative match driver that freezes cosmetics before combat, keeps combat free of Web3 calls and publishes the finished result once with a visible outcome;
- an editable RIFT RELAY arena blockout;
- all six weapon blockout prefabs (KESTREL-7, PULSE-9, WITNESS, BREACH-12, ANCHOR, RELAY-3) with server-defined `WeaponDefinition` stats; in-match switching is not implemented yet;
- lobby archive displays and a first-person view model that recolor through the NFT skin catalog when a confirmed finish is equipped (visuals only, never combat numbers);
- a combat-only HUD with live magazine/reserve ammo, reload state and hit feedback, with no wallet, token or chain status;
- visual-only player and bot tracers that do not participate in authoritative damage;
- CC0 skeletal soldier FBX characters with generated idle, locomotion, firing and death animation states;
- staged magazine reload motion, weapon/camera recoil and original synthesized weapon audio;
- independently generated animation clips and a skeletal first-person arms view model with runtime bounds recovery;
- head/torso/leg hit zones, spawn protection and deterministic movement/ADS/bloom accuracy;
- a line-of-sight bot with reaction time, sight memory, range control and strafing;
- Build Settings entries for both scenes.

Controls: WASD move, Shift sprint, Space jump, mouse aim, left mouse fire, R reload, Escape unlock/lock cursor, and R restart after the result.

This is a production-oriented low-poly vertical slice, not final custom character art, motion capture, studio audio, networking or a production backend. NFT finishes are presentation-only and never change weapon statistics.
