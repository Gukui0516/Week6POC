using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;
using GameCore.Data;

/// <summary>
/// 인벤토리 블록 버튼 컴포넌트
/// 활성화 여부와 선택 가능 여부를 구분하여 표시
/// </summary>
public class InventoryButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public BlockType blockType;
    private InventoryController inventoryController;
    private Button button;
    private Image buttonImage;
    private TextMeshProUGUI text;
    private Color originalColor;

    // Drag
    private bool isDragging = false;
    private Vector3 originalScale;

    // 상태 표시용 오버레이
    private GameObject disabledOverlay;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        originalColor = buttonImage.color;
        originalScale = transform.localScale;

        button.onClick.AddListener(OnClick);

        CreateDisabledOverlay();
    }

    /// <summary>
    /// 비활성화 오버레이 생성 (선택 불가능 표시용)
    /// </summary>
    private void CreateDisabledOverlay()
    {
        disabledOverlay = new GameObject("DisabledOverlay");
        disabledOverlay.transform.SetParent(transform, false);

        var rect = disabledOverlay.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        var image = disabledOverlay.AddComponent<Image>();
        image.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // 회색 반투명
        image.raycastTarget = false;

        // 금지 아이콘 텍스트 추가
        var iconObj = new GameObject("DisabledIcon");
        iconObj.transform.SetParent(disabledOverlay.transform, false);

        var iconText = iconObj.AddComponent<TextMeshProUGUI>();
        iconText.text = "✖"; // 또는 "🚫"
        iconText.fontSize = 40;
        iconText.color = new Color(1f, 0.3f, 0.3f); // 빨간색
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.fontStyle = FontStyles.Bold;
        iconText.raycastTarget = false;

        var iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.sizeDelta = Vector2.zero;

        disabledOverlay.SetActive(false);
    }

    public void SetInventoryController(InventoryController controller)
    {
        inventoryController = controller;
    }

    private void OnClick()
    {
        if (isDragging) return;

        if (GameManager.Instance == null || inventoryController == null) return;

        var turn = GameManager.Instance.GetCurrentTurn();
        if (turn == null || turn.availableBlocks == null) return;

        var availableBlock = turn.availableBlocks.FirstOrDefault(b => b.type == blockType);
        if (availableBlock == null)
        {
            Debug.Log($"블록 타입 {blockType}이(가) 활성 카드에 없습니다.");
            return;
        }

        // 선택 가능한지 확인
        var cardManager = GameManager.Instance.GetTurnManager()?.GetCardManager();
        if (cardManager != null && !cardManager.CanSelectCard(blockType))
        {
            Debug.Log($"{blockType}은(는) 이전 턴에 사용하여 선택할 수 없습니다.");
            return;
        }

        inventoryController.SelectBlock(blockType, this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameManager.Instance == null || inventoryController == null) return;

        var turn = GameManager.Instance.GetCurrentTurn();
        if (turn == null) return;

        var availableBlock = turn.availableBlocks.FirstOrDefault(b => b.type == blockType);
        if (availableBlock == null) return;

        // 선택 가능한지 확인
        var cardManager = GameManager.Instance.GetTurnManager()?.GetCardManager();
        if (cardManager != null && !cardManager.CanSelectCard(blockType))
        {
            Debug.Log($"{blockType}은(는) 선택할 수 없습니다.");
            return;
        }

        isDragging = true;
        inventoryController.OnBeginDrag(blockType, this);
        transform.localScale = Vector3.one * 1.2f;
    }

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            buttonImage.color = Color.yellow;
            transform.localScale = Vector3.one * 1.1f;
        }
        else
        {
            buttonImage.color = originalColor;
            transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// 카드 개수와 선택 가능 여부를 함께 업데이트
    /// </summary>
    public void UpdateDisplay(int count, bool canSelect)
    {
        if (text != null)
        {
            // 선택 불가능한 경우 표시 추가
            string selectableText = canSelect ? "" : " [사용불가]";
            text.text = $"{blockType}\n×{count}{selectableText}";

            if (button != null)
            {
                // 개수가 있고 선택 가능할 때만 활성화
                button.interactable = count > 0 && canSelect;
            }

            // 텍스트 색상 조절
            if (count == 0)
            {
                text.color = new Color(1f, 1f, 1f, 0.3f);
            }
            else if (!canSelect)
            {
                text.color = new Color(1f, 0.5f, 0.5f); // 빨간 톤
            }
            else
            {
                text.color = Color.black;
            }
        }

        // 오버레이 표시 (개수는 있지만 선택 불가능)
        if (disabledOverlay != null)
        {
            disabledOverlay.SetActive(count > 0 && !canSelect);
        }
    }

    /// <summary>
    /// 레거시 메서드 (하위 호환성)
    /// </summary>
    public void UpdateCount(int count)
    {
        UpdateDisplay(count, true);
    }

    private void OnDestroy()
    {
        if (TooltipController.Instance != null)
        {
            TooltipController.Instance.HideTooltip();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        inventoryController.OnDragging(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        transform.localScale = originalScale;
        inventoryController.OnEndDrag();
    }
}