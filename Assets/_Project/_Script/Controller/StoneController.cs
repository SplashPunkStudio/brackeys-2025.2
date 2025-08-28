using UnityEngine;
using TMPro; // Using TextMeshPro

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

    [Header("Visuals")]
    [SerializeField] private LineRenderer _aimLineRenderer; // Line Renderer component to draw the aim line
    [SerializeField] private ParticleSystem _impactParticlesPrefab; // NEW: Prefab for collision impact particles

    // --- Private Game Variables ---
    private Rigidbody2D m_rigidbody; // Rigidbody2D component of the stone
    private Vector2 m_dragStartPos; // Mouse position when the click started
    private bool m_isDragging = false; // Indicates if the mouse is being dragged for aiming

    void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody2D>();
        m_rigidbody.gravityScale = 0; // Ensures no gravity for a top-down game
        m_rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation; // Freeze Z-rotation

        // Apply the configured mass to the Rigidbody
        m_rigidbody.mass = _rigidbodyMass;

        // Linear drag (friction) should be configured directly in the Rigidbody2D component in the Unity Inspector
    }

    // OnMouseDown is called when the user has pressed the mouse button while over the Collider.
    void OnMouseDown()
    {
        // Only allow starting the drag if:
        // 1. This is the correct stone (gameManager.CurrentStone).
        // 2. The stone is currently stopped (velocity near zero).
        // 3. The game state is Aiming.
        if (gameManager != null && gameManager.CurrentStone == this.gameObject &&
            m_rigidbody.linearVelocity.magnitude < 0.1f && gameManager.CurrentGameState == GameManager.GameState.Aiming)
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
                    m_rigidbody.linearVelocity.magnitude < 0.1f && gameManager.CurrentGameState == GameManager.GameState.Aiming)
                {
                    Vector2 dragEndPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    Vector2 rawDirection = (m_dragStartPos - dragEndPos).normalized;
                    Vector2 finalDirection = ApplyAngleLimit(rawDirection);
                    float forceMagnitude = Mathf.Min(Vector2.Distance(m_dragStartPos, dragEndPos), _maxLaunchForce);

                    m_rigidbody.AddForce(finalDirection * forceMagnitude * _launchForceMultiplier, ForceMode2D.Impulse);
                    gameManager.OnStoneLaunched();
                    this.enabled = false; // Disable this StoneController after launch
                }
                // Reset dragging state regardless of successful launch, and hide aim line
                m_isDragging = false;
                if (_aimLineRenderer != null) { _aimLineRenderer.enabled = false; }
            }
        }
        else // If not dragging, ensure aim line is off (this covers cases where dragging was reset or never started)
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

    // --- Handle Arcade Collisions ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Important: Collision detection should trigger particle effects for *both* colliding objects,
        // regardless of which stone initiated the "arcade push".
        // This ensures visual feedback from all collisions.
        
        // NEW: Play impact particle effect at the collision point
        if (_impactParticlesPrefab != null)
        {
            // Get the first contact point of the collision
            Vector2 collisionPoint = collision.contacts[0].point;
            
            // Instantiate the particles
            ParticleSystem particles = Instantiate(_impactParticlesPrefab, collisionPoint, Quaternion.identity);
            
            // Play the particles (they should be configured with Play On Awake = false)
            particles.Play();
            
            // Destroy the particle system GameObject after its duration to clean up
            Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
        }

        // --- Arcade Physics Logic (Only for the active launched stone) ---
        // This part remains the same to ensure only the active stone applies the arcade push.
        if (gameManager == null || gameManager.CurrentStone != this.gameObject)
        {
            return; // This stone is not the actively launched stone.
        }

        // Check if the collided object is another stone
        StoneController otherStone = collision.gameObject.GetComponent<StoneController>();
        if (otherStone != null)
        {
            Rigidbody2D otherRb = otherStone.GetComponent<Rigidbody2D>();
            if (otherRb != null)
            {
                // Calculate direction from this (the launched) stone to the hit stone
                Vector2 pushDirection = (otherStone.transform.position - transform.position).normalized;
                
                // Calculate impact force based on launched stone's current linear velocity
                float actualImpactForce = m_rigidbody.linearVelocity.magnitude * _collisionForceMultiplier;

                // Apply a significant impulse to the *hit* stone
                otherRb.AddForce(pushDirection * actualImpactForce, ForceMode2D.Impulse);
                
                // Ensure *this* (launched) stone retains a percentage of its velocity
                m_rigidbody.linearVelocity *= _retainedVelocityRatio;
            }
        }
    }
}