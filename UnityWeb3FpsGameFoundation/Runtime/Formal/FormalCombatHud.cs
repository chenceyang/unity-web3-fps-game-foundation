using UnityEngine;
using UnityEngine.UIElements;
using Web3Fps.GameFoundation.Gameplay.Combat;
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
        [SerializeField] private PanelSettings fallbackPanelSettings;

        private Label _score;
        private Label _timer;
        private Label _health;
        private Label _ammo;
        private Label _reserve;
        private Label _reload;
        private Label _hitMarker;
        private VisualElement _healthFill;
        private VisualElement _result;
        private Label _resultTitle;
        private PanelSettings _runtimePanelSettings;
        private HitscanWeapon _weapon;
        private float _hitMarkerUntil;

        public void Configure(
            UIDocument uiDocument,
            PrototypeMatchController matchController,
            PrototypeParticipant localPlayer,
            PanelSettings fallbackSettings = null)
        {
            document = uiDocument;
            match = matchController;
            player = localPlayer;
            fallbackPanelSettings = fallbackSettings;
            BindWeapon();
        }

        private void OnEnable()
        {
            if (document == null) document = GetComponent<UIDocument>();
            EnsurePanelSettings();
            var root = document == null ? null : document.rootVisualElement;
            if (root == null) return;
            _score = root.Q<Label>("score-label");
            _timer = root.Q<Label>("timer-label");
            _health = root.Q<Label>("health-label");
            _ammo = root.Q<Label>("ammo-label");
            _reserve = root.Q<Label>("reserve-label");
            _reload = root.Q<Label>("reload-label");
            _hitMarker = root.Q<Label>("hit-marker");
            _healthFill = root.Q<VisualElement>("health-fill");
            _result = root.Q<VisualElement>("result-panel");
            _resultTitle = root.Q<Label>("result-title");
            BindWeapon();
        }

        private void OnDisable()
        {
            if (_weapon != null) _weapon.ShotResolved -= OnShotResolved;
        }

        private void OnDestroy()
        {
            if (_runtimePanelSettings != null) Destroy(_runtimePanelSettings);
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
            _runtimePanelSettings.name = "ASH LEDGER Combat Runtime Panel Settings";
            FormalUiPanelDefaults.Configure(_runtimePanelSettings);
            document.panelSettings = _runtimePanelSettings;
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
            UpdateWeaponReadout();
            if (_hitMarker != null)
                _hitMarker.style.display = Time.unscaledTime < _hitMarkerUntil ? DisplayStyle.Flex : DisplayStyle.None;
            if (_result == null) return;
            _result.style.display = match.IsFinished ? DisplayStyle.Flex : DisplayStyle.None;
            if (!match.IsFinished || _resultTitle == null) return;
            _resultTitle.text = match.Rules.Outcome == LocalPrototypeOutcome.PlayerWin
                ? "VICTORY"
                : match.Rules.Outcome == LocalPrototypeOutcome.BotWin ? "DEFEAT" : "DRAW";
        }

        private void UpdateWeaponReadout()
        {
            if (_weapon == null) return;
            if (_ammo != null) _ammo.text = _weapon.MagazineAmmo.ToString("00");
            if (_reserve != null) _reserve.text = "/ " + _weapon.ReserveAmmo.ToString("000");
            if (_reload == null) return;
            if (_weapon.IsReloading)
            {
                _reload.text = "RELOADING  " + Mathf.RoundToInt(_weapon.ReloadProgress * 100f).ToString("00") + "%";
                _reload.EnableInClassList("reload-alert", true);
            }
            else if (_weapon.MagazineAmmo == 0)
            {
                _reload.text = _weapon.ReserveAmmo > 0 ? "EMPTY // PRESS R" : "AMMUNITION DEPLETED";
                _reload.EnableInClassList("reload-alert", true);
            }
            else
            {
                _reload.text = "R // RELOAD";
                _reload.EnableInClassList("reload-alert", false);
            }
        }

        private void OnShotResolved(ShotResult result)
        {
            if (!result.Accepted || !result.Hit) return;
            _hitMarkerUntil = Time.unscaledTime + 0.13f;
        }

        private void BindWeapon()
        {
            if (_weapon != null) _weapon.ShotResolved -= OnShotResolved;
            _weapon = player == null ? null : player.GetComponent<HitscanWeapon>();
            if (isActiveAndEnabled && _weapon != null) _weapon.ShotResolved += OnShotResolved;
        }
    }
}
