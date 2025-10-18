using System.Collections.Generic;
using GameCore.Data;

// 점수 계산 결과를 상세히 담는 클래스
[System.Serializable]
public class ScoreBreakdown
{
    public CardType cardType;
    public int baseScore;
    public List<CardData> modifiers;
    public int finalScore;

    public ScoreBreakdown(CardType type)
    {
        cardType = type;
        modifiers = new List<CardData>();
    }
}