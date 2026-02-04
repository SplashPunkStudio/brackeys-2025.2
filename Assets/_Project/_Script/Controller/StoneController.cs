using UnityEngine;
using TMPro; // Using TextMeshPro
using System.Collections.Generic; // Necessário para List
using Unity.Cinemachine; // Para CinemachineImpulseSource

public class StoneController : MonoBehaviour
{
    // Public field (follows camelCase convention for public variables)
    public GameManager gameManager; // Reference to the GameManager (assigned by it)
    public GameManager.TeamType teamType; // The team this stone belongs to

    // --- Editor-exposed Configurations (Serialized Private Fields) ---
    [Header("Launch Settings")]
    [SerializeField] private float _maxLaunchForce = 15f; // Maximum base launch force for the stone
    [SerializeField] private float _launchForceMultiplier = 1.0f; // Multiplier for the final launch force
    [SerializeField] private float _maxAimAngle = 45f; // Max angle deviation from forward (in degrees, e.g., 45 for -45 to +45)

    [Header("Collision Settings (Arcade)")]
    [SerializeField] private float _rigidbodyMass = 1f; // Mass of the stone's Rigidbody. Influences general physics.
    [SerializeField] private float _collisionForceMultiplier = 5f; // Multiplier for force applied to *hit* stone, based on launched stone's velocity
    [SerializeField] private float _retainedVelocityRatio = 0.8f; // % of velocity retained by the *launched* stone after collision (0.8 = 80%)
    [SerializeField] private float _minImpactVelocityForParticles = 0.1f; // Velocidade mínima para emitir partículas de impacto (nesta pedra)

    [Header("Visuals")]
    [SerializeField] private LineRenderer _aimLineRenderer; // Line Renderer component to draw the aim line
    [SerializeField] private ParticleSystem _impactParticlesPrefab; // Prefab for collision impact particles

    // --- Animação ---
    [Header("Animation")]
    [SerializeField] private Animator _animator; // Componente Animator para as animações

    // --- Sons ---
    [Header("Sounds")]
    [SerializeField] private SO_Sound _slideSound; // Som para o deslize da pedra
    [SerializeField] private SO_Sound _impactSound; // Som para o impacto da pedra
    private bool m_isSlidingSoundPlaying = false; // Flag para controlar se o som de deslize está tocando

    // --- Screen Shake ---
    [Header("Impulse")]
    [SerializeField] private CinemachineImpulseSource _impulseSource; // NOVO: Referência para o CinemachineImpulseSource


    // --- Private Game Variables ---
    private Rigidbody2D m_rigidbody; // Rigidbody2D component of the stone
    private Vector2 m_dragStartPos; // Mouse position when the click started
    private bool m_isDragging = false; // Indicates if the mouse is being dragged for aiming
    private CircleCollider2D m_circleCollider; // Referência ao CircleCollider2D da pedra

    // --- Variáveis para varredura dinâmica ---
    private PhysicsMaterial2D m_originalPhysicsMaterial;
    private PhysicsMaterial2D m_dynamicPhysicsMaterial; // Material que será modificado em tempo de execução


    // --- Lista de poderes ativos nesta pedra ---
    private List<PowerUpType> activePowers = new List<PowerUpType>();

    void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody2D>();
        m_circleCollider = GetComponent<CircleCollider2D>(); // Obtenha o CircleCollider2D

        m_rigidbody.gravityScale = 0; // Ensures no gravity for a top-down game
        m_rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation; // Freeze Z-rotation

        // Apply the configured mass to the Rigidbody
        m_rigidbody.mass = _rigidbodyMass;

        // Tenta obter o Animator, se não for atribuído via Inspector
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
        // NOVO: Tenta obter o CinemachineImpulseSource
        if (_impulseSource == null)
        {
            _impulseSource = GetComponent<CinemachineImpulseSource>();
            if (_impulseSource == null) {
                Debug.LogWarning("No CinemachineImpulseSource found on " + gameObject.name + ". Screen shake will not work.");
            }
        }


