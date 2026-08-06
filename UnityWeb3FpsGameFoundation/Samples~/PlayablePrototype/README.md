# Playable Local Prototype

This sample is generated into the Unity project so its scene and prefabs remain editable.

1. Import this sample from Package Manager (optional; this guide is the imported asset).
2. Run `Tools > Web3 FPS > Create Local Prototype Scene`.
3. Open `Assets/Web3FpsPrototype/Prototype.unity` and press Play.

Controls: WASD move, Shift sprint, Space jump, mouse look, left click fire, Escape unlocks the cursor,
and R restarts after the result screen.

The generated deathmatch is deliberately offline and cosmetic-only. It uses
`LocalAuthoritativeShotSink`; replace that sink with the selected network SDK's authoritative server adapter
before treating any hit, score, or result as production truth.
