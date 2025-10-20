using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameCore.Data;

/// <summary>
/// 블록 퍼즐 게임의 메인 UI 컨트롤러
/// 
/// 역할:
/// 1. 모든 UI 관련 컨트롤러/매니저 초기화 및 조율
/// 2. 보드 UI 표시 (점수, 턴 정보)
/// 3. GameManager 이벤트 구독 및 UI 업데이트
/// 4. 타일에 대한 인터페이스 제공 (GameFlowController로 위임)
/// 
/// 책임 분리:
/// - TileGridManager: 타일 그리드 관리
/// - InventoryController: 인벤토리 UI 관리
/// - GameStateUIManager: 게임 상태 UI (오버/승리)
/// - GameFlowController: 게임 로직 중개
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("점수 텍스트 색상 설정")]
    public Color scoreHighColor = Color.green;     // 목표 달성
    public Color scoreMidColor = Color.yellow;     // 70% 이상
    public Color scoreLowColor = Color.white;      // 70% 미만

    [Header("진행바 색상 설정")]
    public Color progressCompleteColor = Color.green;   // 100% 이상
    public Color progressHighColor = Color.yellow;      // 70% 이상
    public Color progressLowColor = new Color(1f, 0.5f, 0f); // 70% 미만 (주황색)

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
        // 싱글톤을 통해 GameManager 참조
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("[BlockPuzzleUIController] GameManager 싱글톤을 찾을 수 없습니다!");
            return;
        }

        // GameManager의 Start가 실행될 때까지 대기
        StartCoroutine(InitializeAfterGameManager());
    }

    private System.Collections.IEnumerator InitializeAfterGameManager()
    {
        int waitCount = 0;

        // GameManager의 보드가 초기화될 때까지 대기
        while (gameManager.GetBoard() == null)
        {
            waitCount++;
            if (waitCount > 100) // 무한 루프 방지
            {
                Debug.LogError("[BlockPuzzleUIController] GameManager 초기화 대기 시간 초과!");
                yield break;
            }
            yield return null;
        }

        // 모든 컨트롤러 초기화
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

    /// <summary>
    /// UI 요소 참조 캐싱
    /// </summary>
    private void CacheUIReferences()
    {
        // UI 요소 찾기 (필요시)
        var gameButtons = FindObjectsByType<GameUIButton>(FindObjectsSortMode.None);
        var modeToggle = FindFirstObjectByType<BlockPuzzleModeToggle>();
    }

    /// <summary>
    /// TileGridManager 초기화
    /// </summary>
    private void InitializeTileGridManager()
    {
        tileGridManager = FindFirstObjectByType<TileGridManager>();

        if (tileGridManager == null)
        {
            GameObject tileManagerObj = new GameObject("TileGridManager");
            tileGridManager = tileManagerObj.AddComponent<TileGridManager>();
        }

        tileGridManager.Initialize(gameManager);

        // 각 타일에 UIController 참조 설정 (기존 호환성 유지)
        var tiles = tileGridManager.GetAllTiles();
        if (tiles != null)
        {
            foreach (var tile in tiles)
            {
                tile.SetUIController(this);
            }
        }
    }

    /// <summary>
    /// InventoryController 초기화
    /// </summary>
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

    /// <summary>
    /// GameStateUIManager 초기화
    /// </summary>
    private void InitializeGameStateUIManager()
    {
        gameStateUIManager = FindFirstObjectByType<GameStateUIManager>();

        if (gameStateUIManager == null)
        {
            GameObject stateUIObj = new GameObject("GameStateUIManager");
            gameStateUIManager = stateUIObj.AddComponent<GameStateUIManager>();
        }

        // scoreText를 GameStateUIManager에 전달
        if (scoreText != null)
        {
            gameStateUIManager.SetStateMessageText(scoreText);
        }

        gameStateUIManager.Initialize(gameManager, inventoryController, tileGridManager);
    }

    /// <summary>
    /// GameFlowController 초기화
    /// </summary>
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
        }
    }

    #endregion

    #region UI Update

    /// <summary>
    /// UI 전체 업데이트 (점수, 인벤토리, 보드)
    /// </summary>
    private void UpdateUI()
    {
        var turn = gameManager?.GetCurrentTurn();
        if (turn != null)
        {
            int currentTurnScore = gameManager.GetTotalScore();
            int cumulativeScore = gameManager.GetCumulativeScore();

            currentScoreText.text = $"현재 점수 : {currentTurnScore}";

            // 인벤토리 업데이트 (위임)
            inventoryController?.UpdateInventory();
        }

        UpdateBoard();
    }

    /// <summary>
    /// 보드 시각 업데이트 (TileGridManager에 위임)
    /// </summary>
    private void UpdateBoard()
    {
        tileGridManager?.UpdateAllTiles();
    }

    #endregion

    #region Public API (타일 인터페이스)

    /// <summary>
    /// 타일에서 호출: 선택된 블록을 지정 위치에 배치 시도
    /// GameFlowController에 위임
    /// </summary>
    /// <param name="x">X 좌표</param>
    /// <param name="y">Y 좌표</param>
    /// <returns>배치 성공 여부</returns>
    public bool TryPlaceSelectedBlock(int x, int y)
    {
        if (gameFlowController == null)
        {
            Debug.LogWarning("[BlockPuzzleUIController] GameFlowController가 초기화되지 않았습니다.");
            return false;
        }

        return gameFlowController.TryPlaceSelectedBlock(x, y);
    }

    /// <summary>
    /// 현재 선택된 카드 타입 반환
    /// GameFlowController에 위임
    /// </summary>
    public CardType? GetSelectedCardType() => gameFlowController?.GetSelectedCardType();

    #endregion
}