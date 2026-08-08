using System;
using UnityEngine;

namespace Web3Fps.GameFoundation.Gameplay.Combat
{
    public sealed class HitscanWeapon : MonoBehaviour
    {
        [SerializeField] private string shooterId = "local-player";
        [SerializeField] private Transform muzzle;
        [SerializeField] private MonoBehaviour shotSink;
        [SerializeField, Min(0.1f)] private float damage = 25f;
        [SerializeField, Min(1f)] private float range = 150f;
        [SerializeField, Min(0.1f)] private float roundsPerSecond = 10f;
        [SerializeField, Min(1)] private int magazineCapacity = 30;
        [SerializeField, Min(0)] private int startingReserveAmmo = 120;
        [SerializeField, Min(0.1f)] private float reloadDuration = 1.65f;
        [Header("Handling")]
        [SerializeField, Min(0f)] private float hipSpreadDegrees = 1.15f;
        [SerializeField, Min(0f)] private float aimSpreadDegrees = 0.28f;
        [SerializeField, Min(0f)] private float movementSpreadDegrees = 1.35f;
        [SerializeField, Min(0f)] private float bloomPerShotDegrees = 0.16f;
        [SerializeField, Min(0f)] private float maximumBloomDegrees = 1.8f;
        [SerializeField, Min(0f)] private float bloomRecoveryPerSecond = 3.4f;

        private IShotCommandSink _sink;
        private WeaponAmmoState _ammo;
        private double _nextLocalShotAt;
        private double _reloadCompleteAt;
        private uint _sequence;
        private float _movementAmount;
        private float _bloom;
        private bool _aimHeld;

        public event Action<ShotResult> ShotResolved;
        public event Action AmmoChanged;
        public event Action ReloadStarted;
        public event Action ReloadCompleted;

        public int MagazineAmmo => _ammo == null ? magazineCapacity : _ammo.Magazine;
        public int MagazineCapacity => _ammo == null ? magazineCapacity : _ammo.MagazineCapacity;
        public int ReserveAmmo => _ammo == null ? startingReserveAmmo : _ammo.Reserve;
        public bool IsReloading => _ammo != null && _ammo.IsReloading;
        public float ReloadProgress => !IsReloading || reloadDuration <= 0f
            ? 0f
            : Mathf.Clamp01(1f - (float)((_reloadCompleteAt - Time.timeAsDouble) / reloadDuration));
        public float CurrentSpreadDegrees => WeaponAccuracyMath.CalculateSpread(
            hipSpreadDegrees, aimSpreadDegrees, movementSpreadDegrees, _movementAmount, _aimHeld, _bloom);

        private void Awake()
        {
            ResolveSink();
            InitializeAmmo();
        }

        private void Update()
        {
            _bloom = Mathf.MoveTowards(_bloom, 0f, bloomRecoveryPerSecond * Time.deltaTime);
            if (!IsReloading || Time.timeAsDouble < _reloadCompleteAt) return;
            _ammo.CompleteReload();
            AmmoChanged?.Invoke();
            ReloadCompleted?.Invoke();
        }

        private void OnDisable()
        {
            if (_ammo == null) return;
            _ammo.CancelReload();
        }

        public void Configure(string playerId, Transform muzzleTransform, MonoBehaviour commandSink)
        {
            shooterId = string.IsNullOrWhiteSpace(playerId) ? "local-player" : playerId;
            muzzle = muzzleTransform;
            shotSink = commandSink;
            ResolveSink();
        }

        public void ConfigureAmmo(int capacity, int reserve, float durationSeconds)
        {
            magazineCapacity = Mathf.Max(1, capacity);
            startingReserveAmmo = Mathf.Max(0, reserve);
            reloadDuration = Mathf.Max(0.1f, durationSeconds);
            InitializeAmmo();
            AmmoChanged?.Invoke();
        }

        // Combat numbers come only from the server-approved WeaponDefinition catalog;
        // NFT skin content must never reach this method.
        public void ApplyDefinition(WeaponDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            damage = Mathf.Max(0.1f, definition.damage);
            range = Mathf.Max(1f, definition.range);
            roundsPerSecond = Mathf.Max(0.1f, definition.roundsPerSecond);
            hipSpreadDegrees = Mathf.Max(0f, definition.hipSpreadDegrees);
            aimSpreadDegrees = Mathf.Max(0f, definition.aimSpreadDegrees);
            movementSpreadDegrees = Mathf.Max(0f, definition.movementSpreadDegrees);
            bloomPerShotDegrees = Mathf.Max(0f, definition.bloomPerShotDegrees);
            maximumBloomDegrees = Mathf.Max(0f, definition.maximumBloomDegrees);
            bloomRecoveryPerSecond = Mathf.Max(0f, definition.bloomRecoveryPerSecond);
            _ammo = null;
            ConfigureAmmo(definition.magazineCapacity, definition.reserveAmmo, definition.reloadSeconds);
        }

        public void SetHandlingState(float movementAmount, bool aimHeld)
        {
            _movementAmount = Mathf.Clamp01(movementAmount);
            _aimHeld = aimHeld;
        }

        private void ResolveSink()
        {
            _sink = shotSink as IShotCommandSink;
            if (_sink == null) Debug.LogError("HitscanWeapon requires a component implementing IShotCommandSink", this);
        }

        public bool TryFire(Vector3 aimDirection)
        {
            InitializeAmmo();
            if (_sink == null || muzzle == null || !_ammo.CanFire || Time.timeAsDouble < _nextLocalShotAt) return false;
            _nextLocalShotAt = Time.timeAsDouble + 1d / roundsPerSecond;
            var sequence = ++_sequence;
            var result = _sink.Submit(new ShotCommand
            {
                ShooterId = shooterId,
                ShooterObject = gameObject,
                Origin = muzzle.position,
                Direction = WeaponAccuracyMath.ApplySpread(aimDirection.normalized, sequence, CurrentSpreadDegrees),
                Sequence = sequence,
                ClientTimestamp = Time.timeAsDouble,
                Damage = damage,
                Range = range
            });
            if (result.Accepted)
            {
                _ammo.ConsumeRound();
                _bloom = Mathf.Min(maximumBloomDegrees, _bloom + bloomPerShotDegrees);
                AmmoChanged?.Invoke();
            }
            ShotResolved?.Invoke(result);
            return result.Accepted;
        }

        public bool TryReload()
        {
            InitializeAmmo();
            if (!_ammo.BeginReload()) return false;
            _reloadCompleteAt = Time.timeAsDouble + reloadDuration;
            ReloadStarted?.Invoke();
            AmmoChanged?.Invoke();
            return true;
        }

        public void RefillAmmo()
        {
            _ammo = new WeaponAmmoState(magazineCapacity, startingReserveAmmo);
            _reloadCompleteAt = 0d;
            _bloom = 0f;
            AmmoChanged?.Invoke();
        }

        private void InitializeAmmo()
        {
            if (_ammo == null) _ammo = new WeaponAmmoState(magazineCapacity, startingReserveAmmo);
        }
    }
}
