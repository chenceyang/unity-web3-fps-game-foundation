using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using Web3Fps.GameFoundation.Match;
using Web3Fps.GameFoundation.Networking;

namespace Web3Fps.GameFoundation.Server
{
    public enum MatchPublishOutcome
    {
        NotAttempted,
        Published,
        Duplicate,
        Conflict,
        Failed
    }

    public sealed class LocalMatchPublishReport
    {
        public string MatchId { get; }
        public MatchPublishOutcome Outcome { get; }
        public string ResultHash { get; }
        public string Message { get; }

        public LocalMatchPublishReport(string matchId, MatchPublishOutcome outcome, string resultHash, string message)
        {
            MatchId = matchId ?? string.Empty;
            Outcome = outcome;
            ResultHash = resultHash ?? string.Empty;
            Message = message ?? string.Empty;
        }
    }

    /// <summary>
    /// Frozen pre-match state handed from the lobby confirm step to the local combat
    /// scene. Entitlement was already resolved when a ticket exists, so combat runs
    /// with zero gateway calls; the publisher/archive references are used strictly
    /// after the match ends.
    /// </summary>
    public sealed class LocalMatchTicket
    {
        public string MatchId { get; }
        public string PlayerId { get; }
        public string Wallet { get; }
        public PlayerLoadoutSnapshot Snapshot { get; }
        public bool UsedFallback { get; }
        public MatchPublishCoordinator Publisher { get; }
        public ITournamentGateway ArchiveGateway { get; }

        public LocalMatchTicket(
            string matchId,
            string playerId,
            string wallet,
            PlayerLoadoutSnapshot snapshot,
            bool usedFallback,
            MatchPublishCoordinator publisher = null,
            ITournamentGateway archiveGateway = null)
        {
            if (string.IsNullOrWhiteSpace(matchId)) throw new ArgumentException("matchId is required", nameof(matchId));
            if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("playerId is required", nameof(playerId));
            MatchId = matchId;
            PlayerId = playerId;
            Wallet = wallet ?? string.Empty;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            UsedFallback = usedFallback;
            Publisher = publisher;
            ArchiveGateway = archiveGateway;
        }
    }

    /// <summary>
    /// In-process scene handoff for the local vertical slice. A networking adapter
    /// replaces this static exchange with real server-to-client replication.
    /// </summary>
    public static class LocalMatchHandoff
    {
        private static LocalMatchTicket _pending;

        public static LocalMatchPublishReport LastReport { get; private set; }

        public static void Stage(LocalMatchTicket ticket) => _pending = ticket;

        public static LocalMatchTicket TakePending()
        {
            var ticket = _pending;
            _pending = null;
            return ticket;
        }

        public static void Record(LocalMatchPublishReport report) => LastReport = report;

        public static void Reset()
        {
            _pending = null;
            LastReport = null;
        }
    }

    /// <summary>
    /// Pure helpers for the local dedicated-server role: freeze the loadout before
    /// combat, derive backend reward slots from the finished result and map the
    /// publish attempt to a lobby-visible outcome.
    /// </summary>
    public static class LocalMatchFlow
    {
        public const int WinnerRewardSlot = 0;

        public static string CreateMatchId() => "rift-relay-" + Guid.NewGuid().ToString("N");

        public static LoadoutEntitlementRequest CreateEntitlementRequest(
            string matchId,
            string playerId,
            string wallet,
            string[] equippedTokenIds)
        {
            if (string.IsNullOrWhiteSpace(matchId)) throw new ArgumentException("matchId is required", nameof(matchId));
            if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("playerId is required", nameof(playerId));
            if (equippedTokenIds == null) throw new ArgumentNullException(nameof(equippedTokenIds));
            var tokens = new string[equippedTokenIds.Length];
            for (var i = 0; i < tokens.Length; i++) tokens[i] = equippedTokenIds[i] ?? string.Empty;
            return new LoadoutEntitlementRequest
            {
                matchId = matchId,
                playerId = playerId,
                wallet = wallet ?? string.Empty,
                tokenIdsBySlot = tokens
            };
        }

        // Mock mode only: the mock entitlement gateway is the local stand-in for the
        // chain, so it is seeded from the mock inventory the same backend would read.
        // The real backend performs its own chain reads and ignores client claims.
        public static void SyncMockChainView(IEntitlementGateway gateway, PlayerAssets assets)
        {
            var mock = gateway as MockEntitlementGateway;
            if (mock == null || assets?.items == null) return;
            foreach (var item in assets.items)
            {
                if (item != null && item.IsConfirmed && !string.IsNullOrWhiteSpace(item.tokenId))
                    mock.Grant(item.tokenId, item.skinDefId);
            }
        }

        public static async Task<LocalMatchTicket> FreezeLoadoutAsync(
            LoadoutSnapshotResolver resolver,
            LoadoutEntitlementRequest request,
            MatchPublishCoordinator publisher = null,
            ITournamentGateway archiveGateway = null,
            CancellationToken ct = default)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            if (request == null) throw new ArgumentNullException(nameof(request));
            var resolution = await resolver.ResolveOrDefaultAsync(request, ct);
            return new LocalMatchTicket(
                request.matchId,
                request.playerId,
                request.wallet,
                resolution.Snapshot,
                resolution.UsedFallback,
                publisher,
                archiveGateway);
        }

