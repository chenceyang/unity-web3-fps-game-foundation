using System;
using UnityEngine;
using Web3Fps.GameFoundation.Gameplay;
using Web3Fps.GameFoundation.Gameplay.Combat;

namespace Web3Fps.GameFoundation.Formal
{
    [DisallowMultipleComponent]
    public sealed class FirstPersonWeaponPresentation : MonoBehaviour
    {
        [SerializeField] private HitscanWeapon weapon;
        [SerializeField] private FirstPersonLook look;
        [SerializeField] private Transform weaponRoot;
        [SerializeField] private Transform magazine;
        [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private Vector3 _magazineBasePosition;
        private Vector3 _positionKick;
        private Vector3 _rotationKick;
        private AudioSource _audio;
        private AudioClip _shotClip;
        private AudioClip _reloadOpenClip;
        private AudioClip _reloadCloseClip;

        public void Configure(
            HitscanWeapon targetWeapon,
            FirstPersonLook firstPersonLook,
            Transform presentationRoot,
            Transform magazineTransform)
        {
            Unsubscribe();
            weapon = targetWeapon;
            look = firstPersonLook;
            weaponRoot = presentationRoot == null ? transform : presentationRoot;
            magazine = magazineTransform;
            CaptureBasePose();
            if (isActiveAndEnabled) Subscribe();
        }

        private void Awake()
        {
            if (weaponRoot == null) weaponRoot = transform;
            CaptureBasePose();
            // Clip synthesis is ~10k samples of math; doing it lazily put that cost
            // inside the first trigger pull as a one-off hitch. Warm it up instead.
            EnsureAudio();
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            Unsubscribe();
            RestoreBasePose();
        }

        private void OnDestroy()
        {
            if (_shotClip != null) Destroy(_shotClip);
            if (_reloadOpenClip != null) Destroy(_reloadOpenClip);
            if (_reloadCloseClip != null) Destroy(_reloadCloseClip);
        }

        private void LateUpdate()
        {
            if (weaponRoot == null || weapon == null) return;
            _positionKick = Vector3.Lerp(_positionKick, Vector3.zero, 1f - Mathf.Exp(-18f * Time.deltaTime));
            _rotationKick = Vector3.Lerp(_rotationKick, Vector3.zero, 1f - Mathf.Exp(-21f * Time.deltaTime));

            var reloadPose = WeaponPresentationMath.EvaluateReload(weapon.IsReloading ? weapon.ReloadProgress : 0f);
            var reloadPosition = new Vector3(0.06f, -0.19f, -0.08f) * reloadPose.Arc;
            var reloadRotation = new Vector3(20f, -18f, 58f) * reloadPose.Arc;
            weaponRoot.localPosition = _basePosition + _positionKick + reloadPosition;
            weaponRoot.localRotation = _baseRotation * Quaternion.Euler(_rotationKick + reloadRotation);
            if (magazine != null)
                magazine.localPosition = _magazineBasePosition + Vector3.down * (0.3f * reloadPose.MagazineDrop);
        }

        private void Subscribe()
        {
            if (weapon == null) return;
            weapon.ShotResolved += OnShotResolved;
            weapon.ReloadStarted += OnReloadStarted;
            weapon.ReloadCompleted += OnReloadCompleted;
        }

        private void Unsubscribe()
        {
            if (weapon == null) return;
            weapon.ShotResolved -= OnShotResolved;
            weapon.ReloadStarted -= OnReloadStarted;
            weapon.ReloadCompleted -= OnReloadCompleted;
        }

        private void OnShotResolved(ShotResult result)
        {
            if (!result.Accepted) return;
            var yaw = UnityEngine.Random.Range(-1.1f, 1.1f);
            _positionKick += new Vector3(0f, -0.025f, -0.1f);
            _rotationKick += new Vector3(-7.5f, yaw, UnityEngine.Random.Range(-1.2f, 1.2f));
            if (look != null) look.AddRecoil(1.35f, yaw * 0.35f);
            EnsureAudio();
            _audio.PlayOneShot(_shotClip, volume);
        }

        private void OnReloadStarted()
        {
            EnsureAudio();
            _audio.PlayOneShot(_reloadOpenClip, volume * 0.75f);
        }

        private void OnReloadCompleted()
        {
            EnsureAudio();
            _audio.PlayOneShot(_reloadCloseClip, volume * 0.9f);
        }

        private void CaptureBasePose()
        {
            if (weaponRoot != null)
            {
                _basePosition = weaponRoot.localPosition;
                _baseRotation = weaponRoot.localRotation;
            }
            if (magazine != null) _magazineBasePosition = magazine.localPosition;
        }

        private void RestoreBasePose()
        {
            if (weaponRoot != null)
            {
                weaponRoot.localPosition = _basePosition;
                weaponRoot.localRotation = _baseRotation;
            }
            if (magazine != null) magazine.localPosition = _magazineBasePosition;
        }

        private void EnsureAudio()
        {
            if (_audio == null)
            {
                _audio = GetComponent<AudioSource>();
                if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
                _audio.spatialBlend = 0f;
                _audio.dopplerLevel = 0f;
            }
            if (_shotClip == null) _shotClip = CreateShotClip();
            if (_reloadOpenClip == null) _reloadOpenClip = CreateMechanicalClip("Magazine Release", 0.17f, 760f);
            if (_reloadCloseClip == null) _reloadCloseClip = CreateMechanicalClip("Magazine Seat", 0.2f, 430f);
        }

        private static AudioClip CreateShotClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.24f;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            var random = new System.Random(7319);
            var filteredNoise = 0f;
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var noise = (float)(random.NextDouble() * 2d - 1d);
                filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.32f);
                var crack = noise * Mathf.Exp(-t * 58f) * 0.72f;
                var body = Mathf.Sin(2f * Mathf.PI * (92f + 34f * Mathf.Exp(-t * 10f)) * t) * Mathf.Exp(-t * 14f) * 0.62f;
                var mechanism = filteredNoise * Mathf.Exp(-t * 19f) * 0.25f;
                data[i] = Mathf.Clamp(crack + body + mechanism, -1f, 1f);
            }
            var clip = AudioClip.Create("ASH LEDGER Kestrel Shot", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateMechanicalClip(string name, float duration, float frequency)
        {
            const int sampleRate = 44100;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];
            var random = new System.Random(name == "Magazine Release" ? 1907 : 4813);
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Exp(-t * 34f);
                var tone = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.46f;
                var click = (float)(random.NextDouble() * 2d - 1d) * 0.38f;
                data[i] = (tone + click) * envelope;
            }
            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
