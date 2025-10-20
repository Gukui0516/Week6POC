using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameCore.Data;

/// <summary>
/// 블록 퍼즐 게임의 메인 UI 컨트롤러
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("점수 텍스트 색상 설정")]
    public Color scoreHighColor = Color.green;
    public Color scoreMidColor = Color.yellow;
    public Color scoreLowColor = Color.white;

    [Header("진행바 색상 설정")]
    public Color progressCompleteColor = Color.green;
    public Color progressHighColor = Color.yellow;
    public Color progressLowColor = new Color(1f, 0.5f, 0f);

    [Header("⭐ 상점 Canvas")]
    [SerializeField] private GameObject shopCanvas; // Inspector에서 할당

    #region Dependencies
    private GameManager gameManager;
    private InventoryController inventoryController;
    private TileGridManager tileGridManager;
    private GameStateUIManager gameStateUIManager;
    private GameFlowController gameFlowController;
    #endregion

    #region UI Elements
    [SerializeField]
    private TextMeshProUGUI turnText;
    [SerializeField]
    private TextMeshProUGUI currentScoreText;
    [SerializeField]
    private TextMeshProUGUI scoreText;
    [SerializeField]
    private Slider progressBar;
    #endregion

    #region Unity Lifecycle

    public void SetGameManager(GameManager gm)
    {
        gameManager = gm;
    }

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("[UIController] GameManager 싱글톤을 찾을 수 없습니다!");
            return;
        }

        // 상점 Canvas 초기 상태: 비활성화
        if (shopCanvas != null)
        {
            shopCanvas.SetActive(false);
        }

        StartCoroutine(InitializeAfterGameManager());
    }

    private System.Collections.IEnumerator InitializeAfterGameManager()
    {
        int waitCount = 0;

        while (gameManager.GetBoard() == null)
        {
            waitCount++;
            if (waitCount > 100)
            {
                Debug.LogError("[UIController] GameManager 초기화 대기 시간 초과!");
                yield break;
            }
            yield return null;
        }

        CacheUIReferences();
        InitializeTileGridManager();
        InitializeInventoryController();
        InitializeGameStateUIManager();
        InitializeGameFlowController();
        SubscribeToEvents();
        UpdateUI();
    }

    #endregion

    #region Initialization

    private void CacheUIReferences()
    {
        var gameButtons = FindObjectsByType<GameUIButton>(FindObjectsSortMode.None);
        var modeToggle = FindFirstObjectByType<BlockPuzzleModeToggle>();
    }

    private void InitializeTileGridManager()
    {
        tileGridManager = FindFirstObjectByType<TileGridManager>();

        if (tileGridManager == null)
        {
            GameObject tileManagerObj = new GameObject("TileGridManager");
            tileGridManager = tileManagerObj.AddComponent<TileGridManager>();
        }

        tileGridManager.Initialize(gameManager);

        var tiles = tileGridManager.GetAllTiles();
        if (tiles != null)
        {
            foreach (var tile in tiles)
            {
                tile.SetUIController(this);
            }
        }
    }

    private void InitializeInventoryController()
    {
        inventoryController = FindFirstObjectByType<InventoryController>();

        if (inventoryController == null)
        {
            GameObject inventoryObj = new GameObject("InventoryController");
            inventoryController = inventoryObj.AddComponent<InventoryController>();
        }

        inventoryController.Initialize(gameManager);
    }

    private void InitializeGameStateUIManager()
    {
        gameStateUIManager = FindFirstObjectByType<GameStateUIManager>();

        if (gameStateUIManager == null)
        {
            GameObject stateUIObj = new GameObject("GameStateUIManager");
            gameStateUIManager = stateUIObj.AddComponent<GameStateUIManager>();
        }

        if (scoreText != null)
        {
            gameStateUIManager.SetStateMessageText(scoreText);
        }

        gameStateUIManager.Initialize(gameManager, inventoryController, tileGridManager);
    }

    private void InitializeGameFlowController()
    {
        gameFlowController = FindFirstObjectByType<GameFlowController>();

        if (gameFlowController == null)
        {
            GameObject flowControllerObj = new GameObject("GameFlowController");
            gameFlowController = flowControllerObj.AddComponent<GameFlowController>();
        }

        gameFlowController.Initialize(gameManager, inventoryController, tileGridManager, gameStateUIManager, this);
    }

    /// <summary>
    /// GameManager 이벤트 구독
    /// </summary>
    private void SubscribeToEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnTurnStart += (turn) => UpdateUI();
            gameManager.OnScoreUpdated += (score) => UpdateUI();
            gameManager.OnBoardUpdated += UpdateBoard;
            gameManager.OnGameStateChanged += OnGameStateChanged; // ⭐ 게임 상태 변화 구독
        }
    }

    #endregion

    #region Game State Handling

    /// <summary>
    /// ⭐ 게임 상태 변경 시 상점 Canvas 표시/숨김
    /// </summary>
    private void OnGameStateChanged(GameState state)
    {
        if (shopCanvas == null)
        {
            Debug.LogWarning("[UIController] shopCanvas가 할당되지 않았습니다!");
            return;
        }

        switch (state)
        {
            case GameState.Shop:
                // 상점 열기
                shopCanvas.SetActive(true);
                Debug.Log("[UIController] 상점 Canvas 활성화");
                break;

            case GameState.Playing:
                // 상점 닫기 (게임 진행 중)
                shopCanvas.SetActive(false);
                break;

            case GameState.GameOver:
            case GameState.Victory:
                // 게임 종료 시에도 상점 닫기
                shopCanvas.SetActive(false);
                break;
        }
    }

    #endregion

    #region UI Update

    private void UpdateUI()
    {
        var turn = gameManager?.GetCurrentTurn();
        if (turn != null)
        {
            int currentTurnScore = gameManager.GetTotalScore();
            int cumulativeScore = gameManager.GetCumulativeScore();

            currentScoreText.text = $"현재 점수 : {currentTurnScore}";

            inventoryController?.UpdateInventory();
        }

        UpdateBoard();
    }

    private void UpdateBoard()
    {
        tileGridManager?.UpdateAllTiles();
    }

    #endregion

    #region Public API

    public bool TryPlaceSelectedBlock(int x, int y)
    {
        if (gameFlowController == null)
        {
            Debug.LogWarning("[UIController] GameFlowController가 초기화되지 않았습니다.");
            return false;
        }

        return gameFlowController.TryPlaceSelectedBlock(x, y);
    }

    public CardType? GetSelectedCardType() => gameFlowController?.GetSelectedCardType();

    #endregion

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= OnGameStateChanged;
        }
    }
}