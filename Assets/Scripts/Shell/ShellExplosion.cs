using UnityEngine;
using UnityEngine.Networking;

public class ShellExplosion : NetworkBehaviour
{
    public LayerMask m_TankMask;
    public ParticleSystem m_ExplosionParticles;       
    public AudioSource m_ExplosionAudio;              
    public float m_MaxDamage = 100f;                  
    public float m_ExplosionForce = 1000f;            
    public float m_MaxLifeTime = 2f;                  
    public float m_ExplosionRadius = 5f;              

	private bool m_Exploded;

    private void Start()
    {
		m_Exploded = false;

        Destroy(gameObject, m_MaxLifeTime);
    }


    private void OnTriggerEnter(Collider other)
    {
        // Find all the tanks in an area around the shell and damage them.

		if (m_Exploded) {
			return;
		}

		Vector3 triggerPosition = transform.position;
		Collider[] colliders = Physics.OverlapSphere(triggerPosition, m_ExplosionRadius, m_TankMask.value);

		foreach (Collider collider in colliders) {
			float damage = CalculateDamage (collider.transform.position);

			TankHealth hitTankHealth = collider.gameObject.GetComponent<TankHealth> ();
			hitTankHealth.TakeDamage (damage);

			Rigidbody hitRigidbody = collider.gameObject.GetComponent<Rigidbody> ();
			hitRigidbody.AddExplosionForce (m_ExplosionForce, triggerPosition, m_ExplosionRadius);
		}

		m_ExplosionParticles.Play ();
		m_ExplosionAudio.Play ();

		MeshRenderer meshRenderer = GetComponent<MeshRenderer> ();
		meshRenderer.enabled = false;

		Rigidbody rigidbody = GetComponent<Rigidbody> ();
		rigidbody.isKinematic = true;

		Destroy (gameObject, 1f);
    }


    private float CalculateDamage(Vector3 targetPosition)
    {
        // Calculate the amount of damage a target should take based on it's position.
		return m_MaxDamage * (1 - Mathf.Min(1, ((transform.position - targetPosition).magnitude / m_ExplosionRadius)));
    }
}