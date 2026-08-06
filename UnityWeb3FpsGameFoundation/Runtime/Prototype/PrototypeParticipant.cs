using UnityEngine;
using Web3Fps.GameFoundation.Gameplay.Combat;

namespace Web3Fps.GameFoundation.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class PrototypeParticipant : MonoBehaviour
    {
        [SerializeField] private string participantId = "participant";
        [SerializeField] private string displayName = "Participant";
        [SerializeField] private string teamId = "team";
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private MonoBehaviour[] controlledBehaviours = new MonoBehaviour[0];

        private Health _health;
        private CharacterController _characterController;
        private Vector3 _fallbackSpawnPosition;
        private Quaternion _fallbackSpawnRotation;

        public string ParticipantId => participantId;
        public string DisplayName => displayName;
        public string TeamId => teamId;
        public Health Health => _health;
        public Vector3 AimPoint => transform.position + Vector3.up * 1.35f;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _characterController = GetComponent<CharacterController>();
            _fallbackSpawnPosition = transform.position;
            _fallbackSpawnRotation = transform.rotation;
        }

        public void Configure(
            string id,
            string label,
            string team,
            Transform spawn,
            MonoBehaviour[] controllers)
        {
            participantId = string.IsNullOrWhiteSpace(id) ? "participant" : id;
            displayName = string.IsNullOrWhiteSpace(label) ? participantId : label;
            teamId = string.IsNullOrWhiteSpace(team) ? "team" : team;
            spawnPoint = spawn;
            controlledBehaviours = controllers ?? new MonoBehaviour[0];
            _health = GetComponent<Health>();
            _characterController = GetComponent<CharacterController>();
        }

        public void SetControlEnabled(bool value)
        {
            for (var i = 0; i < controlledBehaviours.Length; i++)
            {
                if (controlledBehaviours[i] != null) controlledBehaviours[i].enabled = value;
            }
        }

        public void Respawn()
        {
            var controllerWasEnabled = _characterController != null && _characterController.enabled;
            if (_characterController != null) _characterController.enabled = false;
            transform.SetPositionAndRotation(
                spawnPoint != null ? spawnPoint.position : _fallbackSpawnPosition,
                spawnPoint != null ? spawnPoint.rotation : _fallbackSpawnRotation);
            if (_characterController != null) _characterController.enabled = controllerWasEnabled;
            _health.RestoreToFull();
            SetControlEnabled(true);
        }
    }
}
