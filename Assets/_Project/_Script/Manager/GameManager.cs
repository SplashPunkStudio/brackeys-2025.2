using UnityEngine;
using TMPro; // Using TextMeshPro for UI Text elements
using UnityEngine.UI; // Required for Button
using System.Collections.Generic; // Required for List
using System.Linq; // Required for LINQ operations (Min, OrderBy, etc.)

public class GameManager : MonoBehaviour
{
    // --- Enums ---
    public enum TeamType { Red, Blue }
    public enum GameState { BeforeGame, Aiming, StoneMoving, EndScoring, BetweenEnds, GameOver }

    // --- Editor-exposed Configurations (Serialized Private Fields) ---
    [Header("Game Settings")]
    [SerializeField] private int _numberOfEnds = 8; // Total number of ends in the game
    [SerializeField] private int _stonesPerTeamPerEnd = 4; // Number of stones each team throws per end

    [Header("Prefabs & Spawn")]
    [SerializeField] private GameObject _redStonePrefab; // Prefab for Red Team stone
    [SerializeField] private GameObject _blueStonePrefab; // Prefab for Blue Team stone
    [SerializeField] private Transform _stoneSpawnPoint; // Point where stones will be instantiated

    [Header("Scoring Zones")]
    // The innermost collider, representing the "Button" or center for scoring distance
    [SerializeField] private CircleCollider2D _buttonCollider;
    // The outermost collider, defining the "House" (stones outside this don't score)
    [SerializeField] private CircleCollider2D _houseCollider;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _redTeamScoreText;
    [SerializeField] private TextMeshProUGUI _blueTeamScoreText;
    [SerializeField] private TextMeshProUGUI _currentEndText;
    [SerializeField] private TextMeshProUGUI _currentTeamText;
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Button _nextEndButton;
    [SerializeField] private Button _restartGameButton;

    // --- Private Game Variables (m_ convention for non-serialized) ---
    // Exposed via public properties for controlled external access
    private GameState m_currentGameState;
    public GameState CurrentGameState { get { return m_currentGameState; } } // Public property for read access

    private GameObject m_currentStone;
    public GameObject CurrentStone { get { return m_currentStone; } } // Public property for read access

    private int m_currentEnd;
    private int m_redTeamScore;
    private int m_blueTeamScore;
    private TeamType m_currentTeam;
    private int m_stonesThrownThisEnd;
    private List<StoneController> m_allStonesInPlay = new List<StoneController>(); // All stones on the ice

    // --- Lifecycle Methods ---
    void Start()
    {
        StartGame();
    }

    void Update()
    {
        // Only check stone movement if in StoneMoving state and a stone is in play
        if (m_currentGameState == GameState.StoneMoving && m_currentStone != null)
        {
            Rigidbody2D rb = m_currentStone.GetComponent<Rigidbody2D>();
            // Use linearVelocity.magnitude and angularVelocity.magnitude
            if (rb != null && rb.linearVelocity.magnitude < 0.1f && rb.angularVelocity < 0.1f) // Stone has stopped moving
            {
                // Explicitly stop the stone to prevent lingering movement
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;

                // Deactivate the StoneController of the just-stopped stone
                if (m_currentStone.GetComponent<StoneController>() != null)
                {
                    m_currentStone.GetComponent<StoneController>().enabled = false;
                }

                m_currentGameState = GameState.Aiming; // Back to aiming for the next stone or end
                CheckEndCondition(); // Determine if all stones are thrown for this end
            }
        }
    }

    // --- Game Flow Methods ---

    // Initializes and starts a new game
    public void StartGame()
    {
        m_currentEnd = 0;
        m_redTeamScore = 0;
        m_blueTeamScore = 0;
        m_stonesThrownThisEnd = 0;
        m_currentGameState = GameState.BeforeGame; // Initial state

        // Reset UI buttons
        _nextEndButton.gameObject.SetActive(false);
        _restartGameButton.gameObject.SetActive(false);
        _messageText.text = "";

        ClearAllStones(); // Clear any existing stones from previous game/editor
        StartNewEnd(); // Begin the first end
    }

    // Starts a new end (round) of curling
    private void StartNewEnd()
    {
        m_currentEnd++;
        m_stonesThrownThisEnd = 0;
        m_currentTeam = TeamType.Red; // Red team always starts each end in this prototype

        ClearAllStones(); // Remove all stones from the previous end
        
        m_currentGameState = GameState.Aiming;
        UpdateUI(); // Update UI for the new end
        SpawnStone(); // Spawn the first stone of the new end
    }

    // Clears all stones from the ice
    private void ClearAllStones()
    {
        foreach (var stone in m_allStonesInPlay)
        {
            if (stone != null)
            {
                Destroy(stone.gameObject);
            }
        }
        m_allStonesInPlay.Clear();
        m_currentStone = null;
    }

    // Spawns a stone for the current team
    private void SpawnStone()
    {
        GameObject stoneToSpawn = (m_currentTeam == TeamType.Red) ? _redStonePrefab : _blueStonePrefab;
        m_currentStone = Instantiate(stoneToSpawn, _stoneSpawnPoint.position, Quaternion.identity);

        StoneController stoneController = m_currentStone.GetComponent<StoneController>();
        if (stoneController != null)
        {
            stoneController.gameManager = this; // Pass reference to this GameManager
            stoneController.teamType = m_currentTeam; // Assign team type to the stone
            stoneController.enabled = true; // IMPORTANT: Enable this stone's controller for input
            m_allStonesInPlay.Add(stoneController); // Add to the list of stones in play
        }
    }

