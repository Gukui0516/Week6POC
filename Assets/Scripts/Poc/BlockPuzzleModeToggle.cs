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

        // 초기 토글 상태를 현재 모드에 맞게 설정
        if (toggle != null && gm != null)
        {
            toggle.SetIsOnWithoutNotify(gm.GetTileMode() == GameCore.Data.TileMode.WithNumbers);
        }
    }

    private void OnToggleChanged(bool value)
    {
        if (gameManager == null) return;

        gameManager.SetTileMode(value);
    }
}