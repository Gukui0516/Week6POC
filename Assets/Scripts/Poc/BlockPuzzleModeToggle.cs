using UnityEngine;
using UnityEngine.UI;

// 타일 모드 토글 컴포넌트 (숫자 없음 / 숫자 있음)
public class BlockPuzzleModeToggle : MonoBehaviour
{
    private GameManager gameManager;
    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    public void SetGameManager(GameManager gm)
    {
        gameManager = gm;
    }

    private void OnToggleChanged(bool value)
    {
        if (gameManager == null) return;

        gameManager.SetTileMode(value);
    }
}
