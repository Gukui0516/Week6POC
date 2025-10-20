using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private List<ShopCardUI> shopCards;
    [SerializeField] private List<ShopOwnCardUI> ownedCards;

    private CardManager cardManager;
    private const int OfferCount = 3;
    private readonly List<CardType> currentOffers = new();

    private CardType? currentSelectedShopType;
    private CardType? currentSelectedDeckType;

    private int? currentSelectedShopIndex;
    private int? currentSelectedDeckIndex;

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;

    }

    /// <summary>
    /// 게임 상태가 상점으로 변경되면 상점 초기화
    /// </summary>
    private void OnGameStateChanged(GameCore.Data.GameState state)
    {
        if (state == GameCore.Data.GameState.Shop) InitializeShop();
    }

    /// <summary>
    /// 상점 초기화 (상점이 열릴 때마다 호출)
    /// </summary>
    private void InitializeShop()
    {
        // CardManager 최신 정보 가져오기
        cardManager = GameManager.Instance?.GetTurnManager()?.GetCardManager();

        if (cardManager == null)
        {
            Debug.LogError("[ShopManager] CardManager를 찾을 수 없습니다!");
            return;
        }

        // 새로운 제안 생성
        RollOffers();

        // UI 업데이트
        SetOwnedCardUI();
        SetShopCardUI();

        Debug.Log("[ShopManager] 상점 초기화 완료");
    }

    /// <summary>
    /// 상점에 덱에 없는 타입들 중 3가지를 랜덤으로 제시
    /// </summary>
    public void RollOffers()
    {
        currentOffers.Clear();

        if (cardManager == null)
        {
            Debug.LogWarning("[ShopManager] CardManager가 초기화되지 않았습니다!");
            return;
        }

        var owned = cardManager.GetOwnedTypes();
        var allTypes = Enum.GetValues(typeof(CardType)).Cast<CardType>().ToList();

        var candidates = allTypes.Where(t => !owned.Contains(t)).ToList();
        if (candidates.Count == 0)
        {
            Debug.Log("[ShopManager] 덱에 없는 카드 타입이 없습니다.");
            return;
        }

        // 무작위 3개
        var shuffled = candidates.OrderBy(_ => UnityEngine.Random.value).ToList();
        for (int i = 0; i < Mathf.Min(OfferCount, shuffled.Count); i++)
            currentOffers.Add(shuffled[i]);
    }


    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    /// <summary>
    /// 인덱스와 타입을 함께 선택하는 API (UI에서 호출)
    /// </summary>
    public void SelectShop(int shopIndex, CardType shopType)
    {
        currentSelectedShopIndex = shopIndex;
        currentSelectedShopType = shopType;
        TrySwap();
    }

    public void SelectDeck(int deckIndex, CardType deckType)
    {
        currentSelectedDeckIndex = deckIndex;
        currentSelectedDeckType = deckType;
        TrySwap();
    }

    // 기존의 타입 전용 호출들과 호환 유지(예전 호출이 남아있을 경우)
    public void SetcurrentSelectedShopTypes(CardType shopType)
    {
        currentSelectedShopIndex = null;
        currentSelectedShopType = shopType;
        TrySwap();
    }

    public void SetcurrentSelectedDeckTypes(CardType deckType)
    {
        currentSelectedDeckIndex = null;
        currentSelectedDeckType = deckType;
        TrySwap();
    }

    private void SetOwnedCardUI()
    {
        if (cardManager == null) return;

        var ownedTypes = cardManager.GetOwnedTypes().ToList();
        for (int i = 0; i < ownedCards.Count; i++)
        {
            if (i < ownedTypes.Count)
            {
                ownedCards[i].SetCardUI(ownedTypes[i]);
                // 슬롯 인덱스 할당 (UI가 누르면 자신 인덱스를 넘기게 함)
                ownedCards[i].Index = i;
            }
        }
    }

    private void SetShopCardUI()
    {
        for (int i = 0; i < Mathf.Min(3, currentOffers.Count); i++)
        {
            shopCards[i].SetCardUI(currentOffers[i]);
            // 상점 슬롯 인덱스 할당
            shopCards[i].Index = i;
        }
    }

    /// <summary>
    /// 덱 인덱스와 상점 인덱스를 사용해 '칸 그대로' 서로 교환한다.
    /// </summary>
    public bool TrySwap()
    {
        // 인덱스 + 타입이 모두 선택되어야 실행
        if (currentSelectedDeckIndex == null || currentSelectedDeckType == null ||
            currentSelectedShopIndex == null || currentSelectedShopType == null)
        {
            Debug.LogWarning("[ShopManager] 교체할 덱/상점 슬롯이 선택되지 않았습니다.");
            return false;
        }

        int deckIdx = currentSelectedDeckIndex.Value;
        int shopIdx = currentSelectedShopIndex.Value;
        var shopType = currentSelectedShopType.Value;
        var deckType = currentSelectedDeckType.Value;

        // 덱의 특정 인덱스에 상점 타입을 넣고, 기존 덱 타입을 outType으로 받음
        if (!cardManager.TryReplaceTypeAtIndex(deckIdx, shopType, out var outType))
        {
            Debug.LogWarning("[ShopManager] 덱 인덱스 교체 실패");
            return false;
        }

        // 상점의 해당 인덱스 자리에는 덱에서 나온 타입을 넣음
        if (shopIdx >= 0 && shopIdx < currentOffers.Count)
        {
            currentOffers[shopIdx] = outType;
        }

        Debug.Log($"[ShopManager] 교체 완료: 덱[{deckIdx}] ({outType}) ⇄ 상점[{shopIdx}] ({shopType})");

        // 선택 초기화
        currentSelectedDeckIndex = null;
        currentSelectedDeckType = null;
        currentSelectedShopIndex = null;
        currentSelectedShopType = null;

        // UI 갱신
        SetOwnedCardUI();
        SetShopCardUI();

        // 선택 해제
        DeselectAll();

        // ⭐ CardManager의 OnDeckChanged 이벤트가 자동으로 발생하여
        // InventoryController.RebuildInventoryUI()가 호출됨
        // (CardManager.TryReplaceTypeAtIndex 내부에서 OnDeckChanged?.Invoke() 호출)

        return true;
    }
    private void DebugOwnedDeck(string tag = "OwnedDeck")
    {
        if (cardManager == null)
        {
            Debug.LogWarning("[ShopManager] DebugOwnedDeck: cardManager == null");
            return;
        }

        var owned = cardManager.GetOwnedTypes()?.ToList() ?? new List<CardType>();
        string list = owned.Count == 0
            ? "(empty)"
            : string.Join(", ", owned.Select((t, i) => $"[{i}] {t}"));
        Debug.Log($"[ShopManager] {tag}: {list}");
    }

    private void DebugShopOffers(string tag = "ShopOffers")
    {
        string list = (currentOffers == null || currentOffers.Count == 0)
            ? "(empty)"
            : string.Join(", ", currentOffers.Select((t, i) => $"[{i}] {t}"));
        Debug.Log($"[ShopManager] {tag}: {list}");
    }

    private void DeselectAll()
    {
        foreach (var shopCard in shopCards)
        {
            shopCard.Deselect();
        }
        foreach (var ownedCard in ownedCards)
        {
            ownedCard.Deselect();
        }
    }
}