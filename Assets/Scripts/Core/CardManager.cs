using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCore.Data;

/// <summary>
/// 플레이어의 카드 덱을 관리하는 매니저
/// ⭐ 턴 간 카드 누적: 이전 턴에서 사용하지 않은 카드는 다음 턴에도 유지
/// ⭐ 스테이지 변경 시 초기화
/// </summary>
public class CardManager
{
    // 플레이어가 소유한 모든 카드 (영구적)
    private List<(CardType type, int count)> ownedCards = new();

    // ⭐ 현재 활성화된 카드 (턴마다 누적, 스테이지 변경 시 초기화)
    private List<CardType> activeCards = new();

    // 이전 턴에 사용한 카드들 (제한용)
    private Queue<HashSet<CardType>> usedCardsHistory = new Queue<HashSet<CardType>>();

    // 현재 턴에 사용 가능한 카드 여부
    private Dictionary<CardType, bool> canSelectThisTurn = new Dictionary<CardType, bool>();

    private GameConfig gameConfig;
    private StageSO currentStage;

    public System.Action OnDeckChanged; // 덱 변경 이벤트
    public System.Action OnActiveCardsChanged; // 활성 카드 변경 이벤트

    public CardManager(GameConfig config)
    {
        gameConfig = config;
        InitializeStartingDeck();
    }

    /// <summary>
    /// 시작 덱 초기화 (빈 덱으로 시작, 스테이지가 카드를 제공)
    /// </summary>
    private void InitializeStartingDeck()
    {
        ownedCards.Clear();
        activeCards.Clear(); // ⭐ 활성 카드도 초기화
        Debug.Log($"[CardManager] 시작 덱 초기화: 빈 덱 (스테이지에서 카드 제공)");
        OnDeckChanged?.Invoke();
    }

    /// <summary>
    /// 스테이지 설정 (스테이지 변경 시)
    /// </summary>
    public void SetStage(StageSO stage)
    {
        currentStage = stage;
        usedCardsHistory.Clear();
        canSelectThisTurn.Clear();
        activeCards.Clear(); // ⭐ 스테이지 변경 시 활성 카드 초기화

        Debug.Log($"[CardManager] 스테이지 {stage.stageId} 설정 (activeCards 초기화)");
    }

    /// <summary>
    /// 새 게임 시작 (덱 초기화)
    /// </summary>
    public void ResetDeck()
    {
        InitializeStartingDeck();
        usedCardsHistory.Clear();
        canSelectThisTurn.Clear();
    }

    /// <summary>
    /// 덱에 카드 추가 (해금) - CardSO의 count만큼 자동으로 추가
    /// </summary>
    public void AddCardToDeck(CardType cardType)
    {
        // ⭐ 이미 덱에 있는지 확인
        if (HasType(cardType))
        {
            Debug.Log($"[CardManager] {cardType}은(는) 이미 덱에 있습니다. 추가하지 않습니다.");
            return;
        }

        // CardSO에서 count 값 가져오기
        var cardData = CardDataLoader.GetData(cardType);
        if (cardData == null)
        {
            Debug.LogError($"[CardManager] {cardType}의 CardSO를 찾을 수 없습니다!");
            return;
        }

        ownedCards.Add((cardType, cardData.count));

        Debug.Log($"[CardManager] {cardType} 카드 {cardData.count}장 추가됨 (CardSO count 기준)");
        OnDeckChanged?.Invoke();
    }

