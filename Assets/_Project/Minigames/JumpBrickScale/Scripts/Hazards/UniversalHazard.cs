using System;
using UnityEngine;

public class UniversalHazard : MonoBehaviour
{
    public event Action<GameObject> OnLavaCollision;

    [Header("Explosion Settings")]
    public GameObject explosionPrefab;

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