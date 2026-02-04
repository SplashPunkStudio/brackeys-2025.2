using UnityEngine;

public class PowerUpController : MonoBehaviour
{
    [SerializeField] private PowerUpType _powerUpType = PowerUpType.SuperStrength;

    // Referência ao GameManager para informar sobre a coleta
    private GameManager gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("PowerUpController: GameManager não encontrado na cena!");
            this.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        StoneController stone = other.GetComponent<StoneController>();
        if (stone != null)
        {
            if (gameManager != null)
            {
                gameManager.CollectPowerUp(_powerUpType, stone.teamType);
                Debug.Log($"Power-up '{_powerUpType}' coletado pela pedra da equipe {stone.teamType}!");
            }
            Destroy(gameObject); // Destrói o GameObject do power-up após a coleta
        }
    }
}