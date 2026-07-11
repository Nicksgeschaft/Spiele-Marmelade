using SpieleMarmelade.Shared.Combat;
using UnityEngine;

namespace SpieleMarmelade.Shared.World
{
    // Drop on a BrickBar prefab instance and assign Health — no other wiring needed. Refreshes
    // the bar whenever the watched Health takes damage or dies.
    [RequireComponent(typeof(BrickBar))]
    public class BrickHealthBarView : MonoBehaviour
    {
        [SerializeField] private Health health;

        private BrickBar _bar;

        private void Awake() => _bar = GetComponent<BrickBar>();

        private void OnEnable()
        {
            if (health == null) return;
            health.OnDamaged.AddListener(Refresh);
            health.OnDeath.AddListener(Refresh);
            Refresh();
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.OnDamaged.RemoveListener(Refresh);
            health.OnDeath.RemoveListener(Refresh);
        }

        private void Refresh()
        {
            if (health == null) return;
            _bar.SetValue01(health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 0f);
        }
    }
}
