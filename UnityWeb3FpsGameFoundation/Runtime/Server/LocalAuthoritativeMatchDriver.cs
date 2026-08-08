using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Web3Fps.GameFoundation.Formal;
using Web3Fps.GameFoundation.Match;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Server
{
    /// <summary>
    /// Local-process stand-in for the dedicated-server match boundary, following the
    /// LocalAuthoritativeShotSink philosophy: in the vertical slice this process plays
    /// the server role, and a networking adapter later replaces the driver together
    /// with LocalMatchHandoff. Entitlement is frozen before this scene loads and the
    /// result is published once after the match ends — the combat loop itself makes
    /// zero gateway calls. AuthoritativeMatchSession stays reserved for the networking
    /// phase, where the server also owns per-kill scoring; here score authority lives
    /// in PrototypeMatchController, so the driver reuses the same resolver, hasher and
    /// publish-once seams instead of forcing a second score coordinator.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalAuthoritativeMatchDriver : MonoBehaviour
    {
        [SerializeField] private PrototypeMatchController match;
        [SerializeField] private PrototypeParticipant localPlayer;
        [SerializeField] private FormalSkinApplicator viewModelApplicator;

        private LocalMatchTicket _ticket;
        private MatchPublishCoordinator _publisher;
        private bool _firstMatchIdServed;

        public LocalMatchTicket Ticket => _ticket;
        public LocalMatchPublishReport PublishReport { get; private set; }

        public event Action<LocalMatchPublishReport> PublishCompleted;

        public void Configure(
            PrototypeMatchController matchController,
            PrototypeParticipant player,
            FormalSkinApplicator weaponApplicator)
        {
            match = matchController;
            localPlayer = player;
            viewModelApplicator = weaponApplicator;
        }

        private void Awake()
        {
            _ticket = LocalMatchHandoff.TakePending()
                      ?? LocalMatchFlow.CreateOfflineTicket(FormalContentCatalog.CosmeticSlotNames.Length);
            _publisher = _ticket.Publisher ?? new MatchPublishCoordinator(new MockMatchResultPublisher());
            // Rematches keep the frozen snapshot (no gateway inside the combat scene);
            // the networked server re-runs entitlement per match instead.
            if (match != null) match.SetMatchIdProvider(NextMatchId);
        }

        private void OnEnable()
        {
            if (match != null) match.MatchFinished += OnMatchFinished;
            if (localPlayer != null) localPlayer.Respawned += ApplyFrozenSkins;
            ApplyFrozenSkins();
        }

        private void OnDisable()
        {
            if (match != null) match.MatchFinished -= OnMatchFinished;
            if (localPlayer != null) localPlayer.Respawned -= ApplyFrozenSkins;
        }

        private void ApplyFrozenSkins()
        {
            if (viewModelApplicator == null) return;
            var slots = _ticket?.Snapshot?.slots;
            var index = (int)FormalCosmeticSlot.WeaponFinish;
            var slot = slots != null && index < slots.Length ? slots[index] : null;
            if (slot == null || slot.UsesDefault) viewModelApplicator.ClearOverrides();
            else viewModelApplicator.Apply(FormalSkinCatalog.GetOrDefault(slot.skinDefId));
        }

        private string NextMatchId()
        {
            if (!_firstMatchIdServed)
            {
                _firstMatchIdServed = true;
                return _ticket.MatchId;
            }
            return LocalMatchFlow.CreateMatchId();
        }

        private void OnMatchFinished(MatchResult result)
        {
            if (result == null) return;
            var mapped = LocalMatchFlow.WithLocalIdentity(
                result,
                localPlayer == null ? string.Empty : localPlayer.ParticipantId,
                _ticket.PlayerId,
                _ticket.Wallet);
            var payload = MatchResultHasher.CreatePayload(LocalMatchFlow.WithDerivedRewards(mapped));
            // Deliberately not cancelled on scene unload: the local process is playing
            // the server role, and the server-side publish must survive the player
            // returning to the lobby. LocalMatchHandoff keeps the outcome visible there.
            _ = PublishAsync(payload);
        }

        private async Task PublishAsync(MatchAttestationPayload payload)
        {
            var report = await LocalMatchFlow.PublishAsync(_publisher, payload, CancellationToken.None);
            PublishReport = report;
            LocalMatchHandoff.Record(report);
            PublishCompleted?.Invoke(report);
        }
    }
}
