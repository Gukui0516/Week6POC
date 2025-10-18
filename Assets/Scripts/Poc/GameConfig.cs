using System.Collections.Generic;
using UnityEngine;
using GameCore.Data;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Turn Settings")]
    [Tooltip("게임의 최대 턴 수 (전체 턴)")]
    public int maxTurns = 20;

    [Tooltip("체크포인트: 특정 턴에 도달 시 누적 점수 체크")]
    public List<Milestone> milestones = new List<Milestone>
    {
        new Milestone(5, 50),    // 5턴까지 50점
        new Milestone(10, 100),  // 10턴까지 100점
        new Milestone(15, 150),  // 15턴까지 150점
        new Milestone(20, 200)   // 20턴까지 200점
    };

    [Header("Mode Settings")]
    public bool useNumbersMode = false; // true면 숫자 모드, false면 숫자 없음 모드

    [Header("Tile Number Distribution (WithNumbers Mode)")]
    [Range(0, 9)] public int maxTileNumber0 = 2;
    [Range(0, 9)] public int maxTileNumber1 = 3;
    [Range(0, 9)] public int maxTileNumber2 = 2;
    [Range(0, 9)] public int maxTileNumber3 = 2;
    [Range(0.1f, 10f)] public float weightTileNumber0 = 1f;
    [Range(0.1f, 10f)] public float weightTileNumber1 = 2f;
    [Range(0.1f, 10f)] public float weightTileNumber2 = 2f;
    [Range(0.1f, 10f)] public float weightTileNumber3 = 1f;

    [Header("Block Supply Settings")]
    [Range(0, 9)] public int minBlockTypes = 3;
    [Range(0, 9)] public int maxBlockTypes = 5;
    [Tooltip("true면 이전 N턴에 사용한(배치한) 블록 타입은 다음 턴에 제외")]
    public bool excludePreviousTurnTypes = false;
    [Tooltip("제외할 이전 턴의 개수 (1 = 바로 직전 턴만, 2 = 최근 2턴, 등)")]
    [Range(1, 10)] public int excludeTurnCount = 2;

    [Header("Board Settings")]
    public const int BOARD_SIZE = 3;

    // 설정 검증 메소드
    public bool IsValid()
    {
        if (maxTurns <= 0) return false;
        if (minBlockTypes > maxBlockTypes) return false;
        if (milestones == null || milestones.Count == 0) return false;

        // 마일스톤 순서 확인
        for (int i = 0; i < milestones.Count - 1; i++)
        {
            if (milestones[i].checkTurn >= milestones[i + 1].checkTurn)
                return false;
        }

        return true;
    }
}