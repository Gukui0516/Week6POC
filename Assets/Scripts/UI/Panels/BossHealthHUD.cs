using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider easeHpSlider;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Boss Shake Effect")]
    // 변수 타입을 RectTransform으로 유지하고, 이름만 더 명확하게 변경
    [SerializeField] private RectTransform bossImageRectTransform;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 10f;

    [Header("Settings")]
    [SerializeField] private float lerpSpeed = 0.05f;

    private StageSO currentStage;
    private float currentValue;
    private float previousValue;

    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    private void Start()
    {
        // ... (기존 Start 코드와 동일) ...

        // 보스 Image의 원래 위치 저장
        if (bossImageRectTransform != null)
        {
            originalPosition = bossImageRectTransform.anchoredPosition;
        }
    }

    private void Update()
    {
        CalculateTargetHealth();
        ApplyLerpAnimation();

        // 데미지 감지 및 흔들림 효과 호출
        if (currentValue < previousValue)
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                bossImageRectTransform.anchoredPosition = originalPosition;
            }
            shakeCoroutine = StartCoroutine(ShakeEffect());
        }

        previousValue = currentValue;
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

        currentValue = Mathf.Clamp01(remainingScore / targetScore);

        if (hpText != null)
        {
            hpText.text = $"{remainingScore} / {targetScore}";
        }
    }

    /// <summary>
    /// Lerp 애니메이션 적용
    /// </summary>
    private void ApplyLerpAnimation()
    {
        if (hpSlider != null && hpSlider.value != currentValue)
        {
            hpSlider.value = currentValue;
        }

        if (easeHpSlider != null && easeHpSlider.value != hpSlider.value)
        {
            easeHpSlider.value = Mathf.Lerp(easeHpSlider.value, currentValue, lerpSpeed);
        }
    }

    /// <summary>
    /// 보스 Image를 흔드는 효과를 주는 코루틴
    /// </summary>
    private IEnumerator ShakeEffect()
    {
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            float xOffset = Random.Range(-1f, 1f) * shakeMagnitude;
            // RectTransform의 위치를 변경할 때는 anchoredPosition을 사용합니다.
            bossImageRectTransform.anchoredPosition = originalPosition + new Vector3(xOffset, 0f, 0f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 흔들림이 끝나면 원래 위치로 복귀
        bossImageRectTransform.anchoredPosition = originalPosition;
        shakeCoroutine = null;
    }
}