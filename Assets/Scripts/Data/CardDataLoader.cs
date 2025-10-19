using System.Collections.Generic;
using UnityEngine;

public class CardDataLoader : MonoBehaviour
{
    [SerializeField] private List<CardData> allCardSOList;
    public static List<CardData> allCardSOs;

    private void Awake()
    {
        allCardSOs = allCardSOList;
    }

    // 카드 타입에 해당하는 정보 가져오기
    public static CardData GetData(CardType cardType)
    {
        foreach (var card in allCardSOs)
        {
            if (card.cardType == cardType)
                return card;
        }

        return null;
    }

    // 툴팁용 포맷팅된 텍스트 반환
    public static string GetTooltipText(CardType cardType)
    {
        var info = GetData(cardType);
        return $"<b>{info.name}</b>\n\n{info.description}\n\n<color=yellow>{info.synergyDescription}</color>\n\n<color=red>{info.penaltyDescription}</color>";
    }
}