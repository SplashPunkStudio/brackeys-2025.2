using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine; // Namespace para Cinemachine

public class GameManager : MonoBehaviour
{
    // --- Enums ---
    public enum TeamType { Red, Blue }
    public enum GameState { BeforeGame, Aiming, StoneMoving, EndScoring, BetweenEnds, GameOver }

    // --- Editor-exposed Configurations (Serialized Private Fields) ---
    [Header("Game Settings")]
    [SerializeField] private int _numberOfEnds = 8;
    [SerializeField] private int _stonesPerTeamPerEnd = 4;
    [SerializeField] private SO_Sound _gameMusic; // Assumindo que SO_Sound é um ScriptableObject para sons

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
    
    [SerializeField] private TextMeshProUGUI _roundText; // NOVO: Texto para exibir o round atual
    
    // Imagens dos times na HUD para destaque
    [Header("Team HUD Images")]
    [SerializeField] private Image _redTeamHighlightImage;
    [SerializeField] private Image _blueTeamHighlightImage;
    [SerializeField] private Color _activeTeamColor = Color.white;
    [SerializeField] private Color _inactiveTeamColor = Color.gray;

    // Botões dos power-ups na HUD
    [Header("Power-up HUD Buttons")]
    [SerializeField] private Button[] _redTeamPowerUpButtons; // Array de botões para power-ups do time Vermelho
    [SerializeField] private Button[] _blueTeamPowerUpButtons; // Array de botões para power-ups do time Azul
    [SerializeField] private Sprite _defaultPowerUpSprite; // Sprite padrão para ícones de power-up vazios
    [SerializeField] private Sprite _superStrengthSprite; // Sprite específico para SuperStrength (adicione mais para outros power-ups)


    // --- Material de física para o gelo varrido ---
    [Header("Physics Materials")]
    [SerializeField] private PhysicsMaterial2D _sweptIceMaterial; // Material de Physics 2D para o gelo varrido

    // --- Configurações de Varredura ---
    [Header("Sweeping Settings")]
    [SerializeField] private float _sweepPressGain = 0.25f; // Quanto de intensidade cada toque na barra de espaço adiciona
    [SerializeField] private float _sweepDecayRate = 0.5f; // Taxa de decaimento da intensidade da varredura por segundo
    [SerializeField] private float _maxSweepIntensity = 1.0f; // Intensidade máxima da varredura (0 a 1)
    [SerializeField] private float _minSweptFrictionValue = 0.05f; // O valor mínimo de atrito quando varrendo com intensidade máxima

    // --- Câmera Cinemachine ---
    [Header("Cinemachine Camera")]
    [SerializeField] private CinemachineCamera _mainCinemachineVCam; 
    [SerializeField] private CinemachineCamera _initialTargetVCam; // NOVO: Câmera para focar no alvo inicial
    [SerializeField] private Transform _initialCameraTargetPoint; // NOVO: Ponto para a câmera inicial focar

    [Header("Victory References")]
    [SerializeField] private InspectorScene _sceneRematch;
    [SerializeField] private InspectorScene _sceneMenu;
    [SerializeField] private GameObject _ctnVictory;
    [SerializeField] private Sprite _sprParty1;
    [SerializeField] private Sprite _sprParty2;
    [SerializeField] private Sprite _sprParty1Win;
    [SerializeField] private Sprite _sprParty2Win;
    [SerializeField] private Image _imgP1;
    [SerializeField] private Image _imgP2;
    [SerializeField] private Image _imgVictory;
    [SerializeField] private Button _btnRematch;
    [SerializeField] private Button _btnMenu;


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

    // --- Variável para armazenar múltiplos power-ups por equipe ---
    private Dictionary<TeamType, List<PowerUpType>> teamPowerUps;
    private const int MAX_POWER_UPS_PER_TEAM = 3; // Limite de poderes por equipe
    private const float MIN_VELOCITY_FOR_SLIDE_SOUND = 0.05f; // Velocidade mínima para o som de deslize


    // --- Variáveis de estado da varredura ---
    private float m_currentSweepIntensity = 0f;


