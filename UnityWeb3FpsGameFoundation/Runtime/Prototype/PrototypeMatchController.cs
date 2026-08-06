using System;
using System.Collections;
using UnityEngine;
using Web3Fps.GameFoundation.Gameplay.Combat;
using Web3Fps.GameFoundation.Match;

namespace Web3Fps.GameFoundation.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeMatchController : MonoBehaviour
    {
        [SerializeField] private PrototypeParticipant player;
        [SerializeField] private PrototypeParticipant bot;
        [SerializeField, Min(1)] private int targetKills = 5;
        [SerializeField, Min(10f)] private float durationSeconds = 180f;
        [SerializeField, Min(0f)] private float respawnDelay = 2f;

        private LocalDeathmatchRules _rules;
        private MatchCoordinator _coordinator;
        private long _startedAt;

        public bool IsRunning => _rules != null && _rules.Phase == LocalPrototypePhase.Running;
        public bool IsFinished => _rules != null && _rules.Phase == LocalPrototypePhase.Finished;
        public LocalDeathmatchRules Rules => _rules;
        public MatchResult LastResult { get; private set; }

        public event Action StateChanged;
        public event Action<MatchResult> MatchFinished;

        public void Configure(
            PrototypeParticipant localPlayer,
            PrototypeParticipant opponent,
            int killsToWin = 5,
            float matchDurationSeconds = 180f,
            float deathRespawnDelay = 2f)
        {
            player = localPlayer;
            bot = opponent;
            targetKills = Mathf.Max(1, killsToWin);
            durationSeconds = Mathf.Max(10f, matchDurationSeconds);
            respawnDelay = Mathf.Max(0f, deathRespawnDelay);
        }

        private void Start()
        {
            if (player == null || bot == null)
            {
                Debug.LogError("PrototypeMatchController requires player and bot participants", this);
                enabled = false;
                return;
            }
            player.Health.Died += OnPlayerDied;
            bot.Health.Died += OnBotDied;
            BeginMatch();
        }

        private void OnDestroy()
        {
            if (player != null && player.Health != null) player.Health.Died -= OnPlayerDied;
            if (bot != null && bot.Health != null) bot.Health.Died -= OnBotDied;
        }

        private void Update()
        {
            if (!IsRunning) return;
            var wasRunning = IsRunning;
            _rules.Tick(Time.deltaTime);
            if (wasRunning && IsFinished) FinishMatch();
            StateChanged?.Invoke();
        }

        public void RestartMatch()
        {
            StopAllCoroutines();
            BeginMatch();
        }

        private void BeginMatch()
        {
            _rules = new LocalDeathmatchRules(targetKills, durationSeconds);
            _rules.Start();
            _coordinator = new MatchCoordinator();
            _coordinator.RegisterPlayer(player.ParticipantId, player.TeamId);
            _coordinator.RegisterPlayer(bot.ParticipantId, bot.TeamId);
            _startedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _coordinator.Start(
                "local-" + Guid.NewGuid().ToString("N"),
                "prototype-deathmatch",
                "prototype-arena",
                Application.version,
                _startedAt);
            LastResult = null;
            player.Respawn();
            bot.Respawn();
            StateChanged?.Invoke();
        }

        private void OnPlayerDied(DamageInfo damage)
        {
            if (!IsRunning) return;
            player.SetControlEnabled(false);
            var killer = FindParticipant(damage.Source);
            if (killer == bot)
            {
                _rules.RecordBotKill();
                _coordinator.RecordKill(bot.ParticipantId, player.ParticipantId);
            }
            else
            {
                _coordinator.RecordKill(player.ParticipantId, player.ParticipantId);
            }
            ResolveDeath(player);
        }

        private void OnBotDied(DamageInfo damage)
        {
            if (!IsRunning) return;
            bot.SetControlEnabled(false);
            var killer = FindParticipant(damage.Source);
            if (killer == player)
            {
                _rules.RecordPlayerKill();
                _coordinator.RecordKill(player.ParticipantId, bot.ParticipantId);
            }
            else
            {
                _coordinator.RecordKill(bot.ParticipantId, bot.ParticipantId);
            }
            ResolveDeath(bot);
        }

        private void ResolveDeath(PrototypeParticipant participant)
        {
            StateChanged?.Invoke();
            if (IsFinished)
            {
                FinishMatch();
                return;
            }
            StartCoroutine(RespawnAfterDelay(participant));
        }

        private IEnumerator RespawnAfterDelay(PrototypeParticipant participant)
        {
            if (respawnDelay > 0f) yield return new WaitForSeconds(respawnDelay);
            if (IsRunning) participant.Respawn();
        }

        private void FinishMatch()
        {
            if (_coordinator == null || _coordinator.Lifecycle != MatchLifecycle.Running) return;
            StopAllCoroutines();
            player.SetControlEnabled(false);
            bot.SetControlEnabled(false);
            LastResult = _coordinator.Finish(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            StateChanged?.Invoke();
            MatchFinished?.Invoke(LastResult);
        }

        private static PrototypeParticipant FindParticipant(GameObject source)
        {
            return source == null ? null : source.GetComponentInParent<PrototypeParticipant>();
        }
    }
}
