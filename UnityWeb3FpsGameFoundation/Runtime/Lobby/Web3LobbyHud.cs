using UnityEngine;

namespace Web3Fps.GameFoundation.Lobby
{
    [DisallowMultipleComponent]
    public sealed class Web3LobbyHud : MonoBehaviour
    {
        [SerializeField] private Web3LobbyController controller;
        private Vector2 _scroll;
        private GUIStyle _titleStyle;
        private GUIStyle _sectionStyle;

        public void Configure(Web3LobbyController lobbyController) => controller = lobbyController;

        private void OnGUI()
        {
            EnsureStyles();
            var width = Mathf.Min(900f, Screen.width - 32f);
            var height = Mathf.Max(300f, Screen.height - 32f);
            GUILayout.BeginArea(new Rect((Screen.width - width) * 0.5f, 16f, width, height), GUI.skin.box);
            GUILayout.Label("WEB3 FPS · LOBBY", _titleStyle);
            GUILayout.Label("Web3 operations run only in this lobby. Asset failure never disables normal play.");

            var session = controller == null ? null : controller.Session;
            if (session == null)
            {
                GUILayout.Space(16f);
                GUILayout.Label("Initializing game foundation…");
                GUILayout.EndArea();
                return;
            }

            GUILayout.Space(8f);
            GUILayout.Label(session.StatusMessage, GUI.skin.box);
            GUI.enabled = !session.IsBusy;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(120f))) _ = controller.RefreshAsync();
            if (GUILayout.Button(session.Assets.HasWallet ? "Wallet Bound" : "Bind Wallet", GUILayout.Width(150f)) &&
                !session.Assets.HasWallet) _ = controller.BindWalletAsync();
            GUILayout.Label(session.Assets.HasWallet ? session.Assets.wallet : "No wallet (normal play available)");
            GUILayout.EndHorizontal();

            _scroll = GUILayout.BeginScrollView(_scroll);
            DrawLoadout(session);
            DrawRewards(session);
            DrawTournaments(session);
            GUILayout.EndScrollView();
            GUI.enabled = true;
            GUILayout.EndArea();
        }

        private void DrawLoadout(Web3LobbySession session)
        {
            GUILayout.Space(12f);
            GUILayout.Label("COSMETIC LOADOUT", _sectionStyle);
            for (var slot = 0; slot < 3; slot++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Slot " + (slot + 1) + ": " + DisplayToken(session.GetEquippedTokenId(slot)), GUILayout.Width(470f));
                if (GUILayout.Button("Use Default", GUILayout.Width(120f))) _ = controller.UseDefaultAsync(slot);
                GUILayout.EndHorizontal();
            }

            var items = session.Assets.items;
            if (items == null || items.Length == 0)
            {
                GUILayout.Label("No confirmed NFT skins. Default cosmetics remain usable.");
                return;
            }
            foreach (var item in items)
            {
                if (item == null) continue;
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(
                    "Token " + item.tokenId + " · Skin " + item.skinDefId + " · Rarity " + item.rarity +
                    " · " + (item.IsConfirmed ? "confirmed" : item.state),
                    GUILayout.Width(560f));
                GUI.enabled = !session.IsBusy && item.IsConfirmed;
                for (var slot = 0; slot < 3; slot++)
                {
                    var selectedSlot = slot;
                    if (GUILayout.Button("Equip " + (slot + 1), GUILayout.Width(82f)))
                        _ = controller.EquipAsync(selectedSlot, item.tokenId);
                }
                GUI.enabled = !session.IsBusy;
                GUILayout.EndHorizontal();
            }
        }

        private void DrawRewards(Web3LobbySession session)
        {
            GUILayout.Space(12f);
            GUILayout.Label("PENDING REWARDS", _sectionStyle);
            var rewards = session.Assets.pendingRewards;
            if (rewards == null || rewards.Length == 0)
            {
                GUILayout.Label("No pending rewards.");
                return;
            }
            foreach (var reward in rewards)
            {
                if (reward == null) continue;
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label("Reward " + reward.rewardId + " · Skin " + reward.skinDefId + " · Rarity " + reward.rarity);
                if (GUILayout.Button("Claim", GUILayout.Width(110f))) _ = controller.ClaimRewardAsync(reward.rewardId);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawTournaments(Web3LobbySession session)
        {
            GUILayout.Space(12f);
            GUILayout.Label("TOURNAMENTS", _sectionStyle);
            if (session.Tournaments.Length == 0)
            {
                GUILayout.Label("Tournament service unavailable or no open tournaments.");
                return;
            }
            foreach (var tournament in session.Tournaments)
            {
                if (tournament == null) continue;
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(tournament.title + " · " + tournament.state + " · " +
                                tournament.participantCount + "/" + tournament.maxParticipants + " players");
                GUILayout.Label("Entry fee (wei): " + tournament.entryFeeWei + " · Prize pool (wei): " + tournament.prizePoolWei);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Register", GUILayout.Width(110f))) _ = controller.RegisterTournamentAsync(tournament.tournamentId);
                if (GUILayout.Button("Sponsor entry fee", GUILayout.Width(150f)))
                    _ = controller.SponsorTournamentAsync(tournament.tournamentId, tournament.entryFeeWei);
                if (GUILayout.Button("Claim Prize", GUILayout.Width(110f))) _ = controller.ClaimPrizeAsync(tournament.tournamentId);
                if (GUILayout.Button("Refund", GUILayout.Width(90f))) _ = controller.ClaimRefundAsync(tournament.tournamentId);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;
            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            _sectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
        }

        private static string DisplayToken(string tokenId)
        {
            return string.IsNullOrWhiteSpace(tokenId) ? "Default" : tokenId;
        }
    }
}
