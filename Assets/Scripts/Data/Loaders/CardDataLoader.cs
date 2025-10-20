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
        if (info == null) return "카드 정보를 찾을 수 없습니다.";

        // 기본 정보: 카드 이름, 툴팁 설명, 기본 점수
        string tooltip = $"<b>{info.cardName}</b>\n\n";

        // tooltipDescription이 있으면 추가
        if (!string.IsNullOrEmpty(info.tooltipDescription))
        {
            tooltip += $"{info.tooltipDescription}\n\n";
        }

        tooltip += $"<color=#FFD700>기본 점수: {info.baseScore}</color>\n";

        tooltip += "\n";

        // 시너지가 있으면 추가
        if (!string.IsNullOrEmpty(info.synergyDescription))
        {
            tooltip += $"<color=yellow>{info.synergyDescription}</color>";
        }

        tooltip += "\n";

        // 패널티가 있으면 추가
        if (!string.IsNullOrEmpty(info.penaltyDescription))
        {
            tooltip += $"<color=red>{info.penaltyDescription}</color>";
        }

        return tooltip;
    }
}