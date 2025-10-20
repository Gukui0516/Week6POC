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

    public IReadOnlyList<CardType> CurrentOffers => currentOffers;
    public Action OnOffersChanged;

    private void Start()
    {
        // ⭐ 게임 상태 변경 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
    }

    /// <summary>
    /// 게임 상태가 상점으로 변경되면 상점 초기화
    /// </summary>
    private void OnGameStateChanged(GameCore.Data.GameState state)
    {
        if (state == GameCore.Data.GameState.Shop)
        {
            InitializeShop();
        }
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
            Debug.LogWarning("[ShopManager] 덱에 없는 타입이 없습니다. 빈 상점으로 표시됩니다.");
            OnOffersChanged?.Invoke();
            return;
        }

        // 무작위 3개
        var shuffled = candidates.OrderBy(_ => UnityEngine.Random.value).ToList();
        for (int i = 0; i < Mathf.Min(OfferCount, shuffled.Count); i++)
            currentOffers.Add(shuffled[i]);

        Debug.Log($"[ShopManager] 새로운 제시: {string.Join(", ", currentOffers)}");
        OnOffersChanged?.Invoke();
    }

    private void SetOwnedCardUI()
    {
        if (cardManager == null) return;

        var ownedTypes = cardManager.GetOwnedTypes().ToList();
        for (int i = 0; i < ownedCards.Count; i++)
        {
            if (i < ownedTypes.Count)
                ownedCards[i].SetCardUI(ownedTypes[i]);
        }
    }

    private void SetShopCardUI()
    {
        for (int i = 0; i < Mathf.Min(3, currentOffers.Count); i++)
        {
            shopCards[i].SetCardUI(currentOffers[i]);
        }
    }

    /// <summary>
    /// 덱의 deckType과 상점의 shopType을 '서로 교체'
    /// </summary>
    public bool TrySwap(CardType deckType, CardType shopType)
    {
        if (cardManager == null)
        {
            Debug.LogWarning("[ShopManager] CardManager가 초기화되지 않았습니다!");
            return false;
        }

        if (!currentOffers.Contains(shopType))
        {
            Debug.LogWarning($"[ShopManager] 교체 실패: 상점에 {shopType} 없음");
            return false;
        }
        if (!cardManager.HasType(deckType))
        {
            Debug.LogWarning($"[ShopManager] 교체 실패: 덱에 {deckType} 없음");
            return false;
        }

        // 카드 매니저에 교체 요청
        bool ok = cardManager.TryReplaceType(deckType, shopType);
        if (!ok) return false;

        // 상점 목록 갱신: shopType 자리를 deckType으로 대체
        int idx = currentOffers.IndexOf(shopType);
        currentOffers[idx] = deckType;

        Debug.Log($"[ShopManager] 교체 완료: 덱 {deckType} → 상점, 상점 {shopType} → 덱");

        // UI 갱신
        SetOwnedCardUI();
        SetShopCardUI();

        OnOffersChanged?.Invoke();
        return true;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }
}