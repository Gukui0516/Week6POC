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
            Debug.Log($"[InventoryButton] ButtonIcon 설정됨: {buttonIcon.name}");
        }
        else
        {
            Debug.LogWarning($"[InventoryButton] ButtonIcon을 찾지 못함. buttonImage를 직접 스케일합니다.");
        }

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

    // 점수를 기반으로 스케일 계산 (1점=0.5, 15점=1.0)
    private float CalculateScaleFromScore(int score)
    {
        int clampedScore = Mathf.Clamp(score, 1, 15);
        float scale = 0.5f + (clampedScore - 1) * (0.5f / 14f);
        return scale;
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

        // 드래그 중 시각적 피드백
        int baseScore = GameManager.Instance.GetCardBaseScore(CardType);
        float baseScale = CalculateScaleFromScore(baseScore);

        Transform targetTransform = buttonIcon != null ? buttonIcon.transform : buttonImage.transform;
        if (targetTransform != null)
        {
            targetTransform.localScale = Vector3.one * baseScale * 1.2f;
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

        // 원래 크기로 복원
        int baseScore = GameManager.Instance?.GetCardBaseScore(CardType) ?? 1;
        float baseScale = CalculateScaleFromScore(baseScore);

        Transform targetTransform = buttonIcon != null ? buttonIcon.transform : buttonImage.transform;
        if (targetTransform != null)
        {
            targetTransform.localScale = Vector3.one * baseScale;
        }

        inventoryController.OnEndDrag();
    }

    public void SetSelected(bool selected)
    {
        Transform targetTransform = buttonIcon != null ? buttonIcon.transform : buttonImage.transform;

        if (selected)
        {
            buttonImage.color = Color.yellow;

            int baseScore = GameManager.Instance?.GetCardBaseScore(CardType) ?? 1;
            float baseScale = CalculateScaleFromScore(baseScore);

            if (targetTransform != null)
            {
                targetTransform.localScale = Vector3.one * baseScale * 1.1f;
            }
        }
        else
        {
            buttonImage.color = originalColor;

            int baseScore = GameManager.Instance?.GetCardBaseScore(CardType) ?? 1;
            float baseScale = CalculateScaleFromScore(baseScore);

            if (targetTransform != null)
            {
                targetTransform.localScale = Vector3.one * baseScale;
            }
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
            text.text = $"{CardType}\n×{count}{selectableText}";

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

        // 블록의 기본 점수에 따라 아이콘 크기 설정
        if (GameManager.Instance != null && count > 0)
        {
            int baseScore = GameManager.Instance.GetCardBaseScore(CardType);
            float scale = CalculateScaleFromScore(baseScore);

            // 선택되지 않은 상태에서만 기본 크기 적용
            if (buttonImage.color != Color.yellow)
            {
                Transform targetTransform = buttonIcon != null ? buttonIcon.transform : buttonImage.transform;
                if (targetTransform != null)
                {
                    targetTransform.localScale = Vector3.one * scale;
                }
            }
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
}