﻿using UnityEngine;
using UnityEngine.UI;

public class TankHealth : MonoBehaviour
{
    public float m_StartingHealth = 100f;          
    public Slider m_Slider;                        
    public Image m_FillImage;                      
    public Color m_FullHealthColor = Color.green;  
    public Color m_ZeroHealthColor = Color.red;    
    public GameObject m_ExplosionPrefab;
    
    private AudioSource m_ExplosionAudio;          
    private ParticleSystem m_ExplosionParticles;   
    private float m_CurrentHealth;  
    private bool m_Dead;            


    private void Awake()
    {
        m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
        m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();

        m_ExplosionParticles.gameObject.SetActive(false);
    }


    private void OnEnable()
    {
        m_CurrentHealth = m_StartingHealth;
        m_Dead = false;

        SetHealthUI();
    }

    public void TakeDamage(float amount)
    {
        // Adjust the tank's current health, update the UI based on the new health and check whether or not the tank is dead.
		m_CurrentHealth = Mathf.Max(0, m_CurrentHealth - amount);

		m_Slider.value = m_CurrentHealth;

		if (m_CurrentHealth == 0 && !m_Dead) {
			m_Dead = true;

			OnDeath ();
		}
    }


    private void SetHealthUI()
    {
        // Adjust the value and colour of the slider.
		Image backgroundImage = m_Slider.GetComponentInChildren<Image>();

		backgroundImage.color = m_ZeroHealthColor;

		m_FillImage.color = m_FullHealthColor;

		m_Slider.value = m_CurrentHealth;
    }


    private void OnDeath()
    {
        // Play the effects for the death of the tank and deactivate it.
		m_ExplosionParticles.gameObject.SetActive(true);
		m_ExplosionParticles.transform.position = transform.position;

		m_ExplosionParticles.Play ();
		m_ExplosionAudio.Play ();

		gameObject.SetActive (false);

//		TankMovement tankMovement = gameObject.GetComponent<TankMovement>();
//		tankMovement.enabled = false;
//
//		TankShooting tankShooting = gameObject.GetComponent<TankShooting>();
//		tankShooting.enabled = false;
    }
}