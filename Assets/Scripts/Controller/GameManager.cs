using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCore.Data;

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
    #endregion

    #region Game State
    private GameState gameState = GameState.Playing;
    private StageSO currentStage;
    private int cumulativeScore = 0;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {

        if (gameConfig == null)
        {
            Debug.LogError("[GameManager] GameConfig가 할당되지 않았습니다! Inspector에서 GameConfig를 할당하세요.");
            return;
        }


        InitializeManagers();

    }

    private void InitializeManagers()
    {

        // 설정 검증
        if (!gameConfig.IsValid())
        {
            return;
        }

        // 매니저 초기화
        boardManager = new BoardManager(gameConfig);

        scoreCalculator = new ScoreCalculator(boardManager);

        turnManager = new TurnManager(gameConfig);

        // 이벤트 연결
        ConnectEvents();
    }

    private void ConnectEvents()
    {
        if (boardManager != null)
        {
            boardManager.OnBoardUpdated += () =>
            {
                scoreCalculator?.UpdateScores();
                OnBoardUpdated?.Invoke();
            };
        }

        if (scoreCalculator != null)
        {
            scoreCalculator.OnScoreUpdated += (score) => OnScoreUpdated?.Invoke(score);
        }

        if (turnManager != null)
        {
            turnManager.OnTurnStart += (turn) => OnTurnStart?.Invoke(turn);
        }
    }
    #endregion

    #region Game Flow
    // 스테이지 시작 (새로운 방식)
    public void StartStage(StageSO stage)
    {
        if (stage == null)
        {
            Debug.LogError("StageSO가 null입니다!");
            return;
        }

        currentStage = stage;
        gameState = GameState.Playing;

        boardManager.ClearAllBlocks();
        cumulativeScore = 0;

        if (gameConfig.useNumbersMode)
        {
            boardManager.InitializeBoard();
        }

        // 카드 덱 초기화 추가
        turnManager.ResetForNewGame();

        // TurnManager에 스테이지 설정
        turnManager.SetStage(stage);

        // 첫 턴 시작
        StartTurn(1);

        OnGameStateChanged?.Invoke(gameState);

        Debug.Log($"[GameManager] 스테이지 {stage.stageId} 시작! (덱 초기화됨)");
    }

    // 기본 게임 시작 (기존 호환성 유지)
    public void StartNewGame()
    {
        var stage = StageManager.Instance?.GetCurrentStage();
        if (stage != null)
        {
            StartStage(stage);
            return;
        }

        // 스테이지 없이 기존 방식으로 시작
        gameState = GameState.Playing;
        boardManager.ClearAllBlocks();
        cumulativeScore = 0;

        if (gameConfig.useNumbersMode)
        {
            boardManager.InitializeBoard();
        }

        // 카드 덱 초기화 추가
        turnManager.ResetForNewGame();

        StartTurn(1);
        OnGameStateChanged?.Invoke(gameState);

        Debug.Log("[GameManager] 기본 게임 시작! (덱 초기화됨)");
    }

    public void StartTurn(int turnNumber)
    {
        if (gameState != GameState.Playing) return;

        // 최대 턴 수 체크
        if (currentStage != null)
        {
            // 스테이지 모드
            if (turnNumber > currentStage.endTurn)
            {
                gameState = GameState.Victory;
                OnGameStateChanged?.Invoke(gameState);
                Debug.Log($"게임 클리어! {currentStage.endTurn}턴 완료!");
                return;
            }
        }
        else
        {
            // 기존 모드
            if (turnNumber > gameConfig.EndStage)
            {
                gameState = GameState.Victory;
                OnGameStateChanged?.Invoke(gameState);
                return;
            }
        }

        // 목표 점수 결정
        int targetScore = GetTargetScore(turnNumber);

        // TurnManager에게 턴 시작 위임
        turnManager.StartTurn(turnNumber, targetScore);
    }

    private int GetTargetScore(int turnNumber)
    {
        return currentStage.target;
    }

    public void EndTurn()
    {
        if (gameState != GameState.Playing) return;

        var turn = turnManager.GetCurrentTurn();
        if (turn == null) return;

        scoreCalculator.CalculateAllScores();
        int turnScore = scoreCalculator.GetTotalScore();

        // 사용한 블록 타입 수집
        var usedTypes = GetUsedBlockTypes(turn.turnNumber);

        // TurnManager에게 턴 종료 위임
        turnManager.EndTurn(turnScore, usedTypes);

        // 누적 점수 업데이트
        cumulativeScore += turnScore;

        Debug.Log($"턴 {turn.turnNumber} 종료 - 이번 턴: {turnScore}점, 누적: {cumulativeScore}점");

        CheckStageMilestone(turn.turnNumber);

    }

    private void CheckStageMilestone(int turnNumber)
    {
        // 스테이지 모드: endTurn 체크
        if (turnNumber >= currentStage.endTurn)
        {
            if (cumulativeScore >= currentStage.target)
            {
                Debug.Log($"✅ 목표 달성! ({cumulativeScore}/{currentStage.target})");
                gameState = GameState.Victory;
                StageManager.Instance?.EndStage(true);
            }
            else
            {
                Debug.Log($"❌ 목표 미달성! ({cumulativeScore}/{currentStage.target})");
                gameState = GameState.GameOver;
                StageManager.Instance?.EndStage(false);
            }

            OnGameStateChanged?.Invoke(gameState);
        }
        else
        {
            ProcessTurnEnd();
            StartTurn(turnNumber + 1);
        }
    }



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
    public Tile[,] GetBoard() => boardManager?.GetBoard();
    public TurnData GetCurrentTurn() => turnManager?.GetCurrentTurn();
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
        if (currentStage != null)
            return currentStage.endTurn;
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

    // 현재 스테이지 정보 (추가)
    public StageSO GetCurrentStage() => currentStage;
    #endregion
}