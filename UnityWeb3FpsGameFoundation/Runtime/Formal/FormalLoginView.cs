using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Web3Fps.GameFoundation.Auth;
using Web3Fps.GameFoundation.Composition;
using Web3Fps.GameFoundation.Lobby;

namespace Web3Fps.GameFoundation.Formal
{
    /// <summary>
    /// Entry overlay for the demo login flow (FORMAL_CONTENT_DESIGN §4.3 登录/游客).
    /// LOGIN calls the demo backend endpoint; GUEST always works offline. The access
    /// token only ever reaches GameFoundationBootstrap.SetAccessToken in memory.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FormalLoginView : MonoBehaviour
    {
        [SerializeField] private GameFoundationBootstrap bootstrap;
        [SerializeField] private Web3LobbyController controller;
        [SerializeField] private UIDocument document;

        private VisualElement _overlay;
        private TextField _playerInput;
        private Label _status;
        private Button _loginButton;
        private Button _guestButton;
        private CancellationTokenSource _lifetime;
        private bool _busy;

        public bool IsAuthenticated { get; private set; }

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
            _overlay = root.Q<VisualElement>("login-overlay");
            _playerInput = root.Q<TextField>("login-player-input");
            _status = root.Q<Label>("login-status-label");
            _loginButton = root.Q<Button>("login-button");
            _guestButton = root.Q<Button>("guest-button");
            if (_loginButton != null) _loginButton.clicked += OnLoginClicked;
            if (_guestButton != null) _guestButton.clicked += OnGuestClicked;
            if (_overlay != null)
                _overlay.style.display = IsAuthenticated ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void OnDisable()
        {
            if (_loginButton != null) _loginButton.clicked -= OnLoginClicked;
            if (_guestButton != null) _guestButton.clicked -= OnGuestClicked;
        }

        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
        }

        private void OnLoginClicked()
        {
            if (_busy) return;
            var playerId = _playerInput == null ? string.Empty : (_playerInput.value ?? string.Empty).Trim();
            if (!DemoLogin.IsValidPlayerId(playerId))
            {
                SetStatus("CALLSIGN MAY USE LETTERS, DIGITS, _ . - (MAX 64).");
                return;
            }
            _ = LoginAsync(playerId);
        }

        private async Task LoginAsync(string playerId)
        {
            var context = bootstrap == null ? null : bootstrap.Context;
            if (context == null)
            {
                SetStatus("GAME FOUNDATION IS STILL INITIALIZING — TRY AGAIN OR ENTER AS GUEST.");
                return;
            }
            _busy = true;
            SetInteractable(false);
            SetStatus("REQUESTING DEMO SESSION…");
            try
            {
                var token = await context.LoginClient.LoginAsync(playerId, _lifetime.Token);
                bootstrap.SetAccessToken(token);
                bootstrap.SetPlayerId(playerId);
                CompleteEntry("SESSION ACTIVE // " + playerId.ToUpperInvariant());
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                SetStatus("LOGIN UNAVAILABLE (" + exception.Message.ToUpperInvariant() + ") // GUEST ENTRY REMAINS OPEN.");
            }
            finally
            {
                _busy = false;
                SetInteractable(true);
            }
        }

        private void OnGuestClicked()
        {
            if (_busy) return;
            if (bootstrap != null)
            {
                bootstrap.SetAccessToken(string.Empty);
                bootstrap.SetPlayerId("guest");
            }
            CompleteEntry("GUEST // DEFAULT GEAR READY");
        }

        private void CompleteEntry(string message)
        {
            IsAuthenticated = true;
            SetStatus(message);
            if (_overlay != null) _overlay.style.display = DisplayStyle.None;
            if (controller != null) _ = controller.RefreshAsync();
        }

        private void SetInteractable(bool value)
        {
            if (_loginButton != null) _loginButton.SetEnabled(value);
            if (_guestButton != null) _guestButton.SetEnabled(value);
        }

        private void SetStatus(string message)
        {
            if (_status != null) _status.text = message ?? string.Empty;
        }
    }
}
