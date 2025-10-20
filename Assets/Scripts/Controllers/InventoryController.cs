using GameCore.Data;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 전용 컨트롤러 - 블록 선택 및 인벤토리 UI 관리
/// CardManager 기반으로 동작
/// </summary>
public class InventoryController : MonoBehaviour
{
    private GameManager gameManager;
    private InventoryButton[] blockButtons;

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

        // 모든 인벤토리 버튼 찾기
        blockButtons = FindObjectsByType<InventoryButton>(FindObjectsSortMode.None);

        Debug.Log($"[InventoryController] 발견된 블록 버튼: {blockButtons?.Length ?? 0}개");

        // 각 버튼에 참조 설정
        foreach (var btn in blockButtons)
        {
            btn.SetInventoryController(this);
        }

        mainCanvas = FindFirstObjectByType<Canvas>();
    }

    /// <summary>
    /// 인벤토리 UI 업데이트 (활성 카드 기반)
    /// </summary>
    public void UpdateInventory()
    {
        if (blockButtons == null || gameManager?.GetCurrentTurn() == null) return;

        var turn = gameManager.GetCurrentTurn();
        var cardManager = gameManager.GetTurnManager()?.GetCardManager();

        if (cardManager == null)
        {
            Debug.LogWarning("[InventoryController] CardManager를 찾을 수 없습니다");
            return;
        }

        // 활성 카드 개수 계산
        var activeCards = turn.availableBlocks.GroupBy(b => b.type).ToDictionary(g => g.Key, g => g.Count());

        foreach (var btn in blockButtons)
        {
            int count = activeCards.ContainsKey(btn.CardType) ? activeCards[btn.CardType] : 0;

            // 선택 가능 여부 확인
            bool canSelect = cardManager.CanSelectCard(btn.CardType);

            // UI 업데이트 (개수, 선택 가능 여부)
            btn.UpdateDisplay(count, canSelect);
        }
    }

    /// <summary>
    /// 블록 선택
    /// </summary>
    public void SelectBlock(CardType CardType, InventoryButton button)
    {
        var cardManager = gameManager?.GetTurnManager()?.GetCardManager();
        if (cardManager == null) return;

        // 선택 가능한지 확인
        if (!cardManager.CanSelectCard(CardType))
        {
            Debug.Log($"[InventoryController] {CardType}은(는) 이전 턴에 사용하여 선택할 수 없습니다");
            return;
        }

        // 같은 블록을 다시 선택하면 선택 해제 (토글)
        if (selectedCardType == CardType && selectedButton == button)
        {
            DeselectBlock();
            Debug.Log($"[InventoryController] {CardType} 선택 해제");
            return;
        }

        // 이전 선택 해제
        if (selectedButton != null)
        {
            selectedButton.SetSelected(false);
        }

        selectedCardType = CardType;
        selectedButton = button;
        selectedButton.SetSelected(true);

        Debug.Log($"블록 {CardType} 선택됨. 배치할 빈 타일을 클릭하세요.");
    }

    /// <summary>
    /// 블록 배치 후 호출 - 선택 상태 유지 여부 결정
    /// </summary>
    public void OnBlockPlaced(bool keepSelection = true)
    {
        if (!keepSelection)
        {
            DeselectBlock();
        }
        else
        {
            // 선택 상태 유지 - 추가 배치 가능
            Debug.Log($"[InventoryController] {selectedCardType} 선택 유지 - 계속 배치 가능");
        }
    }

    /// <summary>
    /// 선택 해제
    /// </summary>
    public void DeselectBlock()
    {
        if (selectedButton != null)
        {
            selectedButton.SetSelected(false);
        }
        selectedCardType = null;
        selectedButton = null;
    }

    /// <summary>
    /// 모든 버튼 활성화
    /// </summary>
    public void EnableAllButtons()
    {
        if (blockButtons == null || gameManager == null) return;

        var turn = gameManager.GetCurrentTurn();
        var cardManager = gameManager.GetTurnManager()?.GetCardManager();

        if (turn == null || cardManager == null) return;

        foreach (var btn in blockButtons)
        {
            var button = btn.GetComponent<Button>();
            if (button != null)
            {
                int count = turn.availableBlocks.Count(b => b.type == btn.CardType);
                bool canSelect = cardManager.CanSelectCard(btn.CardType);

                // 개수가 있고 선택 가능할 때만 활성화
                button.interactable = count > 0 && canSelect;
            }
        }
    }

    /// <summary>
    /// 모든 버튼 비활성화
    /// </summary>
    public void DisableAllButtons()
    {
        if (blockButtons == null) return;

        foreach (var btn in blockButtons)
        {
            var button = btn.GetComponent<Button>();
            if (button != null)
                button.interactable = false;
        }
    }

    #region Drag
    public void OnBeginDrag(CardType blockType, InventoryButton button)
    {
        // 선택 가능한지 먼저 확인
        var cardManager = gameManager?.GetTurnManager()?.GetCardManager();
        if (cardManager != null && !cardManager.CanSelectCard(blockType))
        {
            Debug.Log($"[InventoryController] {blockType}은(는) 선택할 수 없습니다");
            return;
        }

        // 블록 선택
        SelectBlock(blockType, button);

        // 드래그 프리뷰 생성
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

        // 크기 설정 (원본 버튼의 이미지 크기 반영)
        var rectTransform = dragPreview.GetComponent<RectTransform>();
        var originalImageRect = originalImage.GetComponent<RectTransform>();
        if (originalImageRect != null)
        {
            // 원본 이미지의 sizeDelta를 복사
            rectTransform.sizeDelta = originalImageRect.sizeDelta;
        }
        else
        {
            rectTransform.sizeDelta = button.GetComponent<RectTransform>().sizeDelta;
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

        Debug.Log($"[InventoryController] 드래그 프리뷰 생성: {selectedCardType}");
    }
    #endregion

    public CardType? GetSelectedCardType() => selectedCardType;

    /// <summary>
    /// 선택된 블록의 아이콘 스프라이트 가져오기
    /// </summary>
    public Sprite GetSelectedBlockIcon()
    {
        return selectedButton?.GetIconSprite();
    }
}