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
        private int _lastPlayerKills = -1;
        private int _lastBotKills = -1;
        private int _lastSeconds = -1;
        private int _lastHealth = -1;
        private int _lastHealthPercent = -1;
        private int _lastAmmo = -1;
        private int _lastReserve = -1;
        private int _lastReloadState = -1;
        private int _lastReloadPercent = -1;
        private bool _lastHitMarkerVisible;
        private bool _lastResultVisible;

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
            ResetReadoutCache();
            BindWeapon();
        }

        private void ResetReadoutCache()
        {
            _lastPlayerKills = _lastBotKills = _lastSeconds = _lastHealth = _lastHealthPercent = -1;
            _lastAmmo = _lastReserve = _lastReloadState = _lastReloadPercent = -1;
            _lastHitMarkerVisible = _lastResultVisible = false;
            if (_hitMarker != null) _hitMarker.style.display = DisplayStyle.None;
            if (_result != null) _result.style.display = DisplayStyle.None;
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

        // Every readout below only touches UI Toolkit when the underlying value
        // actually changed: rebuilding the strings each frame allocated garbage and
        // re-setting identical text still forces text relayout work in the panel.
        private void Update()
        {
            if (match == null || match.Rules == null || player == null || player.Health == null) return;
            if (_score != null && (_lastPlayerKills != match.Rules.PlayerKills || _lastBotKills != match.Rules.BotKills))
            {
                _lastPlayerKills = match.Rules.PlayerKills;
                _lastBotKills = match.Rules.BotKills;
                _score.text = "COBALT  " + _lastPlayerKills + "  //  " + _lastBotKills + "  CORAL";
            }
            if (_timer != null)
            {
                var seconds = Mathf.CeilToInt(match.Rules.RemainingSeconds);
                if (seconds != _lastSeconds)
                {
                    _lastSeconds = seconds;
                    _timer.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
                }
            }
            if (_health != null)
            {
                var health = Mathf.CeilToInt(player.Health.Current);
                if (health != _lastHealth)
                {
                    _lastHealth = health;
                    _health.text = health.ToString("000");
                }
            }
            if (_healthFill != null)
            {
                var ratio = player.Health.Maximum <= 0f ? 0f : player.Health.Current / player.Health.Maximum;
                var percent = Mathf.RoundToInt(Mathf.Clamp01(ratio) * 100f);
                if (percent != _lastHealthPercent)
                {
                    _lastHealthPercent = percent;
                    _healthFill.style.width = new Length(percent, LengthUnit.Percent);
                }
            }
            UpdateWeaponReadout();
            if (_hitMarker != null)
            {
                var hitMarkerVisible = Time.unscaledTime < _hitMarkerUntil;
                if (hitMarkerVisible != _lastHitMarkerVisible)
                {
                    _lastHitMarkerVisible = hitMarkerVisible;
                    _hitMarker.style.display = hitMarkerVisible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
            if (_result == null) return;
            if (match.IsFinished != _lastResultVisible)
            {
                _lastResultVisible = match.IsFinished;
                _result.style.display = match.IsFinished ? DisplayStyle.Flex : DisplayStyle.None;
                if (match.IsFinished && _resultTitle != null)
                {
                    _resultTitle.text = match.Rules.Outcome == LocalPrototypeOutcome.PlayerWin
                        ? "VICTORY"
                        : match.Rules.Outcome == LocalPrototypeOutcome.BotWin ? "DEFEAT" : "DRAW";
                }
            }
        }

        private void UpdateWeaponReadout()
        {
            if (_weapon == null) return;
            if (_ammo != null && _weapon.MagazineAmmo != _lastAmmo)
            {
                _lastAmmo = _weapon.MagazineAmmo;
                _ammo.text = _lastAmmo.ToString("00");
            }
            if (_reserve != null && _weapon.ReserveAmmo != _lastReserve)
            {
                _lastReserve = _weapon.ReserveAmmo;
                _reserve.text = "/ " + _lastReserve.ToString("000");
            }
            if (_reload == null) return;
            if (_weapon.IsReloading)
            {
                var percent = Mathf.RoundToInt(_weapon.ReloadProgress * 100f);
                if (_lastReloadState != 1 || percent != _lastReloadPercent)
                {
                    _lastReloadState = 1;
                    _lastReloadPercent = percent;
                    _reload.text = "RELOADING  " + percent.ToString("00") + "%";
                    _reload.EnableInClassList("reload-alert", true);
                }
            }
            else if (_weapon.MagazineAmmo == 0)
            {
                var state = _weapon.ReserveAmmo > 0 ? 2 : 3;
                if (_lastReloadState != state)
                {
                    _lastReloadState = state;
                    _reload.text = state == 2 ? "EMPTY // PRESS R" : "AMMUNITION DEPLETED";
                    _reload.EnableInClassList("reload-alert", true);
                }
            }
            else if (_lastReloadState != 0)
            {
                _lastReloadState = 0;
                _reload.text = "R // RELOAD";
                _reload.EnableInClassList("reload-alert", false);
            }
        }

        private void OnShotResolved(ShotResult result)
        {
            if (!result.Accepted || !result.DamageApplied) return;
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