    /// <summary>
    /// ⭐ 턴 시작 - 기존 카드 유지 + 새 카드 추가
    /// </summary>
    public List<Card> ActivateCardsForTurn(int turnNumber)
    {
        // ⭐ Clear 제거 - 이전 턴 카드 유지
        // activeCards.Clear(); // 제거!

        Debug.Log($"[CardManager] 턴 {turnNumber} 시작 - 기존 카드: {activeCards.Count}장");

        // 1. 드로우 개수 결정 (CardType 개수)
        int drawCount = GetDrawCount(turnNumber);

        // 2. 선택 가능한 CardType 풀 생성
        var availablePool = GetAvailableCardsPool();

        // 3. 랜덤하게 CardType 선택 (중복 제거)
        var selectedTypes = SelectRandomCardTypes(availablePool, drawCount);

        // 4. ⭐ 각 CardType의 count개씩 activeCards에 추가 (누적)
        foreach (var cardType in selectedTypes)
        {
            var cardData = CardDataLoader.GetData(cardType);
            if (cardData == null) continue;

            int count = cardData.count;
            for (int i = 0; i < count; i++)
            {
                activeCards.Add(cardType);
            }
        }

        // 5. 각 카드의 선택 가능 여부 업데이트
        UpdateCanSelectStatus();

        // 6. Block 리스트로 변환
        var blocks = activeCards.Select(type => new Card(type)).ToList();

        // 타입별 개수와 선택 가능 개수 출력
        var typeCounts = activeCards.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        int selectableCount = activeCards.Count(card => canSelectThisTurn.GetValueOrDefault(card, false));

        Debug.Log($"[CardManager] 턴 {turnNumber}: {selectedTypes.Count}개 타입 드로우 → 총 {blocks.Count}장 (선택가능: {selectableCount}장)");
        Debug.Log($"[CardManager] 타입별 개수: {string.Join(", ", typeCounts.Select(x => $"{x.Key}×{x.Value}"))}");

        OnActiveCardsChanged?.Invoke();

        return blocks;
    }

    /// <summary>
    /// 드로우 개수 결정 (CardType 개수)
    /// </summary>
    private int GetDrawCount(int turnNumber)
    {
        if (currentStage == null) return 9;

        if (turnNumber == 1)
            return currentStage.firstDraw;
        else if (turnNumber == 2)
            return currentStage.secondDraw;
        else if (turnNumber >= currentStage.endTurn)
            return currentStage.lastDraw;
        else
            return Mathf.Max(currentStage.firstDraw, currentStage.secondDraw, currentStage.lastDraw);
    }

    /// <summary>
    /// 선택 가능한 카드 풀 생성 (소유한 카드 중에서)
    /// </summary>
    private List<CardType> GetAvailableCardsPool()
    {
        // 소유한 모든 카드를 풀에 추가
        var pool = CardPool();

        // 이전 턴에 사용한 카드 타입 제외
        if (currentStage != null && currentStage.excludePreviousTurnTypes)
        {
            var excludedTypes = GetExcludedTypes();
            pool = pool.Where(card => !excludedTypes.Contains(card)).ToList();

            // 모든 카드가 제외되면 다시 전체 사용
            if (pool.Count == 0)
            {
                Debug.LogWarning("[CardManager] 모든 카드 타입이 제외됨 - 전체 덱 사용");
                pool = CardPool();
            }
        }

        Debug.Log($"[CardManager] 선택 가능한 카드 풀: {string.Join(", ", pool.Distinct())}");
        return pool;
    }

    private List<CardType> CardPool()
    {
        var pool = new List<CardType>();
        foreach (var (type, count) in ownedCards)
            for (int i = 0; i < count; i++) pool.Add(type);

        return pool;
    }

    /// <summary>
    /// 랜덤하게 CardType 선택 (중복 없이)
    /// </summary>
    private List<CardType> SelectRandomCardTypes(List<CardType> pool, int count)
    {
        // ⭐ 중복 제거된 CardType 목록 (소유한 타입만)
        var uniqueTypes = pool.Distinct().ToList();

        if (uniqueTypes.Count == 0)
        {
            Debug.LogWarning("[CardManager] 선택 가능한 카드 타입이 없습니다!");
            return new List<CardType>();
        }

        // 셔플
        var shuffled = uniqueTypes.OrderBy(x => Random.value).ToList();

        // count개만큼 선택
        var selected = new List<CardType>();
        for (int i = 0; i < Mathf.Min(count, shuffled.Count); i++)
        {
            selected.Add(shuffled[i]);
        }

        Debug.Log($"[CardManager] 선택된 카드 타입 ({selected.Count}개): {string.Join(", ", selected)}");
        return selected;
    }

