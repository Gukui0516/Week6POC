using System.Collections.Generic;
using System.Linq;
using GameCore.Data;

public class ScoreCalculator
{
    public System.Action<int> OnScoreUpdated;

    private BoardManager boardManager;
    private int totalScore = 0;

    public ScoreCalculator(BoardManager boardManager)
    {
        this.boardManager = boardManager;
    }

    #region Score Calculation
    public void UpdateScores()
    {
        CalculateAllScores();
        OnScoreUpdated?.Invoke(GetTotalScore());
    }

    public void CalculateAllScores()
    {
        var globalData = CalculateGlobalData();
        var board = boardManager.GetBoard();

        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
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
        var occupiedTiles = boardManager.GetOccupiedTiles();
        var emptyCount = boardManager.GetEmptyTiles().Count;

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

        var adjacentTiles = boardManager.GetAdjacentTiles(tile.x, tile.y);
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
        var occupiedTiles = boardManager.GetOccupiedTiles();
        totalScore = occupiedTiles.Sum(tile => tile.calculatedScore);
        return totalScore;
    }
    #endregion
}