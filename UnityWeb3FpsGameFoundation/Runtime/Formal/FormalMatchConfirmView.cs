using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Web3Fps.GameFoundation.Composition;
using Web3Fps.GameFoundation.Lobby;
using Web3Fps.GameFoundation.Server;

namespace Web3Fps.GameFoundation.Formal
{
    /// <summary>
    /// Pre-match confirm page (FORMAL_CONTENT_DESIGN §4.3 匹配确认). CONFIRM runs the
    /// match-start entitlement freeze — the only Web3 call of the match — stages the
    /// frozen ticket for the combat scene and then loads it. Entitlement failure only
    /// degrades cosmetics; it never blocks deployment.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FormalMatchConfirmView : MonoBehaviour
    {
        [SerializeField] private GameFoundationBootstrap bootstrap;
        [SerializeField] private Web3LobbyController controller;
        [SerializeField] private UIDocument document;
        [SerializeField] private string playSceneName = FormalContentCatalog.RiftRelaySceneName;

        private VisualElement _overlay;
        private Label _mode;
        private Label _map;
        private VisualElement _slotList;
        private Label _warning;
        private Label _status;
        private Button _accept;
        private Button _cancel;
        private CancellationTokenSource _lifetime;
        private bool _busy;

        public void Configure(
            GameFoundationBootstrap foundation,
            Web3LobbyController lobbyController,
            UIDocument uiDocument,
            string gameplaySceneName)
        {
            bootstrap = foundation;
            controller = lobbyController;
            document = uiDocument;
            playSceneName = string.IsNullOrWhiteSpace(gameplaySceneName)
                ? FormalContentCatalog.RiftRelaySceneName
                : gameplaySceneName;
        }

        private void Awake()
        {
            _lifetime = new CancellationTokenSource();
        }

        private void OnEnable()
        {
            if (document == null) document = GetComponent<UIDocument>();
            var root = document == null ? null : document.rootVisualElement;
            if (root == null) return;
            _overlay = root.Q<VisualElement>("confirm-overlay");
            _mode = root.Q<Label>("confirm-mode-label");
            _map = root.Q<Label>("confirm-map-label");
            _slotList = root.Q<VisualElement>("confirm-slot-list");
            _warning = root.Q<Label>("confirm-warning-label");
            _status = root.Q<Label>("confirm-status-label");
            _accept = root.Q<Button>("confirm-accept-button");
            _cancel = root.Q<Button>("confirm-cancel-button");
            if (_accept != null) _accept.clicked += OnConfirm;
            if (_cancel != null) _cancel.clicked += Hide;
        }

        private void OnDisable()
        {
            if (_accept != null) _accept.clicked -= OnConfirm;
            if (_cancel != null) _cancel.clicked -= Hide;
        }

        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
        }

        public void Show()
        {
            if (_overlay == null) return;
            RenderSummary();
            _overlay.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (_busy) return;
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
        }

        private void RenderSummary()
        {
            if (_mode != null) _mode.text = "MODE // " + FormalContentCatalog.ModeId.ToUpperInvariant();
            if (_map != null) _map.text = "MAP // " + FormalContentCatalog.MapId.ToUpperInvariant();
            var session = controller == null ? null : controller.Session;
            if (_slotList != null)
            {
                _slotList.Clear();
                var slotCount = ResolveSlotCount();
                for (var slot = 0; slot < slotCount; slot++)
                {
                    var tokenId = session == null ? string.Empty : session.GetEquippedTokenId(slot);
                    var line = new Label(DescribeSlot(slot, tokenId, FindItem(session, tokenId)));
                    line.AddToClassList("slot-line");
                    _slotList.Add(line);
                }
            }
            if (_warning != null) _warning.text = DescribeWarning(session, ResolveSlotCount());
            SetStatus("OWNERSHIP IS VERIFIED SERVER-SIDE BEFORE COMBAT BEGINS.");
        }

        // The configured LoadoutService is the authority on slot count; the catalog
        // names only label the slots that exist.
        private int ResolveSlotCount()
        {
            var context = bootstrap == null ? null : bootstrap.Context;
            var configured = context == null ? FormalContentCatalog.CosmeticSlotNames.Length : context.Loadout.SlotCount;
            return Mathf.Min(configured, FormalContentCatalog.CosmeticSlotNames.Length);
        }

        private void OnConfirm()
        {
            if (_busy) return;
            _ = ConfirmAsync();
        }

