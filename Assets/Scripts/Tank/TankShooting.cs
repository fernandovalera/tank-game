using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;

public class TankShooting : NetworkBehaviour
{
    public Rigidbody m_Shell;            
    public Transform m_FireTransform;    
    public Slider m_AimSlider;           
    public AudioSource m_ShootingAudio;  
    public AudioClip m_ChargingClip;     
    public AudioClip m_FireClip;         
    public float m_MinLaunchForce = 15f; 
    public float m_MaxLaunchForce = 30f; 
    public float m_MaxChargeTime = 0.75f;
	public float m_ReloadTime = 0.5f;

    private string m_FireButton;         
    private float m_CurrentLaunchForce;  
    private float m_ChargeSpeed;         
    private bool m_Fired;                
	private bool m_Reloading;

    private void OnEnable()
    {
        m_CurrentLaunchForce = m_MinLaunchForce;
        m_AimSlider.value = m_MinLaunchForce;
		m_Reloading = false;
    }


    private void Start()
    {
        m_FireButton = "Fire";

        m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;
    }

    [ServerCallback]
    private void Update()
    {
        // Track the current state of the fire button and make decisions based on the current launch force.
		bool isCharging = Input.GetButton(m_FireButton);

		if (isCharging && m_CurrentLaunchForce < m_MaxLaunchForce && !m_Fired) {
			m_ShootingAudio.clip = m_ChargingClip;
			if (!m_ShootingAudio.isPlaying) {
				m_ShootingAudio.Play ();
			}

			m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
			m_AimSlider.value = m_CurrentLaunchForce;
		} else if (m_CurrentLaunchForce > m_MinLaunchForce) {
			Fire ();

			m_Fired = true;

			m_CurrentLaunchForce = m_MinLaunchForce;
			m_AimSlider.value = m_MinLaunchForce;

			m_Reloading = true;

			StartCoroutine (Reload ());

		} else if (!isCharging && !m_Reloading) {
			m_Fired = false;
		}
    }


    private void Fire()
    {
        // Instantiate and launch the shell.
		Rigidbody shell = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation);
        NetworkServer.Spawn(shell.gameObject);

		shell.AddRelativeForce (Vector3.forward * m_CurrentLaunchForce, ForceMode.Impulse);

		Rigidbody tank = GetComponent<Rigidbody> ();

		tank.AddRelativeForce (Vector3.forward * -m_CurrentLaunchForce, ForceMode.Impulse);

		m_ShootingAudio.clip = m_FireClip;
		m_ShootingAudio.Play ();
    }

	private IEnumerator Reload() {

		yield return new WaitForSeconds(m_ReloadTime);

		m_Reloading = false;
	}
}