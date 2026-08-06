# Web3 Lobby

1. Run `Tools > Web3 FPS > Create Web3 Lobby Scene`.
2. Open `Assets/Web3FpsLobby/Lobby.unity` and press Play.
3. The generated scene uses the mock backend and does not open real wallet or transaction pages.

The lobby demonstrates inventory refresh, confirmed-only loadout selection, wallet binding, reward claiming,
tournament listing and transaction status. All actions are asynchronous and remain outside the combat frame loop.

For backend integration, disable the mock backend on `GameFoundationBootstrap`, configure the HTTPS game-backend
base URL and set the player's short-lived access token in memory after login. Never put secrets in the scene.
