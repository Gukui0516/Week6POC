using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCore.Data;

/// <summary>
/// 플레이어의 카드 덱을 관리하는 매니저
/// ⭐ 모든 소유 타입을 매 턴 드로우
/// ⭐ 활성 카드 수 = CardData.count - 보드에 배치된 해당 타입 개수
/// ⭐ 이전 턴에 사용한 CardType은 선택만 불가 (표시는 됨)
/// </summary>
public class CardManager
{
    // 플레이어가 소유한 모든 카드 (영구적)
    private List<(CardType type, int count)> ownedCards = new();

    // 현재 턴에 활성화된 카드
    private List<CardType> activeCards = new();

    // 이전 턴에 사용한 카드들 (제한용)
    private Queue<HashSet<CardType>> usedCardsHistory = new Queue<HashSet<CardType>>();

    // 현재 턴에 사용 가능한 카드 여부
    private Dictionary<CardType, bool> canSelectThisTurn = new Dictionary<CardType, bool>();

    private GameConfig gameConfig;
    private StageSO currentStage;
    private BoardManager boardManager;

    public System.Action OnDeckChanged;
    public System.Action OnActiveCardsChanged;

    public CardManager(GameConfig config)
    {
        gameConfig = config;
        InitializeStartingDeck();
    }

    /// <summary>
    /// BoardManager 설정
    /// </summary>
    public void SetBoardManager(BoardManager board)
    {
        boardManager = board;
    }

    /// <summary>
    /// 시작 덱 초기화
    /// </summary>
    private void InitializeStartingDeck()
    {
        ownedCards.Clear();
        activeCards.Clear();
        Debug.Log($"[CardManager] 시작 덱 초기화: 빈 덱");
        OnDeckChanged?.Invoke();
    }

    /// <summary>
    /// 스테이지 설정
    /// </summary>
    public void SetStage(StageSO stage)
    {
        currentStage = stage;
        usedCardsHistory.Clear();
        canSelectThisTurn.Clear();
        activeCards.Clear();

        Debug.Log($"[CardManager] 스테이지 {stage.stageId} 설정");
    }

    /// <summary>
    /// 새 게임 시작
    /// </summary>
    public void ResetDeck()
    {
        InitializeStartingDeck();
        usedCardsHistory.Clear();
        canSelectThisTurn.Clear();
    }

    /// <summary>
    /// 덱에 카드 추가 (해금)
    /// </summary>
    public void AddCardToDeck(CardType cardType)
    {
        if (HasType(cardType))
        {
            Debug.Log($"[CardManager] {cardType}은(는) 이미 덱에 있습니다.");
            return;
        }

        var cardData = CardDataLoader.GetData(cardType);
        if (cardData == null)
        {
            Debug.LogError($"[CardManager] {cardType}의 CardSO를 찾을 수 없습니다!");
            return;
        }

        ownedCards.Add((cardType, cardData.count));

        Debug.Log($"[CardManager] {cardType} 카드 추가 (최대 {cardData.count}장)");
        OnDeckChanged?.Invoke();
    }

    /// <summary>
    /// ⭐ 턴 시작 - 모든 소유 타입을 드로우 (보드 상황 반영)
    /// </summary>
    public List<Card> ActivateCardsForTurn(int turnNumber)
    {
        activeCards.Clear();

        Debug.Log($"[CardManager] 턴 {turnNumber} 시작 - 모든 소유 타입 드로우");

        // ⭐ 모든 소유 타입에 대해 활성 개수 계산
        foreach (var (cardType, maxCount) in ownedCards)
        {
            // count - 보드에 있는 개수
            int availableCount = GetAvailableCardCount(cardType);

            for (int i = 0; i < availableCount; i++)
            {
                activeCards.Add(cardType);
            }
        }

        // 선택 가능 여부 업데이트
        UpdateCanSelectStatus();

        // Block 리스트로 변환
        var blocks = activeCards.Select(type => new Card(type)).ToList();

        // 로그 출력
        var typeCounts = activeCards.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        int selectableCount = activeCards.Count(card => canSelectThisTurn.GetValueOrDefault(card, true));

        Debug.Log($"[CardManager] 턴 {turnNumber}: 총 {blocks.Count}장 활성화 (선택가능: {selectableCount}장)");

        foreach (var (cardType, maxCount) in ownedCards)
        {
            int onBoard = GetBlockCountOnBoard(cardType);
            int activeCount = typeCounts.GetValueOrDefault(cardType, 0);
            bool canSelect = canSelectThisTurn.GetValueOrDefault(cardType, true);
            string status = canSelect ? "선택가능" : "사용불가";

            Debug.Log($"  - {cardType}: {activeCount}장 활성 (보드: {onBoard}, 최대: {maxCount}) [{status}]");
        }

        OnActiveCardsChanged?.Invoke();

        return blocks;
    }

