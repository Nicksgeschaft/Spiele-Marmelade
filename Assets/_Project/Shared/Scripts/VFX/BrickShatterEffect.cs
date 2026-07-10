using GameJamUniverse.World;
using UnityEngine;

namespace GameJamUniverse.Shared.VFX
{
    // Runtime "shatters into bricks" effect. Call Shatter() (e.g. wired to Health.OnDeath in
    // the Inspector) to spawn small brick-fragment cubes that fly outward and self-destruct.
    // Fragment color is sampled from this object's own renderer, so a red player and a green
    // slime each shatter in their own color with zero extra configuration.
    public class BrickShatterEffect : MonoBehaviour
    {
        private const string BaseColorProperty = "_BaseColor";

        [SerializeField] private int fragmentCount = 8;
        [SerializeField] private float fragmentSize = WorldConstants.PlateWidth;
        [SerializeField] private Vector2 forceRange = new(1f, 3f);
        [SerializeField] private Vector2 torqueRange = new(1f, 4f);
        [SerializeField] private float lifetime = 2f;

        public void Shatter()
        {
            Color color = SampleColor(GetComponentInChildren<MeshRenderer>());
            Vector3 origin = transform.position;

            for (int i = 0; i < fragmentCount; i++)
            {
                GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fragment.transform.position = origin + Random.insideUnitSphere * fragmentSize;
                fragment.transform.localScale = Vector3.one * fragmentSize;

                SetColor(fragment.GetComponent<MeshRenderer>(), color);

                Rigidbody rb = fragment.AddComponent<Rigidbody>();
                Vector3 direction = Random.onUnitSphere;
                direction.y = Mathf.Abs(direction.y);
                rb.AddForce(direction * Random.Range(forceRange.x, forceRange.y), ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * Random.Range(torqueRange.x, torqueRange.y), ForceMode.Impulse);

                Destroy(fragment, lifetime);
            }
        }

        private static Color SampleColor(MeshRenderer renderer)
        {
            if (renderer == null || renderer.sharedMaterial == null) return Color.white;
            Material mat = renderer.sharedMaterial;
            return mat.HasProperty(BaseColorProperty) ? mat.GetColor(BaseColorProperty) : mat.color;
        }

        private static void SetColor(MeshRenderer renderer, Color color)
        {
            if (renderer == null) return;
            Material mat = renderer.material; // instance copy, safe to mutate
            if (mat.HasProperty(BaseColorProperty)) mat.SetColor(BaseColorProperty, color);
            else mat.color = color;
        }
    }
}
