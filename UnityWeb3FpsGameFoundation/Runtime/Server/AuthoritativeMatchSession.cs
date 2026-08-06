using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Web3Fps.GameFoundation.Match;

namespace Web3Fps.GameFoundation.Server
{
    public enum AuthoritativeMatchLifecycle
    {
        Waiting,
        ResolvingLoadouts,
        Running,
        Finished
    }

    [Serializable]
    public sealed class AuthoritativePlayerRegistration
    {
        public string playerId = string.Empty;
        public string teamId = string.Empty;
        public string wallet = string.Empty;
        public string[] tokenIdsBySlot = Array.Empty<string>();
    }

    /// <summary>
    /// SDK-neutral dedicated-server match boundary. Player cosmetic intent is resolved and frozen
    /// before the authoritative match starts; asset failures never reject an otherwise valid player.
    /// </summary>
    public sealed class AuthoritativeMatchSession
    {
        private readonly LoadoutSnapshotResolver _snapshotResolver;
        private readonly Dictionary<string, AuthoritativePlayerRegistration> _registrations =
            new Dictionary<string, AuthoritativePlayerRegistration>(StringComparer.Ordinal);
        private readonly Dictionary<string, PlayerLoadoutSnapshot> _snapshots =
            new Dictionary<string, PlayerLoadoutSnapshot>(StringComparer.Ordinal);
        private MatchCoordinator _coordinator;
        private MatchResult _result;

        public AuthoritativeMatchLifecycle Lifecycle { get; private set; } = AuthoritativeMatchLifecycle.Waiting;
        public IReadOnlyDictionary<string, PlayerLoadoutSnapshot> LoadoutSnapshots => _snapshots;
        public MatchResult Result => _result;

        public AuthoritativeMatchSession(LoadoutSnapshotResolver snapshotResolver)
        {
            _snapshotResolver = snapshotResolver ?? throw new ArgumentNullException(nameof(snapshotResolver));
        }

        public void RegisterPlayer(AuthoritativePlayerRegistration registration)
        {
            if (Lifecycle != AuthoritativeMatchLifecycle.Waiting)
                throw new InvalidOperationException("Players can only register while the session is waiting");
            if (registration == null) throw new ArgumentNullException(nameof(registration));
            Require(registration.playerId, nameof(registration.playerId));
            Require(registration.teamId, nameof(registration.teamId));
            if (registration.tokenIdsBySlot == null)
                throw new ArgumentException("tokenIdsBySlot is required", nameof(registration));
            if (_registrations.ContainsKey(registration.playerId))
                throw new InvalidOperationException("Duplicate player " + registration.playerId);
            _registrations.Add(registration.playerId, Clone(registration));
        }

        public async Task StartAsync(
            string matchId,
            string modeId,
            string mapId,
            string serverBuild,
            long startedAt,
            string tournamentId = "",
            CancellationToken ct = default)
        {
            if (Lifecycle != AuthoritativeMatchLifecycle.Waiting)
                throw new InvalidOperationException("Match session has already started");
            if (_registrations.Count == 0) throw new InvalidOperationException("At least one player is required");
            Require(matchId, nameof(matchId));

            Lifecycle = AuthoritativeMatchLifecycle.ResolvingLoadouts;
            var resolved = new Dictionary<string, PlayerLoadoutSnapshot>(StringComparer.Ordinal);
            try
            {
                foreach (var registration in _registrations.Values.OrderBy(x => x.playerId, StringComparer.Ordinal))
                {
                    ct.ThrowIfCancellationRequested();
                    var resolution = await _snapshotResolver.ResolveOrDefaultAsync(
                        new LoadoutEntitlementRequest
                        {
                            matchId = matchId,
                            playerId = registration.playerId,
                            wallet = registration.wallet,
                            tokenIdsBySlot = CloneTokens(registration.tokenIdsBySlot)
                        },
                        ct);
                    resolved.Add(registration.playerId, resolution.Snapshot);
                }
            }
            catch
            {
                Lifecycle = AuthoritativeMatchLifecycle.Waiting;
                throw;
            }

            var coordinator = new MatchCoordinator();
            foreach (var registration in _registrations.Values.OrderBy(x => x.playerId, StringComparer.Ordinal))
                coordinator.RegisterPlayer(registration.playerId, registration.teamId, registration.wallet);
            coordinator.Start(matchId, modeId, mapId, serverBuild, startedAt, tournamentId);

            _snapshots.Clear();
            foreach (var pair in resolved) _snapshots.Add(pair.Key, pair.Value);
            _coordinator = coordinator;
            Lifecycle = AuthoritativeMatchLifecycle.Running;
        }

        public PlayerLoadoutSnapshot GetLoadoutSnapshot(string playerId)
        {
            PlayerLoadoutSnapshot snapshot;
            if (!_snapshots.TryGetValue(playerId, out snapshot))
                throw new KeyNotFoundException("No frozen loadout snapshot for player " + playerId);
            return snapshot;
        }

        public void RecordKill(string killerId, string victimId, long scoreAward = 100)
        {
            RequireRunning();
            _coordinator.RecordKill(killerId, victimId, scoreAward);
        }

        public void RecordAssist(string playerId, long scoreAward = 50)
        {
            RequireRunning();
            _coordinator.RecordAssist(playerId, scoreAward);
        }

        public void AddScore(string playerId, long delta)
        {
            RequireRunning();
            _coordinator.AddScore(playerId, delta);
        }

        public MatchResult Finish(long endedAt, string antiCheatState = "passed", MatchRewardSlot[] rewards = null)
        {
            if (Lifecycle == AuthoritativeMatchLifecycle.Finished) return _result;
            RequireRunning();
            _result = _coordinator.Finish(endedAt, antiCheatState, rewards);
            Lifecycle = AuthoritativeMatchLifecycle.Finished;
            return _result;
        }

        public MatchAttestationPayload CreateAttestationPayload()
        {
            if (Lifecycle != AuthoritativeMatchLifecycle.Finished || _result == null)
                throw new InvalidOperationException("Finish the match before creating an attestation payload");
            return MatchResultHasher.CreatePayload(_result);
        }

        private void RequireRunning()
        {
            if (Lifecycle != AuthoritativeMatchLifecycle.Running)
                throw new InvalidOperationException("Match session is not running");
        }

        private static AuthoritativePlayerRegistration Clone(AuthoritativePlayerRegistration registration)
        {
            return new AuthoritativePlayerRegistration
            {
                playerId = registration.playerId,
                teamId = registration.teamId,
                wallet = registration.wallet ?? string.Empty,
                tokenIdsBySlot = CloneTokens(registration.tokenIdsBySlot)
            };
        }

        private static string[] CloneTokens(string[] tokens)
        {
            var clone = new string[tokens.Length];
            for (var i = 0; i < clone.Length; i++) clone[i] = tokens[i] ?? string.Empty;
            return clone;
        }

        private static void Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name + " is required", name);
        }
    }
}
