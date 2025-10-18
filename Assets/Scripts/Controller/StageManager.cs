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
    private int currentStageId = 1;

    public System.Action<StageSO> OnStageStarted;
    public System.Action<StageSO, bool> OnStageEnded; // stage, isCleared

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
        currentStageId = stageId;

        ApplyStageToGameManager(stage);

        OnStageStarted?.Invoke(stage);

        Debug.Log($"스테이지 {stageId} 시작!");
    }

    private void ApplyStageToGameManager(StageSO stage)
    {
        if (GameManager.Instance == null) return;

        // GameManager에 스테이지 설정 적용
        GameManager.Instance.ApplyStageConfig(stage);
    }

    public void EndStage(bool isCleared)
    {
        if (currentStage == null) return;

        OnStageEnded?.Invoke(currentStage, isCleared);

        if (isCleared)
        {
            Debug.Log($"스테이지 {currentStageId} 클리어!");
        }
        else
        {
            Debug.Log($"스테이지 {currentStageId} 실패!");
        }
    }

    public StageSO GetCurrentStage() => currentStage;
    public int GetCurrentStageId() => currentStageId;
    public StageCollectionSO GetStageCollection() => stageCollection;
}