    /// <summary>
    /// 각 카드의 선택 가능 여부 업데이트
    /// ⭐ 이전 턴에 사용한 타입은 false로 설정
    /// </summary>
    private void UpdateCanSelectStatus()
    {
        canSelectThisTurn.Clear();
        var excludedTypes = GetExcludedTypes();

        foreach (var card in activeCards)
        {
            // 이전 턴에 사용하지 않았으면 선택 가능
            canSelectThisTurn[card] = !excludedTypes.Contains(card);
        }

        Debug.Log($"[CardManager] 선택 불가 타입: {string.Join(", ", excludedTypes)}");
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
        // 1. 활성 카드에 있는지 확인
        if (!activeCards.Contains(cardType))
        {
            Debug.Log($"[CardManager] {cardType}은(는) 활성 카드가 아닙니다");
            return false;
        }

        // 2. 선택 가능한지 확인
        if (!CanSelectCard(cardType))
        {
            Debug.Log($"[CardManager] {cardType}은(는) 이전 턴에 사용하여 선택할 수 없습니다");
            return false;
        }

        // 3. ⭐ 활성 카드에서 1개만 제거 (타입 전체가 아님)
        activeCards.Remove(cardType); // 첫 번째 것만 제거
        OnActiveCardsChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 카드 반환 (활성 카드에 추가)
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
        if (currentStage != null && currentStage.excludePreviousTurnTypes && usedTypes.Count > 0)
        {
            var usedSet = new HashSet<CardType>(usedTypes);
            usedCardsHistory.Enqueue(usedSet);

            // 설정된 턴 수를 초과하면 오래된 기록 제거
            while (usedCardsHistory.Count > gameConfig.excludeTurnCount)
            {
                usedCardsHistory.Dequeue();
            }

            Debug.Log($"[CardManager] 사용 기록: {string.Join(", ", usedTypes)}");
        }
    }

    /// <summary>
    /// 특정 턴에 카드 해금 (StageSO.unlockCard 기반)
    /// CardSO의 count만큼 덱에 추가
    /// </summary>
    public void UnlockCardsForTurn(int turnNumber)
    {
        if (currentStage == null || currentStage.unlockCard == null) return;

        List<int> ar = currentStage.unlockCard;

        foreach (int card in ar)
        {
            // cardId를 CardType으로 변환 (1=Orc, 2=Werewolf, ..., 12=Slime)
            if (card >= 1 && card <= 20)
            {
                CardType newCard = (CardType)(card - 1);
                AddCardToDeck(newCard); // CardSO의 count만큼 자동 추가

                Debug.Log($"[CardManager] 턴 {turnNumber}: {newCard} 카드 해금! (덱에 추가됨)");
            }
            else
            {
                Debug.LogError("Card Value Error: " + card);
            }
        }
    }

    #region Getters
    public List<(CardType, int)> GetOwnedCards() => ownedCards.Select(kvp => (kvp.type, kvp.count)).ToList();
    public List<CardType> GetActiveCards() => new List<CardType>(activeCards);
    public bool CanSelectCard(CardType cardType) => canSelectThisTurn.GetValueOrDefault(cardType, true);

    /// <summary>
    /// 특정 카드의 선택 가능 여부와 활성화 여부 반환
    /// </summary>
    public (bool isActive, bool canSelect) GetCardStatus(CardType cardType)
    {
        bool isActive = activeCards.Contains(cardType);
        bool canSelect = canSelectThisTurn.GetValueOrDefault(cardType, false);
        return (isActive, canSelect);
    }

    /// <summary>
    /// 활성 카드 중 특정 타입의 개수
    /// </summary>
    public int GetActiveCardCount(CardType cardType)
    {
        return activeCards.Count(c => c == cardType);
    }

    /// <summary>현재 덱이 보유 중인 CardType의 집합</summary>
    public HashSet<CardType> GetOwnedTypes()
        => ownedCards.Select(x => x.type).ToHashSet();

    /// <summary>덱이 해당 타입을 보유 중인가?</summary>
    public bool HasType(CardType type)
        => ownedCards.Any(x => x.type == type);

    /// <summary>현재 덱의 유니크 타입 수</summary>
    public int GetUniqueTypeCount()
        => GetOwnedTypes().Count;
    #endregion

    #region Shop System
    private const int DeckUniqueLimit = 7;

    /// <summary>덱에서 해당 타입(그 타입의 count 전체)을 제거</summary>
    public bool TryRemoveTypeFromDeck(CardType type)
    {
        int idx = ownedCards.FindIndex(x => x.type == type);
        if (idx < 0) return false;

        ownedCards.RemoveAt(idx);
        Debug.Log($"[CardManager] 덱에서 {type} 타입 제거");
        OnDeckChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 덱 타입 교체: outType(덱에서 제거) ↔ inType(상점에서 입수).
    /// </summary>
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

    /// <summary>
    /// 덱의 특정 인덱스 위치에 있는 타입을 새로운 타입으로 교체 (칸 유지)
    /// </summary>
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