        // Armazena o PhysicsMaterial2D original e cria um material dinâmico
        if (m_circleCollider != null)
        {
            m_originalPhysicsMaterial = m_circleCollider.sharedMaterial;
            // Cria uma nova instância de material para que possamos modificá-la em tempo de execução sem afetar outros objetos
            m_dynamicPhysicsMaterial = new PhysicsMaterial2D(m_originalPhysicsMaterial.name + "_Dynamic");
            m_dynamicPhysicsMaterial.friction = m_originalPhysicsMaterial.friction;
            m_dynamicPhysicsMaterial.bounciness = m_originalPhysicsMaterial.bounciness;
            
            // Atribui o material dinâmico ao collider da pedra
            m_circleCollider.sharedMaterial = m_dynamicPhysicsMaterial;
        }
    }

    void OnDisable() // Chamado quando o GameObject ou o script é desativado ou destruído
    {
        HandleSlideSound(false, 0f); // Garante que o som de deslize pare
        // Se a pedra for destruída ou desativada, seu material deve ser limpo ou resetado
        if (m_circleCollider != null && m_circleCollider.sharedMaterial != m_originalPhysicsMaterial)
        {
            m_circleCollider.sharedMaterial = m_originalPhysicsMaterial; // Restaura o material original
        }
    }


    // OnMouseDown is called when the user has pressed the mouse button while over the Collider.
    void OnMouseDown()
    {
        // Only allow starting the drag if:
        // 1. This is the correct stone (gameManager.CurrentStone).
        // 2. The stone is currently stopped (velocity near zero, using a small threshold).
        // 3. The game state is Aiming.
        if (gameManager != null && gameManager.CurrentStone == this.gameObject &&
            m_rigidbody.linearVelocity.magnitude < 0.01f && gameManager.CurrentGameState == GameManager.GameState.Aiming) // Usando 0.01f como threshold
        {
            m_dragStartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            m_isDragging = true;
            if (_aimLineRenderer != null)
            {
                _aimLineRenderer.enabled = true;
                _aimLineRenderer.positionCount = 2;
                _aimLineRenderer.SetPosition(0, transform.position); 
            }
        }
    }

    void Update()
    {
        // If dragging is active (meaning OnMouseDown was successful for this stone)
        if (m_isDragging)
        {
            UpdateAimLine(); // Always update aim line while dragging

            // Check for mouse button release to launch
            if (Input.GetMouseButtonUp(0))
            {
                // Re-confirm conditions to launch (important if state changed during drag)
                // This prevents launching if conditions somehow became invalid during drag (e.g., stone was hit by another)
                if (gameManager != null && gameManager.CurrentStone == this.gameObject &&
                    m_rigidbody.linearVelocity.magnitude < 0.01f && gameManager.CurrentGameState == GameManager.GameState.Aiming) // Usando 0.01f como threshold
                {
                    Vector2 dragEndPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector2 rawDirection = (m_dragStartPos - dragEndPos).normalized;
                    Vector2 finalDirection = ApplyAngleLimit(rawDirection);
                    float forceMagnitude = Mathf.Min(Vector2.Distance(m_dragStartPos, dragEndPos), _maxLaunchForce);

                    m_rigidbody.AddForce(finalDirection * forceMagnitude * _launchForceMultiplier, ForceMode2D.Impulse);
                    gameManager.OnStoneLaunched();
                    this.enabled = false; // Disable this StoneController after launch (não precisa de Update mais)
                }
                // Reset dragging state regardless of successful launch, and hide aim line
                m_isDragging = false;
                if (_aimLineRenderer != null) { _aimLineRenderer.enabled = false; }
            }
        }
        else // Se não está arrastando, garante que a linha de mira esteja desligada.
        {
            if (_aimLineRenderer != null) { _aimLineRenderer.enabled = false; }
        }
    }

    // Updates the visualization of the aim line
    private void UpdateAimLine()
    {
        if (_aimLineRenderer == null) return;

        Vector2 currentMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 rawDirection = (m_dragStartPos - currentMousePos); // Raw vector (magnitude for force representation)

        // Apply angle limitation for visual feedback
        Vector2 visualDirection = ApplyAngleLimit(rawDirection.normalized); 

        float currentForceMagnitude = Mathf.Min(rawDirection.magnitude, _maxLaunchForce);

        // The factor 0.5f is to visually adjust the line's length based on the force magnitude
        Vector2 aimEndPoint = (Vector2)transform.position + visualDirection * currentForceMagnitude * 0.5f;

        _aimLineRenderer.SetPosition(0, transform.position); 
        _aimLineRenderer.SetPosition(1, aimEndPoint); 
    }

    // Helper method to apply the angle limitation relative to positive X-axis
    private Vector2 ApplyAngleLimit(Vector2 direction)
    {
        float currentAngle = Vector2.SignedAngle(Vector2.right, direction);
        float clampedAngle = Mathf.Clamp(currentAngle, -_maxAimAngle, _maxAimAngle);
        Quaternion rotation = Quaternion.AngleAxis(clampedAngle, Vector3.forward);
        return rotation * Vector2.right; // Apply the rotation to the "right" vector (positive X-axis)
    }

    // --- Método para Adicionar Power-up à Pedra ---
    public void AddPowerUp(PowerUpType powerUp)
    {
        if (!activePowers.Contains(powerUp)) // Evita duplicidade se por algum erro o mesmo poder for adicionado mais de uma vez
        {
            activePowers.Add(powerUp);
            Debug.Log($"<color=lime>{gameObject.name} adquiriu o poder: {powerUp}!</color>");
            // Opcional: Adicionar algum efeito visual ou som para indicar o poder
        }
    }

    // --- Método para controlar a intensidade da varredura ---
    public void SetSweepIntensity(float intensity, float minSweptFrictionValue)
    {
        if (m_dynamicPhysicsMaterial == null) return;

        // O atrito será um valor interpolado entre o atrito original e o atrito mínimo varrido, baseado na intensidade
        // intensity = 0 -> fricção original; intensity = 1 -> minSweptFrictionValue
        m_dynamicPhysicsMaterial.friction = Mathf.Lerp(m_originalPhysicsMaterial.friction, minSweptFrictionValue, intensity);
    }

    // --- Método público para o GameManager controlar o som de deslize ---
    public void HandleSlideSound(bool isMoving, float volume)
    {
        if (_slideSound == null) return; // Se não houver som atribuído, ignora

        // Ajusta o volume do som de deslize
        _slideSound.SetVolume(volume);

        if (isMoving && !m_isSlidingSoundPlaying)
        {
            _slideSound.Play();
            m_isSlidingSoundPlaying = true;
        }
        else if (!isMoving && m_isSlidingSoundPlaying)
        {
            _slideSound.Stop();
            m_isSlidingSoundPlaying = false;
        }
    }


    // --- Handle Arcade Collisions ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Pega o StoneController do outro objeto colidido
        StoneController otherStone = collision.gameObject.GetComponent<StoneController>();

        // Lógica unificada para emissão de partículas e som de impacto
        // Esta lógica garante que, para cada par de objetos StoneController que colidem,
        // apenas UMA das duas pedras (aquela com o GetInstanceID() menor)
        // será responsável por emitir as partículas e tocar o som de impacto.
        // Se colidir com algo que NÃO É uma StoneController (ex: parede), a própria pedra emite.
        bool shouldEmitEffect = true;
        if (otherStone != null)
        {
            // Se ambos são pedras, só a pedra com o ID de instância menor emite (para evitar duplicação)
            if (this.gameObject.GetInstanceID() > otherStone.gameObject.GetInstanceID())
            {
                shouldEmitEffect = false;
            }
        }

        if (shouldEmitEffect)
        {
            // Tocar o som de impacto, se houver
            if (_impactSound != null)
            {
                _impactSound.Play();
            }

            // Emitir partículas de impacto, se houver e a pedra estiver em movimento significativo
            if (_impactParticlesPrefab != null && m_rigidbody.linearVelocity.magnitude >= _minImpactVelocityForParticles)
            {
                Vector2 collisionPoint = collision.contacts[0].point;
                ParticleSystem particles = Instantiate(_impactParticlesPrefab, collisionPoint, Quaternion.identity);
                particles.Play();
                // Destrói o sistema de partículas após o tempo necessário para tocar
                Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
            }

            // NOVO: Gerar Screen Shake
            if (_impulseSource != null)
            {
                _impulseSource.GenerateImpulse();
            }
        }
        
        // NOVO: Chama a animação de impacto com Play() para AMBAS as pedras (se tiverem Animator)
        if (_animator != null)
        {
            _animator.Play("Hit"); // Assumindo que você tem um estado de animação chamado "Hit"
        }
        // Se a outra pedra também tem um StoneController e um Animator, ela também receberá o Hit
        if (otherStone != null && otherStone._animator != null) // Certifica que o outro tem Animator
        {
            otherStone._animator.Play("Hit");
        }


        // --- Arcade Physics Logic ---
        // Only proceed if this is the currently launched stone AND the game is in a moving state
        // (to prevent stones from applying arcade effects if they move due to other forces after initial launch)
        if (gameManager == null || gameManager.CurrentStone != this.gameObject || gameManager.CurrentGameState != GameManager.GameState.StoneMoving)
        {
            return;
        }

        // Já obtivemos otherStone acima, então usamos a variável existente
        if (otherStone != null) // If collided with another stone
        {
            Rigidbody2D otherRb = otherStone.GetComponent<Rigidbody2D>();
            if (otherRb != null)
            {
                Vector2 pushDirection = (otherStone.transform.position - transform.position).normalized;

                // --- SUPER STRENGTH POWER-UP LOGIC ---
                if (activePowers.Contains(PowerUpType.SuperStrength))
                {
                    Debug.Log($"<color=purple>{gameObject.name} (Super Força) colidiu com {otherStone.gameObject.name}. Aplicando Super Força.</color>");

                    // 1. A pedra com Super Força para imediatamente
                    m_rigidbody.linearVelocity = Vector2.zero;
                    m_rigidbody.angularVelocity = 0f;

                    // 2. A pedra atingida é empurrada muito forte (força alta e fixa)
                    float superStrengthPushForce = 3f; // NOVO VALOR: 3f
                    otherRb.AddForce(pushDirection * superStrengthPushForce, ForceMode2D.Impulse);
                    Debug.Log($"<color=purple>Empurrando {otherStone.gameObject.name} com força de {superStrengthPushForce} (Super Força).</color>");

                    // Remove o poder de Super Força da lista de poderes ativos desta pedra após o uso
                    activePowers.Remove(PowerUpType.SuperStrength);
                    // Opcional: Remover efeito visual/sonoro específico da Super Força
                }
                else // --- LÓGICA DE FÍSICA ARCADE REGULAR (se Super Força NÃO estiver ativa) ---
                {
                    // Calcula a força do impacto baseada na velocidade linear atual da pedra lançada
                    float actualImpactForce = m_rigidbody.linearVelocity.magnitude * _collisionForceMultiplier;

                    // Aplica um impulso na pedra atingida
                    otherRb.AddForce(pushDirection * actualImpactForce, ForceMode2D.Impulse);
                    
                    // Garante que *esta* (lançada) pedra retém uma porcentagem de sua velocidade
                    m_rigidbody.linearVelocity *= _retainedVelocityRatio;

                    Debug.Log($"<color=orange>{gameObject.name} (Normal) colidiu com {otherStone.gameObject.name}. Aplicando força normal.</color>");
                }
            }
        }
    }
}