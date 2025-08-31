using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    // --- Enums ---
    public enum TeamType { Red, Blue }
    public enum GameState { BeforeGame, Aiming, StoneMoving, EndScoring, BetweenEnds, GameOver }

    // --- Editor-exposed Configurations (Serialized Private Fields) ---
    [Header("Game Settings")]
    [SerializeField] private int _numberOfEnds = 8;
    [SerializeField] private int _stonesPerTeamPerEnd = 4;

    [Header("Prefabs & Spawn")]
    [SerializeField] private GameObject _redStonePrefab;
    [SerializeField] private GameObject _blueStonePrefab;
    [SerializeField] private Transform _stoneSpawnPoint;

    [Header("Scoring Zones")]
    [SerializeField] private CircleCollider2D _buttonCollider;
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
    private GameState m_currentGameState;
    public GameState CurrentGameState { get { return m_currentGameState; } }

    private GameObject m_currentStone;
    public GameObject CurrentStone { get { return m_currentStone; } }

    private int m_currentEnd;
    private int m_redTeamScore;
    private int m_blueTeamScore;
    private TeamType m_currentTeam;
    private int m_stonesThrownThisEnd;
    private List<StoneController> m_allStonesInPlay = new List<StoneController>();

    // --- Lifecycle Methods ---
    void Start()
    {
        StartGame();
    }

    void Update()
    {
        // A VERIFICAÇÃO DO MOVIMENTO DAS PEDRAS FOI ALTERADA AQUI
        if (m_currentGameState == GameState.StoneMoving)
        {
            bool anyStoneStillMoving = false;
            // Itera por todas as pedras em jogo para verificar se alguma ainda está se movendo
            foreach (StoneController stone in m_allStonesInPlay)
            {
                if (stone != null && stone.gameObject.activeInHierarchy) // Garante que a pedra existe e está ativa na cena
                {
                    Rigidbody2D rb = stone.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        // Se a velocidade linear ou angular for maior que um pequeno valor (quase parado)
                        if (rb.linearVelocity.magnitude >= 0.1f || rb.angularVelocity >= 0.1f)
                        {
                            anyStoneStillMoving = true;
                            break; // Encontramos uma pedra se movendo, não precisamos verificar as outras
                        }
                        // Se a pedra está quase parada, force-a a parar completamente para evitar "tremores"
                        else if (rb.linearVelocity.magnitude < 0.1f && rb.angularVelocity < 0.1f)
                        {
                            rb.linearVelocity = Vector2.zero;
                            rb.angularVelocity = 0f;
                        }
                    }
                }
            }

            // Se nenhuma pedra está mais se movendo, então a fase de movimento terminou
            if (!anyStoneStillMoving)
            {
                m_currentGameState = GameState.Aiming; // Volta ao estado de mira para a próxima ação
                CheckEndCondition(); // Agora sim, verifica a condição do End (próxima pedra ou pontuação)
            }
        }
    }

    // --- Game Flow Methods ---

    public void StartGame()
    {
        m_currentEnd = 0;
        m_redTeamScore = 0;
        m_blueTeamScore = 0;
        m_stonesThrownThisEnd = 0;
        m_currentGameState = GameState.BeforeGame;

        _nextEndButton.gameObject.SetActive(false);
        _restartGameButton.gameObject.SetActive(false);
        _messageText.text = "";

        ClearAllStones();
        StartNewEnd();
    }

    private void StartNewEnd()
    {
        m_currentEnd++;
        m_stonesThrownThisEnd = 0;
        m_currentTeam = TeamType.Red;

        ClearAllStones();
        
        m_currentGameState = GameState.Aiming;
        UpdateUI();
        SpawnStone();
    }

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

    private void SpawnStone()
    {
        GameObject stoneToSpawn = (m_currentTeam == TeamType.Red) ? _redStonePrefab : _blueStonePrefab;
        m_currentStone = Instantiate(stoneToSpawn, _stoneSpawnPoint.position, Quaternion.identity);
        m_currentStone.name = $"{m_currentTeam}_{m_allStonesInPlay.Count / 2 + m_allStonesInPlay.Count % 2}";

        StoneController stoneController = m_currentStone.GetComponent<StoneController>();
        if (stoneController != null)
        {
            stoneController.gameManager = this;
            stoneController.teamType = m_currentTeam;
            stoneController.enabled = true; // Habilita o StoneController da pedra para input
            m_allStonesInPlay.Add(stoneController);
        }
    }

    public void OnStoneLaunched()
    {
        m_stonesThrownThisEnd++;
        m_currentGameState = GameState.StoneMoving; // Estado de movimento geral
        Debug.Log("Stone launched. Current state: " + m_currentGameState);
        UpdateUI();
    }

    private void CheckEndCondition()
    {
        if (m_stonesThrownThisEnd < (_stonesPerTeamPerEnd * 2))
        {
            SwitchTurn();
            SpawnStone();
            // m_currentGameState já será Aiming pela lógica de Update() quando todas as pedras pararem
            UpdateUI();
        }
        else
        {
            m_currentGameState = GameState.EndScoring;
            ScoreCurrentEnd();
        }
    }

    private void SwitchTurn()
    {
        m_currentTeam = (m_currentTeam == TeamType.Red) ? TeamType.Blue : TeamType.Red;
    }

    // --- Scoring Logic ---

    private void ScoreCurrentEnd()
    {
        _messageText.text = "End Scored!";
        _nextEndButton.gameObject.SetActive(true);

        List<StoneController> stonesInHouse = m_allStonesInPlay
            .Where(s => s != null && s.GetComponent<CircleCollider2D>() != null)
            .Where(s => {
                float distanceToButtonCenter = Vector2.Distance(s.transform.position, _buttonCollider.transform.position);
                float stoneRadius = s.GetComponent<CircleCollider2D>().radius * s.transform.localScale.x;
                float effectiveHouseRadius = _houseCollider.radius * _houseCollider.transform.localScale.x;
                return distanceToButtonCenter <= effectiveHouseRadius + stoneRadius;
            })
            .ToList();

        if (stonesInHouse.Count == 0)
        {
            Debug.Log("No stones in the house. Score for this end: 0.");
            m_currentGameState = GameState.BetweenEnds;
            UpdateUI();
            return;
        }

        List<StoneController> sortedStones = stonesInHouse
            .OrderBy(s => Vector2.Distance(s.transform.position, _buttonCollider.transform.position))
            .ToList();

        StoneController closestStone = sortedStones.FirstOrDefault();
        if (closestStone == null)
        {
             Debug.LogWarning("No closest stone found despite stones being in house. This should not happen.");
             m_currentGameState = GameState.BetweenEnds;
             UpdateUI();
             return;
        }

        TeamType scoringTeam = closestStone.teamType;
        float closestOpponentDistance = float.MaxValue;

        foreach (var stone in sortedStones)
        {
            if (stone.teamType != scoringTeam)
            {
                closestOpponentDistance = Vector2.Distance(stone.transform.position, _buttonCollider.transform.position);
                break;
            }
        }

        int endPoints = 0;
        if (closestOpponentDistance == float.MaxValue)
        {
            endPoints = stonesInHouse.Count(s => s.teamType == scoringTeam);
        }
        else
        {
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
        StartGame();
    }

    // --- UI Update ---

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
            case GameState.BeforeGame:
                _currentTeamText.text = "Starting Game...";
                break;
        }
    }
}