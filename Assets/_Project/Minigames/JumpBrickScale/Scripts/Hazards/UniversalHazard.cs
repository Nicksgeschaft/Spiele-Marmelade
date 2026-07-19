using System;
using UnityEngine;

public class UniversalHazard : MonoBehaviour
{
    public event Action<GameObject> OnLavaCollision;

    [Header("Explosion Settings")]
    public GameObject explosionPrefab;

    [Header("Audio")]
    [Tooltip("Id aus der AudioLibrary (sfx-Liste), z.B. 'hazard_lava' oder 'hazard_mine'. " +
             "Wird von PlayerHazardResponder abgespielt - und zwar nur wenn wirklich ein Brick " +
             "verloren geht, damit die 5 nebeneinanderliegenden Lava-Segmente nicht 5x tönen.")]
    public string hitSfxId;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnLavaCollision?.Invoke(other.gameObject);

            if (gameObject.CompareTag("Mine"))
            {
                if (explosionPrefab != null)
                {
                    Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                }

                Destroy(gameObject);
            }
        }
    }
}