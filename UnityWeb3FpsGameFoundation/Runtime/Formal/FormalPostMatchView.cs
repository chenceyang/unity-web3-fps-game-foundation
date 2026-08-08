using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Web3Fps.GameFoundation.Match;
using Web3Fps.GameFoundation.Prototype;
using Web3Fps.GameFoundation.Server;

namespace Web3Fps.GameFoundation.Formal
{
    /// <summary>
    /// Standalone post-match page (FORMAL_CONTENT_DESIGN §4.3 赛后页). Shows the final
    /// score, the publish outcome and the public attestation record when reachable.
    /// Attestation failure or an unreachable archive never hides the scoreboard and
    /// never blocks REMATCH or BACK TO LOBBY (PRD MAT-006).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class FormalPostMatchView : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private PrototypeMatchController match;
        [SerializeField] private PrototypeParticipant localPlayer;
        [SerializeField] private LocalAuthoritativeMatchDriver driver;
        [SerializeField] private string lobbySceneName = FormalContentCatalog.LobbySceneName;
        [SerializeField] private PanelSettings fallbackPanelSettings;

        private VisualElement _root;
        private Label _result;
        private Label _score;
        private Label _placement;
        private Label _publish;
        private Label _attest;
        private Button _rematch;
        private Button _lobby;
        private PanelSettings _runtimePanelSettings;
        private CancellationTokenSource _lifetime;
        private bool _archiveFetchBusy;
        private bool _visible;

        public void Configure(
            UIDocument uiDocument,
            PrototypeMatchController matchController,
            PrototypeParticipant player,
            LocalAuthoritativeMatchDriver matchDriver,
            string lobbyScene,
            PanelSettings fallbackSettings = null)
        {
            document = uiDocument;
            match = matchController;
            localPlayer = player;
            driver = matchDriver;
            lobbySceneName = string.IsNullOrWhiteSpace(lobbyScene) ? FormalContentCatalog.LobbySceneName : lobbyScene;
            fallbackPanelSettings = fallbackSettings;
        }

        private void Awake()
        {
            _lifetime = new CancellationTokenSource();
        }

        private void OnEnable()
        {
            if (document == null) document = GetComponent<UIDocument>();
            EnsurePanelSettings();
            var root = document == null ? null : document.rootVisualElement;
            if (root == null) return;
            _root = root.Q<VisualElement>("post-root");
            _result = root.Q<Label>("post-result-label");
            _score = root.Q<Label>("post-score-label");
            _placement = root.Q<Label>("post-placement-label");
            _publish = root.Q<Label>("post-publish-label");
            _attest = root.Q<Label>("post-attest-label");
            _rematch = root.Q<Button>("rematch-button");
            _lobby = root.Q<Button>("lobby-button");
            if (_rematch != null) _rematch.clicked += Rematch;
            if (_lobby != null) _lobby.clicked += BackToLobby;
            if (match != null) match.MatchFinished += OnMatchFinished;
            if (driver != null) driver.PublishCompleted += OnPublishCompleted;
            Hide();
        }

        private void OnDisable()
        {
            if (_rematch != null) _rematch.clicked -= Rematch;
            if (_lobby != null) _lobby.clicked -= BackToLobby;
            if (match != null) match.MatchFinished -= OnMatchFinished;
            if (driver != null) driver.PublishCompleted -= OnPublishCompleted;
        }

        private void OnDestroy()
        {
            if (_runtimePanelSettings != null) Destroy(_runtimePanelSettings);
            _lifetime?.Cancel();
            _lifetime?.Dispose();
        }

        private void Update()
        {
            if (_visible && Input.GetKeyDown(KeyCode.R)) Rematch();
        }

        private void EnsurePanelSettings()
        {
            if (document == null || document.panelSettings != null) return;
            if (fallbackPanelSettings != null)
            {
                document.panelSettings = fallbackPanelSettings;
                return;
            }
            _runtimePanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _runtimePanelSettings.name = "ASH LEDGER Post-Match Runtime Panel Settings";
            FormalUiPanelDefaults.Configure(_runtimePanelSettings);
            _runtimePanelSettings.sortingOrder = FormalUiPanelDefaults.SortingOrder + 10;
            document.panelSettings = _runtimePanelSettings;
        }

