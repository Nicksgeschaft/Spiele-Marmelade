using SpieleMarmelade.Shared;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // Fires the three Survivor abilities from player input. Movement, facing and the camera are
    // NOT handled here — the shared Player_TopDownFree prefab already carries the project's
    // TopDownFreeMovement (CharacterController-based) for that. The original AssetJam port bundled
    // movement into its own Rigidbody controller, which fought that prefab and made the player roll
    // and drift; this component does one thing instead.
    //
    // These three Fire() calls are the "play it by hand" path for testing the Survivor half on its
    // own. In the full game the same abilities are driven by the Tetris half through
    // TetrisToSurvivorBridge — one brick spawn, one ability.
    //
    // Abilities sit on the existing shared actions: Ability1 = Welle, Ability2 = Sichel,
    // AbilitySpecial = Feuerschuss.
    public class SurvivorAbilities : MonoBehaviour
    {
        [Header("Attacks (leer = automatisch von diesem GameObject)")]
        [SerializeField] private WaveAuraAttack waveAura;
        [SerializeField] private NatureScytheAttack scytheAttack;
        [SerializeField] private FireShotAttack fireShotAttack;

        private PlayerInputReader _input;

        private void Awake()
        {
            // Auto-grab so dropping this on the player next to the attack components just works.
            if (waveAura == null) waveAura = GetComponent<WaveAuraAttack>();
            if (scytheAttack == null) scytheAttack = GetComponent<NatureScytheAttack>();
            if (fireShotAttack == null) fireShotAttack = GetComponent<FireShotAttack>();

            _input = GetComponent<PlayerInputReader>();
            if (_input == null) _input = gameObject.AddComponent<PlayerInputReader>();
        }

        private void OnEnable()
        {
            _input.Ability1Performed       += FireWave;
            _input.Ability2Performed       += FireScythe;
            _input.AbilitySpecialPerformed += FireShot;
        }

        private void OnDisable()
        {
            _input.Ability1Performed       -= FireWave;
            _input.Ability2Performed       -= FireScythe;
            _input.AbilitySpecialPerformed -= FireShot;
        }

        private void FireWave()
        {
            if (waveAura != null) waveAura.Fire();
        }

        private void FireScythe()
        {
            if (scytheAttack != null) scytheAttack.Fire();
        }

        private void FireShot()
        {
            if (fireShotAttack != null) fireShotAttack.Fire();
        }
    }
}
