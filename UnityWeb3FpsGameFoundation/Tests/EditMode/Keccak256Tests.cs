using System.Text;
using NUnit.Framework;
using Web3Fps.GameFoundation.Crypto;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class Keccak256Tests
    {
        [Test]
        public void EmptyVectorMatchesEthereumKeccak()
        {
            Assert.That(Keccak256.ComputeHex(new byte[0], false), Is.EqualTo(
                "c5d2460186f7233c927e7db2dcc703c0e500b653ca82273b7bfad8045d85a470"));
        }

        [Test]
        public void AbcVectorMatchesEthereumKeccak()
        {
            Assert.That(Keccak256.ComputeHex(Encoding.UTF8.GetBytes("abc"), false), Is.EqualTo(
                "4e03657aea45a94fc7d47ba826c8d667c0d1e6e33a64a036ec44f58fa12d6c45"));
        }

        [Test]
        public void VerifyAcceptsPrefixedMixedCaseHash()
        {
            var bytes = Encoding.UTF8.GetBytes("abc");
            Assert.That(Keccak256.Verify(bytes,
                "0x4E03657AEA45A94FC7D47BA826C8D667C0D1E6E33A64A036EC44F58FA12D6C45"), Is.True);
        }
    }
}