    /// <summary>
    /// ⭐ 특정 CardType의 활성 가능 개수 = count - 보드 배치 개수
    /// </summary>
    private int GetAvailableCardCount(CardType cardType)
    {
        var cardData = CardDataLoader.GetData(cardType);
        if (cardData == null) return 0;

        int maxCount = cardData.count;
        int onBoardCount = GetBlockCountOnBoard(cardType);
        Debug.Log("onBoardCount" + onBoardCount);
        int available = Mathf.Max(0, maxCount - onBoardCount);

        return available;
    }

    /// <summary>
    /// ⭐ 보드에 배치된 특정 타입의 블록 개수
    /// </summary>
    private int GetBlockCountOnBoard(CardType cardType)
    {
        if (boardManager == null) return 0;

        var occupiedTiles = boardManager.GetOccupiedTiles();
        return occupiedTiles.Count(tile => tile.block.type == cardType);
    }

    /// <summary>
    /// 선택 가능 여부 업데이트
    /// ⭐ 이전 N턴에 사용한 타입만 false
    /// </summary>
    private void UpdateCanSelectStatus()
    {
        canSelectThisTurn.Clear();
        var excludedTypes = GetExcludedTypes();

        // 모든 활성 카드에 대해 설정
        foreach (var card in activeCards.Distinct())
        {
            canSelectThisTurn[card] = !excludedTypes.Contains(card);
        }

        if (excludedTypes.Count > 0)
        {
            Debug.Log($"[CardManager] 선택 불가 타입: {string.Join(", ", excludedTypes)}");
        }
    }

    /// <summary>
    /// 제외해야 할 카드 타입 반환
    /// </summary>
    private HashSet<CardType> GetExcludedTypes()
    {
        var excluded = new HashSet<CardType>();

        if (currentStage != null && currentStage.excludePreviousTurnTypes)
        {
            foreach (var turnTypes in usedCardsHistory)
            {
                foreach (var type in turnTypes)
                {
                    excluded.Add(type);
                }
            }
        }

        return excluded;
    }

