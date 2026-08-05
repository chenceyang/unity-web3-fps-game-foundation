using System.Threading;
using System.Threading.Tasks;

namespace Web3Fps.GameFoundation.Tournaments
{
    public interface ITournamentGateway
    {
        Task<TournamentList> GetTournamentsAsync(CancellationToken ct = default);
        Task<TournamentSummary> GetTournamentAsync(string tournamentId, CancellationToken ct = default);
        Task<TransactionIntent> BeginRegisterAsync(string tournamentId, CancellationToken ct = default);
        Task<TransactionIntent> BeginSponsorAsync(string tournamentId, string amountWei, CancellationToken ct = default);
        Task<TransactionIntent> BeginClaimPrizeAsync(string tournamentId, CancellationToken ct = default);
        Task<TransactionIntent> BeginClaimRefundAsync(string tournamentId, CancellationToken ct = default);
        Task<TransactionStatus> PollTransactionAsync(string intentId, CancellationToken ct = default);
    }
}
