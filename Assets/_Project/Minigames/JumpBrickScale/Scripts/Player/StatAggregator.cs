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
            // Ability powers first - the resolved values below are derived from them.
            float airJumpPower = AggregateRaw(PlayerStatType.AirJumpPower);
            float dashPower = AggregateRaw(PlayerStatType.DashPower);
            float wallJumpPower = AggregateRaw(PlayerStatType.WallJumpPower);
            float jumpHeight = Aggregate(PlayerStatType.JumpHeight, baseStats.jumpHeight);
            float moveSpeed = Aggregate(PlayerStatType.MoveSpeed, baseStats.moveSpeed);

            _current = new PlayerRuntimeStats
            {
                AirJumpPower = airJumpPower,
                DashPower = dashPower,
                WallJumpPower = wallJumpPower,

                // Each further brick of a kind makes its ability stronger, so one pickup is a small
                // boost and stacking them is what turns it into a real tool.
                AirJumpHeight = airJumpPower >= 1f
                    ? jumpHeight * (baseStats.airJumpHeightFactor + baseStats.airJumpFactorPerPower * (airJumpPower - 1f))
                    : 0f,
                DashSpeed = dashPower >= 1f
                    ? baseStats.dashSpeed + baseStats.dashSpeedPerPower * (dashPower - 1f)
                    : 0f,
                DashDuration = baseStats.dashDuration,
                DashCooldown = baseStats.dashCooldown,
                DoubleTapWindow = baseStats.doubleTapWindow,
                WallJumpHeight = wallJumpPower >= 1f
                    ? jumpHeight * (baseStats.wallJumpHeightFactor + baseStats.wallJumpFactorPerPower * (wallJumpPower - 1f))
                    : 0f,
                WallJumpPush = moveSpeed * baseStats.wallJumpPushFactor,
                WallCheckDistance = baseStats.wallCheckDistance,

                MoveSpeed = moveSpeed,
                GroundAcceleration = Aggregate(PlayerStatType.GroundAcceleration,
                    new PlayerMovementStats.ClampedStat
                    {
                        baseValue = baseStats.groundAcceleration,
                        minValue = 1f,
                        maxValue = 500f,
                    }),
                GroundDeceleration = baseStats.groundDeceleration,
                AirControl = baseStats.airControl,
                JumpHeight = jumpHeight,
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

        // Abilities have no base value in PlayerMovementStats - they only exist because bricks grant
        // them - so this is the plain sum of what's attached, with no clamp.
        private float AggregateRaw(PlayerStatType stat)
        {
            float total = 0f;
            foreach (BrickNode brick in _assembly.ConnectedBricks)
            {
                if (brick.Definition == null) continue;
                foreach (BrickDefinition.BrickStatModifier modifier in brick.Definition.statModifiers)
                {
                    if (modifier.stat == stat) total += modifier.value;
                }
            }
            return total;
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
