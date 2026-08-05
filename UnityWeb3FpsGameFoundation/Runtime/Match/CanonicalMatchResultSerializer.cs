using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Web3Fps.GameFoundation.Match
{
    /// <summary>
    /// Deterministic JSON writer for the fixed MatchResult schema. Property order is lexical,
    /// numbers are integers, strings use JSON escapes and arrays are sorted by the game contract.
    /// </summary>
    public static class CanonicalMatchResultSerializer
    {
        public static byte[] SerializeUtf8(MatchResult result)
        {
            Validate(result);
            var json = Serialize(result);
            return new UTF8Encoding(false).GetBytes(json);
        }

        public static string Serialize(MatchResult result)
        {
            Validate(result);
            var players = result.players.OrderBy(x => x.playerId, StringComparer.Ordinal).ToArray();
            var rewards = result.rewardSlots
                .OrderBy(x => x.slot, StringComparer.Ordinal)
                .ThenBy(x => x.playerId, StringComparer.Ordinal)
                .ToArray();

            var b = new StringBuilder(1024);
            b.Append('{');
            Property(b, "antiCheatState", result.antiCheatState); b.Append(',');
            Property(b, "endedAt", result.endedAt); b.Append(',');
            Property(b, "mapId", result.mapId); b.Append(',');
            Property(b, "matchId", result.matchId); b.Append(',');
            Property(b, "modeId", result.modeId); b.Append(',');
            Name(b, "players"); b.Append('[');
            for (var i = 0; i < players.Length; i++)
            {
                if (i > 0) b.Append(',');
                WritePlayer(b, players[i]);
            }
            b.Append(']').Append(',');
            Name(b, "rewardSlots"); b.Append('[');
            for (var i = 0; i < rewards.Length; i++)
            {
                if (i > 0) b.Append(',');
                WriteReward(b, rewards[i]);
            }
            b.Append(']').Append(',');
            Property(b, "serverBuild", result.serverBuild); b.Append(',');
            Property(b, "startedAt", result.startedAt); b.Append(',');
            Name(b, "tournamentId");
            if (string.IsNullOrEmpty(result.tournamentId)) b.Append("null"); else String(b, result.tournamentId);
            b.Append(',');
            Property(b, "version", result.version);
            b.Append('}');
            return b.ToString();
        }

        public static void Validate(MatchResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            Required(result.version, "version");
            Required(result.matchId, "matchId");
            Required(result.modeId, "modeId");
            Required(result.mapId, "mapId");
            Required(result.serverBuild, "serverBuild");
            Required(result.antiCheatState, "antiCheatState");
            if (result.startedAt < 0 || result.endedAt < result.startedAt)
                throw new ArgumentException("Match timestamps are invalid", nameof(result));
            if (result.players == null || result.players.Length == 0)
                throw new ArgumentException("At least one player is required", nameof(result));
            if (result.rewardSlots == null) result.rewardSlots = Array.Empty<MatchRewardSlot>();

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var player in result.players)
            {
                if (player == null) throw new ArgumentException("Player entries cannot be null", nameof(result));
                Required(player.playerId, "players.playerId");
                Required(player.teamId, "players.teamId");
                Required(player.result, "players.result");
                if (!ids.Add(player.playerId)) throw new ArgumentException("Duplicate playerId " + player.playerId);
                if (player.placement < 1) throw new ArgumentException("placement must start at 1");
                if (player.kills < 0 || player.deaths < 0 || player.assists < 0)
                    throw new ArgumentException("Combat statistics cannot be negative");
            }

            foreach (var reward in result.rewardSlots)
            {
                if (reward == null) throw new ArgumentException("Reward entries cannot be null", nameof(result));
                Required(reward.slot, "rewardSlots.slot");
                Required(reward.playerId, "rewardSlots.playerId");
                Required(reward.rewardId, "rewardSlots.rewardId");
                if (!ids.Contains(reward.playerId))
                    throw new ArgumentException("Reward references unknown player " + reward.playerId);
            }
        }

        private static void WritePlayer(StringBuilder b, MatchPlayerResult p)
        {
            b.Append('{');
            Property(b, "assists", p.assists); b.Append(',');
            Property(b, "deaths", p.deaths); b.Append(',');
            Property(b, "kills", p.kills); b.Append(',');
            Property(b, "placement", p.placement); b.Append(',');
            Property(b, "playerId", p.playerId); b.Append(',');
            Property(b, "result", p.result); b.Append(',');
            Property(b, "score", p.score); b.Append(',');
            Property(b, "teamId", p.teamId); b.Append(',');
            Property(b, "wallet", p.wallet ?? string.Empty);
            b.Append('}');
        }

        private static void WriteReward(StringBuilder b, MatchRewardSlot r)
        {
            b.Append('{');
            Property(b, "playerId", r.playerId); b.Append(',');
            Property(b, "rewardId", r.rewardId); b.Append(',');
            Property(b, "slot", r.slot);
            b.Append('}');
        }

        private static void Property(StringBuilder b, string name, string value)
        {
            Name(b, name); String(b, value ?? string.Empty);
        }

        private static void Property(StringBuilder b, string name, long value)
        {
            Name(b, name); b.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void Name(StringBuilder b, string name)
        {
            String(b, name); b.Append(':');
        }

        private static void String(StringBuilder b, string value)
        {
            b.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                switch (ch)
                {
                    case '"': b.Append("\\\""); break;
                    case '\\': b.Append("\\\\"); break;
                    case '\b': b.Append("\\b"); break;
                    case '\f': b.Append("\\f"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\t': b.Append("\\t"); break;
                    default:
                        if (ch < 0x20) b.Append("\\u").Append(((int)ch).ToString("x4"));
                        else b.Append(ch);
                        break;
                }
            }
            b.Append('"');
        }

        private static void Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name + " is required");
        }
    }
}
