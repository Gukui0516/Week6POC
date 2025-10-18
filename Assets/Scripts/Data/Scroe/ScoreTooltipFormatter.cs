using System.Text;
using GameCore.Data;

// 점수 계산식을 툴팁용 텍스트로 포맷팅하는 클래스 (컴팩트 버전)
public static class ScoreTooltipFormatter
{
    public static string GetScoreBreakdownText(ScoreBreakdown breakdown)
    {
        if (breakdown == null) return "점수 정보 없음";

        var sb = new StringBuilder();

        // 첫 번째 줄: 헤더와 기본 점수
        sb.AppendLine($"<color=white>블록 {breakdown.blockType}</color>\n\n<color=yellow>기본점수: +{breakdown.baseScore}</color>");

        // 두 번째 줄: 수정자들 (있을 때만)
        if (breakdown.modifiers.Count > 0)
        {
            for (int i = 0; i < breakdown.modifiers.Count; i++)
            {
                var modifier = breakdown.modifiers[i];
                string sign = modifier.value >= 0 ? "+" : "";
                string valueColor = modifier.value > 0 ? "green" : (modifier.value < 0 ? "red" : "yellow");

                if (i > 0) sb.AppendLine();
                sb.Append($" <color=yellow>{modifier.description}:</color> <color={valueColor}>{sign}{modifier.value}</color> ");
            }
            sb.AppendLine();
        }
        sb.AppendLine();

        // 세 번째 줄: 최종 점수
        string finalColor = breakdown.finalScore > 0 ? "green" : (breakdown.finalScore < 0 ? "red" : "yellow");
        sb.Append($"<color={finalColor}><b>최종 점수: {breakdown.finalScore:+#;-#;0}</b></color>");

        return sb.ToString();
    }
}