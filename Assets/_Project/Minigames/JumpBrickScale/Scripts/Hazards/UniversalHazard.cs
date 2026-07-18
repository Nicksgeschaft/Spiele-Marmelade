using System;
using UnityEngine;

public class LavaHazard : MonoBehaviour
{
    // das event musst gecallt werden + Player braucht den tag Player

    public event Action<GameObject> OnLavaCollision;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            OnLavaCollision?.Invoke(other.gameObject);
        }
    }
}