    // --- Lifecycle Methods ---
    void Awake() // Garante que o dicionário seja inicializado antes de Start()
    {
        teamPowerUps = new Dictionary<TeamType, List<PowerUpType>>();
        teamPowerUps[TeamType.Red] = new List<PowerUpType>();
        teamPowerUps[TeamType.Blue] = new List<PowerUpType>();

        _btnRematch.Setup(BTN_Rematch);
        _btnMenu.Setup(BTN_Menu);

        // NOVO: Configura o alvo da câmera inicial (se houver)
        if (_initialTargetVCam != null && _initialCameraTargetPoint != null)
        {
            _initialTargetVCam.Follow = _initialCameraTargetPoint;
            _initialTargetVCam.LookAt = _initialCameraTargetPoint;
        }
    }

    void Start()
    {
        // Verifica se _gameMusic não é nulo antes de tentar Play()
        if (_gameMusic != null)
        {
            _gameMusic.Play();
        }

        StartGame();
    }

    void Update()
    {
        if (m_currentGameState == GameState.StoneMoving)
        {
            // --- Lógica de varredura com a tecla Espaço (apenas a pedra atual) ---
            if (m_currentStone != null)
            {
                StoneController currentStoneController = m_currentStone.GetComponent<StoneController>();
                if (currentStoneController != null && currentStoneController.teamType == m_currentTeam) // Apenas a pedra da equipe atual pode ser varrida
                {
                    // Decaimento da intensidade da varredura
                    m_currentSweepIntensity = Mathf.Max(0f, m_currentSweepIntensity - _sweepDecayRate * Time.deltaTime);

                    // Aumento da intensidade com o toque rápido da barra de espaço
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        m_currentSweepIntensity = Mathf.Min(_maxSweepIntensity, m_currentSweepIntensity + _sweepPressGain);
                    }
                    
                    // Aplica a intensidade da varredura à pedra
                    currentStoneController.SetSweepIntensity(m_currentSweepIntensity, _minSweptFrictionValue);
                }
                else // Se não é a pedra da equipe atual, garante que qualquer varredura seja desativada
                {
                    // Reseta a intensidade global da varredura para pedras não-controladas
                    m_currentSweepIntensity = 0f;
                    if (currentStoneController != null) currentStoneController.SetSweepIntensity(0f, _minSweptFrictionValue);
                }
            }


            bool anyStoneStillMoving = false;
            // Itera por todas as pedras em jogo para verificar se alguma ainda está se movendo
            foreach (StoneController stone in m_allStonesInPlay)
            {
                if (stone != null && stone.gameObject.activeInHierarchy) // Garante que a pedra existe e está ativa na cena
                {
                    Rigidbody2D rb = stone.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        // Gerencia o som de deslize da pedra com volume dinâmico
                        float currentSpeed = rb.linearVelocity.magnitude;
                        bool isStoneMovingSignificant = currentSpeed >= MIN_VELOCITY_FOR_SLIDE_SOUND;
                        
                        // Mapeia a velocidade para um volume entre 0 e 1 (ou qualquer range desejado)
                        // Ajuste os valores de 0.5f e 5.0f para o range de velocidade que você espera
                        float slideVolume = Mathf.InverseLerp(0.5f, 5.0f, currentSpeed); // Velocidade 0.5f = volume 0, Velocidade 5.0f = volume 1
                        slideVolume = Mathf.Clamp01(slideVolume); // Garante que o volume esteja entre 0 e 1

                        stone.HandleSlideSound(isStoneMovingSignificant, slideVolume);


                        // Se a velocidade linear ou angular for maior que um pequeno valor (quase parado)
                        // Use um epsilon (0.01f) para compensar imprecisões de ponto de flutuação
                        if (rb.linearVelocity.magnitude >= 0.1f || rb.angularVelocity >= 0.1f)
                        {
                            anyStoneStillMoving = true;
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
                // Garantir que a varredura e todos os sons de deslize parem quando as pedras param
                m_currentSweepIntensity = 0f; // Reseta a intensidade da varredura
                if (m_currentStone != null)
                {
                    StoneController currentStoneController = m_currentStone.GetComponent<StoneController>();
                    if (currentStoneController != null) currentStoneController.SetSweepIntensity(0f, _minSweptFrictionValue); // Garante atrito normal
                }
                
                foreach (StoneController stone in m_allStonesInPlay)
                {
                    if (stone != null)
                    {
                        stone.HandleSlideSound(false, 0f); // Parar som de deslize para todas as pedras
                    }
                }

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

        // Resetar os power-ups de ambas as equipes no início do jogo
        teamPowerUps[TeamType.Red].Clear();
        teamPowerUps[TeamType.Blue].Clear();
        m_currentSweepIntensity = 0f; // Garante que a varredura também seja resetada

        _nextEndButton.gameObject.SetActive(false);
        _restartGameButton.gameObject.SetActive(false);
        _messageText.text = "";

        ClearAllStones();
        StartNewEnd();
    }

    private void StartNewEnd()
    {
        m_currentEnd++;
        m_stonesThrownThisEnd = 0; // Reinicia a contagem de pedras jogadas para o novo round/end
        m_currentTeam = TeamType.Red;
        m_currentSweepIntensity = 0f; // Reseta a intensidade da varredura no início de cada End

        ClearAllStones(); // Limpa as pedras da rodada anterior
        
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
                stone.HandleSlideSound(false, 0f); // Garante que os sons de deslize das pedras destruídas parem
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

            // Gerenciamento dos botões de power-up
            UpdatePowerUpHUDButtons(); // Atualiza os botões diretamente
            UpdatePowerUpButtonInteractivity(); // Ativa/desativa baseado no estado do jogo

            // NOVO: Define o alvo da câmera Cinemachine para a pedra recém-lançada
            // e ajusta prioridades para a transição suave
            if (_mainCinemachineVCam != null)
            {
                _mainCinemachineVCam.Follow = m_currentStone.transform;
                _mainCinemachineVCam.LookAt = m_currentStone.transform;

                // Prioridade da câmera que segue a pedra deve ser maior
                _mainCinemachineVCam.Priority = 11; 
                // Prioridade da câmera que foca no alvo inicial deve ser menor para iniciar a transição
                if (_initialTargetVCam != null)
                {
                    _initialTargetVCam.Priority = 10; // Uma prioridade menor que a da pedra lançada
                }
            }
        }
    }

    public void OnStoneLaunched()
    {
        m_stonesThrownThisEnd++; // Incrementa a contagem de pedras lançadas neste End (Round)
        m_currentGameState = GameState.StoneMoving; // Estado de movimento geral
        Debug.Log("Stone launched. Current state: " + m_currentGameState);
        UpdateUI();
        UpdatePowerUpButtonInteractivity(); // Desativa os botões após o lançamento
    }

    private void CheckEndCondition()
    {
        if (m_stonesThrownThisEnd < (_stonesPerTeamPerEnd * 2))
        {
            SwitchTurn();
            SpawnStone(); // SpawnStone já define o novo alvo da câmera e atualiza os botões de power-up
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

    // --- Método: Coletar Power-up (gerencia a lista por equipe) ---
    public void CollectPowerUp(PowerUpType type, TeamType collectingTeam)
    {
        // Certifica-se de que a equipe é válida (não TeamType.None)
        if (collectingTeam != TeamType.Red && collectingTeam != TeamType.Blue) return;

        List<PowerUpType> currentTeamPowers = teamPowerUps[collectingTeam];

        // Implementação da lógica FIFO (First-In, First-Out)
        if (currentTeamPowers.Count >= MAX_POWER_UPS_PER_TEAM)
        {
            currentTeamPowers.RemoveAt(0); // Remove o item mais antigo (primeiro da lista)
            _messageText.text = $"Limite de poderes atingido para {collectingTeam}. Poder mais antigo descartado.\n"; // Adicionado \n para quebra de linha
        }

        currentTeamPowers.Add(type); // Adiciona o novo poder
        _messageText.text += $"Power-up {type.ToString()} coletado por {collectingTeam}! Total: {currentTeamPowers.Count}/{MAX_POWER_UPS_PER_TEAM}";
        
        UpdateUI(); // Atualiza a UI para refletir os poderes coletados
        UpdatePowerUpHUDButtons(); // Atualiza os botões na HUD
        UpdatePowerUpButtonInteractivity(); // Atualiza o estado de interatividade
    }

    // Método para ativar um power-up específico pelo índice (chamado pelos botões da HUD)
    public void ActivatePowerUp(int powerUpIndex)
    {
        // Só permite ativar se a pedra atual existe e o jogo está em modo de mira
        if (m_currentStone == null || m_currentGameState != GameState.Aiming)
        {
            Debug.LogWarning("Cannot activate power-up: No current stone or not in aiming state.");
            return;
        }

        List<PowerUpType> currentTeamPowers = teamPowerUps[m_currentTeam];

        // Verifica se o índice é válido e se há um poder naquele slot
        if (powerUpIndex >= 0 && powerUpIndex < currentTeamPowers.Count)
        {
            PowerUpType activatedPower = currentTeamPowers[powerUpIndex]; // Pega o poder do slot clicado
            StoneController stoneController = m_currentStone.GetComponent<StoneController>();
            if (stoneController != null)
            {
                stoneController.AddPowerUp(activatedPower); // Aplica o poder à pedra atual
                currentTeamPowers.RemoveAt(powerUpIndex); // Remove o poder específico da lista
                _messageText.text = $"Power-up '{activatedPower}' ativado pela equipe {m_currentTeam}!";
                Debug.Log($"Activated power-up: {activatedPower} for {m_currentTeam}");
            }
            UpdatePowerUpHUDButtons(); // Atualiza os botões após o uso
            UpdatePowerUpButtonInteractivity(); // Atualiza o estado de interatividade
        }
        else
        {
            _messageText.text = $"Equipe {m_currentTeam} não tem poder no slot {powerUpIndex + 1} para ativar!";
            Debug.LogWarning($"Attempted to activate power-up but {m_currentTeam} has no power at index {powerUpIndex}.");
            UpdatePowerUpHUDButtons(); // Garante que os botões estejam corretos
            UpdatePowerUpButtonInteractivity(); // Garante que o botão esteja desativado
        }
    }


    // --- Scoring Logic ---
    private void ScoreCurrentEnd()
    {
        _messageText.text = "End Scored!";
        _nextEndButton.gameObject.SetActive(true);

        List<StoneController> stonesInHouse = m_allStonesInPlay
            .Where(s => s != null && s.GetComponent<CircleCollider2D>() != null)
            .Where(s => {
                // _buttonCollider agora é um CircleCollider2D, então usamos .transform.position para o centro
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

        bool? player1Win = null;

        string winnerMessage;
        if (m_redTeamScore > m_blueTeamScore)
        {
            winnerMessage = "Game Over! Red Team Wins!";
            player1Win = true;
        }
        else if (m_blueTeamScore > m_redTeamScore)
        {
            winnerMessage = "Game Over! Blue Team Wins!";
            player1Win = false;
        }
        else
        {
            winnerMessage = "Game Over! It's a Tie!";
        }
        _messageText.text = winnerMessage;

        _ctnVictory.SetActive(true);
        var player1IsParty1 = InstanceInfo.Player1Party1;

        _imgP1.sprite = player1IsParty1 ? _sprParty1 : _sprParty2;
        _imgP2.sprite = player1IsParty1 ? _sprParty2 : _sprParty1;

        _imgVictory.sprite = !player1Win.HasValue ? null : (player1Win.Value ? _sprParty1Win : _sprParty2);

        UpdateUI();
    }

    private void BTN_Rematch()
    {
        Manager_Events.Scene.TransitionWithLoading.Notify(_sceneRematch);
    }

    private void BTN_Menu()
    {
        Manager_Events.Scene.Transition.Notify(_sceneMenu);
    }

    // Mapeia PowerUpType para Sprite
    private Sprite GetSpriteForPowerUp(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.SuperStrength:
                return _superStrengthSprite;
            // Adicione mais casos para outros PowerUpTypes
            default:
                return _defaultPowerUpSprite; // Ou um sprite de placeholder
        }
    }

    // Atualiza os sprites e a interatividade dos botões de power-up na HUD
    private void UpdatePowerUpHUDButtons()
    {
        // Limpa e atualiza todos os botões de ambos os times
        UpdatePowerUpButtonsForTeam(TeamType.Red, _redTeamPowerUpButtons);
        UpdatePowerUpButtonsForTeam(TeamType.Blue, _blueTeamPowerUpButtons);
    }

    private void UpdatePowerUpButtonsForTeam(TeamType team, Button[] teamButtons)
    {
        List<PowerUpType> currentTeamPowers = teamPowerUps[team];

        for (int i = 0; i < MAX_POWER_UPS_PER_TEAM; i++)
        {
            if (i < teamButtons.Length && teamButtons[i] != null)
            {
                teamButtons[i].onClick.RemoveAllListeners(); // Limpa listeners anteriores

                // NOVO: Pega a imagem do componente filho do botão
                Image buttonIconImage = teamButtons[i].transform.GetChild(0).GetComponent<Image>(); 
                if (buttonIconImage == null)
                {
                    Debug.LogWarning($"Button {teamButtons[i].name} does not have a child Image component. Cannot update sprite.");
                    continue;
                }

                if (i < currentTeamPowers.Count)
                {
                    buttonIconImage.sprite = GetSpriteForPowerUp(currentTeamPowers[i]);
                    buttonIconImage.enabled = true; // Mostra a imagem do ícone
                    int index = i; // Captura o valor de i para o lambda
                    teamButtons[i].onClick.AddListener(() => ActivatePowerUp(index));
                }
                else
                {
                    buttonIconImage.sprite = null; // Limpa o sprite se o slot estiver vazio
                    buttonIconImage.enabled = false; // Oculta a imagem do ícone
                }
            }
        }
    }

    // Controla a interatividade dos botões de power-up (baseado no GameState e na vez do time)
    private void UpdatePowerUpButtonInteractivity()
    {
        // Atualiza botões do time Vermelho
        for (int i = 0; i < _redTeamPowerUpButtons.Length; i++)
        {
            if (_redTeamPowerUpButtons[i] != null)
            {
                _redTeamPowerUpButtons[i].interactable = 
                    (m_currentGameState == GameState.Aiming) && 
                    (m_currentTeam == TeamType.Red) && 
                    (i < teamPowerUps[TeamType.Red].Count);
            }
        }

        // Atualiza botões do time Azul
        for (int i = 0; i < _blueTeamPowerUpButtons.Length; i++)
        {
            if (_blueTeamPowerUpButtons[i] != null)
            {
                _blueTeamPowerUpButtons[i].interactable = 
                    (m_currentGameState == GameState.Aiming) && 
                    (m_currentTeam == TeamType.Blue) && 
                    (i < teamPowerUps[TeamType.Blue].Count);
            }
        }
    }


    // --- UI Update ---
    private void UpdateUI()
    {
        _redTeamScoreText.text = "Red: " + m_redTeamScore;
        _blueTeamScoreText.text = "Blue: " + m_blueTeamScore;
        _currentEndText.text = "End: " + m_currentEnd + "/" + _numberOfEnds;
        _roundText.text = "Round: " + (m_stonesThrownThisEnd / 2 + 1) + "/" + _stonesPerTeamPerEnd; // NOVO: Atualiza o texto do round (Pedras por equipe)

        // Destaca a imagem do time da vez
        _redTeamHighlightImage.color = (m_currentTeam == TeamType.Red) ? _activeTeamColor : _inactiveTeamColor;
        _blueTeamHighlightImage.color = (m_currentTeam == TeamType.Blue) ? _activeTeamColor : _inactiveTeamColor;

        switch (m_currentGameState)
        {
            case GameState.Aiming:
                _currentTeamText.text = "Turn: " + m_currentTeam + " (Aiming)";
                break;
            case GameState.StoneMoving:
                _currentTeamText.text = "Turn: " + m_currentTeam + " (Stone Moving)";
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

        // --- Atualização da UI para mostrar os poderes coletados ---
        string redPowers = teamPowerUps[TeamType.Red].Any() ? string.Join(", ", teamPowerUps[TeamType.Red]) : "None";
        string bluePowers = teamPowerUps[TeamType.Blue].Any() ? string.Join(", ", teamPowerUps[TeamType.Blue]) : "None";
        _messageText.text = $"Red Powers: {redPowers} ({teamPowerUps[TeamType.Red].Count}/{MAX_POWER_UPS_PER_TEAM})\nBlue Powers: {bluePowers} ({teamPowerUps[TeamType.Blue].Count}/{MAX_POWER_UPS_PER_TEAM})";
        
        UpdatePowerUpHUDButtons(); // Chama a atualização dos botões de power-up
        UpdatePowerUpButtonInteractivity(); // Garante que a interatividade esteja correta após a atualização
    }
}