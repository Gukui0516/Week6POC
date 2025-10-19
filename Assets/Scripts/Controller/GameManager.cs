using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCore.Data;

/// <summary>
/// 게임 전체 흐름 조율 - StageManager와 TurnManager를 연결
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();

                if (instance == null)
                {
                    Debug.LogError("[GameManager] 씬에 GameManager가 없습니다!");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.LogWarning("[GameManager] 중복된 GameManager 발견! 파괴합니다.");
            Destroy(gameObject);
        }
    }
    #endregion

    #region Events
    public System.Action<TurnData> OnTurnStart;
    public System.Action<int> OnScoreUpdated;
    public System.Action<GameState> OnGameStateChanged;
    public System.Action OnBoardUpdated;
    #endregion

    #region Dependencies
    [Header("Configuration")]
    [SerializeField] private GameConfig gameConfig;

    private BoardManager boardManager;
    private ScoreCalculator scoreCalculator;
    private TurnManager turnManager;
    private StageManager stageManager;
    #endregion

    #region Game State
    private GameState gameState = GameState.Playing;
    private int cumulativeScore = 0; // 스테이지 내 누적 점수
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        if (gameConfig == null)
        {
            Debug.LogError("[GameManager] GameConfig가 할당되지 않았습니다!");
            return;
        }

        InitializeManagers();
        SubscribeToEvents();

        // 게임 시작
        StartGame();
    }

    private void InitializeManagers()
    {
        if (!gameConfig.IsValid())
        {
            Debug.LogError("[GameManager] GameConfig가 유효하지 않습니다!");
            return;
        }

        // 매니저 초기화
        boardManager = new BoardManager(gameConfig);
        scoreCalculator = new ScoreCalculator(boardManager);
        turnManager = new TurnManager(gameConfig);
        stageManager = StageManager.Instance;

        Debug.Log("[GameManager] 모든 매니저 초기화 완료");
    }

    private void SubscribeToEvents()
    {
        // Board 이벤트
        if (boardManager != null)
        {
            boardManager.OnBoardUpdated += () =>
            {
                scoreCalculator?.UpdateScores();
                OnBoardUpdated?.Invoke();
            };
        }

        // Score 이벤트
        if (scoreCalculator != null)
        {
            scoreCalculator.OnScoreUpdated += (score) => OnScoreUpdated?.Invoke(score);
        }

        // Turn 이벤트
        if (turnManager != null)
        {
            turnManager.OnTurnStart += (turn) => OnTurnStart?.Invoke(turn);
        }

        // Stage 이벤트
        if (stageManager != null)
        {
            stageManager.OnStageStarted += OnStageStarted;
            stageManager.OnStageEnded += OnStageEnded;
        }

        Debug.Log("[GameManager] 모든 이벤트 구독 완료");
    }
    #endregion

    #region Game Flow
    /// <summary>
    /// 게임 시작 (첫 스테이지부터)
    /// </summary>
    public void StartGame()
    {
        Debug.Log("[GameManager] 게임 시작!");
        stageManager.StartStage(1);
    }

    /// <summary>
    /// 새 게임 (재시작)
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("[GameManager] 새 게임 시작!");
        stageManager.RestartFromFirstStage();
    }

    /// <summary>
    /// 스테이지 시작 이벤트 핸들러
    /// </summary>
    private void OnStageStarted(StageSO stage)
    {
        Debug.Log($"[GameManager] 스테이지 {stage.stageId} 시작 처리");

        gameState = GameState.Playing;
        cumulativeScore = 0;

        // 보드 초기화
        boardManager.ClearAllBlocks();
        if (gameConfig.useNumbersMode)
        {
            boardManager.InitializeBoard();
        }

        // TurnManager에 스테이지 설정 및 첫 턴 시작
        turnManager.ResetForStage(stage);
        turnManager.StartNextTurn();

        OnGameStateChanged?.Invoke(gameState);
    }

    /// <summary>
    /// 스테이지 종료 이벤트 핸들러
    /// </summary>
    private void OnStageEnded(StageSO stage, bool isCleared)
    {
        Debug.Log($"[GameManager] 스테이지 {stage.stageId} {(isCleared ? "클리어" : "실패")} 처리");

        if (isCleared)
        {
            gameState = GameState.Victory;
            // 다음 스테이지로 이동하려면: stageManager.MoveToNextStage();
        }
        else
        {
            gameState = GameState.GameOver;
        }

        OnGameStateChanged?.Invoke(gameState);
    }

    /// <summary>
    /// 턴 종료 버튼 클릭
    /// </summary>
    public void EndTurn()
    {
        if (gameState != GameState.Playing) return;

        var turn = turnManager.GetCurrentTurn();
        if (turn == null) return;

        // 점수 계산
        scoreCalculator.CalculateAllScores();
        int turnScore = scoreCalculator.GetTotalScore();
        if(turnScore < 0)
        {
            turnScore = 0;
        }

        // 사용한 블록 타입 수집
        var usedTypes = GetUsedBlockTypes(turn.turnNumber);

        // TurnManager에게 턴 종료 위임
        turnManager.EndTurn(turnScore, usedTypes);

        // 누적 점수 업데이트
        cumulativeScore += turnScore;

        Debug.Log($"[GameManager] 턴 {turn.turnNumber} 종료 - 이번 턴: {turnScore}점, 누적: {cumulativeScore}점");

        // 스테이지 완료 체크
        CheckStageCompletion();
    }

    /// <summary>
    /// 스테이지 완료 조건 체크
    /// </summary>
    private void CheckStageCompletion()
    {
        var stage = turnManager.GetCurrentStage();
        if (stage == null) return;

        // 마지막 턴이면 스테이지 종료
        if (turnManager.IsLastTurn())
        {
            bool isCleared = cumulativeScore >= stage.target;

            Debug.Log($"[GameManager] 스테이지 완료 체크: {cumulativeScore}/{stage.target} → {(isCleared ? "성공" : "실패")}");

            stageManager.EndStage(isCleared);
        }
        else
        {
            // 다음 턴으로
            ProcessTurnEnd();
            turnManager.StartNextTurn();
        }
    }

    /// <summary>
    /// 턴 종료 후 보드 처리
    /// </summary>
    private void ProcessTurnEnd()
    {
        if (!gameConfig.useNumbersMode)
        {
            // 일반 모드: 매턴 모든 블록 초기화
            boardManager.ClearAllBlocks();
        }
        else
        {
            // 숫자 모드: 타일 숫자 감소 및 0 이하인 블록만 제거
            boardManager.DecrementAllTileNumbers();
        }

        scoreCalculator.UpdateScores();
    }

    /// <summary>
    /// 현재 턴에 사용한 블록 타입 수집
    /// </summary>
    private List<CardType> GetUsedBlockTypes(int turnNumber)
    {
        var usedTypes = new List<CardType>();
        var occupiedTiles = boardManager.GetOccupiedTiles();

        foreach (var tile in occupiedTiles)
        {
            if (tile.placedTurn == turnNumber)
            {
                if (!usedTypes.Contains(tile.block.type))
                {
                    usedTypes.Add(tile.block.type);
                }
            }
        }

        return usedTypes;
    }
    #endregion

    #region Block Management
    public bool PlaceBlock(int x, int y, CardType blockType)
    {
        if (gameState != GameState.Playing) return false;
        if (!boardManager.IsValidPosition(x, y)) return false;

        var tile = boardManager.GetTile(x, y);
        if (tile == null || !tile.IsEmpty) return false;

        // TurnManager에서 카드 사용 시도
        if (!turnManager.UseCard(blockType))
        {
            Debug.Log($"블록 타입 {blockType}이(가) 인벤토리에 없습니다.");
            return false;
        }

        var turn = turnManager.GetCurrentTurn();
        if (turn == null)
        {
            // 실패 시 카드 반환
            turnManager.ReturnCard(new Card(blockType));
            return false;
        }

        var block = new Card(blockType);

        // 보드에 배치
        bool success = boardManager.PlaceBlock(x, y, block, turn.turnNumber);

        if (!success)
        {
            // 실패 시 카드 반환
            turnManager.ReturnCard(block);
        }

        return success;
    }

    public bool RemoveBlock(int x, int y)
    {
        if (!boardManager.IsValidPosition(x, y)) return false;

        var tile = boardManager.GetTile(x, y);
        if (tile == null || !tile.HasBlock) return false;

        var turn = turnManager.GetCurrentTurn();
        if (turn == null) return false;

        // 현재 턴에 배치된 블록만 제거 가능
        if (!tile.IsRemovable(turn.turnNumber))
        {
            Debug.Log("이전 턴에 배치된 블록은 제거할 수 없습니다.");
            return false;
        }

        // 블록 정보 저장
        var removedBlock = tile.block;

        // 보드에서 제거
        bool success = boardManager.RemoveBlock(x, y, turn.turnNumber);

        if (success && removedBlock != null)
        {
            // 카드를 인벤토리에 반환
            turnManager.ReturnCard(removedBlock);
        }

        return success;
    }
    #endregion

    #region Mode Management
    public void SetTileMode(bool useNumbers)
    {
        boardManager.SetTileMode(useNumbers);
    }
    #endregion

    #region Getters
    public TurnManager GetTurnManager() => turnManager;
    public StageManager GetStageManager() => stageManager;
    public Tile[,] GetBoard() => boardManager?.GetBoard();
    public TurnData GetCurrentTurn() => turnManager?.GetCurrentTurn();
    public int GetCurrentTurnNumber() => turnManager?.GetCurrentTurnNumber() ?? 0;
    public StageSO GetCurrentStage() => turnManager?.GetCurrentStage();
    public GameState GetGameState() => gameState;
    public TileMode GetTileMode() => gameConfig.useNumbersMode ? TileMode.WithNumbers : TileMode.NoNumbers;
    public Tile GetTile(int x, int y) => boardManager?.GetTile(x, y);
    public int GetCumulativeScore() => cumulativeScore;
    public int GetTotalScore() => scoreCalculator?.GetTotalScore() ?? 0;
    public List<Tile> GetEmptyTiles() => boardManager?.GetEmptyTiles();
    public List<Tile> GetOccupiedTiles() => boardManager?.GetOccupiedTiles();
    public List<Tile> GetAdjacentTiles(int x, int y) => boardManager?.GetAdjacentTiles(x, y);

    // UI에서 필요한 설정값들
    public int GetCurrentStageMaxTurns()
    {
        var stage = turnManager?.GetCurrentStage();
        if (stage != null)
            return stage.endTurn;
        return gameConfig.EndStage;
    }

    // 툴팁용
    public ScoreBreakdown GetScoreBreakdown(int x, int y)
    {
        return scoreCalculator?.GetScoreBreakdown(x, y);
    }

    // 미리보기용
    public BoardPreview GetBoardPreview(int x, int y, CardType blockType)
    {
        if (scoreCalculator == null || !IsValidPosition(x, y)) return null;

        var tile = GetTile(x, y);
        if (tile == null || !tile.IsEmpty) return null;

        return scoreCalculator.CalculateFullBoardPreview(x, y, blockType);
    }

    // 카드 타입의 기본 점수 가져오기 (인벤토리 아이콘 스케일용)
    public int GetCardBaseScore(CardType cardType)
    {
        var card = new Card(cardType);
        return card.baseScore;
    }

    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < GameConfig.BOARD_SIZE && y >= 0 && y < GameConfig.BOARD_SIZE;
    }
    #endregion
}