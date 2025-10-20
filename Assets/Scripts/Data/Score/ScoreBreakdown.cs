using System.Collections.Generic;
using GameCore.Data;

// 점수 계산 결과를 상세히 담는 클래스
[System.Serializable]
public class ScoreBreakdown
{
    public CardType cardType;
    public string cardName;
    public int baseScore;
    public List<CardData> modifiers;
    public int finalScore;

    public ScoreBreakdown(CardType type)
    {
        cardType = type;

        // SO에서 카드 이름 가져오기
        var cardData = CardDataLoader.GetData(type);
        cardName = cardData != null ? cardData.cardName : type.ToString();

        modifiers = new List<CardData>();
    }
}