using UnityEngine;
using UnityEngine.UIElements;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Formal
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class FormalCombatHud : MonoBehaviour
    {
        [SerializeField] private UIDocument document;
        [SerializeField] private PrototypeMatchController match;
        [SerializeField] private PrototypeParticipant player;

        private Label _score;
        private Label _timer;
        private Label _health;
        private VisualElement _healthFill;
        private VisualElement _result;
        private Label _resultTitle;

        public void Configure(UIDocument uiDocument, PrototypeMatchController matchController, PrototypeParticipant localPlayer)
        {
            document = uiDocument;
            match = matchController;
            player = localPlayer;
        }

        private void OnEnable()
        {
            if (document == null) document = GetComponent<UIDocument>();
            var root = document == null ? null : document.rootVisualElement;
            if (root == null) return;
            _score = root.Q<Label>("score-label");
            _timer = root.Q<Label>("timer-label");
            _health = root.Q<Label>("health-label");
            _healthFill = root.Q<VisualElement>("health-fill");
            _result = root.Q<VisualElement>("result-panel");
            _resultTitle = root.Q<Label>("result-title");
        }

        private void Update()
        {
            if (match == null || match.Rules == null || player == null || player.Health == null) return;
            if (_score != null) _score.text = "COBALT  " + match.Rules.PlayerKills + "  //  " + match.Rules.BotKills + "  CORAL";
            if (_timer != null)
            {
                var seconds = Mathf.CeilToInt(match.Rules.RemainingSeconds);
                _timer.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
            }
            if (_health != null) _health.text = Mathf.CeilToInt(player.Health.Current).ToString("000");
            if (_healthFill != null)
            {
                var ratio = player.Health.Maximum <= 0f ? 0f : player.Health.Current / player.Health.Maximum;
                _healthFill.style.width = new Length(Mathf.Clamp01(ratio) * 100f, LengthUnit.Percent);
            }
            if (_result == null) return;
            _result.style.display = match.IsFinished ? DisplayStyle.Flex : DisplayStyle.None;
            if (!match.IsFinished || _resultTitle == null) return;
            _resultTitle.text = match.Rules.Outcome == LocalPrototypeOutcome.PlayerWin
                ? "VICTORY"
                : match.Rules.Outcome == LocalPrototypeOutcome.BotWin ? "DEFEAT" : "DRAW";
        }
    }
}
