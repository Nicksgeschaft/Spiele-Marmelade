using System;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Rebuilds PlayerRuntimeStats from PlayerMovementStats plus every connected brick's
    // BrickDefinition.statModifiers whenever the assembly changes. Everything else
    // (PlayerMovementController, PlayerGroundSensor, ...) reads Current instead of touching
    // PlayerMovementStats or PlayerAssembly's bricks directly. See Docs section 3.2 and 9.
    //
    // Current lazily rebuilds on first access rather than in Awake(), since it depends on
    // PlayerAssembly already having registered the Main-Brick - Unity doesn't guarantee Awake()
    // order between sibling components. Only ever read Current from Update/FixedUpdate/LateUpdate,
    // never from another component's own Awake().
    [RequireComponent(typeof(PlayerAssembly))]
    public class StatAggregator : MonoBehaviour
    {
        [SerializeField] private PlayerMovementStats baseStats;

        private PlayerAssembly _assembly;
        private PlayerRuntimeStats _current;

        public PlayerRuntimeStats Current
        {
            get
            {
                if (_current == null)
                {
                    Rebuild();
                }
                return _current;
            }
        }

        public event Action<PlayerRuntimeStats> OnRuntimeStatsChanged;

        private void Awake()
        {
            _assembly = GetComponent<PlayerAssembly>();
        }

        private void OnEnable()
        {
            _assembly.OnAssemblyChanged += Rebuild;
        }

        private void OnDisable()
        {
            _assembly.OnAssemblyChanged -= Rebuild;
        }

        private void Rebuild()
        {
            _current = new PlayerRuntimeStats
            {
                MoveSpeed = Aggregate(PlayerStatType.MoveSpeed, baseStats.moveSpeed),
                GroundAcceleration = baseStats.groundAcceleration,
                GroundDeceleration = baseStats.groundDeceleration,
                AirControl = baseStats.airControl,
                JumpHeight = Aggregate(PlayerStatType.JumpHeight, baseStats.jumpHeight),
                GravityMagnitude = Aggregate(PlayerStatType.GravityMagnitude, baseStats.gravityMagnitude),
                FallGravityMultiplier = baseStats.fallGravityMultiplier,
                LowJumpGravityMultiplier = baseStats.lowJumpGravityMultiplier,
                MaxFallSpeed = baseStats.maxFallSpeed,
                // Weight is never a StatModifier - the physical brick.weight already drives this
                // through PlayerAssembly.TotalMass (Docs section 3.2).
                TotalWeight = _assembly.TotalMass > 0f ? _assembly.TotalMass : baseStats.baseWeight.baseValue,
                MaxAngularVelocity = baseStats.maxAngularVelocity,
                AngularDrag = baseStats.angularDrag,
                CoyoteTime = baseStats.coyoteTime,
                JumpBufferTime = baseStats.jumpBufferTime,
                GroundNormalThreshold = baseStats.groundNormalThreshold,
                AttachCooldown = baseStats.attachCooldown,
            };

            OnRuntimeStatsChanged?.Invoke(_current);
        }

        private float Aggregate(PlayerStatType stat, PlayerMovementStats.ClampedStat clamped)
        {
            float additiveSum = 0f;
            float multiplicativeProduct = 1f;

            foreach (BrickNode brick in _assembly.ConnectedBricks)
            {
                BrickDefinition definition = brick.Definition;
                if (definition == null)
                {
                    continue;
                }

                foreach (BrickDefinition.BrickStatModifier modifier in definition.statModifiers)
                {
                    if (modifier.stat != stat)
                    {
                        continue;
                    }

                    if (modifier.mode == StatModifierMode.Additive)
                    {
                        additiveSum += modifier.value;
                    }
                    else
                    {
                        multiplicativeProduct *= modifier.value;
                    }
                }
            }

            return clamped.Clamp((clamped.baseValue + additiveSum) * multiplicativeProduct);
        }
    }
}
