using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCore.Data;

public class BoardManager
{
    public System.Action OnBoardUpdated;

    private Tile[,] board;
    private GameConfig config;

    public BoardManager(GameConfig gameConfig)
    {
        config = gameConfig;
        InitializeBoard();
    }

    #region Board Initialization
    public void InitializeBoard()
    {
        board = new Tile[GameConfig.BOARD_SIZE, GameConfig.BOARD_SIZE];

        // 먼저 모든 타일 생성
        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
            {
                board[x, y] = new Tile(x, y);
            }
        }

        // 숫자 모드일 때 모든 타일에 숫자 할당
        if (config.useNumbersMode)
        {
            AssignInitialTileNumbers();
        }
    }

    private void AssignInitialTileNumbers()
    {
        // 초기화 시에는 가중치를 고려하여 숫자 할당
        List<int> availableNumbers = new List<int>();

        // 각 숫자별 최대 개수만큼 풀에 추가
        for (int i = 0; i < config.maxTileNumber0; i++) availableNumbers.Add(0);
        for (int i = 0; i < config.maxTileNumber1; i++) availableNumbers.Add(1);
        for (int i = 0; i < config.maxTileNumber2; i++) availableNumbers.Add(2);
        for (int i = 0; i < config.maxTileNumber3; i++) availableNumbers.Add(3);

        // 랜덤하게 섞기
        availableNumbers = availableNumbers.OrderBy(x => Random.value).ToList();

        int index = 0;
        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
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
    #endregion

    #region Block Placement
    public bool PlaceBlock(int x, int y, Card block, int currentTurn)
    {
        if (!IsValidPosition(x, y) || !board[x, y].IsEmpty) return false;

        board[x, y].block = block;
        board[x, y].placedTurn = currentTurn; // 배치된 턴 기록

        OnBoardUpdated?.Invoke();
        return true;
    }

    public bool RemoveBlock(int x, int y, int currentTurn)
    {
        if (!IsValidPosition(x, y)) return false;
        if (!board[x, y].HasBlock) return false;

        // 현재 턴에 배치된 블록만 제거 가능
        if (!board[x, y].IsRemovable(currentTurn))
        {
            Debug.Log("이전 턴에 배치된 블록은 제거할 수 없습니다.");
            return false;
        }

        var removedBlock = board[x, y].block;
        board[x, y].block = null;
        board[x, y].calculatedScore = 0;
        board[x, y].placedTurn = 0; // 턴 정보 초기화

        // 숫자 모드가 아닐 때만 타일 숫자 초기화
        if (!config.useNumbersMode)
        {
            board[x, y].tileNumber = 0;
        }

        OnBoardUpdated?.Invoke();
        return true;
    }
    #endregion

    #region Board Operations
    public void ClearAllBlocks()
    {
        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
            {
                board[x, y].block = null;
                board[x, y].calculatedScore = 0;
                board[x, y].placedTurn = 0;
            }
        }
        OnBoardUpdated?.Invoke();
    }

    public void DecrementAllTileNumbers()
    {
        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
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
        OnBoardUpdated?.Invoke();
    }

    private int GenerateRandomTileNumber()
    {
        List<int> pool = new List<int>();

        int count0 = 0, count1 = 0, count2 = 0, count3 = 0;
        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
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

        if (count0 < config.maxTileNumber0)
        {
            for (int i = 0; i < Mathf.RoundToInt(config.weightTileNumber0 * 10); i++)
                pool.Add(0);
        }
        if (count1 < config.maxTileNumber1)
        {
            for (int i = 0; i < Mathf.RoundToInt(config.weightTileNumber1 * 10); i++)
                pool.Add(1);
        }
        if (count2 < config.maxTileNumber2)
        {
            for (int i = 0; i < Mathf.RoundToInt(config.weightTileNumber2 * 10); i++)
                pool.Add(2);
        }
        if (count3 < config.maxTileNumber3)
        {
            for (int i = 0; i < Mathf.RoundToInt(config.weightTileNumber3 * 10); i++)
                pool.Add(3);
        }

        if (pool.Count == 0)
            return Random.Range(0, 4);

        return pool[Random.Range(0, pool.Count)];
    }
    #endregion

    #region Tile Queries
    public List<Tile> GetEmptyTiles()
    {
        List<Tile> emptyTiles = new List<Tile>();
        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
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
        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
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

    public bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < GameConfig.BOARD_SIZE && y >= 0 && y < GameConfig.BOARD_SIZE;
    }
    #endregion

    #region Mode Management
    public void SetTileMode(bool useNumbers)
    {
        config.useNumbersMode = useNumbers;

        // WithNumbers 모드로 전환 시 기존 블록이 있는 타일에만 숫자 부여
        if (useNumbers)
        {
            for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
            {
                for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
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
            for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
            {
                for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
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
    public Tile GetTile(int x, int y) => IsValidPosition(x, y) ? board[x, y] : null;
    #endregion
}