using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 개별 카드 정보 아이템 컴포넌트
/// Icon, Name, TooltipDescription 자식 오브젝트를 찾아서 CardData로 채움
/// 
/// 사용 방법:
/// 1. Info_Block 프리팹에 기본 IconDescriptionPanel 오브젝트가 있어야 함
/// 2. CardSO의 iconDescriptionPanelPrefab 필드에 각 카드에 맞는 IconDescriptionPanel 프리팹 할당
/// 3. CardInfoItem이 자동으로 기존 패널을 SO의 프리팹으로 교체
/// </summary>
public class CardInfoItem : MonoBehaviour
{
    [Header("Card Type")]
    [SerializeField] private CardType cardType;

    [Header("UI References (Auto-Find)")]
    private Image backgroundImage; // ⭐ 배경 이미지 추가
    private Image iconImage;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI tooltipDescriptionText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI baseScoreText;

    [Header("Icon Description Panel")]
    private GameObject originalIconPanel; // 기존 IconDescriptionPanel (교체될 대상)
    private GameObject replacedIconPanel; // SO에서 받아서 교체한 새 패널

    private void Awake()
    {
        FindUIReferences();
    }

    private void Start()
    {
        // CardDataLoader가 초기화된 후 데이터 로드
        LoadCardData();
    }

    private void OnDestroy()
    {
        // 생성된 IconDescriptionPanel 정리
        if (replacedIconPanel != null)
        {
            Destroy(replacedIconPanel);
        }
    }

    /// <summary>
    /// UI 참조 자동 찾기
    /// </summary>
    private void FindUIReferences()
    {
        // ⭐ 배경 이미지 찾기 (자기 자신의 Image 컴포넌트)
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            Debug.LogWarning($"[CardInfoItem] {gameObject.name}: 배경 Image 컴포넌트를 찾을 수 없습니다.");
        }

        // ⭐ 기존 IconDescriptionPanel 찾기 (교체될 대상)
        Transform iconPanelTransform = transform.Find("IconDescriptionPanel");
        if (iconPanelTransform != null)
        {
            originalIconPanel = iconPanelTransform.gameObject;
            Debug.Log($"[CardInfoItem] {gameObject.name}: 기존 IconDescriptionPanel 찾기 성공");
        }
        else
        {
            Debug.LogWarning($"[CardInfoItem] {gameObject.name}: IconDescriptionPanel 오브젝트를 찾을 수 없습니다.");
        }

