using UnityEngine;
using Web3Fps.GameFoundation.Gameplay.Combat;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Formal
{
    [DisallowMultipleComponent]
    public sealed class FormalCharacterAnimator : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int FireHash = Animator.StringToHash("Fire");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController controller;
        [SerializeField] private Health health;
        [SerializeField] private PrototypeBotController bot;
        [SerializeField, Min(0.1f)] private float fullSpeed = 5f;

        public void Configure(
            Animator targetAnimator,
            CharacterController characterController,
            Health characterHealth,
            PrototypeBotController botController)
        {
            Unsubscribe();
            animator = targetAnimator;
            controller = characterController;
            health = characterHealth;
            bot = botController;
            if (animator != null) animator.applyRootMotion = false;
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (animator == null) return;
            var horizontalSpeed = controller == null
                ? 0f
                : Vector3.ProjectOnPlane(controller.velocity, Vector3.up).magnitude;
            animator.SetFloat(SpeedHash, Mathf.Clamp01(horizontalSpeed / fullSpeed), 0.12f, Time.deltaTime);
            animator.SetBool(DeadHash, health != null && health.IsDead);
        }

        private void Subscribe()
        {
            if (bot != null) bot.ShotResolved += OnShotResolved;
        }

        private void Unsubscribe()
        {
            if (bot != null) bot.ShotResolved -= OnShotResolved;
        }

        private void OnShotResolved(Vector3 origin, Vector3 end, bool hit)
        {
            if (animator != null) animator.SetTrigger(FireHash);
        }
    }
}
