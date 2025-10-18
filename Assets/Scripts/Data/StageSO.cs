using UnityEngine;
using System.Collections.Generic;
using GameCore.Data;

[CreateAssetMenu(fileName = "Stage_", menuName = "Game/Stage Data")]
public class StageSO : ScriptableObject
{
    public int stageId; //현재 스테이지
    public int endTurn = 3; // 최종 턴
    public int target = 50; // 목표 수
    public int firstDraw = 4; // 1 턴 드로우 수
    public int secondDraw = 2;  // 2 턴 드로우 수
    public int lastDraw = 1; // 3 턴 드로우 수
    public bool excludePreviousTurnTypes = true;

    public List<int> ActiveCard; // 매 턴 새로 해금되는 카드
    //1, 2, 3, 4, 5, 6, 7 
}

[CreateAssetMenu(fileName = "StageCollection", menuName = "Game/Stage Collection")]
public class StageCollectionSO : ScriptableObject
{
    public List<StageSO> stages = new List<StageSO>();

    public StageSO GetStage(int stageId)
    {
        return stages.Find(s => s.stageId == stageId);
    }
}