        public static LocalMatchTicket CreateOfflineTicket(int slotCount, string playerId = "guest")
        {
            if (slotCount < 1) throw new ArgumentOutOfRangeException(nameof(slotCount));
            var slots = new ResolvedSkinSlot[slotCount];
            for (var i = 0; i < slots.Length; i++)
                slots[i] = new ResolvedSkinSlot { slot = i, resolution = "default", reason = "offline" };
            var matchId = CreateMatchId();
            var snapshot = new PlayerLoadoutSnapshot
            {
                snapshotId = "offline-" + Guid.NewGuid().ToString("N"),
                matchId = matchId,
                playerId = playerId,
                wallet = string.Empty,
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                slots = slots
            };
            return new LocalMatchTicket(matchId, playerId, string.Empty, snapshot, false);
        }

        public static bool HasDegradedSlots(PlayerLoadoutSnapshot snapshot)
        {
            if (snapshot?.slots == null) return false;
            foreach (var slot in snapshot.slots)
            {
                if (slot != null && slot.UsesDefault && !string.IsNullOrEmpty(slot.requestedTokenId)) return true;
            }
            return false;
        }

        // The server knows the participant→account mapping; published results must
        // carry account playerIds so backend rewards land on the right player. Skips
        // itself when the mapping would collide with another participant id.
        public static MatchResult WithLocalIdentity(
            MatchResult result,
            string participantId,
            string accountPlayerId,
            string wallet)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.players == null ||
                string.IsNullOrWhiteSpace(participantId) ||
                string.IsNullOrWhiteSpace(accountPlayerId) ||
                string.Equals(participantId, accountPlayerId, StringComparison.Ordinal))
                return result;
            foreach (var player in result.players)
            {
                if (player != null && string.Equals(player.playerId, accountPlayerId, StringComparison.Ordinal))
                    return result;
            }

            var players = new MatchPlayerResult[result.players.Length];
            for (var i = 0; i < players.Length; i++)
            {
                var source = result.players[i];
                if (source == null) { players[i] = null; continue; }
                var isLocal = string.Equals(source.playerId, participantId, StringComparison.Ordinal);
                players[i] = new MatchPlayerResult
                {
                    playerId = isLocal ? accountPlayerId : source.playerId,
                    wallet = isLocal ? wallet ?? string.Empty : source.wallet,
                    teamId = source.teamId,
                    kills = source.kills,
                    deaths = source.deaths,
                    assists = source.assists,
                    score = source.score,
                    placement = source.placement,
                    result = source.result
                };
            }
            return new MatchResult
            {
                version = result.version,
                matchId = result.matchId,
                modeId = result.modeId,
                mapId = result.mapId,
                startedAt = result.startedAt,
                endedAt = result.endedAt,
                serverBuild = result.serverBuild,
                tournamentId = result.tournamentId,
                antiCheatState = result.antiCheatState,
                players = players,
                rewardSlots = result.rewardSlots
            };
        }

        // Demo reward policy: one reward slot for the first-placed player. The backend
        // derives the actual skin/wear from the rewardId, so the id must stay
        // deterministic for the same match to keep the mint request idempotent.
        public static MatchResult WithDerivedRewards(MatchResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            return new MatchResult
            {
                version = result.version,
                matchId = result.matchId,
                modeId = result.modeId,
                mapId = result.mapId,
                startedAt = result.startedAt,
                endedAt = result.endedAt,
                serverBuild = result.serverBuild,
                tournamentId = result.tournamentId,
                antiCheatState = result.antiCheatState,
                players = result.players,
                rewardSlots = BuildRewardSlots(result)
            };
        }

        public static MatchRewardSlot[] BuildRewardSlots(MatchResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.players == null || !string.Equals(result.antiCheatState, "passed", StringComparison.Ordinal))
                return Array.Empty<MatchRewardSlot>();
            MatchPlayerResult winner = null;
            foreach (var player in result.players)
            {
                if (player == null) continue;
                if (winner == null || player.placement < winner.placement) winner = player;
            }
            if (winner == null || string.IsNullOrWhiteSpace(result.matchId)) return Array.Empty<MatchRewardSlot>();
            var rewardId = "rw_" + result.matchId + "_" + WinnerRewardSlot;
            if (rewardId.Length > 128) return Array.Empty<MatchRewardSlot>();
            return new[]
            {
                new MatchRewardSlot { slot = WinnerRewardSlot, playerId = winner.playerId, rewardId = rewardId }
            };
        }

        public static async Task<LocalMatchPublishReport> PublishAsync(
            MatchPublishCoordinator publisher,
            MatchAttestationPayload payload,
            CancellationToken ct = default)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            var matchId = payload.Result.matchId;
            if (publisher == null)
            {
                return new LocalMatchPublishReport(
                    matchId, MatchPublishOutcome.NotAttempted, payload.ResultHash, "No publisher is configured.");
            }
            try
            {
                var published = await publisher.PublishOnceAsync(payload, ct);
                return new LocalMatchPublishReport(
                    matchId,
                    published ? MatchPublishOutcome.Published : MatchPublishOutcome.Duplicate,
                    payload.ResultHash,
                    published ? "Result accepted by the backend." : "Identical result was already published.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (MatchResultConflictException exception)
            {
                return new LocalMatchPublishReport(
                    matchId, MatchPublishOutcome.Conflict, payload.ResultHash, exception.Message);
            }
            catch (Exception exception)
            {
                return new LocalMatchPublishReport(
                    matchId, MatchPublishOutcome.Failed, payload.ResultHash, exception.Message);
            }
        }
    }
}
