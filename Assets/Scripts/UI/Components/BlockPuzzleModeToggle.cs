using UnityEngine;
using UnityEngine.UI;

// 타일 모드 토글 컴포넌트 (숫자 없음 / 숫자 있음) 
public class BlockPuzzleModeToggle : MonoBehaviour
{
    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void Start()
    {
        // 초기 토글 상태를 현재 모드에 맞게 설정
        if (toggle != null && GameManager.Instance != null)
        {
            toggle.SetIsOnWithoutNotify(GameManager.Instance.GetTileMode() == GameCore.Data.TileMode.WithNumbers);
        }
    }

    private void OnToggleChanged(bool value)
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.SetTileMode(value);
    }
}