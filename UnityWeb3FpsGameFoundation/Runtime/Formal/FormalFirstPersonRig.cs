using UnityEngine;
using Web3Fps.GameFoundation.Gameplay.Combat;

namespace Web3Fps.GameFoundation.Formal
{
    [DisallowMultipleComponent]
    public sealed class FormalFirstPersonRig : MonoBehaviour
    {
        private static readonly int FireHash = Animator.StringToHash("Fire");

        [SerializeField] private HitscanWeapon weapon;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform rigRoot;
        [SerializeField, Min(0f)] private float reloadLowering = 0.42f;
        [SerializeField, Min(0f)] private float smoothing = 18f;

        private Vector3 _basePosition;
        private Quaternion _baseRotation;

        public void Configure(HitscanWeapon sourceWeapon, Animator targetAnimator, Transform targetRigRoot)
        {
            Unsubscribe();
            weapon = sourceWeapon;
            animator = targetAnimator;
            rigRoot = targetRigRoot;
            CapturePose();
            if (isActiveAndEnabled) Subscribe();
        }

        private void Awake() => CapturePose();
        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void LateUpdate()
        {
            if (weapon == null || rigRoot == null) return;
            var arc = WeaponPresentationMath.EvaluateReload(weapon.IsReloading ? weapon.ReloadProgress : 0f).Arc;
            var desiredPosition = _basePosition + Vector3.down * (arc * reloadLowering);
            var desiredRotation = _baseRotation * Quaternion.Euler(arc * 22f, 0f, arc * -13f);
            var blend = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            rigRoot.localPosition = Vector3.Lerp(rigRoot.localPosition, desiredPosition, blend);
            rigRoot.localRotation = Quaternion.Slerp(rigRoot.localRotation, desiredRotation, blend);
        }

        private void CapturePose()
        {
            if (rigRoot == null) return;
            _basePosition = rigRoot.localPosition;
            _baseRotation = rigRoot.localRotation;
        }

        private void Subscribe()
        {
            if (weapon != null) weapon.ShotResolved += OnShotResolved;
        }

        private void Unsubscribe()
        {
            if (weapon != null) weapon.ShotResolved -= OnShotResolved;
        }

        private void OnShotResolved(ShotResult result)
        {
            if (result.Accepted && animator != null) animator.SetTrigger(FireHash);
        }
    }
}
