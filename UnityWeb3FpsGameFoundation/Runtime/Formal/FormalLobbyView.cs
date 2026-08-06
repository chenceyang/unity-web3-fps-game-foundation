using System;
using Game.Web3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Web3Fps.GameFoundation.Lobby;
using Web3Fps.GameFoundation.Tournaments;

namespace Web3Fps.GameFoundation.Formal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class FormalLobbyView : MonoBehaviour
    {
        [SerializeField] private Web3LobbyController controller;
        [SerializeField] private UIDocument document;
        [SerializeField] private string playSceneName = FormalContentCatalog.RiftRelaySceneName;

        private VisualElement _root;
        private Label _status;
        private Label _wallet;
        private Label _proofFeed;
        private VisualElement _assetList;
        private VisualElement _rewardList;
        private VisualElement _tournamentList;
        private VisualElement[] _panels;
        private Button _playButton;
        private Button _refreshButton;
        private Button _walletButton;

        public void Configure(Web3LobbyController lobbyController, UIDocument uiDocument, string gameplaySceneName)
        {
            controller = lobbyController;
            document = uiDocument;
            playSceneName = string.IsNullOrWhiteSpace(gameplaySceneName)
                ? FormalContentCatalog.RiftRelaySceneName
                : gameplaySceneName;
        }

        private void OnEnable()
        {
            if (document == null) document = GetComponent<UIDocument>();
            _root = document == null ? null : document.rootVisualElement;
            if (_root == null) return;
            CacheElements();
            BindStaticActions();
            if (controller != null) controller.Changed += Render;
            ShowPanel(0);
            Render();
        }

        private void OnDisable()
        {
            if (controller != null) controller.Changed -= Render;
            if (_playButton != null) _playButton.clicked -= Play;
            if (_refreshButton != null) _refreshButton.clicked -= Refresh;
            if (_walletButton != null) _walletButton.clicked -= BindWallet;
        }

        private void CacheElements()
        {
            _status = _root.Q<Label>("status-label");
            _wallet = _root.Q<Label>("wallet-label");
            _proofFeed = _root.Q<Label>("proof-feed-label");
            _assetList = _root.Q<VisualElement>("asset-list");
            _rewardList = _root.Q<VisualElement>("reward-list");
            _tournamentList = _root.Q<VisualElement>("tournament-list");
            _playButton = _root.Q<Button>("play-button");
            _refreshButton = _root.Q<Button>("refresh-button");
            _walletButton = _root.Q<Button>("wallet-button");
            _panels = new[]
            {
                _root.Q<VisualElement>("panel-play"),
                _root.Q<VisualElement>("panel-arsenal"),
                _root.Q<VisualElement>("panel-vault"),
                _root.Q<VisualElement>("panel-tournaments"),
                _root.Q<VisualElement>("panel-profile")
            };
        }

        private void BindStaticActions()
        {
            if (_playButton != null) _playButton.clicked += Play;
            if (_refreshButton != null) _refreshButton.clicked += Refresh;
            if (_walletButton != null) _walletButton.clicked += BindWallet;
            BindNavigation("nav-play", 0);
            BindNavigation("nav-arsenal", 1);
            BindNavigation("nav-vault", 2);
            BindNavigation("nav-tournaments", 3);
            BindNavigation("nav-profile", 4);
        }

        private void BindNavigation(string elementName, int panelIndex)
        {
            var button = _root.Q<Button>(elementName);
            if (button != null) button.clicked += () => ShowPanel(panelIndex);
        }

        private void ShowPanel(int selected)
        {
            if (_panels == null) return;
            for (var i = 0; i < _panels.Length; i++)
            {
                if (_panels[i] != null)
                    _panels[i].style.display = i == selected ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void Play()
        {
            if (Application.CanStreamedLevelBeLoaded(playSceneName))
                SceneManager.LoadScene(playSceneName);
            else
                Debug.LogWarning("Gameplay scene is not available in Build Settings: " + playSceneName, this);
        }

        private void Refresh()
        {
            if (controller != null) _ = controller.RefreshAsync();
        }

        private void BindWallet()
        {
            var session = controller == null ? null : controller.Session;
            if (session != null && !session.Assets.HasWallet) _ = controller.BindWalletAsync();
        }

        private void Render()
        {
            var session = controller == null ? null : controller.Session;
            if (session == null)
            {
                SetText(_status, "INITIALIZING GAME FOUNDATION…");
                SetText(_wallet, "GUEST // DEFAULT GEAR READY");
                return;
            }

            SetText(_status, session.IsBusy ? "SYNCING PUBLIC ARCHIVE…" : session.StatusMessage.ToUpperInvariant());
            SetText(_wallet, session.Assets.HasWallet
                ? "BOUND // " + Shorten(session.Assets.wallet)
                : "GUEST // OPTIONAL WALLET");
            SetText(_proofFeed, session.LastErrorCode.Length == 0
                ? "MATCH RESULTS REMAIN PLAYABLE WHILE PUBLIC VERIFICATION RUNS ASYNCHRONOUSLY."
                : "ARCHIVE DEGRADED // NORMAL PLAY AVAILABLE");
            if (_playButton != null) _playButton.SetEnabled(!session.IsBusy);
            if (_refreshButton != null) _refreshButton.SetEnabled(!session.IsBusy);
            if (_walletButton != null)
            {
                _walletButton.text = session.Assets.HasWallet ? "WALLET BOUND" : "BIND OPTIONAL WALLET";
                _walletButton.SetEnabled(!session.IsBusy && !session.Assets.HasWallet);
            }
            RenderAssets(session);
            RenderRewards(session);
            RenderTournaments(session);
        }

        private void RenderAssets(Web3LobbySession session)
        {
            if (_assetList == null) return;
            _assetList.Clear();
            var defaults = CreateCard("DEFAULT LOADOUT", "Competitive baseline // always available", "READY");
            _assetList.Add(defaults);
            var items = session.Assets.items ?? Array.Empty<SkinItem>();
            foreach (var item in items)
            {
                if (item == null) continue;
                var card = CreateCard(
                    "RELIC // " + ShortenToken(item.tokenId),
                    "SERIAL #" + item.serial + " / " + item.maxSupply + "   SEASON " + item.seasonId +
                    "   WEAR " + Mathf.RoundToInt(item.wear * 100f) + "%",
                    item.IsConfirmed ? "VERIFIED" : item.state.ToUpperInvariant());
                var equip = new Button(() =>
                {
                    if (item.IsConfirmed) _ = controller.EquipAsync((int)FormalCosmeticSlot.WeaponFinish, item.tokenId);
                }) { text = item.IsConfirmed ? "EQUIP FINISH" : "PENDING" };
                equip.AddToClassList("card-action");
                equip.SetEnabled(item.IsConfirmed && !session.IsBusy);
                card.Add(equip);
                _assetList.Add(card);
            }
        }

        private void RenderRewards(Web3LobbySession session)
        {
            if (_rewardList == null) return;
            _rewardList.Clear();
            var rewards = session.Assets.pendingRewards ?? Array.Empty<PendingReward>();
            if (rewards.Length == 0)
            {
                _rewardList.Add(CreateCard("NO PENDING REWARDS", "Verified rewards will appear here.", "CLEAR"));
                return;
            }
            foreach (var reward in rewards)
            {
                if (reward == null) continue;
                var rewardId = reward.rewardId;
                var card = CreateCard("REWARD // " + rewardId, "SKIN " + reward.skinDefId + "   RARITY " + reward.rarity, "EARNED");
                var claim = new Button(() => _ = controller.ClaimRewardAsync(rewardId)) { text = "CLAIM" };
                claim.AddToClassList("card-action");
                claim.SetEnabled(!session.IsBusy);
                card.Add(claim);
                _rewardList.Add(card);
            }
        }

        private void RenderTournaments(Web3LobbySession session)
        {
            if (_tournamentList == null) return;
            _tournamentList.Clear();
            if (session.Tournaments.Length == 0)
            {
                _tournamentList.Add(CreateCard("NO OPEN TOURNAMENT", "Normal matchmaking remains online.", "STANDBY"));
                return;
            }
            foreach (var tournament in session.Tournaments)
            {
                if (tournament == null) continue;
                var tournamentId = tournament.tournamentId;
                var card = CreateCard(
                    tournament.title.ToUpperInvariant(),
                    tournament.participantCount + "/" + tournament.maxParticipants + " OPERATORS   POOL " + tournament.prizePoolWei + " WEI",
                    tournament.state.ToUpperInvariant());
                var register = new Button(() => _ = controller.RegisterTournamentAsync(tournamentId))
                {
                    text = tournament.registered ? "REGISTERED" : "REGISTER"
                };
                register.AddToClassList("card-action");
                register.SetEnabled(!session.IsBusy && !tournament.registered && tournament.ParsedState == TournamentState.Open);
                card.Add(register);
                _tournamentList.Add(card);
            }
        }

        private static VisualElement CreateCard(string title, string detail, string state)
        {
            var card = new VisualElement();
            card.AddToClassList("data-card");
            var header = new VisualElement();
            header.AddToClassList("card-header");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("card-title");
            var stateLabel = new Label(state);
            stateLabel.AddToClassList("state-chip");
            header.Add(titleLabel);
            header.Add(stateLabel);
            card.Add(header);
            var detailLabel = new Label(detail);
            detailLabel.AddToClassList("card-detail");
            card.Add(detailLabel);
            return card;
        }

        private static void SetText(Label label, string text)
        {
            if (label != null) label.text = text ?? string.Empty;
        }

        private static string Shorten(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "UNBOUND";
            return value.Length <= 12 ? value : value.Substring(0, 6) + "…" + value.Substring(value.Length - 4);
        }

        private static string ShortenToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "DEFAULT";
            return value.Length <= 14 ? value : value.Substring(0, 7) + "…" + value.Substring(value.Length - 5);
        }
    }
}
