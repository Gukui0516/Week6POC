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
public class InventoryButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    public CardType CardType;

    private InventoryController inventoryController;
    private Button button;
    private Image buttonImage;
    private GameObject buttonIcon;
    private TextMeshProUGUI text;
    private Color originalColor;

    // Drag
    private bool isDragging = false;
    private Vector3 originalScale;

    // 상태 표시용 오버레이
    private GameObject disabledOverlay;

    // ToolBox 관련
    private bool isHoveringForToolBox = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        originalColor = buttonImage.color;
        originalScale = transform.localScale;

        // 버튼 아이콘 찾기
        Transform iconTransform = transform.Find("ButtonIcon");
        if (iconTransform == null)
            iconTransform = transform.Find("Icon");
        if (iconTransform == null)
            iconTransform = transform.Find("Sprite");

        // Image 컴포넌트를 가진 자식 찾기
        if (iconTransform == null)
        {
            Image[] childImages = GetComponentsInChildren<Image>();
            foreach (var img in childImages)
            {
                if (img != buttonImage && img.GetComponent<TextMeshProUGUI>() == null)
                {
                    iconTransform = img.transform;
                    Debug.Log($"[InventoryButton] 아이콘 찾음: {img.gameObject.name}");
                    break;
                }
            }
        }

        if (iconTransform != null)
        {
            buttonIcon = iconTransform.gameObject;
        }
        else
        {
            Debug.LogWarning($"[InventoryButton] ButtonIcon을 찾지 못함.");
        }

        button.onClick.AddListener(OnClick);

        CreateDisabledOverlay();
    }

    private void Start()
    {
        // CardDataLoader가 초기화된 후 아이콘 로드
        LoadIconFromCardData();
    }

    /// <summary>
    /// CardData SO에서 아이콘 스프라이트 로드
    /// </summary>
    private void LoadIconFromCardData()
    {
        var cardData = CardDataLoader.GetData(CardType);
        if (cardData != null && cardData.iconSprite != null && buttonIcon != null)
        {
            Image iconImage = buttonIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = cardData.iconSprite;
                Debug.Log($"[InventoryButton] {CardType} 아이콘 로드 완료");
            }
            else
            {
                Debug.LogWarning($"[InventoryButton] ButtonIcon에 Image 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning($"[InventoryButton] {CardType}의 CardData 또는 iconSprite를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 현재 카드의 아이콘 스프라이트 가져오기
    /// </summary>
    public Sprite GetIconSprite()
    {
        var cardData = CardDataLoader.GetData(CardType);
        return cardData?.iconSprite;
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
        image.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        image.raycastTarget = false;

        // 금지 아이콘 텍스트 추가
        var iconObj = new GameObject("DisabledIcon");
        iconObj.transform.SetParent(disabledOverlay.transform, false);

        var iconText = iconObj.AddComponent<TextMeshProUGUI>();
        iconText.text = "✖";
        iconText.fontSize = 40;
        iconText.color = new Color(1f, 0.3f, 0.3f);
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

        var availableBlock = turn.availableBlocks.FirstOrDefault(b => b.type == CardType);
        if (availableBlock == null)
        {
            Debug.Log($"블록 타입 {CardType}이(가) 활성 카드에 없습니다.");
            return;
        }

        // 선택 가능한지 확인
        var cardManager = GameManager.Instance.GetTurnManager()?.GetCardManager();
        if (cardManager != null && !cardManager.CanSelectCard(CardType))
        {
            Debug.Log($"{CardType}은(는) 이전 턴에 사용하여 선택할 수 없습니다.");
            return;
        }

        inventoryController.SelectBlock(CardType, this);
    }

    #region Drag Handlers

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameManager.Instance == null || inventoryController == null) return;

        var turn = GameManager.Instance.GetCurrentTurn();
        if (turn == null) return;

        var availableBlock = turn.availableBlocks.FirstOrDefault(b => b.type == CardType);
        if (availableBlock == null) return;

        // 선택 가능한지 확인
        var cardManager = GameManager.Instance.GetTurnManager()?.GetCardManager();
        if (cardManager != null && !cardManager.CanSelectCard(CardType))
        {
            Debug.Log($"{CardType}은(는) 선택할 수 없습니다.");
            return;
        }

        isDragging = true;
        inventoryController.OnBeginDrag(CardType, this);
        transform.localScale = Vector3.one * 1.2f;

        // 드래그 시작하면 ToolBox 숨기기
        HideToolBoxInfo();
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

    #endregion

    #region Pointer Handlers (ToolBox)

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 드래그 중이 아닐 때만 ToolBox 표시
        if (!isDragging)
        {
            isHoveringForToolBox = true;
            ShowToolBoxInfo();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isHoveringForToolBox)
        {
            isHoveringForToolBox = false;
            HideToolBoxInfo();
        }
    }

    #endregion

    #region ToolBox System

    private void ShowToolBoxInfo()
    {
        if (ToolBoxController.Instance == null) return;

        // 카드 정보 가져오기
        string toolboxText = CardDataLoader.GetTooltipText(CardType);

        // 활성 상태 추가 표시
        if (GameManager.Instance != null)
        {
            var turn = GameManager.Instance.GetCurrentTurn();
            if (turn != null)
            {
                int count = turn.availableBlocks.Count(b => b.type == CardType);
                var cardManager = GameManager.Instance.GetTurnManager()?.GetCardManager();
                bool canSelect = cardManager?.CanSelectCard(CardType) ?? true;

                string statusText = $"\n\n<color=cyan>보유 개수: {count}</color>";
                if (!canSelect)
                {
                    statusText += "\n<color=red>[이전 턴에 사용하여 선택 불가]</color>";
                }

                toolboxText += statusText;
            }
        }

        ToolBoxController.Instance.ShowToolBox(toolboxText);
    }

    private void HideToolBoxInfo()
    {
        if (ToolBoxController.Instance != null)
        {
            ToolBoxController.Instance.HideToolBox();
        }
    }

    #endregion

    public void SetSelected(bool selected)
    {
        if (selected)
        {
            buttonImage.color = Color.yellow;
        }
        else
        {
            buttonImage.color = originalColor;
        }
    }

    /// <summary>
    /// 카드 개수와 선택 가능 여부를 함께 업데이트
    /// </summary>
    public void UpdateDisplay(int count, bool canSelect)
    {
        if (text != null)
        {
            string selectableText = canSelect ? "" : " [사용불가]";
            text.text = $"{selectableText}";

            if (button != null)
            {
                button.interactable = count > 0 && canSelect;
            }

            // 텍스트 색상 조절
            if (count == 0)
            {
                text.color = new Color(1f, 1f, 1f, 0.3f);
            }
            else if (!canSelect)
            {
                text.color = new Color(1f, 0.5f, 0.5f);
            }
            else
            {
                text.color = Color.black;
            }
        }

        // 오버레이 표시
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
        // ToolBox 숨기기
        if (isHoveringForToolBox && ToolBoxController.Instance != null)
        {
            ToolBoxController.Instance.HideToolBox();
        }
    }
}