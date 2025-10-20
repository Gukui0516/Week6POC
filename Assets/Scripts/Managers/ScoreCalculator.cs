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

    // 카드 타입에서 카드 이름 가져오기
    private string GetCardName(CardType cardType)
    {
        var cardData = CardDataLoader.GetData(cardType);
        return cardData != null ? cardData.cardName : cardType.ToString();
    }

    private int CalculateTileScore(Tile tile, GlobalScoreData globalData, List<GameCore.Data.ScoreModifier> modifiers = null)
    {
        bool trackModifiers = (modifiers != null);
        var block = tile.block;
        int score = block.baseScore;

        var adjacentTiles = boardManager.GetAdjacentTiles(tile.x, tile.y);
        var adjacentBlocks = adjacentTiles.Where(t => t.HasBlock).Select(t => t.block).ToList();

        switch (block.type)
        {
            #region Orc
            case CardType.Orc:
                // (-1): 인접한 블록 중, 오크가 있으면
                int adjacentOrcCount = adjacentBlocks.Count(b => b.type == CardType.Orc);
                if (adjacentOrcCount >= 1)
                {
                    score -= 1;
                    if (trackModifiers)
                        modifiers.Add(new GameCore.Data.ScoreModifier(
                            $"인접한 {GetCardName(CardType.Orc)} {adjacentOrcCount}개",
                            -1,
                            $"인접 {GetCardName(CardType.Orc)} 있을 때 -1"
                        ));
                }
                break;
            #endregion

            #region Werewolf
            case CardType.Werewolf:
                // (+1): 인접한 블록 중, 늑대인간이 아닌 블록이 2장 이상이면
                int nonWerewolfCount = adjacentBlocks.Count(b => b.type != CardType.Werewolf);
                if (nonWerewolfCount >= 2)
                {
                    score += 1;
                    if (trackModifiers)
                        modifiers.Add(new GameCore.Data.ScoreModifier(
                            $"{GetCardName(CardType.Werewolf)}가 아닌 인접 블록 {nonWerewolfCount}개",
                            1,
                            "2개 이상일 때 +1"
                        ));
                }
                else if (trackModifiers && nonWerewolfCount > 0)
                {
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        $"{GetCardName(CardType.Werewolf)}가 아닌 인접 블록 {nonWerewolfCount}개",
                        0,
                        "1개 이하일 때 보너스 없음"
                    ));
                }
                break;
            #endregion

            #region Goblin
            case CardType.Goblin:
                // (+2): 인접한 고블린 블록 하나당
                int adjacentGoblinCount = adjacentBlocks.Count(b => b.type == CardType.Goblin);
                int goblinBonus = adjacentGoblinCount * 2;
                score += goblinBonus;
                if (trackModifiers && goblinBonus > 0)
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        $"인접한 {GetCardName(CardType.Goblin)} {adjacentGoblinCount}개",
                        goblinBonus,
                        $"{GetCardName(CardType.Goblin)} 1개당 +2"
                    ));
                break;
            #endregion

            #region Elf
            case CardType.Elf:
                // 유니크: 전장에 있는, 자신과 종족이 같은 블록 하나당 (-1)
                ApplyUniquePenalty(CardType.Elf, globalData, ref score, modifiers, trackModifiers);

                // (-1): 전장에 있는, 빈 타일 하나당
                int emptyPenalty = -globalData.emptyTileCount;
                score += emptyPenalty;
                if (trackModifiers && emptyPenalty != 0)
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        $"빈 타일 {globalData.emptyTileCount}개",
                        emptyPenalty,
                        "빈 타일 1개당 -1"
                    ));
                break;
            #endregion

            #region Dwarf
            case CardType.Dwarf:
                // (+1): 인접한 드워프 블록이 정확히 1개일 때
                int adjacentDwarfCount = adjacentBlocks.Count(b => b.type == CardType.Dwarf);
                if (adjacentDwarfCount == 1)
                {
                    score += 1;
                    if (trackModifiers)
                        modifiers.Add(new GameCore.Data.ScoreModifier(
                            $"인접한 {GetCardName(CardType.Dwarf)} 1개",
                            1,
                            "정확히 1개일 때 +1"
                        ));
                }
                // (-1): 인접한 드워프 블록이 2개 이상일 때
                else if (adjacentDwarfCount >= 2)
                {
                    score -= 1;
                    if (trackModifiers)
                        modifiers.Add(new GameCore.Data.ScoreModifier(
                            $"인접한 {GetCardName(CardType.Dwarf)} {adjacentDwarfCount}개",
                            -1,
                            "2개 이상일 때 -1"
                        ));
                }
                else if (trackModifiers && adjacentDwarfCount == 0)
                {
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        $"인접한 {GetCardName(CardType.Dwarf)} 없음",
                        0,
                        "보너스 없음"
                    ));
                }
                break;
            #endregion

            #region Angel
            case CardType.Angel:
                // (+1): 전장에 있는, 천사가 아닌 블록 종류 하나당
                int angelBonus = globalData.uniqueTypesExcludingF;
                score += angelBonus;
                if (trackModifiers && angelBonus > 0)
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        $"{GetCardName(CardType.Angel)} 제외 블록 종류 {globalData.uniqueTypesExcludingF}개",
                        angelBonus,
                        "종류 1개당 +1"
                    ));

                // 유니크: 전장에 있는, 자신과 종족이 같은 블록 하나당 (-1)
                ApplyUniquePenalty(CardType.Angel, globalData, ref score, modifiers, trackModifiers);
                break;
            #endregion

            #region Dragon
            case CardType.Dragon:
                // (+1): 주위에 있는 타일 하나당 (상하좌우 + 대각선)
                var surroundingTiles = boardManager.GetSurroundingTiles(tile.x, tile.y);
                int surroundingTileCount = surroundingTiles.Count;
                score += surroundingTileCount;
                if (trackModifiers && surroundingTileCount > 0)
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        $"주위 타일 {surroundingTileCount}개",
                        surroundingTileCount,
                        "타일 1개당 +1"
                    ));

                // 유니크: 전장에 있는, 자신과 종족이 같은 블록 하나당 (-1)
                ApplyUniquePenalty(CardType.Dragon, globalData, ref score, modifiers, trackModifiers);

                // (-1): 인접한 블록 하나당 (상하좌우만)
                int adjacentBlockCount = adjacentBlocks.Count;
                int dragonBlockPenalty = -adjacentBlockCount;
                score += dragonBlockPenalty;
                if (trackModifiers && dragonBlockPenalty != 0)
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        $"인접한 블록 {adjacentBlockCount}개",
                        dragonBlockPenalty,
                        "인접 블록 1개당 -1"
                    ));
                break;
            #endregion

            #region Devil
            case CardType.Devil:
                // (+1): 이 가로 줄에, 악마가 없으면 (자신 제외)
                int devilsInRow = GetBlockCountInRow(tile.y, CardType.Devil) - 1; // 자신 제외
                if (devilsInRow == 0)
                {
                    score += 1;
                    if (trackModifiers)
                        modifiers.Add(new GameCore.Data.ScoreModifier(
                            $"이 가로줄에 다른 {GetCardName(CardType.Devil)} 없음",
                            1,
                            $"같은 줄에 {GetCardName(CardType.Devil)} 없을 때 +1"
                        ));
                }
                else if (trackModifiers)
                {
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        $"이 가로줄에 다른 {GetCardName(CardType.Devil)} {devilsInRow}개",
                        0,
                        $"같은 줄에 {GetCardName(CardType.Devil)} 있을 때 보너스 없음"
                    ));
                }
                break;
            #endregion

            #region Vampire
            case CardType.Vampire:
                // (+1): 이 세로 줄에 있는 다른 종족 하나당
                int otherTypesInColumn = GetOtherTypeCountInColumn(tile.x, CardType.Vampire);
                int vampireBonus = otherTypesInColumn;
                score += vampireBonus;
                if (trackModifiers && vampireBonus > 0)
                {
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        $"이 세로줄에 다른 종족 {otherTypesInColumn}개",
                        vampireBonus,
                        "다른 종족 1개당 +1"
                    ));
                }
                else if (trackModifiers)
                {
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        "이 세로줄에 다른 종족 없음",
                        0,
                        "다른 종족 없을 때 보너스 없음"
                    ));
                }
                break;
            #endregion

            #region Naga
            case CardType.Naga:
                // (+2): 주위에 있는 다른 블록 종류 하나당 (상하좌우 + 대각선)
                var surroundingTilesForNaga = boardManager.GetSurroundingTiles(tile.x, tile.y);
                var surroundingBlocks = surroundingTilesForNaga.Where(t => t.HasBlock).Select(t => t.block).ToList();
                var uniqueSurroundingTypes = surroundingBlocks.Select(b => b.type).Distinct().Count();
                int nagaBonus = uniqueSurroundingTypes * 2;
                score += nagaBonus;
                if (trackModifiers && nagaBonus > 0)
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        $"주위 다른 블록 종류 {uniqueSurroundingTypes}개",
                        nagaBonus,
                        "종류 1개당 +2"
                    ));
                break;
            #endregion

            #region Robot
            case CardType.Robot:
                // (+7): 정중앙이 빈 타일이면
                var centerTile = boardManager.GetTile(1, 1); // 3x3 보드의 중앙은 (1,1)
                if (centerTile != null && centerTile.IsEmpty)
                {
                    score += 7;
                    if (trackModifiers)
                        modifiers.Add(new GameCore.Data.ScoreModifier(
                            "정중앙이 빈 타일",
                            7,
                            "중앙이 비어있을 때 +7"
                        ));
                }
                else if (trackModifiers)
                {
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        "정중앙에 블록 있음",
                        0,
                        "중앙이 차있을 때 보너스 없음"
                    ));
                }
                break;
            #endregion

            #region Slime
            case CardType.Slime:
                // (+5): 슬라임 하나당 (자신 포함)
                int totalSlimeCount = globalData.blockCounts.GetValueOrDefault(CardType.Slime, 0);
                int slimeBonus = totalSlimeCount * 5;
                score += slimeBonus;
                if (trackModifiers && slimeBonus > 0)
                    modifiers.Add(new GameCore.Data.ScoreModifier(
                        $"전장의 {GetCardName(CardType.Slime)} {totalSlimeCount}개",
                        slimeBonus,
                        $"{GetCardName(CardType.Slime)} 1개당 +5 (자신 포함)"
                    ));

                // (-10): 같은 턴에 슬라임을 두번 냈으면
                int slimesThisTurn = GetSlimeCountPlacedThisTurn(tile.placedTurn);
                if (slimesThisTurn >= 2)
                {
                    score -= 10;
                    if (trackModifiers)
                        modifiers.Add(new GameCore.Data.ScoreModifier(
                            $"같은 턴에 {GetCardName(CardType.Slime)} {slimesThisTurn}개 배치",
                            -10,
                            "같은 턴에 2개 이상 배치 시 -10"
                        ));
                }
                break;
                #endregion
        }

        return score;
    }

    // 유니크 페널티 계산: 자신을 제외한 전장에 있는 같은 종족 하나당 -1점
    private void ApplyUniquePenalty(CardType cardType, GlobalScoreData globalData, ref int score, List<GameCore.Data.ScoreModifier> modifiers, bool trackModifiers)
    {
        int totalCount = globalData.blockCounts.GetValueOrDefault(cardType, 0);
        int penalty = -(totalCount - 1); // 자신 제외
        score += penalty;

        if (trackModifiers && penalty != 0)
        {
            modifiers.Add(new GameCore.Data.ScoreModifier(
                $"전장에 있는 {GetCardName(cardType)} 블록 하나당 {totalCount - 1}개",
                penalty,
                "같은 종족 1개당 -1"
            ));
        }
    }

    // 가로 줄에서 특정 타입의 블록 개수를 세는 헬퍼 메서드
    private int GetBlockCountInRow(int row, CardType cardType)
    {
        int count = 0;
        var board = boardManager.GetBoard();

        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            var tile = board[x, row];
            if (tile.HasBlock && tile.block.type == cardType)
            {
                count++;
            }
        }

        return count;
    }

    // 가로 줄에 다른 종족이 있는지 확인하는 헬퍼 메서드
    private bool HasOtherTypeInRow(int row, CardType excludeType)
    {
        var board = boardManager.GetBoard();

        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            var tile = board[x, row];
            if (tile.HasBlock && tile.block.type != excludeType)
            {
                return true;
            }
        }

        return false;
    }

    // 세로 줄에서 다른 종족의 개수를 세는 헬퍼 메서드
    private int GetOtherTypeCountInColumn(int column, CardType excludeType)
    {
        int count = 0;
        var board = boardManager.GetBoard();

        for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
        {
            var tile = board[column, y];
            if (tile.HasBlock && tile.block.type != excludeType)
            {
                count++;
            }
        }

        return count;
    }

    // 같은 턴에 배치된 슬라임 개수를 세는 헬퍼 메서드
    private int GetSlimeCountPlacedThisTurn(int turnNumber)
    {
        int count = 0;
        var board = boardManager.GetBoard();

        for (int x = 0; x < GameConfig.BOARD_SIZE; x++)
        {
            for (int y = 0; y < GameConfig.BOARD_SIZE; y++)
            {
                var tile = board[x, y];
                if (tile.HasBlock &&
                    tile.block.type == CardType.Slime &&
                    tile.placedTurn == turnNumber)
                {
                    count++;
                }
            }
        }

        return count;
    }


    // 상세 계산 정보와 함께 점수 계산
    private int CalculateTileScoreWithBreakdown(Tile tile, GlobalScoreData globalData, ScoreBreakdown breakdown)
    {
        var block = tile.block;
        breakdown.baseScore = block.baseScore;
        breakdown.modifiers.Clear();

        // 수정자 추적 리스트 생성
        var modifiers = new List<GameCore.Data.ScoreModifier>();

        // 계산 + 추적을 동시에 수행
        int score = CalculateTileScore(tile, globalData, modifiers);

        // ScoreModifier를 CardData로 변환하여 breakdown에 추가
        foreach (var mod in modifiers)
        {
            breakdown.modifiers.Add(new CardData(
                mod.description,
                mod.value,
                mod.reason
            ));
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
    public BoardPreview CalculateFullBoardPreview(int previewX, int previewY, CardType blockType, int currentTurnNumber = 0)
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
        var originalPlacedTurn = board[previewX, previewY].placedTurn;
        board[previewX, previewY].block = tempBlock;
        board[previewX, previewY].placedTurn = currentTurnNumber;

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
        board[previewX, previewY].placedTurn = originalPlacedTurn;
        CalculateAllScores();

        return preview;
    }
    #endregion
}