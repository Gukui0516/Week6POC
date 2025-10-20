using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCore.Data;

/// <summary>
/// 턴 생명주기 관리 전담 (스테이지 내부)
/// Turn 1 → Turn 2 → Turn 3... (within a stage)
/// </summary>
public class TurnManager
{
    public System.Action<TurnData> OnTurnStart;
    public System.Action OnTurnEnd;

    private int currentTurnNumber = 0;
    private TurnData currentTurn;
    private CardManager cardManager;
    private StageSO currentStage;
    private GameConfig gameConfig;

    public TurnManager(GameConfig config)
    {
        gameConfig = config;
        cardManager = new CardManager(config);
    }

    /// <summary>
    /// 새 스테이지 시작 시 턴 초기화
    /// </summary>
    public void ResetForStage(StageSO stage)
    {
        currentStage = stage;
        currentTurnNumber = 0;
        currentTurn = null;

        // 첫 스테이지일 때만 덱 리셋
        if (stage.stageId == 1)
        {
            cardManager.ResetDeck();
            Debug.Log($"[TurnManager] 첫 스테이지 - 덱 초기화");
        }
        else
        {
            // 이후 스테이지는 덱 유지, 히스토리만 클리어
            Debug.Log($"[TurnManager] 이전 스테이지 덱 유지 - 보유 카드: {string.Join(", ", cardManager.GetOwnedTypes())}");
        }

        cardManager.SetStage(stage);

        Debug.Log($"[TurnManager] 스테이지 {stage.stageId}에 맞춰 턴 초기화 (턴 번호: 0)");
    }

    /// <summary>
    /// 다음 턴 시작 (턴 번호 자동 증가)
    /// </summary>
    public void StartNextTurn()
    {
        if (currentStage == null)
        {
            Debug.LogError("[TurnManager] 스테이지가 설정되지 않았습니다!");
            return;
        }

        currentTurnNumber++;

        // 스테이지 최대 턴 체크
        if (currentTurnNumber > currentStage.endTurn)
        {
            Debug.LogWarning($"[TurnManager] 스테이지 최대 턴({currentStage.endTurn})을 초과했습니다!");
            return;
        }

        // 턴 데이터 생성
        int targetScore = currentStage.target;
        currentTurn = new TurnData(currentTurnNumber, targetScore);

        // 1. 해당 턴에 새로 해금되는 카드를 덱에 추가
        cardManager.UnlockCardsForTurn(currentTurnNumber);

        // 2. 소유한 카드 중에서 활성화 (이전 턴 사용 타입 제외)
        currentTurn.availableBlocks = cardManager.ActivateCardsForTurn(currentTurnNumber);

        OnTurnStart?.Invoke(currentTurn);

        Debug.Log($"[TurnManager] 턴 {currentTurnNumber}/{currentStage.endTurn} 시작 - 활성 카드 {currentTurn.availableBlocks.Count}장");
    }

    /// <summary>
    /// 턴 종료
    /// </summary>
    public void EndTurn(int currentTurnScore, List<CardType> usedBlockTypes)
    {
        if (currentTurn == null) return;

        currentTurn.currentTurnScore = currentTurnScore;

        // 사용한 카드 기록 (다음 턴 제한용)
        cardManager.OnTurnEnd(usedBlockTypes);

        Debug.Log($"[TurnManager] 턴 {currentTurnNumber} 종료 - 점수: {currentTurnScore}");

        OnTurnEnd?.Invoke();
    }

    /// <summary>
    /// 스테이지의 마지막 턴인지 확인
    /// </summary>
    public bool IsLastTurn()
    {
        return currentStage != null && currentTurnNumber >= currentStage.endTurn;
    }

    /// <summary>
    /// 스테이지 내 남은 턴 수
    /// </summary>
    public int GetRemainingTurns()
    {
        if (currentStage == null) return 0;
        return Mathf.Max(0, currentStage.endTurn - currentTurnNumber);
    }

    #region Card Management
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
        Debug.Log("ReturnCard");
        if (currentTurn != null && block != null)
        {
            Debug.Log("ReturnCard 11");

            currentTurn.availableBlocks.Add(block);
            cardManager.ReturnCard(block.type);
        }
    }
    #endregion

    #region Getters
    public TurnData GetCurrentTurn() => currentTurn;
    public int GetCurrentTurnNumber() => currentTurnNumber;
    public StageSO GetCurrentStage() => currentStage;
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