using System;
using System.Collections.Generic;
using System.Linq;

namespace Web3Fps.GameFoundation.Match
{
    public enum MatchLifecycle
    {
        Waiting,
        Running,
        Finished
    }

    /// <summary>Authoritative-server match state independent of a networking package.</summary>
    public sealed class MatchCoordinator
    {
        private sealed class MutablePlayer
        {
            public string PlayerId;
            public string Wallet;
            public string TeamId;
            public int Kills;
            public int Deaths;
            public int Assists;
            public long Score;
        }

        private readonly Dictionary<string, MutablePlayer> _players = new Dictionary<string, MutablePlayer>(StringComparer.Ordinal);
        private string _matchId = string.Empty;
        private string _modeId = string.Empty;
        private string _mapId = string.Empty;
        private string _serverBuild = string.Empty;
        private string _tournamentId = string.Empty;
        private long _startedAt;

        public MatchLifecycle Lifecycle { get; private set; } = MatchLifecycle.Waiting;

        public void RegisterPlayer(string playerId, string teamId, string wallet = "")
        {
            if (Lifecycle != MatchLifecycle.Waiting) throw new InvalidOperationException("Players can only register before start");
            Require(playerId, nameof(playerId));
            Require(teamId, nameof(teamId));
            if (_players.ContainsKey(playerId)) throw new InvalidOperationException("Duplicate player " + playerId);
            _players[playerId] = new MutablePlayer { PlayerId = playerId, TeamId = teamId, Wallet = wallet ?? string.Empty };
        }

        public void Start(string matchId, string modeId, string mapId, string serverBuild, long startedAt, string tournamentId = "")
        {
            if (Lifecycle != MatchLifecycle.Waiting) throw new InvalidOperationException("Match already started");
            if (_players.Count == 0) throw new InvalidOperationException("At least one player is required");
            Require(matchId, nameof(matchId)); Require(modeId, nameof(modeId)); Require(mapId, nameof(mapId)); Require(serverBuild, nameof(serverBuild));
            if (startedAt < 0) throw new ArgumentOutOfRangeException(nameof(startedAt));
            _matchId = matchId; _modeId = modeId; _mapId = mapId; _serverBuild = serverBuild;
            _tournamentId = tournamentId ?? string.Empty; _startedAt = startedAt;
            Lifecycle = MatchLifecycle.Running;
        }

        public void RecordKill(string killerId, string victimId, long scoreAward = 100)
        {
            RequireRunning();
            var killer = Get(killerId); var victim = Get(victimId);
            if (killerId == victimId) { victim.Deaths++; victim.Score -= Math.Abs(scoreAward); return; }
            killer.Kills++; killer.Score += scoreAward; victim.Deaths++;
        }

        public void RecordAssist(string playerId, long scoreAward = 50)
        {
            RequireRunning(); var player = Get(playerId); player.Assists++; player.Score += scoreAward;
        }

        public void AddScore(string playerId, long delta)
        {
            RequireRunning(); Get(playerId).Score += delta;
        }

        public MatchResult Finish(long endedAt, string antiCheatState = "passed", MatchRewardSlot[] rewards = null)
        {
            RequireRunning();
            if (endedAt < _startedAt) throw new ArgumentOutOfRangeException(nameof(endedAt));
            Require(antiCheatState, nameof(antiCheatState));
            var ordered = _players.Values
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Kills)
                .ThenBy(x => x.PlayerId, StringComparer.Ordinal)
                .ToArray();
            var results = new MatchPlayerResult[ordered.Length];
            for (var i = 0; i < ordered.Length; i++)
            {
                var p = ordered[i];
                results[i] = new MatchPlayerResult
                {
                    playerId = p.PlayerId, wallet = p.Wallet, teamId = p.TeamId,
                    kills = p.Kills, deaths = p.Deaths, assists = p.Assists, score = p.Score,
                    placement = i + 1, result = i == 0 ? "win" : "loss"
                };
            }

            Lifecycle = MatchLifecycle.Finished;
            return new MatchResult
            {
                version = "1.0", matchId = _matchId, modeId = _modeId, mapId = _mapId,
                startedAt = _startedAt, endedAt = endedAt, serverBuild = _serverBuild,
                tournamentId = _tournamentId, antiCheatState = antiCheatState,
                players = results, rewardSlots = rewards ?? Array.Empty<MatchRewardSlot>()
            };
        }

        private MutablePlayer Get(string playerId)
        {
            MutablePlayer player;
            if (!_players.TryGetValue(playerId, out player)) throw new KeyNotFoundException("Unknown player " + playerId);
            return player;
        }

        private void RequireRunning()
        {
            if (Lifecycle != MatchLifecycle.Running) throw new InvalidOperationException("Match is not running");
        }

        private static void Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name + " is required", name);
        }
    }
}