    public bool TryUseCard(CardType cardType)
    {
        if (!activeCards.Contains(cardType))
        {
            Debug.Log($"[CardManager] {cardType}은(는) 활성 카드가 아닙니다");
            return false;
        }

        if (!CanSelectCard(cardType))
        {
            Debug.Log($"[CardManager] {cardType}은(는) 이전 턴에 사용하여 선택할 수 없습니다");
            return false;
        }

        activeCards.Remove(cardType);
        OnActiveCardsChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 카드 반환
    /// </summary>
    public void ReturnCard(CardType cardType)
    {
        activeCards.Add(cardType);
        UpdateCanSelectStatus();
        OnActiveCardsChanged?.Invoke();
    }

    /// <summary>
    /// 턴 종료 - 사용한 카드 기록
    /// </summary>
    public void OnTurnEnd(List<CardType> usedTypes)
    {
        if (currentStage != null && currentStage.excludePreviousTurnTypes)
        {
             var usedSet = new HashSet<CardType>(usedTypes);
             usedCardsHistory.Enqueue(usedSet);

             while (usedCardsHistory.Count > gameConfig.excludeTurnCount)
             {
                 usedCardsHistory.Dequeue();
             }

             Debug.Log($"[CardManager] 이번 턴 사용 타입: {string.Join(", ", usedTypes)}");


            //usedCardsHistory.Clear();
        }
    }

    /// <summary>
    /// 특정 턴에 카드 해금
    /// </summary>
    public void UnlockCardsForTurn(int turnNumber)
    {
        if (currentStage == null || currentStage.unlockCard == null) return;

        List<int> ar = currentStage.unlockCard;

        foreach (int card in ar)
        {
            if (card >= 1 && card <= 20)
            {
                CardType newCard = (CardType)(card - 1);
                AddCardToDeck(newCard);

                Debug.Log($"[CardManager] 턴 {turnNumber}: {newCard} 카드 해금!");
            }
        }
    }

    #region Getters
    public List<(CardType, int)> GetOwnedCards() => ownedCards.Select(kvp => (kvp.type, kvp.count)).ToList();
    public List<CardType> GetActiveCards() => new List<CardType>(activeCards);
    public bool CanSelectCard(CardType cardType) => canSelectThisTurn.GetValueOrDefault(cardType, true);

    public (bool isActive, bool canSelect) GetCardStatus(CardType cardType)
    {
        bool isActive = activeCards.Contains(cardType);
        bool canSelect = canSelectThisTurn.GetValueOrDefault(cardType, true);
        return (isActive, canSelect);
    }

    public int GetActiveCardCount(CardType cardType)
    {
        return activeCards.Count(c => c == cardType);
    }

    public HashSet<CardType> GetOwnedTypes()
        => ownedCards.Select(x => x.type).ToHashSet();

    public bool HasType(CardType type)
        => ownedCards.Any(x => x.type == type);

    public int GetUniqueTypeCount()
        => GetOwnedTypes().Count;
    #endregion

    #region Shop System
    private const int DeckUniqueLimit = 7;

    public bool TryRemoveTypeFromDeck(CardType type)
    {
        int idx = ownedCards.FindIndex(x => x.type == type);
        if (idx < 0) return false;

        ownedCards.RemoveAt(idx);
        Debug.Log($"[CardManager] 덱에서 {type} 타입 제거");
        OnDeckChanged?.Invoke();

        return true;
    }

    public bool TryReplaceType(CardType outType, CardType inType)
    {
        var owned = GetOwnedTypes();

        if (!owned.Contains(outType))
        {
            Debug.LogWarning($"[CardManager] 교체 실패: 덱에 {outType} 없음");
            return false;
        }
        if (owned.Contains(inType))
        {
            Debug.LogWarning($"[CardManager] 교체 실패: {inType}는 이미 덱에 존재");
            return false;
        }

        if (!TryRemoveTypeFromDeck(outType))
            return false;

        AddCardToDeck(inType);

        if (GetUniqueTypeCount() > DeckUniqueLimit)
        {
            Debug.LogError("[CardManager] 유니크 타입 수 7 초과! 롤백");
            TryRemoveTypeFromDeck(inType);
            AddCardToDeck(outType);
            return false;
        }

        Debug.Log($"[CardManager] {outType} ↔ {inType} 타입 교체 완료");
        return true;
    }

    public bool TryReplaceTypeAtIndex(int deckIndex, CardType inType, out CardType outType)
    {
        outType = default;

        if (deckIndex < 0 || deckIndex >= ownedCards.Count)
        {
            Debug.LogWarning($"[CardManager] 잘못된 deckIndex: {deckIndex}");
            return false;
        }

        var old = ownedCards[deckIndex];
        outType = old.type;

        if (outType == inType)
        {
            Debug.LogWarning("[CardManager] 동일 타입으로 교체 시도(무시)");
            return false;
        }

        for (int i = 0; i < ownedCards.Count; i++)
        {
            if (i == deckIndex) continue;
            if (ownedCards[i].type == inType)
            {
                Debug.LogWarning($"[CardManager] 교체 실패: '{inType}'는 이미 덱에 존재");
                return false;
            }
        }

        for (int i = ownedCards.Count - 1; i >= 0; --i)
        {
            if (i == deckIndex) continue;
            if (ownedCards[i].type == outType)
            {
                ownedCards.RemoveAt(i);
                if (i < deckIndex) deckIndex--;
                Debug.Log($"[CardManager] 방어적 정리: 중복 {outType} 제거 at {i}");
            }
        }

        ownedCards[deckIndex] = (inType, old.count);

        if (GetUniqueTypeCount() > DeckUniqueLimit)
        {
            Debug.LogError("[CardManager] 유니크 타입 수 7 초과! 롤백");
            ownedCards[deckIndex] = old;
            return false;
        }

        OnDeckChanged?.Invoke();
        Debug.Log($"[CardManager] 인덱스 {deckIndex}에서 {outType} → {inType} 교체 완료");
        return true;
    }
    #endregion
}