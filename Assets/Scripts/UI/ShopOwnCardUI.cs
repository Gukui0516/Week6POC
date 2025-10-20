using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopOwnCardUI : MonoBehaviour
{
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private BlockTypeTooltip blockTypeTooltip; // 추가

    [SerializeField] private Image image;

    private CardType cardType;

    // 외부에서 할당될 슬롯 인덱스
    public int Index { get; set; }

    private void Start()
    {
        toggle.onValueChanged.AddListener(OnToggleValueChanged);

        // BlockTypeTooltip이 없으면 자동으로 찾기
        if (blockTypeTooltip == null)
        {
            blockTypeTooltip = GetComponent<BlockTypeTooltip>();
        }
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        if (!isOn) return;
        SelectType();
    }

    public void SetCardUI(CardType type)
    {
        var cardData = CardDataLoader.GetData(type);
        cardType = type;
        icon.sprite = cardData.iconSprite;
        image.color = cardData.backGroundColor;
        cardName.text = cardData.cardName;

        // BlockTypeTooltip의 카드 타입도 업데이트
        if (blockTypeTooltip != null)
        {
            blockTypeTooltip.cardType = type;
            // Inventory 모드인지 확인하고 설정 (필요한 경우)
            blockTypeTooltip.SetTooltipMode(BlockTypeTooltip.TooltipMode.Inventory);
        }
    }

    public void SelectType()
    {
        shopManager.SelectDeck(Index, cardType);
    }

    public void Deselect()
    {
        if (toggle != null) toggle.SetIsOnWithoutNotify(false);
    }
}