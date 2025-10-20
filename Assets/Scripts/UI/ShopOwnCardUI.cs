using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopOwnCardUI : MonoBehaviour
{
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI cardName;
    private CardType cardType;

    // 외부에서 할당될 슬롯 인덱스
    public int Index { get; set; }

    private void Start()
    {
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
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
        cardName.text = cardData.cardName;
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