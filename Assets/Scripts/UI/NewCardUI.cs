using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewCardUI : MonoBehaviour
{
    [SerializeField] private StageManager stageManager;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI baseScore;

    public void SetCardUI(CardType type)
    {
        var cardData = CardDataLoader.GetData(type);
        icon.sprite = cardData.iconSprite;
        cardName.text = cardData.cardName;
        description.text = cardData.description;
        baseScore.text = cardData.baseScore.ToString();
    }
}