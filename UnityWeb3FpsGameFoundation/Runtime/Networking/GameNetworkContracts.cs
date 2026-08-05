using System.Threading;
using System.Threading.Tasks;
using Web3Fps.GameFoundation.Gameplay;
using Web3Fps.GameFoundation.Gameplay.Combat;
using Web3Fps.GameFoundation.Match;

namespace Web3Fps.GameFoundation.Networking
{
    /// <summary>Implement once for NGO, Mirror, FishNet or Photon.</summary>
    public interface IGameNetworkAdapter
    {
        bool IsServer { get; }
        bool IsLocalPlayer(string playerId);
        void SubmitInput(string playerId, PlayerInputFrame input);
        void SubmitShot(ShotCommand command);
        void BroadcastMatchResult(MatchResult result);
    }

    /// <summary>Server-to-backend seam; never implemented by an untrusted client.</summary>
    public interface IMatchResultPublisher
    {
        Task PublishAsync(MatchAttestationPayload payload, CancellationToken ct = default);
    }
}