        private void OnMatchFinished(MatchResult result)
        {
            if (_root == null || result == null) return;
            var participantId = localPlayer == null ? string.Empty : localPlayer.ParticipantId;
            if (_result != null && match != null && match.Rules != null)
            {
                _result.text = match.Rules.Outcome == LocalPrototypeOutcome.PlayerWin
                    ? "VICTORY"
                    : match.Rules.Outcome == LocalPrototypeOutcome.BotWin ? "DEFEAT" : "DRAW";
            }
            if (_score != null && match != null && match.Rules != null)
                _score.text = "COBALT " + match.Rules.PlayerKills + " // " + match.Rules.BotKills + " CORAL";
            if (_placement != null) _placement.text = DescribePlacement(result, participantId);
            if (_publish != null) _publish.text = "RESULT PUBLISH PENDING…";
            if (_attest != null) _attest.text = "PUBLIC VERIFICATION RUNS ASYNCHRONOUSLY.";
            if (driver != null && driver.PublishReport != null) OnPublishCompleted(driver.PublishReport);
            _visible = true;
            _root.style.display = DisplayStyle.Flex;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        private void OnPublishCompleted(LocalMatchPublishReport report)
        {
            if (_publish != null && report != null) _publish.text = DescribePublish(report);
            if (report != null) _ = FetchArchiveRecordAsync(report.MatchId);
        }

        private async Task FetchArchiveRecordAsync(string matchId)
        {
            var gateway = driver == null || driver.Ticket == null ? null : driver.Ticket.ArchiveGateway;
            if (gateway == null || string.IsNullOrWhiteSpace(matchId) || _archiveFetchBusy) return;
            _archiveFetchBusy = true;
            try
            {
                var record = await gateway.GetMatchAsync(matchId, _lifetime.Token);
                if (_attest != null) _attest.text = DescribeAttestation(record);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                if (_attest != null) _attest.text = "ARCHIVE RECORD UNAVAILABLE // SCORE REMAINS VALID.";
            }
            finally
            {
                _archiveFetchBusy = false;
            }
        }

        private void Rematch()
        {
            if (match == null || !match.IsFinished) return;
            Hide();
            match.RestartMatch();
        }

        private void BackToLobby()
        {
            if (Application.CanStreamedLevelBeLoaded(lobbySceneName))
                SceneManager.LoadScene(lobbySceneName);
            else
                Debug.LogWarning("Lobby scene is not available in Build Settings: " + lobbySceneName, this);
        }

        private void Hide()
        {
            _visible = false;
            if (_root != null) _root.style.display = DisplayStyle.None;
        }

        public static string DescribePlacement(MatchResult result, string participantId)
        {
            if (result?.players == null) return "PLACEMENT —";
            foreach (var player in result.players)
            {
                if (player != null && string.Equals(player.playerId, participantId, StringComparison.Ordinal))
                    return "PLACEMENT #" + player.placement + " // " + player.kills + " ELIMINATIONS // SCORE " + player.score;
            }
            return "PLACEMENT —";
        }

        public static string DescribePublish(LocalMatchPublishReport report)
        {
            if (report == null) return "RESULT PUBLISH PENDING…";
            switch (report.Outcome)
            {
                case MatchPublishOutcome.Published:
                    return "RESULT PUBLISHED // HASH " + ShortenHash(report.ResultHash);
                case MatchPublishOutcome.Duplicate:
                    return "RESULT ALREADY PUBLISHED // HASH " + ShortenHash(report.ResultHash);
                case MatchPublishOutcome.Conflict:
                    return "PUBLISH CONFLICT // BACKEND HOLDS A DIFFERENT RESULT — ESCALATE, DO NOT RETRY.";
                case MatchPublishOutcome.Failed:
                    return "PUBLISH FAILED // RESULT KEPT LOCALLY — SCORE UNAFFECTED.";
                default:
                    return "RESULT KEPT LOCALLY // NO PUBLISHER CONFIGURED.";
            }
        }

        public static string DescribeAttestation(MatchRecord record)
        {
            if (record?.attestation == null) return "ARCHIVE RECORD UNAVAILABLE // SCORE REMAINS VALID.";
            if (record.attestation.IsVerified)
                return "PUBLICLY VERIFIED // TX " + ShortenHash(record.attestation.txHash);
            if (record.attestation.state == AttestationState.Failed)
                return "PUBLIC VERIFICATION FAILED // SCORE REMAINS VALID — RETRIES CONTINUE SERVER-SIDE.";
            return "PUBLIC VERIFICATION PROCESSING…";
        }

        public static string ShortenHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash)) return "—";
            return hash.Length <= 14 ? hash : hash.Substring(0, 10) + "…" + hash.Substring(hash.Length - 4);
        }
    }
}
