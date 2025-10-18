using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Events
    public System.Action<TurnData> OnTurnStart;
    public System.Action<int> OnScoreUpdated;
    public System.Action<GameState> OnGameStateChanged;
    public System.Action OnBoardUpdated;
    #endregion

    #region Data Structures
    [System.Serializable]
    public enum BlockType { A, B, C, D, E, F, G }

    [System.Serializable]
    public enum TileMode { NoNumbers, WithNumbers }

    [System.Serializable]
    public enum GameState { Playing, GameOver, Victory }

    [System.Serializable]
    public class Block
    {
        public BlockType type;
        public int baseScore;

        public Block(BlockType type)
        {
            this.type = type;
            this.baseScore = GetBaseScore(type);
        }

        private int GetBaseScore(BlockType type)
        {
            switch (type)
            {
                case BlockType.A: return 2;
                case BlockType.B: return 1;
                case BlockType.C: return 0;
                case BlockType.D: return 1;
                case BlockType.E: return 4;
                case BlockType.F: return 0;
                case BlockType.G: return 5;
                default: return 0;
            }
        }
    }

    [System.Serializable]
    public class Tile
    {
        public int x, y;
        public Block block;
        public int tileNumber;
        public int calculatedScore;
        public int placedTurn; // 블록이 배치된 턴 번호 (0이면 배치 안됨)

        public Tile(int x, int y)
        {
            this.x = x;
            this.y = y;
            this.block = null;
            this.tileNumber = 0;
            this.calculatedScore = 0;
            this.placedTurn = 0;
        }

        public bool IsEmpty => block == null;
        public bool HasBlock => block != null;
        public bool IsRemovable(int currentTurn) => HasBlock && placedTurn == currentTurn;
    }

    [System.Serializable]
    public class TurnData
    {
        public int turnNumber;
        public int targetScore;
        public List<Block> availableBlocks;
        public int currentTurnScore;

        public TurnData(int turnNumber, int targetScore)
        {
            this.turnNumber = turnNumber;
            this.targetScore = targetScore;
            this.availableBlocks = new List<Block>();
            this.currentTurnScore = 0;
        }
    }

    [System.Serializable]
    public class Milestone
    {
        [Tooltip("이 마일스톤을 체크할 턴 번호")]
        public int checkTurn;

        [Tooltip("이 턴까지 달성해야 하는 누적 목표 점수")]
        public int targetScore;

        public Milestone(int turn, int score)
        {
            checkTurn = turn;
            targetScore = score;
        }
    }

    private class GlobalScoreData
    {
        public int emptyTileCount;
        public Dictionary<BlockType, int> blockCounts;
        public int uniqueTypesCount;
        public int uniqueTypesExcludingF;
    }
    #endregion

    #region Fields
    [Header("Turn Settings")]
    [Tooltip("게임의 최대 턴 수 (전체 턴)")]
    public int maxTurns = 20;

    [Tooltip("체크포인트: 특정 턴에 도달 시 누적 점수 체크")]
    public List<Milestone> milestones = new List<Milestone>
    {
        new Milestone(5, 50),    // 5턴까지 50점
        new Milestone(10, 100),  // 10턴까지 100점
        new Milestone(15, 150),  // 15턴까지 150점
        new Milestone(20, 200)   // 20턴까지 200점
    };

    [Header("Mode Settings")]
    public bool useNumbersMode = false; // true면 숫자 모드, false면 숫자 없음 모드

    [Header("Tile Number Distribution (WithNumbers Mode)")]
    [Range(0, 9)] public int maxTileNumber0 = 2;
    [Range(0, 9)] public int maxTileNumber1 = 3;
    [Range(0, 9)] public int maxTileNumber2 = 2;
    [Range(0, 9)] public int maxTileNumber3 = 2;
    [Range(0.1f, 10f)] public float weightTileNumber0 = 1f;
    [Range(0.1f, 10f)] public float weightTileNumber1 = 2f;
    [Range(0.1f, 10f)] public float weightTileNumber2 = 2f;
    [Range(0.1f, 10f)] public float weightTileNumber3 = 1f;

    [Header("Block Supply Settings")]
    [Range(0, 9)] public int minBlockTypes = 3;
    [Range(0, 9)] public int maxBlockTypes = 5;
    [Tooltip("true면 이전 N턴에 사용한(배치한) 블록 타입은 다음 턴에 제외")]
    public bool excludePreviousTurnTypes = false;
    [Tooltip("제외할 이전 턴의 개수 (1 = 바로 직전 턴만, 2 = 최근 2턴, 등)")]
    [Range(1, 10)] public int excludeTurnCount = 2;

    private Tile[,] board;
    private const int BOARD_SIZE = 3;
    private TileMode currentMode = TileMode.NoNumbers; // 내부적으로는 enum 유지
    private GameState gameState = GameState.Playing;
    private TurnData currentTurn;
    private int totalScore = 0;
    private int cumulativeScore = 0; // 게임 전체 누적 점수
    private Queue<HashSet<BlockType>> previousTurnTypesHistory = new Queue<HashSet<BlockType>>(); // 최근 N턴의 블록 타입 기록
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializeBoard();
        StartNewGame();
    }
    #endregion

    #region Board Management
    private void InitializeBoard()
    {
        board = new Tile[BOARD_SIZE, BOARD_SIZE];

        // 먼저 모든 타일 생성
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                board[x, y] = new Tile(x, y);
            }
        }

        // 숫자 모드일 때 모든 타일에 숫자 할당
        if (useNumbersMode)
        {
            AssignInitialTileNumbers();
        }
    }

    private void AssignInitialTileNumbers()
    {
        // 초기화 시에는 가중치를 고려하여 숫자 할당
        List<int> availableNumbers = new List<int>();

        // 각 숫자별 최대 개수만큼 풀에 추가
        for (int i = 0; i < maxTileNumber0; i++) availableNumbers.Add(0);
        for (int i = 0; i < maxTileNumber1; i++) availableNumbers.Add(1);
        for (int i = 0; i < maxTileNumber2; i++) availableNumbers.Add(2);
        for (int i = 0; i < maxTileNumber3; i++) availableNumbers.Add(3);

        // 랜덤하게 섞기
        availableNumbers = availableNumbers.OrderBy(x => Random.value).ToList();

        int index = 0;
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                if (index < availableNumbers.Count)
                {
                    board[x, y].tileNumber = availableNumbers[index];
                    index++;
                }
                else
                {
                    // 풀이 부족하면 랜덤 할당
                    board[x, y].tileNumber = Random.Range(0, 4);
                }
            }
        }
    }

    public bool PlaceBlock(int x, int y, BlockType blockType)
    {
        if (gameState != GameState.Playing) return false;
        if (!IsValidPosition(x, y) || !board[x, y].IsEmpty) return false;

        var availableBlock = currentTurn.availableBlocks.FirstOrDefault(b => b.type == blockType);
        if (availableBlock == null) return false;

        board[x, y].block = availableBlock;
        board[x, y].placedTurn = currentTurn.turnNumber; // 배치된 턴 기록
        currentTurn.availableBlocks.Remove(availableBlock);

        // 숫자 모드일 때는 타일 숫자가 이미 할당되어 있음 (게임 시작 시 할당)
        // 추가 할당 불필요

        UpdateScores();
        return true;
    }

    public bool RemoveBlock(int x, int y)
    {
        if (!IsValidPosition(x, y)) return false;
        if (!board[x, y].HasBlock) return false;

        // 현재 턴에 배치된 블록만 제거 가능
        if (!board[x, y].IsRemovable(currentTurn.turnNumber))
        {
            Debug.Log("이전 턴에 배치된 블록은 제거할 수 없습니다.");
            return false;
        }

        // 블록을 다시 인벤토리에 추가
        var removedBlock = board[x, y].block;
        if (removedBlock != null && currentTurn != null)
        {
            currentTurn.availableBlocks.Add(removedBlock);
        }

        board[x, y].block = null;
        board[x, y].calculatedScore = 0;
        board[x, y].placedTurn = 0; // 턴 정보 초기화
        // 숫자 모드가 아닐 때만 타일 숫자 초기화
        if (!useNumbersMode)
        {
            board[x, y].tileNumber = 0;
        }
        UpdateScores();
        return true;
    }

    private int GenerateRandomTileNumber()
    {
        List<int> pool = new List<int>();

        int count0 = 0, count1 = 0, count2 = 0, count3 = 0;
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                switch (board[x, y].tileNumber)
                {
                    case 0: count0++; break;
                    case 1: count1++; break;
                    case 2: count2++; break;
                    case 3: count3++; break;
                }
            }
        }

        if (count0 < maxTileNumber0)
        {
            for (int i = 0; i < Mathf.RoundToInt(weightTileNumber0 * 10); i++)
                pool.Add(0);
        }
        if (count1 < maxTileNumber1)
        {
            for (int i = 0; i < Mathf.RoundToInt(weightTileNumber1 * 10); i++)
                pool.Add(1);
        }
        if (count2 < maxTileNumber2)
        {
            for (int i = 0; i < Mathf.RoundToInt(weightTileNumber2 * 10); i++)
                pool.Add(2);
        }
        if (count3 < maxTileNumber3)
        {
            for (int i = 0; i < Mathf.RoundToInt(weightTileNumber3 * 10); i++)
                pool.Add(3);
        }

        if (pool.Count == 0)
            return Random.Range(0, 4);

        return pool[Random.Range(0, pool.Count)];
    }

    public void ClearAllBlocks()
    {
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                board[x, y].block = null;
                board[x, y].calculatedScore = 0;
            }
        }
    }

    public List<Tile> GetEmptyTiles()
    {
        List<Tile> emptyTiles = new List<Tile>();
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                if (board[x, y].IsEmpty)
                    emptyTiles.Add(board[x, y]);
            }
        }
        return emptyTiles;
    }

    public List<Tile> GetOccupiedTiles()
    {
        List<Tile> occupiedTiles = new List<Tile>();
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                if (board[x, y].HasBlock)
                    occupiedTiles.Add(board[x, y]);
            }
        }
        return occupiedTiles;
    }

    public List<Tile> GetAdjacentTiles(int x, int y)
    {
        List<Tile> adjacentTiles = new List<Tile>();
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int newX = x + dx[i];
            int newY = y + dy[i];

            if (IsValidPosition(newX, newY))
            {
                adjacentTiles.Add(board[newX, newY]);
            }
        }
        return adjacentTiles;
    }

    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < BOARD_SIZE && y >= 0 && y < BOARD_SIZE;
    }

    private void DecrementAllTileNumbers()
    {
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                if (board[x, y].HasBlock)
                {
                    // 타일 숫자가 0이면 블록 제거 (감소 전 체크)
                    if (board[x, y].tileNumber == 0)
                    {
                        // 블록만 제거하고 인벤토리에 추가하지 않음 (턴 종료 시 소멸)
                        board[x, y].block = null;
                        board[x, y].calculatedScore = 0;
                        board[x, y].placedTurn = 0; // 턴 정보 초기화
                        // 빈 타일이 되면 새로운 숫자 할당
                        board[x, y].tileNumber = GenerateRandomTileNumber();
                    }
                    else
                    {
                        // 블록이 있고 숫자가 1 이상이면 감소
                        board[x, y].tileNumber--;
                    }
                }
                else
                {
                    // 빈 타일: 숫자가 0 이하면 새로 할당
                    if (board[x, y].tileNumber <= 0)
                    {
                        board[x, y].tileNumber = GenerateRandomTileNumber();
                    }
                }
            }
        }
    }
    #endregion

    #region Score Calculation
    public void UpdateScores()
    {
        CalculateAllScores();
        OnScoreUpdated?.Invoke(GetTotalScore());
        OnBoardUpdated?.Invoke();
    }

    private void CalculateAllScores()
    {
        var globalData = CalculateGlobalData();

        for (int x = 0; x < BOARD_SIZE; x++)
        {
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                var tile = board[x, y];
                if (tile.HasBlock)
                {
                    tile.calculatedScore = CalculateTileScore(tile, globalData);
                }
                else
                {
                    tile.calculatedScore = 0;
                }
            }
        }
    }

    private GlobalScoreData CalculateGlobalData()
    {
        var occupiedTiles = GetOccupiedTiles();
        var emptyCount = GetEmptyTiles().Count;

        var blockCounts = new Dictionary<BlockType, int>();
        var uniqueTypes = new HashSet<BlockType>();

        foreach (var tile in occupiedTiles)
        {
            var blockType = tile.block.type;

            if (!blockCounts.ContainsKey(blockType))
                blockCounts[blockType] = 0;
            blockCounts[blockType]++;

            uniqueTypes.Add(blockType);
        }

        return new GlobalScoreData
        {
            emptyTileCount = emptyCount,
            blockCounts = blockCounts,
            uniqueTypesCount = uniqueTypes.Count,
            uniqueTypesExcludingF = uniqueTypes.Where(t => t != BlockType.F).Count()
        };
    }

    private int CalculateTileScore(Tile tile, GlobalScoreData globalData)
    {
        var block = tile.block;
        int score = block.baseScore;

        var adjacentTiles = GetAdjacentTiles(tile.x, tile.y);
        var adjacentBlocks = adjacentTiles.Where(t => t.HasBlock).Select(t => t.block).ToList();

        switch (block.type)
        {
            case BlockType.A:
                int adjacentACount = adjacentBlocks.Count(b => b.type == BlockType.A);
                if (adjacentACount >= 2) score -= 1;
                break;

            case BlockType.B:
                int nonBAdjacentCount = adjacentBlocks.Count(b => b.type != BlockType.B);
                if (nonBAdjacentCount >= 2) score += 1;
                break;

            case BlockType.C:
                int adjacentCCount = adjacentBlocks.Count(b => b.type == BlockType.C);
                score += adjacentCCount;
                break;

            case BlockType.D:
                int adjacentDCount = adjacentBlocks.Count(b => b.type == BlockType.D);
                if (adjacentDCount >= 1 && adjacentDCount <= 2) score += 1;
                else if (adjacentDCount >= 3) score -= 1;
                break;

            case BlockType.E:
                int totalECount = globalData.blockCounts.GetValueOrDefault(BlockType.E, 0);
                score = 4 - ((totalECount - 1) + globalData.emptyTileCount);
                break;

            case BlockType.F:
                int totalFCount = globalData.blockCounts.GetValueOrDefault(BlockType.F, 0);
                // F가 아닌 블록 종류 수 만큼 +1, 다른 F 1개당 -1
                score = globalData.uniqueTypesExcludingF - (totalFCount - 1);
                break;

            case BlockType.G:
                int adjacentBlockCount = adjacentBlocks.Count;
                score += adjacentBlockCount * (-1);

                // 인접한 빈칸 당 -2점 패널티
                int adjacentEmptyCount = adjacentTiles.Count - adjacentBlocks.Count;
                score += adjacentEmptyCount * (-2);
                break;
        }

        return score;
    }

    public int GetTotalScore()
    {
        var occupiedTiles = GetOccupiedTiles();
        totalScore = occupiedTiles.Sum(tile => tile.calculatedScore);
        return totalScore;
    }
    #endregion

    #region Block Supply
    private List<Block> GenerateBlocks()
    {
        var allTypes = System.Enum.GetValues(typeof(BlockType)).Cast<BlockType>().ToList();

        // 이전 N턴 타입 제외 모드가 활성화되어 있으면 제외
        if (excludePreviousTurnTypes && previousTurnTypesHistory.Count > 0)
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

        int typeCount = Random.Range(minBlockTypes, maxBlockTypes + 1);
        typeCount = Mathf.Min(typeCount, allTypes.Count); // 사용 가능한 타입 수보다 많이 선택하지 않도록

        var selectedTypes = allTypes.OrderBy(x => Random.value).Take(typeCount).ToList();

        List<Block> blocks = new List<Block>();

        // 각 선택된 종류마다 최소 1개씩 보장
        foreach (var type in selectedTypes)
        {
            blocks.Add(new Block(type));
        }

        // 나머지는 랜덤하게 채우기 (총 9개)
        while (blocks.Count < 300)
        {
            var randomType = selectedTypes[Random.Range(0, selectedTypes.Count)];
            blocks.Add(new Block(randomType));
        }

        return blocks.OrderBy(x => Random.value).ToList();
    }
    #endregion

    #region Game Flow
    public void StartNewGame()
    {
        gameState = GameState.Playing;
        ClearAllBlocks();
        totalScore = 0;
        cumulativeScore = 0; // 누적 점수 초기화
        previousTurnTypesHistory.Clear(); // 이전 턴 타입 기록 초기화

        // 숫자 모드일 때 모든 타일에 숫자 재할당
        if (useNumbersMode)
        {
            AssignInitialTileNumbers();
        }

        StartTurn(1);
        OnGameStateChanged?.Invoke(gameState);
        OnBoardUpdated?.Invoke(); // UI 업데이트
    }

    public void StartTurn(int turnNumber)
    {
        if (gameState != GameState.Playing) return;

        // 최대 턴 수 체크
        if (turnNumber > maxTurns)
        {
            gameState = GameState.Victory;
            OnGameStateChanged?.Invoke(gameState);
            Debug.Log($"게임 클리어! {maxTurns}턴 완료!");
            return;
        }

        // 다음 마일스톤 찾기 (현재 턴 이후의 가장 가까운 마일스톤)
        Milestone nextMilestone = null;
        foreach (var milestone in milestones)
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

        CalculateAllScores();
        currentTurn.currentTurnScore = GetTotalScore();

        // 이번 턴에 사용된(배치된) 블록 타입 저장 (다음 N턴에서 제외하기 위해)
        if (excludePreviousTurnTypes)
        {
            var currentTurnTypes = new HashSet<BlockType>();

            // 보드에 배치된 블록의 타입만 포함 (사용하지 않은 블록은 제외)
            var occupiedTiles = GetOccupiedTiles();
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
                while (previousTurnTypesHistory.Count > excludeTurnCount)
                {
                    previousTurnTypesHistory.Dequeue();
                }
            }
        }

        // 이번 턴 점수를 누적 점수에 추가
        cumulativeScore += currentTurn.currentTurnScore;

        Debug.Log($"턴 {currentTurn.turnNumber} 종료 - 이번 턴: {currentTurn.currentTurnScore}점, 누적: {cumulativeScore}점");

        // 현재 턴이 마일스톤 체크 턴인지 확인
        Milestone currentMilestone = milestones.Find(m => m.checkTurn == currentTurn.turnNumber);

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
        if (!useNumbersMode)
        {
            // 일반 모드: 매턴 모든 블록 초기화
            ClearAllBlocks();
        }
        else
        {
            // 숫자 모드: 타일 숫자 감소 및 0 이하인 블록만 제거
            // 타일 숫자가 1 이상이면 블록 유지
            DecrementAllTileNumbers();
        }

        UpdateScores();
    }

    public void SetTileMode(bool useNumbers)
    {
        useNumbersMode = useNumbers;
        currentMode = useNumbers ? TileMode.WithNumbers : TileMode.NoNumbers;

        // WithNumbers 모드로 전환 시 기존 블록이 있는 타일에만 숫자 부여
        if (useNumbers)
        {
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                for (int y = 0; y < BOARD_SIZE; y++)
                {
                    if (board[x, y].HasBlock)
                    {
                        board[x, y].tileNumber = GenerateRandomTileNumber();
                    }
                    else
                    {
                        board[x, y].tileNumber = 0;
                    }
                }
            }
        }
        else
        {
            // NoNumbers 모드로 전환 시 모든 숫자 초기화
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                for (int y = 0; y < BOARD_SIZE; y++)
                {
                    board[x, y].tileNumber = 0;
                }
            }
        }

        OnBoardUpdated?.Invoke();
    }
    #endregion

    #region Getters
    public Tile[,] GetBoard() => board;
    public TurnData GetCurrentTurn() => currentTurn;
    public GameState GetGameState() => gameState;
    public TileMode GetTileMode() => currentMode;
    public Tile GetTile(int x, int y) => IsValidPosition(x, y) ? board[x, y] : null;
    public int GetCumulativeScore() => cumulativeScore; // 누적 점수 반환
    #endregion
}