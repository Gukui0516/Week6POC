using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Data
{
    /// <summary>
    /// 블록 잠금/해제 상태를 관리하는 데이터 클래스
    /// </summary>
    [System.Serializable]
    public class BlockUnlockData
    {
        // 각 블록 타입의 잠금 상태 (true = 해제됨, false = 잠김)
        private Dictionary<BlockType, bool> unlockStatus;

        // 이벤트: 블록이 해제될 때 발생
        public System.Action<BlockType> OnBlockUnlocked;

        public BlockUnlockData()
        {
            unlockStatus = new Dictionary<BlockType, bool>();
            InitializeDefaultState();
        }

        /// <summary>
        /// 초기 상태 설정 (A, B, C, D는 개방, E, F, G는 잠금)
        /// </summary>
        private void InitializeDefaultState()
        {
            unlockStatus[BlockType.A] = true;
            unlockStatus[BlockType.B] = true;
            unlockStatus[BlockType.C] = true;
            unlockStatus[BlockType.D] = true;
            unlockStatus[BlockType.E] = false;
            unlockStatus[BlockType.F] = false;
            unlockStatus[BlockType.G] = false;

            Debug.Log("[BlockUnlockData] 초기 상태 설정 완료: A~D 개방, E~G 잠금");
        }

        /// <summary>
        /// 특정 블록이 해제되어 있는지 확인
        /// </summary>
        public bool IsUnlocked(BlockType blockType)
        {
            if (!unlockStatus.ContainsKey(blockType))
            {
                Debug.LogWarning($"[BlockUnlockData] {blockType}의 상태가 없습니다. 기본값(잠김) 반환");
                return false;
            }
            return unlockStatus[blockType];
        }

        /// <summary>
        /// 특정 블록 해제
        /// </summary>
        public void UnlockBlock(BlockType blockType)
        {
            if (!unlockStatus.ContainsKey(blockType))
            {
                Debug.LogWarning($"[BlockUnlockData] {blockType}의 상태가 없습니다.");
                return;
            }

            if (unlockStatus[blockType])
            {
                Debug.Log($"[BlockUnlockData] {blockType}은(는) 이미 해제되어 있습니다.");
                return;
            }

            unlockStatus[blockType] = true;
            Debug.Log($"[BlockUnlockData] {blockType} 블록이 해제되었습니다!");
            
            OnBlockUnlocked?.Invoke(blockType);
        }

        /// <summary>
        /// 특정 블록 잠금 (테스트용)
        /// </summary>
        public void LockBlock(BlockType blockType)
        {
            if (unlockStatus.ContainsKey(blockType))
            {
                unlockStatus[blockType] = false;
                Debug.Log($"[BlockUnlockData] {blockType} 블록이 잠겼습니다.");
            }
        }

        /// <summary>
        /// 모든 잠긴 블록 타입 리스트 반환
        /// </summary>
        public List<BlockType> GetLockedBlocks()
        {
            List<BlockType> lockedBlocks = new List<BlockType>();
            foreach (var pair in unlockStatus)
            {
                if (!pair.Value)
                {
                    lockedBlocks.Add(pair.Key);
                }
            }
            return lockedBlocks;
        }

        /// <summary>
        /// 모든 해제된 블록 타입 리스트 반환
        /// </summary>
        public List<BlockType> GetUnlockedBlocks()
        {
            List<BlockType> unlockedBlocks = new List<BlockType>();
            foreach (var pair in unlockStatus)
            {
                if (pair.Value)
                {
                    unlockedBlocks.Add(pair.Key);
                }
            }
            return unlockedBlocks;
        }

        /// <summary>
        /// 디버그용: 현재 상태 출력
        /// </summary>
        public void PrintStatus()
        {
            Debug.Log("=== Block Unlock Status ===");
            foreach (var pair in unlockStatus)
            {
                string status = pair.Value ? "해제됨" : "잠김";
                Debug.Log($"{pair.Key}: {status}");
            }
        }
    }
}
