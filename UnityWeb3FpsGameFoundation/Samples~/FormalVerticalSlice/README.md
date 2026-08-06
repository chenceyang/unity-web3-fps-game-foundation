# ASH//LEDGER Vertical Slice

This sample is generated from the package so that its scenes, materials and prefabs remain editable in the host Unity project.

1. Import this sample or leave it unimported; the generator itself is already available from the package.
2. Run `Tools > Web3 FPS > Create ASH LEDGER Vertical Slice`.
3. Open `Assets/AshLedgerVerticalSlice/Scenes/AshLedgerLobby.unity` and press Play.
4. Select `DEPLOY TO RIFT RELAY` to enter the generated local combat scene.

The command creates:

- a UI Toolkit lobby connected to the mock NFT, wallet, reward and tournament services;
- an editable RIFT RELAY arena blockout;
- KESTREL-7, PULSE-9 and RELAY-3 blockout prefabs;
- a combat-only HUD with live magazine/reserve ammo, reload state and hit feedback, with no wallet, token or chain status;
- visual-only player and bot tracers that do not participate in authoritative damage;
- CC0 skeletal soldier FBX characters with generated idle, locomotion, firing and death animation states;
- staged magazine reload motion, weapon/camera recoil and original synthesized weapon audio;
- Build Settings entries for both scenes.

Controls: WASD move, Shift sprint, Space jump, mouse aim, left mouse fire, R reload, Escape unlock/lock cursor, and R restart after the result.

This is a production-oriented low-poly vertical slice, not final custom character art, motion capture, studio audio, networking or a production backend. NFT finishes are presentation-only and never change weapon statistics.
