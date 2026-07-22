using SpieleMarmelade.Shared.VFX;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // Generic chase-and-hit enemy for Brickrot. Drop it on ANY brick figure and tune the serialized
    // values per prefab — a fast weak swarmer and a slow tanky brute are the same component with
    // different numbers (scale the transform for size).
    //
    // Built on CharacterController to match the project's own actors (SlimeEnemyAI, the shared
    // top-down player) rather than a Rigidbody: no physics tumbling, no Rigidbody-vs-CharacterController
    // conflict, correct at brick scale. Contact with the player is a distance check, not a trigger,
    // for the same reason.
    //
    // Brickrot wiring: there is no player health. Hitting the player raises SurvivorEvents.OnTakeDamage,
    // which drops a black brick into the Tetris field — that field filling up is the actual lose
    // condition. The enemy itself IS killable: attacks call TakeDamage via IDamageable.
    [RequireComponent(typeof(CharacterController))]
    public class EnemyAI : MonoBehaviour, IDamageable
    {
        [Header("Movement")]
        [Tooltip("Weltunits pro Sekunde (Brick-Maßstab: der Spieler läuft ~0.4).")]
        [SerializeField] private float moveSpeed = 0.3f;

        [Tooltip("Wie schnell sich der Gegner in Laufrichtung dreht. Höher = snappiger.")]
        [SerializeField] private float rotationSpeed = 12f;

        [SerializeField] private float gravity = -2f;

        [Header("Health")]
        [Tooltip("Wie viel Schaden der Gegner einsteckt, bevor er zerspringt.")]
        [SerializeField] private float maxHealth = 30f;

        [Header("Contact (Spieler treffen)")]
        [Tooltip("Ab dieser Distanz zum Spieler gilt der Gegner als 'dran'.")]
        [SerializeField] private float contactRange = 0.08f;

        [Tooltip("Wie viele schwarze Steine ein Treffer im Tetris auslöst.")]
        [Min(1)] [SerializeField] private int contactDamage = 1;

        [Tooltip("An: Kamikaze — der Gegner trifft einmal und zerspringt. Aus: er bleibt und trifft " +
                 "im Cooldown-Takt weiter.")]
        [SerializeField] private bool destroyOnContact = true;

        [Tooltip("Nur wenn 'destroyOnContact' aus ist: Sekunden zwischen zwei Treffern.")]
        [SerializeField] private float contactCooldown = 1f;

        [Header("Events")]
        [Tooltip("Muss dasselbe SurvivorEvents-Asset sein wie beim Minigame-Controller und der " +
                 "TetrisGameConfig — sonst kommt der Schaden nie im Tetris an.")]
        [SerializeField] private SurvivorEvents survivorEvents;

        [Header("Juice")]
        [Tooltip("Kamera-Ruckler beim Sterben. Klein halten — es sterben viele Gegner.")]
        [SerializeField] private float deathShake = 0.16f;

        private CharacterController _cc;
        private HitFeedback _hitFeedback;
        private BrickShatterEffect _shatter;
        private Transform _player;

        private float _health;
        private float _verticalVelocity;
        private float _nextContactTime;
        private bool _dead;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _health = maxHealth;

            // Added at runtime so a bare brick figure feels right with zero wiring — drop the
            // components on the prefab yourself if you want to tune them per enemy type.
            _hitFeedback = GetComponent<HitFeedback>();
            if (_hitFeedback == null) _hitFeedback = gameObject.AddComponent<HitFeedback>();

            _shatter = GetComponent<BrickShatterEffect>();
            if (_shatter == null) _shatter = gameObject.AddComponent<BrickShatterEffect>();
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _player = player.transform;
        }

        private void Update()
        {
            if (_dead || _player == null) return;

            // Chase on the ground plane only — the player's height must never steer the enemy.
            Vector3 toPlayer = _player.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;

            Vector3 move = Vector3.zero;
            if (distance > contactRange && distance > 0.0001f)
            {
                Vector3 dir = toPlayer / distance;
                move = dir * moveSpeed;

                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationSpeed * Time.deltaTime);
            }

            // Gravity so the CharacterController stays pinned to the floor (same pattern as the
            // shared SlimeEnemyAI). Reset to a small downward bias while grounded.
            if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;
            move.y = _verticalVelocity;

            _cc.Move(move * Time.deltaTime);

            if (distance <= contactRange && Time.time >= _nextContactTime)
            {
                HitPlayer();
            }
        }

        // Reaching the player drops black brick(s) into the Tetris field. Kamikaze by default:
        // one enemy, one hit, then it shatters — so "one enemy through your defence = one brick".
        private void HitPlayer()
        {
            if (survivorEvents != null) survivorEvents.InvokeTakeDamage(contactDamage);

            if (destroyOnContact)
            {
                Die();
            }
            else
            {
                _nextContactTime = Time.time + contactCooldown;
            }
        }

        public void TakeDamage(float amount)
        {
            if (_dead) return;

            _health -= amount;

            if (_health <= 0f)
            {
                Die();
                return;
            }

            // Only flash on survivable hits — a dying enemy shatters instead, and doing both at
            // once reads as a stutter.
            if (_hitFeedback != null) _hitFeedback.Play();
        }

        private void Die()
        {
            if (_dead) return;
            _dead = true;

            // Bursts into bricks in the enemy's own colour (BrickShatterEffect samples its
            // renderer), which is the whole reason enemies are built out of bricks.
            if (_shatter != null) _shatter.Shatter();
            CameraShake.Add(deathShake);

            Destroy(gameObject);
        }
    }
}
