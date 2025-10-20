using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{
    [Header("Dynamic Inventory")]
    [SerializeField] private GameObject inventoryButtonPrefab; // 프리팹 참조
    [SerializeField] private Transform inventoryContainer; // 버튼들이 생성될 부모 Transform
    [SerializeField] private int maxInventorySlots = 10; // 최대 슬롯 수

    private GameManager gameManager;
    private List<InventoryButton> activeButtons = new List<InventoryButton>(); // 동적 리스트

    // 선택된 카드 상태
    private CardType? selectedCardType = null;
    private InventoryButton selectedButton = null;

    #region Drag
    private GameObject dragPreview;
    private Canvas mainCanvas;
    #endregion

    public void Initialize(GameManager gm = null)
    {
        gameManager = gm ?? GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("[InventoryController] GameManager를 찾을 수 없습니다!");
            return;
        }

        mainCanvas = FindFirstObjectByType<Canvas>();

        // CardManager의 덱 변경 이벤트 구독
        var cardManager = gameManager.GetTurnManager()?.GetCardManager();
        if (cardManager != null)
        {
            cardManager.OnDeckChanged += RebuildInventoryUI;
        }

        // 초기 인벤토리 생성
        RebuildInventoryUI();
    }

    /// <summary>
    /// 덱 구성에 맞춰 인벤토리 UI 재생성
    /// </summary>
    public void RebuildInventoryUI()
    {
        // 기존 버튼들 제거
        ClearInventory();

        var cardManager = gameManager?.GetTurnManager()?.GetCardManager();
        if (cardManager == null) return;

        // 소유한 카드 타입 가져오기 (순서 유지)
        var ownedCards = cardManager.GetOwnedCards(); // List<(CardType type, int count)>

        // 각 소유 카드에 대해 버튼 생성
        foreach (var (cardType, count) in ownedCards)
        {
            CreateInventoryButton(cardType);
        }

        Debug.Log($"[InventoryController] 인벤토리 UI 재생성: {activeButtons.Count}개 버튼");
    }

    /// <summary>
    /// 개별 인벤토리 버튼 생성
    /// </summary>
    private void CreateInventoryButton(CardType cardType)
    {
        if (inventoryButtonPrefab == null || inventoryContainer == null)
        {
            Debug.LogError("[InventoryController] 프리팹 또는 컨테이너가 할당되지 않았습니다!");
            return;
        }

        // 프리팹 인스턴스화
        GameObject buttonObj = Instantiate(inventoryButtonPrefab, inventoryContainer);
        InventoryButton button = buttonObj.GetComponent<InventoryButton>();

        if (button == null)
        {
            Debug.LogError("[InventoryController] 프리팹에 InventoryButton 컴포넌트가 없습니다!");
            Destroy(buttonObj);
            return;
        }

        // 버튼 설정
        button.CardType = cardType;
        button.SetInventoryController(this);
        button.name = $"InventoryButton_{cardType}";

        activeButtons.Add(button);
    }

    /// <summary>
    /// 모든 인벤토리 버튼 제거
    /// </summary>
    private void ClearInventory()
    {
        foreach (var button in activeButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }
        activeButtons.Clear();

        // 선택 상태도 초기화
        selectedCardType = null;
        selectedButton = null;
    }

    /// <summary>
    /// 인벤토리 UI 업데이트 (활성 카드 기반)
    /// </summary>
    public void UpdateInventory()
    {
        if (activeButtons == null || gameManager?.GetCurrentTurn() == null) return;

        var turn = gameManager.GetCurrentTurn();
        var cardManager = gameManager.GetTurnManager()?.GetCardManager();

        if (cardManager == null) return;

        // 활성 카드 개수 계산
        var activeCards = turn.availableBlocks.GroupBy(b => b.type)
                              .ToDictionary(g => g.Key, g => g.Count());

        foreach (var button in activeButtons)
        {
            int count = activeCards.ContainsKey(button.CardType) ? activeCards[button.CardType] : 0;
            bool canSelect = cardManager.CanSelectCard(button.CardType);

            button.UpdateDisplay(count, canSelect);
        }
    }

    /// <summary>
    /// 블록 선택
    /// </summary>
    public void SelectBlock(CardType cardType, InventoryButton button)
    {
        var cardManager = gameManager?.GetTurnManager()?.GetCardManager();
        if (cardManager == null) return;

        if (!cardManager.CanSelectCard(cardType))
        {
            Debug.Log($"[InventoryController] {cardType}은(는) 이전 턴에 사용하여 선택할 수 없습니다");
            return;
        }

        // 같은 블록을 다시 선택하면 선택 해제
        if (selectedCardType == cardType && selectedButton == button)
        {
            DeselectBlock();
            return;
        }

        // 이전 선택 해제
        if (selectedButton != null)
        {
            selectedButton.SetSelected(false);
        }

        selectedCardType = cardType;
        selectedButton = button;
        selectedButton.SetSelected(true);

        Debug.Log($"블록 {cardType} 선택됨");
    }

    public void OnBlockPlaced(bool keepSelection = true)
    {
        if (!keepSelection)
        {
            DeselectBlock();
        }
    }

    public void DeselectBlock()
    {
        if (selectedButton != null)
        {
            selectedButton.SetSelected(false);
        }
        selectedCardType = null;
        selectedButton = null;
    }

    public void EnableAllButtons()
    {
        if (activeButtons == null || gameManager == null) return;

        var turn = gameManager.GetCurrentTurn();
        var cardManager = gameManager.GetTurnManager()?.GetCardManager();

        if (turn == null || cardManager == null) return;

        foreach (var btn in activeButtons)
        {
            var button = btn.GetComponent<Button>();
            if (button != null)
            {
                int count = turn.availableBlocks.Count(b => b.type == btn.CardType);
                bool canSelect = cardManager.CanSelectCard(btn.CardType);
                button.interactable = count > 0 && canSelect;
            }
        }
    }

    public void DisableAllButtons()
    {
        if (activeButtons == null) return;

        foreach (var btn in activeButtons)
        {
            var button = btn.GetComponent<Button>();
            if (button != null)
                button.interactable = false;
        }
    }

    #region Drag (기존 코드 유지)
    public void OnBeginDrag(CardType blockType, InventoryButton button)
    {
        var cardManager = gameManager?.GetTurnManager()?.GetCardManager();
        if (cardManager != null && !cardManager.CanSelectCard(blockType))
        {
            Debug.Log($"[InventoryController] {blockType}은(는) 선택할 수 없습니다");
            return;
        }

        SelectBlock(blockType, button);
        CreateDragPreview(button);
    }

    public void OnDragging(Vector2 screenPosition)
    {
        if (dragPreview != null)
        {
            dragPreview.transform.position = screenPosition;
        }
    }

    public void OnEndDrag()
    {
        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }
    }

    private void CreateDragPreview(InventoryButton button)
    {
        if (mainCanvas == null) return;

        if (dragPreview != null)
        {
            Destroy(dragPreview);
        }

        dragPreview = new GameObject("DragPreview");
        dragPreview.transform.SetParent(mainCanvas.transform, false);
        dragPreview.transform.SetAsLastSibling();

        var image = dragPreview.AddComponent<Image>();
        var originalImage = button.GetComponent<Image>();
        if (originalImage != null)
        {
            image.sprite = originalImage.sprite;
            image.color = new Color(1, 1, 1, 0.7f);
        }

        image.raycastTarget = false;

        var rectTransform = dragPreview.GetComponent<RectTransform>();
        var originalImageRect = originalImage.GetComponent<RectTransform>();
        if (originalImageRect != null)
        {
            rectTransform.sizeDelta = originalImageRect.sizeDelta;
        }

        var originalText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (originalText != null)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(dragPreview.transform, false);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = originalText.text;
            text.fontSize = originalText.fontSize;
            text.color = new Color(originalText.color.r, originalText.color.g, originalText.color.b, 0.7f);
            text.alignment = originalText.alignment;
            text.raycastTarget = false;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }
    }
    #endregion

    public CardType? GetSelectedCardType() => selectedCardType;

    public Sprite GetSelectedBlockIcon()
    {
        return selectedButton?.GetIconSprite();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        var cardManager = gameManager?.GetTurnManager()?.GetCardManager();
        if (cardManager != null)
        {
            cardManager.OnDeckChanged -= RebuildInventoryUI;
        }
    }
}