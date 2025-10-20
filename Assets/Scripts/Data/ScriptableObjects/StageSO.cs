using UnityEngine;
using System.Collections.Generic;
using GameCore.Data;

/// <summary>
/// 스테이지 데이터
/// </summary>
[CreateAssetMenu(fileName = "Stage_", menuName = "Game/Stage Data")]
public class StageSO : ScriptableObject
{
    [Header("Basic Settings")]
    public int stageId; // 현재 스테이지
    public int endTurn = 3; // 최종 턴
    public int target = 50; // 목표 점수

    [Header("Draw Settings")]
    public int firstDraw = 4; // 1턴 드로우 수
    public int secondDraw = 2; // 2턴 드로우 수
    public int lastDraw = 1; // 마지막 턴 드로우 수

    [Header("Card Restriction")]
    [Tooltip("이전 턴에 사용한 카드를 다음 턴에 제외할지 여부")]
    public bool excludePreviousTurnTypes = true;

    [Header("Card Unlock System")]
    [Tooltip("각 턴마다 해금되는 카드 (1=A, 2=B, 3=C, 4=D, 5=E, 6=F, 7=G)")]
    public List<int> unlockCard = new List<int>();

    /// <summary>
    /// 특정 턴에 덱에 추가되는 카드 타입 반환
    /// </summary>
    public CardType? GetUnlockCardForTurn(int turnNumber)
    {
        int index = turnNumber - 1;

        if (index >= 0 && index < unlockCard.Count)
        {
            int cardId = unlockCard[index];

            if (cardId >= 1 && cardId <= 10)
            {
                return (CardType)(cardId - 1);
            }
        }

        return null;
    }

}
