using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCore.Data;

/// <summary>
/// 턴 관리 및 카드 시스템 (CardManager 사용)
/// </summary>
public class TurnManager
{
    public System.Action<TurnData> OnTurnStart;
    public System.Action OnTurnEnd;

    private TurnData currentTurn;
    private CardManager cardManager;
    private StageSO currentStage;
    private GameConfig gameConfig;

    public TurnManager(GameConfig config)
    {
        gameConfig = config;
        cardManager = new CardManager(config);
    }

    public void SetStage(StageSO stage)
    {
        currentStage = stage;
        cardManager.SetStage(stage);
        Debug.Log($"[TurnManager] 스테이지 {stage.stageId} 설정");
    }

    public void StartTurn(int turnNumber, int targetScore)
    {
        currentTurn = new TurnData(turnNumber, targetScore);

        // 1. 해당 턴에 새로 해금되는 카드 처리
        cardManager.UnlockCardsForTurn(turnNumber);

        // 2. 카드 활성화 (드로우)
        currentTurn.availableBlocks = cardManager.ActivateCardsForTurn(turnNumber);

        OnTurnStart?.Invoke(currentTurn);

        Debug.Log($"턴 {turnNumber} 시작 - 카드 {currentTurn.availableBlocks.Count}장 활성화");
    }

    public void EndTurn(int currentTurnScore, List<CardType> usedBlockTypes)
    {
        if (currentTurn == null) return;

        currentTurn.currentTurnScore = currentTurnScore;

        // 사용한 카드 기록 (다음 턴 제한용)
        cardManager.OnTurnEnd(usedBlockTypes);

        OnTurnEnd?.Invoke();
    }

    /// <summary>
    /// 카드 사용 (배치 시)
    /// </summary>
    public bool UseCard(CardType blockType)
    {
        if (currentTurn == null) return false;

        // CardManager를 통해 카드 사용 시도
        if (!cardManager.TryUseCard(blockType))
        {
            return false;
        }

        // availableBlocks에서도 제거
        var card = currentTurn.availableBlocks.FirstOrDefault(b => b.type == blockType);
        if (card != null)
        {
            currentTurn.availableBlocks.Remove(card);
        }

        return true;
    }

    /// <summary>
    /// 카드 반환 (배치 취소 시)
    /// </summary>
    public void ReturnCard(Card block)
    {
        if (currentTurn != null && block != null)
        {
            currentTurn.availableBlocks.Add(block);
            cardManager.ReturnCard(block.type);
        }
    }

    /// <summary>
    /// 새 게임 시작 (덱 초기화)
    /// </summary>
    public void ResetForNewGame()
    {
        cardManager.ResetDeck();
        currentTurn = null;
    }

    #region Getters
    public TurnData GetCurrentTurn() => currentTurn;
    public CardManager GetCardManager() => cardManager;

    /// <summary>
    /// 특정 카드가 선택 가능한지 여부
    /// </summary>
    public bool CanSelectCard(CardType cardType)
    {
        return cardManager.CanSelectCard(cardType);
    }
    #endregion
}