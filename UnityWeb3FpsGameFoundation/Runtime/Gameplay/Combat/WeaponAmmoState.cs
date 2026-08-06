using System;

namespace Web3Fps.GameFoundation.Gameplay.Combat
{
    [Serializable]
    public sealed class WeaponAmmoState
    {
        public int MagazineCapacity { get; private set; }
        public int Magazine { get; private set; }
        public int Reserve { get; private set; }
        public bool IsReloading { get; private set; }

        public WeaponAmmoState(int magazineCapacity, int reserve)
        {
            Reset(magazineCapacity, reserve);
        }

        public bool CanFire => !IsReloading && Magazine > 0;
        public bool CanReload => !IsReloading && Magazine < MagazineCapacity && Reserve > 0;

        public bool ConsumeRound()
        {
            if (!CanFire) return false;
            Magazine--;
            return true;
        }

        public bool BeginReload()
        {
            if (!CanReload) return false;
            IsReloading = true;
            return true;
        }

        public int CompleteReload()
        {
            if (!IsReloading) return 0;
            var transferred = Math.Min(MagazineCapacity - Magazine, Reserve);
            Magazine += transferred;
            Reserve -= transferred;
            IsReloading = false;
            return transferred;
        }

        public void CancelReload() => IsReloading = false;

        public void Reset(int magazineCapacity, int reserve)
        {
            MagazineCapacity = Math.Max(1, magazineCapacity);
            Magazine = MagazineCapacity;
            Reserve = Math.Max(0, reserve);
            IsReloading = false;
        }
    }
}
