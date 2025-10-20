using UnityEngine;

/// <summary>
/// 스테이지 생명주기 관리 전담
/// Stage 1 → Stage 2 → Stage 3...
/// </summary>
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
    [SerializeField] private NewCardUI newCardUI;

    private StageSO currentStage;
    private int currentStageId = 0;

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

    /// <summary>
    /// 특정 스테이지 시작
    /// </summary>
    public void StartStage(int stageId)
    {
        var stage = stageCollection.GetStage(stageId);
        if (stage == null)
        {
            Debug.LogError($"[StageManager] 스테이지 {stageId}를 찾을 수 없습니다!");
            return;
        }

        currentStage = stage;
        currentStageId = stageId;

        Debug.Log($"[StageManager] 스테이지 {stageId} 시작!");
        OnStageStarted?.Invoke(stage);
    }

    /// <summary>
    /// 다음 스테이지로 이동
    /// </summary>
    public void MoveToNextStage()
    {
        int nextStageId = currentStageId + 1;

        if (nextStageId > stageCollection.GetTotalStageCount())
        {
            Debug.Log($"[StageManager] 모든 스테이지 완료! 게임 클리어!");
            return;
        }

        StartStage(nextStageId);
    }

    /// <summary>
    /// 스테이지 종료 (성공/실패)
    /// </summary>
    public void EndStage(bool isCleared)
    {
        if (currentStage == null) return;

        Debug.Log($"[StageManager] 스테이지 {currentStageId} {(isCleared ? "클리어!" : "실패!")}");
        OnStageEnded?.Invoke(currentStage, isCleared);

        if (isCleared)
        {
            // 진짜 하드코딩 겁나 하기 싫은데 어쩔 수 없이 했음
            if (currentStageId < 4)
            {
                newCardUI.SetCardUI((CardType)currentStage.unlockCard[0]);
                newCardUI.gameObject.SetActive(true);
            }
            // 클리어 시 다음 스테이지로 이동할지 결정
            // 여기서는 자동으로 넘어가지 않고, 외부에서 MoveToNextStage() 호출
        }
    }

    /// <summary>
    /// 첫 스테이지부터 다시 시작
    /// </summary>
    public void RestartFromFirstStage()
    {
        StartStage(1);
    }

    #region Getters
    public StageSO GetCurrentStage() => currentStage;
    public int GetCurrentStageId() => currentStageId;
    public StageCollectionSO GetStageCollection() => stageCollection;
    public int GetTotalStageCount() => stageCollection?.GetTotalStageCount() ?? 0;
    #endregion
}