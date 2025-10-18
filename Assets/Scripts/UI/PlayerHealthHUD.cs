using DG.Tweening.Core.Easing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 HP 및 Shield 표시 전용 HUD
/// </summary>
/// 

//  int currentTurnScore = gameManager.GetTotalScore(); // 현재 보드 점수
//  int cumulativeScore = gameManager.GetCumulativeScore(); // 게임 전체 누적 점수
public class PlayerHealthHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;


    private void Start()
    {


        if (hpSlider == null)
        {
            hpSlider = GetComponent<Slider>();
        }
    }


    private void LateUpdate()
    {
        // HP 업데이트
        UpdateHealth();
    }

   
    /// <summary>
    /// HP 슬라이더 업데이트
    /// </summary>
    private void UpdateHealth()
    {
        if (hpSlider != null)
        {
            float a = GameManager.Instance.GetCumulativeScore();
            float b = GameManager.Instance.GetTotalScore();
            hpSlider.value = a / b;


            hpText.text = a + " / " + b;
        } 

    }


}