using UnityEngine;
using GameCore.Data;
using System.Collections.Generic;

/// <summary>
/// 블록 설명 패널 잠금/해제 시스템 (초간단 버전)
/// GameInfoPanel의 Info_A ~ Info_K 패널의 LockPanel을 직접 제어
/// </summary>
public class BlockUnlockManager : MonoBehaviour
{
    private Dictionary<BlockType, LockPanel> lockPanels = new Dictionary<BlockType, LockPanel>();

    private void Start()
    {
        // LockPanel 찾기
        FindAllLockPanels();

        // 초기 상태 설정 (A, B, C, D만 개방)
        SetupInitialState();
    }

    /// <summary>
    /// 씬에서 모든 LockPanel 찾기
    /// </summary>
    private void FindAllLockPanels()
    {
        // GameInfoPanel 아래의 모든 LockPanel 찾기
        LockPanel[] panels = FindObjectsByType<LockPanel>(FindObjectsSortMode.None);

        foreach (var panel in panels)
        {
            // 부모 오브젝트 이름으로 BlockType 판별
            string parentName = panel.transform.parent.name;

            if (parentName.StartsWith("Info_"))
            {
                string blockTypeName = parentName.Replace("Info_", "");

                if (System.Enum.TryParse<BlockType>(blockTypeName, out BlockType type))
                {
                    lockPanels[type] = panel;
                }
            }
        }

    }

    /// <summary>
    /// 초기 상태 설정 (A, B, C, D는 개방, 나머지는 잠김)
    /// </summary>
    private void SetupInitialState()
    {
        BlockType[] unlockedBlocks = { BlockType.A, BlockType.B, BlockType.C, BlockType.D };
        BlockType[] lockedBlocks = { BlockType.E, BlockType.F, BlockType.G };

        // 개방된 블록
        foreach (var type in unlockedBlocks)
        {
            if (lockPanels.ContainsKey(type))
            {
                lockPanels[type].SetLocked(false);
            }
        }

        // 잠긴 블록
        foreach (var type in lockedBlocks)
        {
            if (lockPanels.ContainsKey(type))
            {
                lockPanels[type].SetLocked(true);
            }
        }
    }

    /// <summary>
    /// 특정 블록 해제
    /// </summary>
    public void UnlockBlock(BlockType type)
    {
        if (lockPanels.ContainsKey(type))
        {
            lockPanels[type].SetLocked(false);
        }
        else
        {
            Debug.LogWarning($"[BlockUnlockManager] {type} LockPanel을 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 다음 잠긴 블록 하나 해제 (테스트용)
    /// </summary>
    public void UnlockNextBlock()
    {
        BlockType[] lockedBlocks = { BlockType.E, BlockType.F, BlockType.G };

        foreach (var type in lockedBlocks)
        {
            if (lockPanels.ContainsKey(type) && lockPanels[type].IsLocked)
            {
                UnlockBlock(type);
                return;
            }
        }

        Debug.Log("[BlockUnlockManager] 모든 블록이 이미 해제되었습니다!");
    }

    private void Update()
    {
        // 테스트: '1' 키를 누르면 다음 블록 해제
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            Debug.Log("[BlockUnlockManager] '1' 키 입력 - 다음 블록 해제");
            UnlockNextBlock();
        }
    }
}
