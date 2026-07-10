using GameJamUniverse.Shared.Combat;
using UnityEngine;

namespace GameJamUniverse.Shared.Enemies
{
    // Simple chase-and-bump enemy: hops toward whatever is tagged "Player", deals contact
    // damage on a cooldown when close enough. No NavMesh — fine for one small hand-built room;
    // would need real pathfinding for anything with obstacles to path around.
    [RequireComponent(typeof(CharacterController))]
    public class SlimeEnemyAI : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 0.3f;
        [SerializeField] private float detectRange = 1.5f;
        [SerializeField] private float hopHeight = 0.05f;
        [SerializeField] private float hopsPerSecond = 2f;
        [SerializeField] private float gravity = -2f;

        [Header("Contact Damage")]
        [SerializeField] private float contactDamage = 10f;
        [SerializeField] private float contactRange = 0.08f;
        [SerializeField] private float contactCooldown = 1f;

        private CharacterController _cc;
        private Transform _player;
        private float _verticalVelocity;
        private float _nextHopTime;
        private float _nextContactTime;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null) _player = playerGo.transform;
        }

        private void Update()
        {
            if (_player == null) return;

            var toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            var move = Vector3.zero;
            if (distance > contactRange && distance <= detectRange)
            {
                var dir = toPlayer.normalized;
                move = dir * moveSpeed;
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            if (_cc.isGrounded)
            {
                _verticalVelocity = -2f;
                if (Time.time >= _nextHopTime)
                {
                    _verticalVelocity = Mathf.Sqrt(hopHeight * -2f * gravity);
                    _nextHopTime = Time.time + 1f / Mathf.Max(0.01f, hopsPerSecond);
                }
            }
            _verticalVelocity += gravity * Time.deltaTime;
            move.y = _verticalVelocity;

            _cc.Move(move * Time.deltaTime);

            if (distance <= contactRange && Time.time >= _nextContactTime)
            {
                var health = _player.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(contactDamage);
                    _nextContactTime = Time.time + contactCooldown;
                }
            }
        }
    }
}
