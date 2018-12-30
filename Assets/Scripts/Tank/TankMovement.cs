using UnityEngine;

public class TankMovement : MonoBehaviour
{
    public int m_PlayerNumber = 1;         
    public float m_Speed = 12f;            
    public float m_TurnSpeed = 180f;       
    public AudioSource m_MovementAudio;    
    public AudioClip m_EngineIdling;       
    public AudioClip m_EngineDriving;      
    public float m_PitchRange = 0.2f;

    private string m_MovementAxisName;     
    private string m_TurnAxisName;         
    private Rigidbody m_Rigidbody;         
    private float m_MovementInputValue;    
    private float m_TurnInputValue;        
    private float m_OriginalPitch;         

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }


    private void OnEnable ()
    {
        m_Rigidbody.isKinematic = false;
        m_MovementInputValue = 0f;
        m_TurnInputValue = 0f;
    }


    private void OnDisable ()
    {
        m_Rigidbody.isKinematic = true;

		m_MovementAudio.Stop ();
    }


    private void Start()
    {
        m_MovementAxisName = "Vertical" + m_PlayerNumber;
        m_TurnAxisName = "Horizontal" + m_PlayerNumber;

        m_OriginalPitch = m_MovementAudio.pitch;
    }

    private void Update()
    {
        // Store the player's input and make sure the audio for the engine is playing.
		m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
		m_TurnInputValue = Input.GetAxis(m_TurnAxisName);

		EngineAudio();
    }


    private void EngineAudio()
    {
        // Play the correct audio clip based on whether or not the tank is moving and what audio is currently playing.
		float MaxInputValue = Mathf.Max (Mathf.Abs(m_MovementInputValue), Mathf.Abs(m_TurnInputValue));

		AudioClip newAudioClip = (MaxInputValue > 0) ? m_EngineDriving : m_EngineIdling;

		if (newAudioClip != m_MovementAudio.clip) {
			m_MovementAudio.clip = newAudioClip;

			m_MovementAudio.Play ();
		}

		if (m_MovementInputValue > 0 | m_TurnInputValue > 0) {
			m_MovementAudio.pitch = m_OriginalPitch + m_PitchRange * MaxInputValue;
		} else {
			m_MovementAudio.pitch = m_OriginalPitch;
		}
    }


    private void FixedUpdate()
    {
        // Move and turn the tank.
		Move();
		Turn();
    }


    private void Move()
    {
        // Adjust the position of the tank based on the player's input.
		Vector3 deltaPosition = m_Rigidbody.transform.forward * m_Speed * Time.fixedDeltaTime * m_MovementInputValue;

		m_Rigidbody.MovePosition(m_Rigidbody.position + deltaPosition);
    }


    private void Turn()
    {
        // Adjust the rotation of the tank based on the player's input.
		Quaternion deltaRotation = Quaternion.Euler(0, m_TurnSpeed * Time.fixedDeltaTime * m_TurnInputValue, 0);

		m_Rigidbody.MoveRotation(m_Rigidbody.rotation * deltaRotation);
    }
}