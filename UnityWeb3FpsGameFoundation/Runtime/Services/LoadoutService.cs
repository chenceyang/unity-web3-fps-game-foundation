using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;

namespace Web3Fps.GameFoundation.Services
{
    public sealed class LoadoutService
    {
        private readonly IGameAssetGateway _gateway;
        private readonly GameAssetService _assets;
        private readonly string[] _tokenIdsBySlot;

        public int SlotCount => _tokenIdsBySlot.Length;
        public event Action<int, string> SlotChanged;

        public LoadoutService(IGameAssetGateway gateway, GameAssetService assets, int slotCount)
        {
            if (slotCount <= 0) throw new ArgumentOutOfRangeException(nameof(slotCount));
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
            _tokenIdsBySlot = new string[slotCount];
            for (var i = 0; i < slotCount; i++) _tokenIdsBySlot[i] = string.Empty;
        }

        public string GetTokenId(int slot)
        {
            ValidateSlot(slot);
            return _tokenIdsBySlot[slot];
        }

        public bool TryEquip(int slot, string tokenId)
        {
            ValidateSlot(slot);
            if (string.IsNullOrWhiteSpace(tokenId))
            {
                UseDefault(slot);
                return true;
            }

            if (_assets.FindConfirmed(tokenId) == null) return false;
            _tokenIdsBySlot[slot] = tokenId;
            SlotChanged?.Invoke(slot, tokenId);
            return true;
        }

        public void UseDefault(int slot)
        {
            ValidateSlot(slot);
            _tokenIdsBySlot[slot] = string.Empty;
            SlotChanged?.Invoke(slot, string.Empty);
        }

        public void ReconcileOwnership()
        {
            for (var i = 0; i < _tokenIdsBySlot.Length; i++)
                if (!string.IsNullOrWhiteSpace(_tokenIdsBySlot[i]) && _assets.FindConfirmed(_tokenIdsBySlot[i]) == null)
                    UseDefault(i);
        }

        public Task SaveIntentAsync(CancellationToken ct = default)
        {
            var copy = new string[_tokenIdsBySlot.Length];
            Array.Copy(_tokenIdsBySlot, copy, copy.Length);
            return _gateway.SetLoadoutAsync(new LoadoutRequest { tokenIdsBySlot = copy }, ct);
        }

        private void ValidateSlot(int slot)
        {
            if (slot < 0 || slot >= _tokenIdsBySlot.Length) throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }
}
