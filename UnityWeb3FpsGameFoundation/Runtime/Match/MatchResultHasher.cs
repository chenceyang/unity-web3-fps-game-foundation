using Web3Fps.GameFoundation.Crypto;

namespace Web3Fps.GameFoundation.Match
{
    public static class MatchResultHasher
    {
        public static MatchAttestationPayload CreatePayload(MatchResult result)
        {
            var canonical = CanonicalMatchResultSerializer.SerializeUtf8(result);
            return new MatchAttestationPayload(
                result,
                canonical,
                Keccak256.ComputeUtf8Hex(result.matchId),
                Keccak256.ComputeHex(canonical));
        }
    }
}
