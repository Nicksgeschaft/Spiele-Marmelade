using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
	// Simple straight-flying projectile. Ported from 2D to 3D physics; travel direction is the
	// transform's forward, which on the ground plane is set by the spawner's yaw.
	[RequireComponent(typeof(Rigidbody))]
	public class Projectile : MonoBehaviour
	{
		[SerializeField] private float speed = 12f;
		[SerializeField] private float lifeTime = 3f;
		[SerializeField] private bool destroyOnHit = true;

		private Rigidbody rb;

		private void Awake()
		{
			rb = GetComponent<Rigidbody>();
		}

		private void OnEnable()
		{
			// Auto cleanup
			Destroy(gameObject, lifeTime);
		}

		private void FixedUpdate()
		{
			rb.linearVelocity = transform.forward * speed;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.CompareTag("Enemy"))
			{
				Destroy(other.gameObject);

				if (destroyOnHit)
					Destroy(gameObject);
			}
		}
	}
}
