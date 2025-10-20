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

    // 외부에서 Set 해줄 인덱스 (ShopManager가 할당)
    public int Index { get; set; }

    private void Start()
    {
        if (toggle != null)
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        if (isOn) SelectType();
    }

    public void SetCardUI(CardType type)
    {
        var cardData = CardDataLoader.GetData(type);
        cardType = type;
        icon.sprite = cardData.iconSprite;
        cardName.text = cardData.cardName;
        description.text = cardData.description;
        baseScore.text = cardData.baseScore.ToString();

        if (toggle != null) toggle.SetIsOnWithoutNotify(false);
    }

    public void SelectType()
    {
        shopManager.SelectShop(Index, cardType);
    }

    public void Deselect()
    {
        if (toggle != null) toggle.SetIsOnWithoutNotify(false);
    }
}