        // Icon 찾기
        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null)
        {
            iconImage = iconTransform.GetComponent<Image>();
        }
        else
        {
            Debug.LogWarning($"[CardInfoItem] {gameObject.name}: Icon 오브젝트를 찾을 수 없습니다.");
        }

        // Name 찾기
        Transform nameTransform = transform.Find("Name");
        if (nameTransform != null)
        {
            nameText = nameTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning($"[CardInfoItem] {gameObject.name}: Name 오브젝트를 찾을 수 없습니다.");
        }

        // TooltipDescription 찾기
        Transform tooltipTransform = transform.Find("TooltipDescription");
        if (tooltipTransform != null)
        {
            tooltipDescriptionText = tooltipTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning($"[CardInfoItem] {gameObject.name}: TooltipDescription 오브젝트를 찾을 수 없습니다.");
        }

        // Description 찾기
        Transform descriptionTransform = transform.Find("Description");
        if (descriptionTransform != null)
        {
            descriptionText = descriptionTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning($"[CardInfoItem] {gameObject.name}: Description 오브젝트를 찾을 수 없습니다.");
        }

        // BaseScore 찾기
        Transform baseScoreTransform = transform.Find("BaseScore");
        if (baseScoreTransform != null)
        {
            baseScoreText = baseScoreTransform.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning($"[CardInfoItem] {gameObject.name}: BaseScore 오브젝트를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// CardDataLoader에서 데이터를 가져와 UI에 표시
    /// </summary>
    private void LoadCardData()
    {
        var cardData = CardDataLoader.GetData(cardType);

        if (cardData == null)
        {
            Debug.LogError($"[CardInfoItem] {cardType}의 CardData를 찾을 수 없습니다.");
            return;
        }

        // ⭐ 배경색 설정
        if (backgroundImage != null)
        {
            backgroundImage.color = cardData.backGroundColor;
            Debug.Log($"[CardInfoItem] {cardType} 배경색 설정 완료: {cardData.backGroundColor}");
        }

        // ⭐ IconDescriptionPanel 교체
        if (originalIconPanel != null && cardData.iconDescriptionPanelPrefab != null)
        {
            // 기존 패널의 위치 정보 저장
            Transform parent = originalIconPanel.transform.parent;
            int siblingIndex = originalIconPanel.transform.GetSiblingIndex();
            RectTransform originalRect = originalIconPanel.GetComponent<RectTransform>();

            // 기존 패널의 RectTransform 정보 저장
            Vector2 anchorMin = originalRect.anchorMin;
            Vector2 anchorMax = originalRect.anchorMax;
            Vector2 anchoredPosition = originalRect.anchoredPosition;
            Vector2 sizeDelta = originalRect.sizeDelta;
            Vector2 pivot = originalRect.pivot;
            Vector3 localScale = originalRect.localScale;

            // 기존 패널 제거
            Destroy(originalIconPanel);

            // SO에서 받은 새 패널 생성
            replacedIconPanel = Instantiate(cardData.iconDescriptionPanelPrefab, parent);
            replacedIconPanel.name = "IconDescriptionPanel"; // 이름 유지

            // 동일한 위치와 설정 적용
            RectTransform newRect = replacedIconPanel.GetComponent<RectTransform>();
            if (newRect != null)
            {
                newRect.anchorMin = anchorMin;
                newRect.anchorMax = anchorMax;
                newRect.anchoredPosition = anchoredPosition;
                newRect.sizeDelta = sizeDelta;
                newRect.pivot = pivot;
                newRect.localScale = localScale;
            }

            // 같은 위치에 배치
            replacedIconPanel.transform.SetSiblingIndex(siblingIndex);

            Debug.Log($"[CardInfoItem] {cardType} IconDescriptionPanel 교체 완료");
        }
        else if (originalIconPanel == null)
        {
            Debug.LogWarning($"[CardInfoItem] {cardType} - 기존 IconDescriptionPanel을 찾을 수 없습니다.");
        }
        else if (cardData.iconDescriptionPanelPrefab == null)
        {
            Debug.LogWarning($"[CardInfoItem] {cardType}의 IconDescriptionPanel 프리팹이 설정되지 않았습니다.");
        }

        // 아이콘 설정
        if (iconImage != null && cardData.iconSprite != null)
        {
            iconImage.sprite = cardData.iconSprite;
            Debug.Log($"[CardInfoItem] {cardType} 아이콘 설정 완료");
        }

        // 이름 설정
        if (nameText != null && !string.IsNullOrEmpty(cardData.cardName))
        {
            nameText.text = $"{cardData.cardName}";
            Debug.Log($"[CardInfoItem] {cardType} 이름 설정 완료: {cardData.cardName}");
        }

        // TooltipDescription 설정 (SO의 tooltipDescription)
        if (tooltipDescriptionText != null && !string.IsNullOrEmpty(cardData.tooltipDescription))
        {
            tooltipDescriptionText.text = $"{cardData.tooltipDescription}";
            Debug.Log($"[CardInfoItem] {cardType} TooltipDescription 설정 완료");
        }

        // Description 설정 (SO의 description)
        if (descriptionText != null && !string.IsNullOrEmpty(cardData.description))
        {
            descriptionText.text = $"{cardData.description}";
            Debug.Log($"[CardInfoItem] {cardType} Description 설정 완료");
        }

        // BaseScore 설정 (SO의 baseScore)
        if (baseScoreText != null)
        {
            baseScoreText.text = $"기본점수: {cardData.baseScore}";
            Debug.Log($"[CardInfoItem] {cardType} BaseScore 설정 완료: {cardData.baseScore}");
        }
    }

    /// <summary>
    /// CardType 설정 (Inspector나 코드에서 사용)
    /// </summary>
    public void SetCardType(CardType type)
    {
        cardType = type;
        if (Application.isPlaying)
        {
            LoadCardData();
        }
    }

    /// <summary>
    /// 수동으로 데이터 새로고침
    /// </summary>
    public void RefreshData()
    {
        LoadCardData();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 컴포넌트 추가 시 자동 설정
    /// </summary>
    private void Reset()
    {
        FindUIReferences();
    }
#endif
}