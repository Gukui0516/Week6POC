using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameCore.Data;

/// <summary>
/// 플레이어의 카드 덱을 관리하는 매니저
/// </summary>
public class CardManager
{
    // 플레이어가 소유한 모든 카드 (영구적)
    private List<(CardType type, int count)> ownedCards = new();

    // 현재 턴에 활성화된 카드 (매 턴 변경)
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
        Debug.Log($"[CardManager] 시작 덱 초기화: 빈 덱 (스테이지에서 카드 제공)");
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

        Debug.Log($"[CardManager] 스테이지 {stage.stageId} 설정");
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
    /// 턴 시작 - 활성 카드 선택
    /// firstDraw=4 → 4개의 CardType 선택 → 각 CardType의 count개씩 활성화
    /// </summary>
    public List<Card> ActivateCardsForTurn(int turnNumber)
    {
        activeCards.Clear();

        // 1. 드로우 개수 결정 (CardType 개수)
        int drawCount = GetDrawCount(turnNumber);

        // 2. 선택 가능한 CardType 풀 생성
        var availablePool = GetAvailableCardsPool();

        // 3. 랜덤하게 CardType 선택 (중복 제거)
        var selectedTypes = SelectRandomCardTypes(availablePool, drawCount);

        // 4. 각 CardType의 count개씩 activeCards에 추가
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

        Debug.Log($"[CardManager] 턴 {turnNumber}: {selectedTypes.Count}개 CardType 활성화 → 총 {blocks.Count}장 (선택가능: {canSelectThisTurn.Count(x => x.Value)}장)");
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

    /// <summary>
    /// 카드 사용 (활성 카드에서 제거)
    /// </summary>
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

        // 3. 활성 카드에서 제거
        activeCards.Remove(cardType);
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

        // 턴 번호에 해당하는 인덱스 계산 (1턴 = 인덱스 0)
        int index = turnNumber - 1;

        List<int> ar = currentStage.unlockCard;

        foreach (int card in ar)
        {

            // cardId를 CardType으로 변환 (1=Orc, 2=Werewolf, ..., 7=Dragon)
            if (card >= 1 && card <= 20)
            {
                CardType newCard = (CardType)(card - 1);
                AddCardToDeck(newCard); // CardSO의 count만큼 자동 추가

                Debug.Log($"[CardManager] 턴 {turnNumber}: {newCard} 카드 해금! (덱에 추가됨)");
            }
            else
            {
                Debug.LogError("Card Value Error  :  " + card);
            }
        }

        if (index >= 0 && index < currentStage.unlockCard.Count)
        {

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
    #endregion

    private const int DeckUniqueLimit = 7;

    /// <summary>현재 덱이 보유 중인 CardType의 집합</summary>
    public HashSet<CardType> GetOwnedTypes()
        => ownedCards.Select(x => x.type).ToHashSet();

    /// <summary>덱이 해당 타입을 보유 중인가?</summary>
    public bool HasType(CardType type)
        => ownedCards.Any(x => x.type == type);

    /// <summary>현재 덱의 유니크 타입 수</summary>
    public int GetUniqueTypeCount()
        => GetOwnedTypes().Count;

    /// <summary>덱에서 해당 타입(그 타입의 count 전체)을 제거</summary>
    public bool TryRemoveTypeFromDeck(CardType type)
    {
        int idx = ownedCards.FindIndex(x => x.type == type);
        if (idx < 0) return false;

        ownedCards.RemoveAt(idx);
        Debug.Log($"[CardManager] 덱에서 {type} 타입 제거");
        OnDeckChanged?.Invoke();

        // 활성 카드 풀/선택 가능 여부가 턴 중이라면 갱신이 필요할 수 있음
        // 여기서는 덱 변경 이벤트만 쏘고, UI/턴 매니저에서 적절히 대응하도록 둡니다.
        return true;
    }

    /// <summary>
    /// 덱 타입 교체: outType(덱에서 제거) ↔ inType(상점에서 입수).
    /// - inType이 이미 덱에 있으면 실패 (유니크 7종 유지 규칙 위배 가능성)
    /// - outType이 덱에 없으면 실패
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

        // outType 제거
        if (!TryRemoveTypeFromDeck(outType))
            return false;

        // inType 추가 (CardSO.count 만큼 자동 추가)
        AddCardToDeck(inType);

        // 유니크 7종 규칙 보장 (교체이므로 항상 유지되지만 안전망)
        if (GetUniqueTypeCount() > DeckUniqueLimit)
        {
            Debug.LogError("[CardManager] 유니크 타입 수 7 초과! 롤백 필요");
            // 간단 롤백
            TryRemoveTypeFromDeck(inType);
            AddCardToDeck(outType);
            return false;
        }

        Debug.Log($"[CardManager] {outType} ↔ {inType} 타입 교체 완료");
        return true;
    }

    /// <summary>
    /// 덱의 특정 인덱스 위치에 있는 타입을 새로운 타입으로 교체합니다 (칸 유지).
    /// - outType은 해당 인덱스에서 제거된 기존 타입을 out param으로 반환합니다.
    /// - inType이 이미 덱의 다른 위치에 있으면 실패합니다.
    /// </summary>
    public bool TryReplaceTypeAtIndex(int deckIndex, CardType inType, out CardType outType)
    {
        outType = default;

        // 인덱스 범위 체크
        if (deckIndex < 0 || deckIndex >= ownedCards.Count)
        {
            Debug.LogWarning($"[CardManager] 잘못된 deckIndex: {deckIndex}");
            return false;
        }

        // 기존 값 보존
        var old = ownedCards[deckIndex];
        outType = old.type;

        // 동일 타입 치환은 의미 없음
        if (outType == inType)
        {
            Debug.LogWarning("[CardManager] 동일 타입으로 교체 시도(무시)");
            return false;
        }

        // inType이 이미 다른 인덱스에 존재하면 유니크 규칙 위반 → 실패
        for (int i = 0; i < ownedCards.Count; i++)
        {
            if (i == deckIndex) continue;
            if (ownedCards[i].type == inType)
            {
                Debug.LogWarning($"[CardManager] 교체 실패: '{inType}'는 이미 덱에 존재");
                return false;
            }
        }

        // 방어적 정리: outType이 덱 내 다른 위치에 중복되어 있으면 제거
        // (혹시 과거 데이터/버그로 중복이 있었다면 여기서 정리)
        for (int i = ownedCards.Count - 1; i >= 0; --i)
        {
            if (i == deckIndex) continue;
            if (ownedCards[i].type == outType)
            {
                ownedCards.RemoveAt(i);
                // 앞쪽 원소가 빠지면 deckIndex가 한 칸 당겨지므로 보정
                if (i < deckIndex) deckIndex--;
                Debug.Log($"[CardManager] 방어적 정리: 중복 {outType} 제거 at {i}");
            }
        }

        // 실제 치환(같은 칸 유지)
        ownedCards[deckIndex] = (inType, old.count);

        // 유니크 제한 안전망 (이상 시 롤백)
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

    private void DeselectAll()
    {
        // 활성 카드 전체 선택 해제
        canSelectThisTurn.Clear();
        OnActiveCardsChanged?.Invoke();
    }
}