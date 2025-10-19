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

    // 새로운 메서드: 점수 계산 상세 정보 반환
    public ScoreBreakdown GetScoreBreakdown(int x, int y)
    {
        var tile = boardManager.GetTile(x, y);
        if (tile == null || !tile.HasBlock) return null;

        var breakdown = new ScoreBreakdown(tile.block.type);
        var globalData = CalculateGlobalData();

        CalculateTileScoreWithBreakdown(tile, globalData, breakdown);
        return breakdown;
    }

    private GlobalScoreData CalculateGlobalData()
    {
        var occupiedTiles = boardManager.GetOccupiedTiles();
        var emptyCount = boardManager.GetEmptyTiles().Count;

        var blockCounts = new Dictionary<CardType, int>();
        var uniqueTypes = new HashSet<CardType>();

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
            uniqueTypesExcludingF = uniqueTypes.Where(t => t != CardType.Angel).Count()
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
            case CardType.Orc:
                int adjacentACount = adjacentBlocks.Count(b => b.type == CardType.Orc);
                if (adjacentACount >= 2) score -= 1;
                break;

            case CardType.Werewolf:
                int nonBAdjacentCount = adjacentBlocks.Count(b => b.type != CardType.Werewolf);
                if (nonBAdjacentCount >= 2) score += 1;
                break;

            case CardType.Goblin:
                int adjacentCCount = adjacentBlocks.Count(b => b.type == CardType.Goblin);
                score += adjacentCCount;
                break;

            case CardType.Elf:
                int adjacentDCount = adjacentBlocks.Count(b => b.type == CardType.Elf);
                if (adjacentDCount >= 1 && adjacentDCount <= 2) score += 1;
                else if (adjacentDCount >= 3) score -= 1;
                break;

            case CardType.Dwarf:
                int totalECount = globalData.blockCounts.GetValueOrDefault(CardType.Dwarf, 0);
                score = 4 - ((totalECount - 1) + globalData.emptyTileCount);
                break;

            case CardType.Angel:
                int totalFCount = globalData.blockCounts.GetValueOrDefault(CardType.Angel, 0);
                score = globalData.uniqueTypesExcludingF - (totalFCount - 1);
                break;

            case CardType.Dragon:
                int adjacentBlockCount = adjacentBlocks.Count;
                score += adjacentBlockCount * (-1);

                int adjacentEmptyCount = adjacentTiles.Count - adjacentBlocks.Count;
                score += adjacentEmptyCount * (-2);
                break;
        }

        return score;
    }

    // 상세 계산 정보와 함께 점수 계산
    private int CalculateTileScoreWithBreakdown(Tile tile, GlobalScoreData globalData, ScoreBreakdown breakdown)
    {
        var block = tile.block;
        breakdown.baseScore = block.baseScore;
        breakdown.modifiers.Clear();

        // 실제 점수는 기존 메서드로 계산
        int score = CalculateTileScore(tile, globalData);

        var adjacentTiles = boardManager.GetAdjacentTiles(tile.x, tile.y);
        var adjacentBlocks = adjacentTiles.Where(t => t.HasBlock).Select(t => t.block).ToList();

        switch (block.type)
        {
            case CardType.Orc:
                int adjacentACount = adjacentBlocks.Count(b => b.type == CardType.Orc);
                if (adjacentACount >= 2)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"인접한 Orc 블록 {adjacentACount}개",
                        -1,
                        "2개 이상일 때 -1"
                    ));
                }
                else if (adjacentACount > 0)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"인접한 Orc 블록 {adjacentACount}개",
                        0,
                        "1개 이하일 때 보너스 없음"
                    ));
                }
                break;

            case CardType.Werewolf:
                int nonBAdjacentCount = adjacentBlocks.Count(b => b.type != CardType.Werewolf);
                if (nonBAdjacentCount >= 2)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"Werewolf가 아닌 인접 블록 {nonBAdjacentCount}개",
                        1,
                        "2개 이상일 때 +1"
                    ));
                }
                else if (nonBAdjacentCount > 0)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"Werewolf가 아닌 인접 블록 {nonBAdjacentCount}개",
                        0,
                        "1개 이하일 때 보너스 없음"
                    ));
                }
                break;

            case CardType.Goblin:
                int adjacentCCount = adjacentBlocks.Count(b => b.type == CardType.Goblin);
                if (adjacentCCount > 0)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"인접한 Goblin 블록 {adjacentCCount}개",
                        adjacentCCount,
                        "Goblin 1개당 +1"
                    ));
                }
                break;

            case CardType.Elf:
                int adjacentDCount = adjacentBlocks.Count(b => b.type == CardType.Elf);
                if (adjacentDCount >= 1 && adjacentDCount <= 2)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"인접한 Elf 블록 {adjacentDCount}개",
                        1,
                        "1~2개일 때 +1"
                    ));
                }
                else if (adjacentDCount >= 3)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"인접한 Elf 블록 {adjacentDCount}개",
                        -1,
                        "3개 이상일 때 -1"
                    ));
                }
                else if (adjacentDCount == 0)
                {
                    breakdown.modifiers.Add(new CardData(
                        "인접한 D 블록 없음",
                        0,
                        "0개일 때 보너스 없음"
                    ));
                }
                break;

            case CardType.Dwarf:
                int totalECount = globalData.blockCounts.GetValueOrDefault(CardType.Dwarf, 0);
                int otherECount = totalECount - 1;

                if (otherECount > 0)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"다른 Dwarf 블록 {otherECount}개",
                        -otherECount,
                        "다른 Dwarf 1개당 -1"
                    ));
                }

                if (globalData.emptyTileCount > 0)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"빈 타일 {globalData.emptyTileCount}개",
                        -globalData.emptyTileCount,
                        "빈 타일 1개당 -1"
                    ));
                }
                break;

            case CardType.Angel:
                int totalFCount = globalData.blockCounts.GetValueOrDefault(CardType.Angel, 0);
                int otherFCount = totalFCount - 1;

                if (globalData.uniqueTypesExcludingF > 0)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"Angel 제외 블록 종류 {globalData.uniqueTypesExcludingF}개",
                        globalData.uniqueTypesExcludingF,
                        "종류 1개당 +1"
                    ));
                }

                if (otherFCount > 0)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"다른 Angel 블록 {otherFCount}개",
                        -otherFCount,
                        "다른 Angel 1개당 -1"
                    ));
                }
                break;

            case CardType.Dragon:
                int adjacentBlockCount = adjacentBlocks.Count;
                int adjacentEmptyCount = adjacentTiles.Count - adjacentBlocks.Count;

                if (adjacentBlockCount > 0)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"인접한 블록 {adjacentBlockCount}개",
                        -adjacentBlockCount,
                        "인접 블록 1개당 -1"
                    ));
                }

                if (adjacentEmptyCount > 0)
                {
                    breakdown.modifiers.Add(new CardData(
                        $"인접한 빈칸 {adjacentEmptyCount}개",
                        -adjacentEmptyCount * 2,
                        "인접 빈칸 1개당 -2"
                    ));
                }
                break;
        }

        breakdown.finalScore = score;
        return score;
    }

    public int GetTotalScore()
    {
        var occupiedTiles = boardManager.GetOccupiedTiles();
        totalScore = occupiedTiles.Sum(tile => tile.calculatedScore);
        return totalScore;
    }

    // 미리보기: 특정 위치에 블록을 배치했을 때 전체 보드의 점수 변화 계산
    public BoardPreview CalculateFullBoardPreview(int previewX, int previewY, CardType blockType)
    {
        var preview = new BoardPreview();
        preview.previewX = previewX;
        preview.previewY = previewY;
        preview.previewBlockType = blockType;

        var board = boardManager.GetBoard();

        // 1. 현재 점수들 저장
        int totalOriginal = 0;
        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
            {
                preview.originalScores[x, y] = board[x, y].calculatedScore;
                totalOriginal += board[x, y].calculatedScore;
            }
        }

        // 2. 임시로 블록 배치
        var tempBlock = new Card(blockType);
        var originalBlock = board[previewX, previewY].block;
        board[previewX, previewY].block = tempBlock;

        // 3. 전체 점수 재계산
        CalculateAllScores();

        // 4. 새로운 점수들 저장
        int totalPreview = 0;
        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
            {
                preview.previewScores[x, y] = board[x, y].calculatedScore;
                totalPreview += board[x, y].calculatedScore;
            }
        }

        preview.totalScoreChange = totalPreview - totalOriginal;

        // 5. 원래 상태로 복원
        board[previewX, previewY].block = originalBlock;
        CalculateAllScores();

        return preview;
    }
    #endregion
}