using UnityEngine;

/// <summary>
/// 게임 중 스테이지 정보 표시
/// Stage와 Turn이 분리된 구조 반영
/// </summary>
public class StageInfoUI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI stageText;
    //[SerializeField] private TMPro.TextMeshProUGUI targetText;
    [SerializeField] private TMPro.TextMeshProUGUI cardsText;

    private void LateUpdate()
    {
        UpdateInfo();
    }


    /// <summary>
    /// 턴별 정보 업데이트 (매 프레임)
    /// </summary>
    private void UpdateInfo()
    {
        if (GameManager.Instance == null) return;

        var turnManager = GameManager.Instance.GetTurnManager();
        if (turnManager == null) return;

        var turn = turnManager.GetCurrentTurn();
        var stage = turnManager.GetCurrentStage();

        if (turn != null && stage != null)
        {

            if (stage != null && stageText != null)
            {
                stageText.text = $"스테이지 {stage.stageId} - {turn.turnNumber}/{stage.endTurn} 턴";
            }
       
          /*  // 목표 점수
            if (targetText != null)
            {
                int current = GameManager.Instance.GetCumulativeScore();
                targetText.text = $"Score: {current}/{stage.target}";
            }*/

            // 활성 카드 수
            if (cardsText != null)
            {
                cardsText.text = $"Cards: {turn.availableBlocks.Count}";
            }
        }
    }
}