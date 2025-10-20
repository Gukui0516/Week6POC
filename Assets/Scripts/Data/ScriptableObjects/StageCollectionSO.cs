
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 컬렉션
/// </summary>
[CreateAssetMenu(fileName = "StageCollection", menuName = "Game/Stage Collection")]
public class StageCollectionSO : ScriptableObject
{
    public List<StageSO> stages = new List<StageSO>();

    public StageSO GetStage(int stageId)
    {
        return stages.Find(s => s.stageId == stageId);
    }

    public int GetTotalStageCount()
    {
        return stages.Count;
    }
}