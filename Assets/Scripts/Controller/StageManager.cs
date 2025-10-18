using UnityEditor.SceneManagement;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    #region Singleton
    private static StageManager instance;
    public static StageManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<StageManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("StageManager");
                    instance = obj.AddComponent<StageManager>();
                }
            }
            return instance;
        }
    }
    #endregion

    [SerializeField] private StageCollectionSO stageCollection;

    private StageSO currentStage;

    public System.Action<StageSO> OnStageStarted;
    public System.Action<StageSO, bool> OnStageEnded;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        // 스테이지 초기화
        StartStage(1);

    }

    public void StartStage(int stageId)
    {
        var stage = stageCollection.GetStage(stageId);
        if (stage == null)
        {
            Debug.LogError($"스테이지 {stageId}를 찾을 수 없습니다!");
            return;
        }

        currentStage = stage;

      
        OnStageStarted?.Invoke(stage);

        Debug.Log($"스테이지 {stageId} 시작!");
    }

    public void EndStage(bool isCleared)
    {
        if (currentStage == null) return;

        OnStageEnded?.Invoke(currentStage, isCleared);

        if (isCleared)
        {
            Debug.Log($"스테이지 {currentStage.stageId} 클리어!");
        }
        else
        {
            Debug.Log($"스테이지 {currentStage.stageId} 실패!");
        }
    }

    public StageSO GetCurrentStage() => currentStage;
    public StageCollectionSO GetStageCollection() => stageCollection;
}