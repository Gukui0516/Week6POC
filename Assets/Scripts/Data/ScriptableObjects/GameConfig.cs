using System.Collections.Generic;
using UnityEngine;
using GameCore.Data;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Turn Settings")]
    [Tooltip("게임의 최대  stage  수 (전체 턴)")]
    public int EndStage = 20;

 

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

/*    [Header("Block Supply Settings")]
    [Range(0, 9)] public int minBlockTypes = 3;
    [Range(0, 9)] public int maxBlockTypes = 5;
    [Tooltip("true면 이전 N턴에 사용한(배치한) 블록 타입은 다음 턴에 제외")]
    public bool excludePreviousTurnTypes = false;

    */
    [Tooltip("제외할 이전 턴의 개수 (1 = 바로 직전 턴만, 2 = 최근 2턴, 등)")]
    [Range(1, 10)] public int excludeTurnCount = 2;

    [Header("Board Settings")]
    public const int BOARD_SIZE = 3;

    // 설정 검증 메소드
    public bool IsValid()
    {
        if (EndStage <= 0) return false;
        //if (minBlockTypes > maxBlockTypes) return false;

       
        return true;
    }
}