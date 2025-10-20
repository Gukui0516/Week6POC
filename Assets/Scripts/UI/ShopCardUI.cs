using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCardUI : MonoBehaviour
{
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI baseScore;
    [SerializeField] private CardType cardType;

    // 색상 설정 추가
    [Header("Toggle Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    private Image toggleBackground; // Toggle의 배경 이미지
    private BlockTypeTooltip blockTypeTooltip; // 추가

    // 외부에서 Set 해줄 인덱스 (ShopManager가 할당)
    public int Index { get; set; }

    private void Start()
    {
        // BlockTypeTooltip 컴포넌트 가져오기
        blockTypeTooltip = GetComponent<BlockTypeTooltip>();

        // Toggle의 배경 이미지 가져오기
        if (toggle != null)
        {
            toggleBackground = toggle.targetGraphic as Image;
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            UpdateToggleColor(toggle.isOn); // 초기 색상 설정
        }
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        UpdateToggleColor(isOn);
        if (isOn) SelectType();
    }

    // 색상 업데이트 메서드 추가
    private void UpdateToggleColor(bool isOn)
    {
        if (toggleBackground != null)
        {
            toggleBackground.color = isOn ? selectedColor : normalColor;
        }
    }

    public void SetCardUI(CardType type)
    {
        var cardData = CardDataLoader.GetData(type);
        cardType = type;
        icon.sprite = cardData.iconSprite;
        cardName.text = cardData.cardName;
        description.text = cardData.description;
        baseScore.text = cardData.baseScore.ToString();

        // BlockTypeTooltip의 카드 타입도 업데이트
        if (blockTypeTooltip != null)
        {
            blockTypeTooltip.cardType = type;
            // Inventory 모드인지 확인하고 설정 (필요한 경우)
            blockTypeTooltip.SetTooltipMode(BlockTypeTooltip.TooltipMode.Inventory);
        }

        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(false);
            UpdateToggleColor(false); // 색상도 초기화
        }
    }

    public void SelectType()
    {
        shopManager.SelectShop(Index, cardType);
    }

    public void Deselect()
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(false);
            UpdateToggleColor(false); // 색상도 초기화
        }
    }
}