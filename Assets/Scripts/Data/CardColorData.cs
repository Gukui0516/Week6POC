using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CardColorData
{
    public CardType cardType;
    public Color32 color;
}

public class CardColorDataList : MonoBehaviour
{
    [SerializeField] private List<CardColorData> cardColorList;
    private static List<CardColorData> cardColors;

    private void Awake()
    {
        cardColors = cardColorList;
    }

    public static Color32 GetColorByCardType(CardType type)
    {
        foreach (var cardColor in cardColors)
        {
            if (cardColor.cardType == type)
            {
                return cardColor.color;
            }
        }

        return new Color32(255, 255, 255, 255); // Default to white if not found
    }
}