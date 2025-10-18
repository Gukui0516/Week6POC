using UnityEngine;
using System.Collections.Generic;
using GameCore.Data;

[CreateAssetMenu(fileName = "Stage_", menuName = "Game/Stage Data")]
public class StageSO : ScriptableObject
{
    public int stageId;
    public int maxTurns = 10;
    public int blocksPerTurn = 4;
    public int endTurn = 3;
    public int target = 50;
    public int firstDraw = 4;
    public int secondDraw = 2;
    public int lastDraw = 1;
    public bool excludePreviousTurnTypes = true;
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