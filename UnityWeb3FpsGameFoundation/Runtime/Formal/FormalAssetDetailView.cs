using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using UnityEngine;
using UnityEngine.UIElements;
using Web3Fps.GameFoundation.Composition;
using Web3Fps.GameFoundation.Lobby;

namespace Web3Fps.GameFoundation.Formal
{
    /// <summary>
    /// Per-item detail page (FORMAL_CONTENT_DESIGN §4.3 资产详情). tokenId stays a
    /// decimal string end to end; the marketplace jump uses the backend ChainConfig
    /// URL and always opens through IExternalUrlLauncher (system browser only).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FormalAssetDetailView : MonoBehaviour
    {
        [SerializeField] private GameFoundationBootstrap bootstrap;
        [SerializeField] private Web3LobbyController controller;
        [SerializeField] private UIDocument document;

        private VisualElement _overlay;
        private Label _title;
        private Label _provenance;
        private Label _token;
        private Label _hash;
        private Label _state;
        private Label _status;
        private Button _equip;
        private Button _useDefault;
        private Button _market;
        private Button _close;
        private SkinItem _item;
        private string _marketplaceUrl;
        private CancellationTokenSource _lifetime;

        public void Configure(
            GameFoundationBootstrap foundation,
            Web3LobbyController lobbyController,
            UIDocument uiDocument)
        {
            bootstrap = foundation;
            controller = lobbyController;
            document = uiDocument;
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
            _overlay = root.Q<VisualElement>("detail-overlay");
            _title = root.Q<Label>("detail-title-label");
            _provenance = root.Q<Label>("detail-provenance-label");
            _token = root.Q<Label>("detail-token-label");
            _hash = root.Q<Label>("detail-hash-label");
            _state = root.Q<Label>("detail-state-label");
            _status = root.Q<Label>("detail-status-label");
            _equip = root.Q<Button>("detail-equip-button");
            _useDefault = root.Q<Button>("detail-default-button");
            _market = root.Q<Button>("detail-market-button");
            _close = root.Q<Button>("detail-close-button");
            if (_equip != null) _equip.clicked += OnEquip;
            if (_useDefault != null) _useDefault.clicked += OnUseDefault;
            if (_market != null) _market.clicked += OnOpenMarket;
            if (_close != null) _close.clicked += Hide;
        }

        private void OnDisable()
        {
            if (_equip != null) _equip.clicked -= OnEquip;
            if (_useDefault != null) _useDefault.clicked -= OnUseDefault;
            if (_market != null) _market.clicked -= OnOpenMarket;
            if (_close != null) _close.clicked -= Hide;
        }

        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
        }

        public void Show(SkinItem item)
        {
            if (item == null || _overlay == null) return;
            _item = item;
            if (_title != null)
                _title.text = string.IsNullOrWhiteSpace(item.name) ? "SKIN " + item.skinDefId : item.name.ToUpperInvariant();
            if (_provenance != null) _provenance.text = DescribeProvenance(item);
            if (_token != null) _token.text = "TOKEN " + (item.tokenId ?? string.Empty);
            if (_hash != null) _hash.text = "CONTENT HASH " + ShortenHash(item.contentHash);
            if (_state != null) _state.text = DescribeState(item.state);
            if (_equip != null) _equip.SetEnabled(item.IsConfirmed);
            SetStatus("MARKET OPENS IN THE SYSTEM BROWSER ONLY.");
            _overlay.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _item = null;
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
        }

        private void OnEquip()
        {
            if (_item == null || controller == null || !_item.IsConfirmed) return;
            var tokenId = _item.tokenId;
            SetStatus("SAVING LOADOUT INTENT…");
            _ = EquipAsync(tokenId);
        }

        private async Task EquipAsync(string tokenId)
        {
            var success = await controller.EquipAsync((int)FormalCosmeticSlot.WeaponFinish, tokenId);
            SetStatus(success
                ? "EQUIPPED // SERVER RE-VERIFIES OWNERSHIP AT MATCH START."
                : "EQUIP FAILED // DEFAULT REMAINS ACTIVE.");
        }

        private void OnUseDefault()
        {
            if (controller == null) return;
            SetStatus("RESTORING DEFAULT FINISH…");
            _ = UseDefaultAsync();
        }

        private async Task UseDefaultAsync()
        {
            var success = await controller.UseDefaultAsync((int)FormalCosmeticSlot.WeaponFinish);
            SetStatus(success ? "DEFAULT FINISH ACTIVE." : "COULD NOT SAVE // DEFAULT STILL GUARANTEED IN MATCH.");
        }

        private void OnOpenMarket()
        {
            _ = OpenMarketAsync();
        }

        private async Task OpenMarketAsync()
        {
            var context = bootstrap == null ? null : bootstrap.Context;
            if (context == null)
            {
                SetStatus("MARKET UNAVAILABLE // RETURN TO VAULT ANY TIME.");
                return;
            }
            try
            {
                if (string.IsNullOrWhiteSpace(_marketplaceUrl))
                {
                    SetStatus("FETCHING CHAIN CONFIG…");
                    var config = await context.AssetGateway.GetConfigAsync(_lifetime.Token);
                    _marketplaceUrl = config == null ? string.Empty : config.marketplaceUrl;
                }
                if (string.IsNullOrWhiteSpace(_marketplaceUrl))
                {
                    SetStatus("NO MARKETPLACE CONFIGURED // RETURN TO VAULT ANY TIME.");
                    return;
                }
                context.UrlLauncher.Open(_marketplaceUrl);
                SetStatus("MARKET OPENED IN THE SYSTEM BROWSER.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                SetStatus("MARKET UNAVAILABLE // RETURN TO VAULT ANY TIME.");
            }
        }

        private void SetStatus(string message)
        {
            if (_status != null) _status.text = message ?? string.Empty;
        }

        public static string DescribeProvenance(SkinItem item)
        {
            if (item == null) return string.Empty;
            return "SERIAL #" + item.serial + " / " + item.maxSupply +
                   " // SEASON " + item.seasonId +
                   " // RARITY " + item.rarity +
                   " // WEAR " + Mathf.RoundToInt(Mathf.Clamp01(item.wear) * 100f) + "%";
        }

        public static string DescribeState(string state)
        {
            if (state == "confirmed") return "STATE CONFIRMED // OWNERSHIP FINAL ON-CHAIN — EQUIPPABLE";
            if (state == "pending") return "STATE PENDING // AWAITING CHAIN FINALITY — CANNOT ENTER A FORMAL LOADOUT";
            return "STATE " + (string.IsNullOrWhiteSpace(state) ? "UNKNOWN" : state.ToUpperInvariant());
        }

        public static string ShortenHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash)) return "—";
            return hash.Length <= 14 ? hash : hash.Substring(0, 10) + "…" + hash.Substring(hash.Length - 4);
        }
    }
}
