using UnityEngine;

namespace Web3Fps.GameFoundation.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private PrototypeMatchController match;
        [SerializeField] private PrototypeParticipant player;

        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _centerStyle;

        public void Configure(PrototypeMatchController matchController, PrototypeParticipant localPlayer)
        {
            match = matchController;
            player = localPlayer;
        }

        private void OnGUI()
        {
            if (match == null || match.Rules == null || player == null || player.Health == null) return;
            EnsureStyles();
            GUI.Box(new Rect(16f, 16f, 330f, 150f), GUIContent.none);
            GUI.Label(new Rect(30f, 26f, 300f, 28f), "WEB3 FPS · LOCAL PROTOTYPE", _titleStyle);
            GUI.Label(new Rect(30f, 58f, 300f, 96f),
                "Health  " + Mathf.CeilToInt(player.Health.Current) + " / " + Mathf.CeilToInt(player.Health.Maximum) +
                "\nScore    You " + match.Rules.PlayerKills + "  :  " + match.Rules.BotKills + " Bot" +
                "\nGoal     First to " + match.Rules.TargetKills +
                "\nTime     " + Mathf.CeilToInt(match.Rules.RemainingSeconds) + "s",
                _bodyStyle);

            GUI.Label(new Rect(Screen.width * 0.5f - 12f, Screen.height * 0.5f - 18f, 24f, 36f), "+", _centerStyle);
            GUI.Label(new Rect(16f, Screen.height - 56f, 760f, 40f),
                "WASD Move · Shift Sprint · Space Jump · Mouse Aim · Left Click Fire · Esc Cursor",
                _bodyStyle);

            if (!match.IsFinished) return;
            var outcome = match.Rules.Outcome == LocalPrototypeOutcome.PlayerWin
                ? "VICTORY"
                : match.Rules.Outcome == LocalPrototypeOutcome.BotWin ? "DEFEAT" : "DRAW";
            GUI.Box(new Rect(Screen.width * 0.5f - 210f, Screen.height * 0.5f - 90f, 420f, 180f), GUIContent.none);
            GUI.Label(new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.5f - 56f, 380f, 54f), outcome, _centerStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 190f, Screen.height * 0.5f + 18f, 380f, 36f),
                "Press R to restart", _centerStyle);
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null) return;
            _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            _bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            _centerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}
