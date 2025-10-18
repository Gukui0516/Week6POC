using UnityEngine;

/// <summary>
/// 블록 버튼의 잠금 상태를 표시하는 UI 컴포넌트
/// InventoryButton의 자식 오브젝트로 배치됩니다
/// 간단하게 GameObject 활성화/비활성화만 사용
/// </summary>
public class LockPanel : MonoBehaviour
{
    private bool isLocked = true;

    /// <summary>
    /// 잠금 상태 설정
    /// </summary>
    /// <param name="locked">true면 잠금(활성화), false면 해제(비활성화)</param>
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        gameObject.SetActive(locked);

    }

    /// <summary>
    /// 현재 잠금 상태 반환
    /// </summary>
    public bool IsLocked => isLocked;
}
