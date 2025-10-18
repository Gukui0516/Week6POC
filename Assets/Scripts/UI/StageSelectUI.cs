using UnityEngine;

/// <summary>
/// 스테이지 선택 UI
/// </summary>
public class StageSelectUI : MonoBehaviour
{
    [SerializeField] private Transform stageButtonContainer;
    [SerializeField] private GameObject stageButtonPrefab;

    private void Start()
    {
        CreateStageButtons();
    }

    private void CreateStageButtons()
    {
        if (StageManager.Instance == null) return;

        var collection = StageManager.Instance.GetStageCollection();
        if (collection == null) return;

        foreach (var stage in collection.stages)
        {
            GameObject buttonObj = Instantiate(stageButtonPrefab, stageButtonContainer);
            var button = buttonObj.GetComponent<StageButton>();

            if (button != null)
            {
                button.Initialize(stage);
            }
        }
    }
}
