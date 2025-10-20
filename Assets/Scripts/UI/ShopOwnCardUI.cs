using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopOwnCardUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI cardName;
    private CardType cardType;

    public void SetCardUI(CardType type)
    {
        var cardData = CardDataLoader.GetData(type);
        cardType = type;
        icon.sprite = cardData.iconSprite;
        cardName.text = cardData.cardName;
    }

    private void Start()
    {
        SetCardUI(CardType.Vampire);
    }
}