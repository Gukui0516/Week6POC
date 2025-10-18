using System.Collections.Generic;
using GameCore.Data;

// 점수 계산 결과를 상세히 담는 클래스
[System.Serializable]
public class ScoreBreakdown
{
    public BlockType blockType;
    public int baseScore;
    public List<ScoreModifier> modifiers;
    public int finalScore;

    public ScoreBreakdown(BlockType type)
    {
        blockType = type;
        modifiers = new List<ScoreModifier>();
    }
}

// 점수 수정 요소
[System.Serializable]
public class ScoreModifier
{
    public string description;  // "인접한 A 블록 2개"
    public int value;          // -1
    public string formula;     // "2개 이상: -1"

    public ScoreModifier(string description, int value, string formula)
    {
        this.description = description;
        this.value = value;
        this.formula = formula;
    }
}