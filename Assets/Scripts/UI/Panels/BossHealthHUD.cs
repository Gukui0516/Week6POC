using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider easeHpSlider; // Lerp용 슬라이더 추가
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 0.05f;

    private StageSO currentStage;
    private float currentValue; // 현재 목표값 (lerp 목표)

    private void Start()
    {
        if (hpSlider == null)
        {
            hpSlider = GetComponent<Slider>();
        }

        // 초기화
        currentValue = 1f;
        if (hpSlider != null) hpSlider.value = 1f;
        if (easeHpSlider != null) easeHpSlider.value = 1f;
    }

    private void Update()
    {
        // HP 목표값 계산
        CalculateTargetHealth();

        // Lerp 애니메이션 적용
        ApplyLerpAnimation();
    }

    /// <summary>
    /// 목표 HP 값 계산
    /// </summary>
    private void CalculateTargetHealth()
    {
        if (GameManager.Instance == null || StageManager.Instance == null)
            return;

        float currentScore = GameManager.Instance.GetCumulativeScore();
        currentStage = StageManager.Instance.GetCurrentStage();

        if (currentStage == null)
            return;

        float targetScore = currentStage.target;
        float remainingScore = Mathf.Max(targetScore - currentScore, 0);

        // 목표값 계산 (1에서 0으로 감소)
        currentValue = Mathf.Clamp01(remainingScore / targetScore);

        // 텍스트 업데이트
        if (hpText != null)
        {
            hpText.text = $"{remainingScore } / {targetScore}";
        }
    }

    /// <summary>
    /// Lerp 애니메이션 적용
    /// </summary>
    private void ApplyLerpAnimation()
    {
        // 메인 슬라이더는 즉시 업데이트
        if (hpSlider != null && hpSlider.value != currentValue)
        {
            hpSlider.value = currentValue;
        }

        // Ease 슬라이더는 Lerp로 부드럽게 따라감
        if (easeHpSlider != null && easeHpSlider.value != hpSlider.value)
        {
            easeHpSlider.value = Mathf.Lerp(easeHpSlider.value, currentValue, lerpSpeed);
        }
    }
}