        private async Task ConfirmAsync()
        {
            var session = controller == null ? null : controller.Session;
            var context = bootstrap == null ? null : bootstrap.Context;
            _busy = true;
            if (_accept != null) _accept.SetEnabled(false);
            if (_cancel != null) _cancel.SetEnabled(false);
            try
            {
                LocalMatchTicket ticket = null;
                if (context != null && session != null)
                {
                    SetStatus("FREEZING LOADOUT SNAPSHOT…");
                    try
                    {
                        var slotCount = context.Loadout.SlotCount;
                        var equipped = new string[slotCount];
                        for (var slot = 0; slot < slotCount; slot++) equipped[slot] = session.GetEquippedTokenId(slot);
                        var request = LocalMatchFlow.CreateEntitlementRequest(
                            LocalMatchFlow.CreateMatchId(), bootstrap.PlayerId, session.Assets.wallet, equipped);
                        LocalMatchFlow.SyncMockChainView(context.EntitlementGateway, session.Assets);
                        ticket = await LocalMatchFlow.FreezeLoadoutAsync(
                            context.EntitlementResolver, request, context.MatchPublisher,
                            context.TournamentGateway, _lifetime.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception)
                    {
                        ticket = null;
                    }
                }
                // Entitlement problems can only degrade cosmetics — deployment proceeds.
                if (ticket == null)
                {
                    ticket = LocalMatchFlow.CreateOfflineTicket(
                        FormalContentCatalog.CosmeticSlotNames.Length,
                        bootstrap == null ? "guest" : bootstrap.PlayerId);
                    SetStatus("ASSET LAYER OFFLINE // DEPLOYING WITH DEFAULT COSMETICS.");
                }
                if (_warning != null && (ticket.UsedFallback || LocalMatchFlow.HasDegradedSlots(ticket.Snapshot)))
                    _warning.text = "ENTITLEMENT DEGRADED // DEFAULT COSMETICS THIS MATCH — PLAY CONTINUES.";
                LocalMatchHandoff.Stage(ticket);
                if (Application.CanStreamedLevelBeLoaded(playSceneName))
                {
                    SceneManager.LoadScene(playSceneName);
                }
                else
                {
                    SetStatus("GAMEPLAY SCENE MISSING FROM BUILD SETTINGS: " + playSceneName.ToUpperInvariant());
                    Debug.LogWarning("Gameplay scene is not available in Build Settings: " + playSceneName, this);
                }
            }
            finally
            {
                _busy = false;
                if (_accept != null) _accept.SetEnabled(true);
                if (_cancel != null) _cancel.SetEnabled(true);
            }
        }

        private void SetStatus(string message)
        {
            if (_status != null) _status.text = message ?? string.Empty;
        }

        private static SkinItem FindItem(Web3LobbySession session, string tokenId)
        {
            if (session == null || string.IsNullOrWhiteSpace(tokenId) || session.Assets?.items == null) return null;
            foreach (var item in session.Assets.items)
            {
                if (item != null && string.Equals(item.tokenId, tokenId, StringComparison.Ordinal)) return item;
            }
            return null;
        }

        public static string DescribeSlot(int slot, string tokenId, SkinItem item)
        {
            var slotName = FormalContentCatalog.GetSlotName(slot).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(tokenId))
                return "SLOT " + (slot + 1) + " " + slotName + " // DEFAULT ISSUE // READY";
            if (item == null)
                return "SLOT " + (slot + 1) + " " + slotName + " // TOKEN " + tokenId + " // NOT OWNED — DEFAULT THIS MATCH";
            var skinName = string.IsNullOrWhiteSpace(item.name) ? "SKIN " + item.skinDefId : item.name.ToUpperInvariant();
            return item.IsConfirmed
                ? "SLOT " + (slot + 1) + " " + slotName + " // " + skinName + " // CONFIRMED"
                : "SLOT " + (slot + 1) + " " + slotName + " // " + skinName + " // PENDING — DEFAULT THIS MATCH";
        }

        public static string DescribeWarning(Web3LobbySession session, int slotCount)
        {
            if (session == null) return "ASSET LAYER OFFLINE // DEFAULT COSMETICS GUARANTEED.";
            if (session.LastErrorCode.Length > 0)
                return "ASSET LAYER DEGRADED // DEFAULT COSMETICS GUARANTEED — PLAY IS NEVER BLOCKED.";
            for (var slot = 0; slot < slotCount; slot++)
            {
                var tokenId = session.GetEquippedTokenId(slot);
                if (string.IsNullOrWhiteSpace(tokenId)) continue;
                var item = FindItem(session, tokenId);
                if (item == null || !item.IsConfirmed)
                    return "SOME EQUIPPED RELICS ARE NOT CONFIRMED // THOSE SLOTS USE DEFAULTS.";
            }
            return string.Empty;
        }
    }
}
