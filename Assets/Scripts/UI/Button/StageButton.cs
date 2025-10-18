using UnityEngine;

/// <summary>
/// 스테이지 버튼
/// </summary>
public class StageButton : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI stageText;
    [SerializeField] private UnityEngine.UI.Button button;

    private StageSO stage;

    public void Initialize(StageSO stageData)
    {
        stage = stageData;

        if (stageText != null)
        {
            stageText.text = $"Stage {stage.stageId}";
        }

        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        StageManager.Instance.StartStage(stage.stageId);

        // 게임 씬으로 전환
        // UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }
}