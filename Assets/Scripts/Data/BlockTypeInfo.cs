using UnityEngine;
using GameCore.Data;

// 블록 타입별 정보를 제공하는 정적 클래스
public static class BlockTypeInfo
{
    public class Info
    {
        public string name;
        public string description;
        public string effect;
        public int baseScore;

        public Info(string name, string description, string effect, int baseScore)
        {
            this.name = name;
            this.description = description;
            this.effect = effect;
            this.baseScore = baseScore;
        }
    }

    // 블록 타입별 정보 데이터
    private static readonly System.Collections.Generic.Dictionary<BlockType, Info> blockInfos =
        new System.Collections.Generic.Dictionary<BlockType, Info>
    {
        { BlockType.A, new Info(
            "블록 A",
            "주변에 같은 타입이 많으면 불리함",
            "기본 점수: +2\n인접한 A가 2개 이상: -1",
            2
        )},

        { BlockType.B, new Info(
            "블록 B",
            "다른 타입과 인접할 때 유리함",
            "기본 점수: +1\nB가 아닌 인접 블록 2개 이상: +1",
            1
        )},

        { BlockType.C, new Info(
            "블록 C",
            "같은 타입끼리 모일수록 강함",
            "기본 점수: 0\n인접한 C 1개당: +1",
            0
        )},

        { BlockType.D, new Info(
            "블록 D",
            "적당히 모여야 좋음",
            "기본 점수: +1\n인접한 D가 1~2개: +1\n인접한 D가 3개 이상: -1",
            1
        )},

        { BlockType.E, new Info(
            "블록 E",
            "보드가 채워질수록 불리함",
            "기본 점수: +4\n점수 = 4 - (다른 E 개수 + 빈칸 개수)",
            4
        )},

        { BlockType.F, new Info(
            "블록 F",
            "다양성을 선호하는 블록",
            "기본 점수: 0\nF 제외 블록 종류 수만큼: +1\n다른 F 1개당: -1",
            0
        )},

        { BlockType.G, new Info(
            "블록 G",
            "고립되어야 강력함",
            "기본 점수: +5\n인접한 블록 1개당: -1\n인접한 빈칸 1개당: -2",
            5
        )}
    };

    // 블록 타입에 해당하는 정보 가져오기
    public static Info GetInfo(BlockType type)
    {
        if (blockInfos.ContainsKey(type))
            return blockInfos[type];

        return new Info("알 수 없음", "정보 없음", "효과 없음", 0);
    }

    // 툴팁용 포맷팅된 텍스트 반환
    public static string GetTooltipText(BlockType type)
    {
        var info = GetInfo(type);
        return $"<b>{info.name}</b>\n\n{info.description}\n\n<color=yellow>{info.effect}</color>";
    }
}