    // Called by the StoneController when a stone is launched
    public void OnStoneLaunched()
    {
        m_stonesThrownThisEnd++;
        m_currentGameState = GameState.StoneMoving;
        Debug.Log("Stone launched. Current state: " + m_currentGameState);
        UpdateUI(); // Update UI to show stone is moving
    }

    // Checks if all stones for the current end have been thrown
    private void CheckEndCondition()
    {
        if (m_stonesThrownThisEnd < (_stonesPerTeamPerEnd * 2))
        {
            SwitchTurn(); // Switch to the other team for the next stone
            SpawnStone(); // Spawn the next stone
            m_currentGameState = GameState.Aiming; // Ensure state is aiming for the newly spawned stone
            UpdateUI(); // Update UI for the new turn
        }
        else
        {
            // All stones thrown for this end, time to score
            m_currentGameState = GameState.EndScoring;
            ScoreCurrentEnd();
        }
    }

    // Switches the turn between Red and Blue teams
    private void SwitchTurn()
    {
        m_currentTeam = (m_currentTeam == TeamType.Red) ? TeamType.Blue : TeamType.Red;
    }

    // --- Scoring Logic ---

    // Calculates and applies score for the current end
    private void ScoreCurrentEnd()
    {
        _messageText.text = "End Scored!";
        _nextEndButton.gameObject.SetActive(true); // Show button to proceed to next end

        // Get all stones that are currently "in the house"
        List<StoneController> stonesInHouse = m_allStonesInPlay
            .Where(s => s != null && Vector2.Distance(s.transform.position, _buttonCollider.transform.position) <= _houseCollider.radius)
            .ToList();

        if (stonesInHouse.Count == 0)
        {
            Debug.Log("No stones in the house. Score for this end: 0.");
            m_currentGameState = GameState.BetweenEnds;
            UpdateUI();
            return;
        }

        // Order stones in house by their distance to the button (closest first)
        List<StoneController> sortedStones = stonesInHouse
            .OrderBy(s => Vector2.Distance(s.transform.position, _buttonCollider.transform.position))
            .ToList();

        // Determine which team has the closest stone in the house
        StoneController closestStone = sortedStones.FirstOrDefault();
        if (closestStone == null) return; // Should not happen if stonesInHouse > 0

        TeamType scoringTeam = closestStone.teamType;
        float closestOpponentDistance = float.MaxValue;

        // Find the closest stone of the OPPONENT team
        foreach (var stone in sortedStones)
        {
            if (stone.teamType != scoringTeam)
            {
                closestOpponentDistance = Vector2.Distance(stone.transform.position, _buttonCollider.transform.position);
                break; // Found the opponent's closest stone
            }
        }

        int endPoints = 0;
        if (closestOpponentDistance == float.MaxValue) // No opponent stones in the house
        {
            // If there are no opponent stones in the house, the scoring team scores for ALL their stones in the house
            endPoints = stonesInHouse.Count(s => s.teamType == scoringTeam);
        }
        else
        {
            // Count how many of the scoring team's stones are closer to the button than the opponent's closest stone
            endPoints = stonesInHouse.Count(s => s.teamType == scoringTeam && Vector2.Distance(s.transform.position, _buttonCollider.transform.position) < closestOpponentDistance);
        }

        if (scoringTeam == TeamType.Red)
        {
            m_redTeamScore += endPoints;
        }
        else
        {
            m_blueTeamScore += endPoints;
        }

        Debug.Log("End " + m_currentEnd + " scored. " + scoringTeam + " scores " + endPoints + " points.");
        m_currentGameState = GameState.BetweenEnds;
        UpdateUI();
    }

    // Public method to advance to the next end (called by UI button)
    public void AdvanceToNextEnd()
    {
        _nextEndButton.gameObject.SetActive(false);
        _messageText.text = "";

        if (m_currentEnd >= _numberOfEnds)
        {
            GameOver();
        }
        else
        {
            StartNewEnd();
        }
    }

    // Handles Game Over state
    private void GameOver()
    {
        m_currentGameState = GameState.GameOver;
        _restartGameButton.gameObject.SetActive(true);

        string winnerMessage;
        if (m_redTeamScore > m_blueTeamScore)
        {
            winnerMessage = "Game Over! Red Team Wins!";
        }
        else if (m_blueTeamScore > m_redTeamScore)
        {
            winnerMessage = "Game Over! Blue Team Wins!";
        }
        else
        {
            winnerMessage = "Game Over! It's a Tie!";
        }
        _messageText.text = winnerMessage;
        UpdateUI();
    }

    // Public method to restart the game (called by UI button)
    public void RestartGame()
    {
        StartGame(); // Simply restarts the entire game flow
    }

    // --- UI Update ---

    // Centralized method to update all UI elements
    private void UpdateUI()
    {
        _redTeamScoreText.text = "Red: " + m_redTeamScore;
        _blueTeamScoreText.text = "Blue: " + m_blueTeamScore;
        _currentEndText.text = "End: " + m_currentEnd + "/" + _numberOfEnds;

        switch (m_currentGameState)
        {
            case GameState.Aiming:
            case GameState.StoneMoving:
                _currentTeamText.text = "Turn: " + m_currentTeam;
                break;
            case GameState.EndScoring:
                _currentTeamText.text = "Scoring End...";
                break;
            case GameState.BetweenEnds:
                _currentTeamText.text = "End Over!";
                break;
            case GameState.GameOver:
                _currentTeamText.text = "Game Over!";
                break;
            case GameState.BeforeGame: // Should only happen briefly at start
                _currentTeamText.text = "Starting Game...";
                break;
        }
    }
}