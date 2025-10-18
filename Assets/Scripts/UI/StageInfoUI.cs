using UnityEngine;

/// <summary>
/// 게임 중 스테이지 정보 표시
/// </summary>
public class StageInfoUI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI stageText;
    [SerializeField] private TMPro.TextMeshProUGUI turnText;
    [SerializeField] private TMPro.TextMeshProUGUI targetText;
    [SerializeField] private TMPro.TextMeshProUGUI cardsText;

    private void Start()
    {
        var stage = StageManager.Instance?.GetCurrentStage();
        if (stage != null && stageText != null)
        {
            stageText.text = $"Stage {stage.stageId}";
        }
    }

    private void LateUpdate()
    {
        UpdateInfo();
    }

    private void UpdateInfo()
    {
        if (GameManager.Instance == null) return;

        var turn = GameManager.Instance.GetCurrentTurn();
        var stage = StageManager.Instance?.GetCurrentStage();

        if (turn != null && stage != null)
        {
            if (turnText != null)
            {
                turnText.text = $"Turn: {turn.turnNumber}/{stage.endTurn}";
            }

            if (targetText != null)
            {
                int current = GameManager.Instance.GetCumulativeScore();
                targetText.text = $"Score: {current}/{stage.target}";
            }

            if (cardsText != null)
            {
                cardsText.text = $"Cards: {turn.availableBlocks.Count}";
            }
        }
    }
}