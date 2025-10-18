using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCore.Data;

public class GameManager : MonoBehaviour
{
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
    #endregion

    #region Game State
    private GameState gameState = GameState.Playing;
    private TurnData currentTurn;
    private int cumulativeScore = 0; // 게임 전체 누적 점수
    private Queue<HashSet<BlockType>> previousTurnTypesHistory = new Queue<HashSet<BlockType>>(); // 최근 N턴의 블록 타입 기록
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        Debug.Log("[GameManager] Start 메소드 시작");

        if (gameConfig == null)
        {
            Debug.LogError("[GameManager] GameConfig가 할당되지 않았습니다! Inspector에서 GameConfig를 할당하세요.");
            return;
        }

        Debug.Log("[GameManager] GameConfig 할당 확인됨");

        InitializeManagers();
        StartNewGame();

        Debug.Log("[GameManager] 초기화 완료");
    }

    private void InitializeManagers()
    {
        Debug.Log("[GameManager] 매니저 초기화 시작");

        // 설정 검증
        if (!gameConfig.IsValid())
        {
            Debug.LogError("[GameManager] GameConfig가 유효하지 않습니다!");
            return;
        }

        // 매니저 초기화
        boardManager = new BoardManager(gameConfig);
        Debug.Log("[GameManager] BoardManager 생성 완료");

        scoreCalculator = new ScoreCalculator(boardManager);
        Debug.Log("[GameManager] ScoreCalculator 생성 완료");

        // 이벤트 연결
        ConnectEvents();
        Debug.Log("[GameManager] 이벤트 연결 완료");
    }

    // private void Start()
    // {
    //     InitializeManagers();
    //     StartNewGame();
    // }

    // private void InitializeManagers()
    // {
    //     // 설정 검증
    //     if (gameConfig == null || !gameConfig.IsValid())
    //     {
    //         Debug.LogError("GameConfig가 없거나 유효하지 않습니다!");
    //         return;
    //     }

    //     // 매니저 초기화
    //     boardManager = new BoardManager(gameConfig);
    //     scoreCalculator = new ScoreCalculator(boardManager);

    //     // 이벤트 연결
    //     ConnectEvents();
    // }

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
    }
    #endregion

    #region Game Flow
    public void StartNewGame()
    {
        gameState = GameState.Playing;
        boardManager.ClearAllBlocks();
        cumulativeScore = 0; // 누적 점수 초기화
        previousTurnTypesHistory.Clear(); // 이전 턴 타입 기록 초기화

        // 숫자 모드일 때 모든 타일에 숫자 재할당
        if (gameConfig.useNumbersMode)
        {
            boardManager.InitializeBoard();
        }

        StartTurn(1);
        OnGameStateChanged?.Invoke(gameState);
    }

    public void StartTurn(int turnNumber)
    {
        if (gameState != GameState.Playing) return;

        // 최대 턴 수 체크
        if (turnNumber > gameConfig.maxTurns)
        {
            gameState = GameState.Victory;
            OnGameStateChanged?.Invoke(gameState);
            Debug.Log($"게임 클리어! {gameConfig.maxTurns}턴 완료!");
            return;
        }

        // 다음 마일스톤 찾기 (현재 턴 이후의 가장 가까운 마일스톤)
        Milestone nextMilestone = null;
        foreach (var milestone in gameConfig.milestones)
        {
            if (milestone.checkTurn >= turnNumber)
            {
                nextMilestone = milestone;
                break;
            }
        }

        // 마일스톤이 없으면 기본값
        int targetScore = nextMilestone != null ? nextMilestone.targetScore : 100;

        currentTurn = new TurnData(turnNumber, targetScore);
        currentTurn.availableBlocks = GenerateBlocks();

        OnTurnStart?.Invoke(currentTurn);
    }

    public void EndTurn()
    {
        if (gameState != GameState.Playing) return;

        scoreCalculator.CalculateAllScores();
        currentTurn.currentTurnScore = scoreCalculator.GetTotalScore();

        // 이번 턴에 사용된(배치된) 블록 타입 저장 (다음 N턴에서 제외하기 위해)
        if (gameConfig.excludePreviousTurnTypes)
        {
            var currentTurnTypes = new HashSet<BlockType>();

            // 보드에 배치된 블록의 타입만 포함 (사용하지 않은 블록은 제외)
            var occupiedTiles = boardManager.GetOccupiedTiles();
            foreach (var tile in occupiedTiles)
            {
                if (tile.placedTurn == currentTurn.turnNumber)
                {
                    currentTurnTypes.Add(tile.block.type);
                }
            }

            // 사용한 블록이 있을 때만 큐에 추가
            if (currentTurnTypes.Count > 0)
            {
                // 큐에 추가
                previousTurnTypesHistory.Enqueue(currentTurnTypes);

                // 설정된 턴 수를 초과하면 가장 오래된 기록 제거
                while (previousTurnTypesHistory.Count > gameConfig.excludeTurnCount)
                {
                    previousTurnTypesHistory.Dequeue();
                }
            }
        }

        // 이번 턴 점수를 누적 점수에 추가
        cumulativeScore += currentTurn.currentTurnScore;

        Debug.Log($"턴 {currentTurn.turnNumber} 종료 - 이번 턴: {currentTurn.currentTurnScore}점, 누적: {cumulativeScore}점");

        // 현재 턴이 마일스톤 체크 턴인지 확인
        Milestone currentMilestone = gameConfig.milestones.Find(m => m.checkTurn == currentTurn.turnNumber);

        if (currentMilestone != null)
        {
            // 마일스톤 턴: 누적 점수 체크
            Debug.Log($"[마일스톤 체크] {currentTurn.turnNumber}턴 - 목표: {currentMilestone.targetScore}점, 달성: {cumulativeScore}점");

            if (cumulativeScore >= currentMilestone.targetScore)
            {
                Debug.Log($"✅ 마일스톤 통과!");
                ProcessTurnEnd();
                StartTurn(currentTurn.turnNumber + 1);
            }
            else
            {
                Debug.Log($"❌ 마일스톤 실패! 게임 오버");
                gameState = GameState.GameOver;
                OnGameStateChanged?.Invoke(gameState);
            }
        }
        else
        {
            // 일반 턴: 그냥 다음 턴으로
            ProcessTurnEnd();
            StartTurn(currentTurn.turnNumber + 1);
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
            // 타일 숫자가 1 이상이면 블록 유지
            boardManager.DecrementAllTileNumbers();
        }

        scoreCalculator.UpdateScores();
    }
    #endregion

    #region Block Management
    public bool PlaceBlock(int x, int y, BlockType blockType)
    {
        if (gameState != GameState.Playing) return false;
        if (!boardManager.IsValidPosition(x, y)) return false;

        var tile = boardManager.GetTile(x, y);
        if (tile == null || !tile.IsEmpty) return false;

        var availableBlock = currentTurn.availableBlocks.FirstOrDefault(b => b.type == blockType);
        if (availableBlock == null) return false;

        // 블록 배치
        bool success = boardManager.PlaceBlock(x, y, availableBlock, currentTurn.turnNumber);
        if (success)
        {
            currentTurn.availableBlocks.Remove(availableBlock);
            return true;
        }

        return false;
    }

    public bool RemoveBlock(int x, int y)
    {
        if (!boardManager.IsValidPosition(x, y)) return false;

        var tile = boardManager.GetTile(x, y);
        if (tile == null || !tile.HasBlock) return false;

        // 현재 턴에 배치된 블록만 제거 가능
        if (!tile.IsRemovable(currentTurn.turnNumber))
        {
            Debug.Log("이전 턴에 배치된 블록은 제거할 수 없습니다.");
            return false;
        }

        // 블록을 다시 인벤토리에 추가
        var removedBlock = tile.block;
        if (removedBlock != null && currentTurn != null)
        {
            currentTurn.availableBlocks.Add(removedBlock);
        }

        return boardManager.RemoveBlock(x, y, currentTurn.turnNumber);
    }

    private List<Block> GenerateBlocks()
    {
        var allTypes = System.Enum.GetValues(typeof(BlockType)).Cast<BlockType>().ToList();

        // 이전 N턴 타입 제외 모드가 활성화되어 있으면 제외
        if (gameConfig.excludePreviousTurnTypes && previousTurnTypesHistory.Count > 0)
        {
            // 최근 N턴의 모든 타입을 합쳐서 제외 목록 생성
            var excludedTypes = new HashSet<BlockType>();
            foreach (var turnTypes in previousTurnTypesHistory)
            {
                foreach (var type in turnTypes)
                {
                    excludedTypes.Add(type);
                }
            }

            allTypes = allTypes.Where(t => !excludedTypes.Contains(t)).ToList();

            // 모든 타입이 제외되면 다시 전체 사용
            if (allTypes.Count == 0)
            {
                allTypes = System.Enum.GetValues(typeof(BlockType)).Cast<BlockType>().ToList();
            }
        }

        int typeCount = Random.Range(gameConfig.minBlockTypes, gameConfig.maxBlockTypes + 1);
        typeCount = Mathf.Min(typeCount, allTypes.Count); // 사용 가능한 타입 수보다 많이 선택하지 않도록

        var selectedTypes = allTypes.OrderBy(x => Random.value).Take(typeCount).ToList();

        List<Block> blocks = new List<Block>();

        // 각 선택된 종류마다 최소 1개씩 보장
        foreach (var type in selectedTypes)
        {
            blocks.Add(new Block(type));
        }

        // 나머지는 랜덤하게 채우기 (총 9개)
        while (blocks.Count < 9)
        {
            var randomType = selectedTypes[Random.Range(0, selectedTypes.Count)];
            blocks.Add(new Block(randomType));
        }

        return blocks.OrderBy(x => Random.value).ToList();
    }
    #endregion

    #region Mode Management
    public void SetTileMode(bool useNumbers)
    {
        boardManager.SetTileMode(useNumbers);
    }
    #endregion

    #region Getters
    public Tile[,] GetBoard() => boardManager.GetBoard();
    public TurnData GetCurrentTurn() => currentTurn;
    public GameState GetGameState() => gameState;
    public TileMode GetTileMode() => gameConfig.useNumbersMode ? TileMode.WithNumbers : TileMode.NoNumbers;
    public Tile GetTile(int x, int y) => boardManager.GetTile(x, y);
    public int GetCumulativeScore() => cumulativeScore; // 누적 점수 반환
    public int GetTotalScore() => scoreCalculator.GetTotalScore(); // 현재 턴 점수 반환
    public List<Tile> GetEmptyTiles() => boardManager.GetEmptyTiles();
    public List<Tile> GetOccupiedTiles() => boardManager.GetOccupiedTiles();
    public List<Tile> GetAdjacentTiles(int x, int y) => boardManager.GetAdjacentTiles(x, y);

    // UI에서 필요한 설정값들을 위한 헬퍼 메소드들
    public int GetMaxTurns() => gameConfig.maxTurns;
    public System.Collections.Generic.List<Milestone> GetMilestones() => gameConfig.milestones;
    public Milestone GetNextMilestone(int currentTurn)
    {
        foreach (var milestone in gameConfig.milestones)
        {
            if (milestone.checkTurn >= currentTurn)
            {
                return milestone;
            }
        }
        return null;
    }
    #endregion
}