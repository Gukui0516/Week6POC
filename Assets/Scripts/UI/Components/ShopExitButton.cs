using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점 나가기 버튼 - 다음 스테이지로 이동
/// </summary>
public class ShopExitButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnExitShop);
        }
    }

    private void OnExitShop()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[ShopExitButton] GameManager를 찾을 수 없습니다!");
            return;
        }

        Debug.Log("[ShopExitButton] 상점 나가기 - 다음 스테이지로");

        // GameManager의 상점 나가기 메서드 호출
        GameManager.Instance.ExitShopAndContinue();
    }
}