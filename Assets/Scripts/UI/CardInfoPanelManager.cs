using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CardInfoPanel의 모든 Info_Block 오브젝트를 자동으로 찾아서
/// CardInfoItem 컴포넌트를 추가하고 적절한 CardType을 할당하는 매니저
/// </summary>
public class CardInfoPanelManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool autoSetupOnAwake = true;
    [SerializeField] private string infoBlockPrefix = "Info_Block_";

    [Header("Info")]
    [SerializeField] private List<CardInfoItem> cardInfoItems = new List<CardInfoItem>();

    private void Awake()
    {
        if (autoSetupOnAwake)
        {
            SetupAllCardInfoItems();
        }
    }

    /// <summary>
    /// 모든 Info_Block 오브젝트를 찾아서 CardInfoItem 설정
    /// </summary>
    [ContextMenu("Setup All Card Info Items")]
    public void SetupAllCardInfoItems()
    {
        cardInfoItems.Clear();

        // BlockinfoPanel의 Viewport > Content 찾기
        Transform contentTransform = transform.Find("Viewport/Content");

        if (contentTransform == null)
        {
            // 현재 오브젝트가 Content일 수도 있음
            contentTransform = transform;
            Debug.LogWarning($"[CardInfoPanelManager] Viewport/Content를 찾을 수 없어 현재 Transform을 사용합니다.");
        }

        // CardType 배열 가져오기 (Orc부터 Slime까지)
        CardType[] allCardTypes = (CardType[])System.Enum.GetValues(typeof(CardType));

        // Info_Block_0부터 Info_Block_11까지 찾기
        for (int i = 0; i < allCardTypes.Length; i++)
        {
            string blockName = $"{infoBlockPrefix}{i}";
            Transform blockTransform = contentTransform.Find(blockName);

            if (blockTransform == null)
            {
                Debug.LogWarning($"[CardInfoPanelManager] {blockName}을 찾을 수 없습니다.");
                continue;
            }

            // CardInfoItem 컴포넌트 추가 또는 가져오기
            CardInfoItem infoItem = blockTransform.GetComponent<CardInfoItem>();
            if (infoItem == null)
            {
                infoItem = blockTransform.gameObject.AddComponent<CardInfoItem>();
                Debug.Log($"[CardInfoPanelManager] {blockName}에 CardInfoItem 컴포넌트 추가");
            }

            // CardType 설정
            CardType cardType = allCardTypes[i];
            infoItem.SetCardType(cardType);

            cardInfoItems.Add(infoItem);

            Debug.Log($"[CardInfoPanelManager] {blockName} -> {cardType} 설정 완료");
        }

        Debug.Log($"[CardInfoPanelManager] 총 {cardInfoItems.Count}개의 CardInfoItem 설정 완료");
    }

    /// <summary>
    /// 모든 카드 정보 새로고침
    /// </summary>
    [ContextMenu("Refresh All Card Data")]
    public void RefreshAllCardData()
    {
        foreach (var item in cardInfoItems)
        {
            if (item != null)
            {
                item.RefreshData();
            }
        }

        Debug.Log($"[CardInfoPanelManager] 모든 카드 데이터 새로고침 완료");
    }

    /// <summary>
    /// 특정 카드 정보 가져오기
    /// </summary>
    public CardInfoItem GetCardInfoItem(CardType cardType)
    {
        foreach (var item in cardInfoItems)
        {
            if (item != null)
            {
                // CardInfoItem에 GetCardType 메서드가 필요할 수 있음
                // 현재는 리스트 인덱스로 판단
                int index = cardInfoItems.IndexOf(item);
                if (index >= 0 && index < 12)
                {
                    CardType itemType = (CardType)index;
                    if (itemType == cardType)
                        return item;
                }
            }
        }
        return null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터 버튼: Content 하위에 Info_Block 12개 자동 생성 (필요시)
    /// </summary>
    [ContextMenu("Create Info Blocks (Editor Only)")]
    private void CreateInfoBlocks()
    {
        Transform contentTransform = transform.Find("Viewport/Content");
        if (contentTransform == null)
        {
            Debug.LogError("[CardInfoPanelManager] Viewport/Content를 찾을 수 없습니다.");
            return;
        }

        CardType[] allCardTypes = (CardType[])System.Enum.GetValues(typeof(CardType));

        for (int i = 0; i < allCardTypes.Length; i++)
        {
            string blockName = $"{infoBlockPrefix}{i}";

            // 이미 존재하는지 확인
            Transform existing = contentTransform.Find(blockName);
            if (existing != null)
            {
                Debug.Log($"[CardInfoPanelManager] {blockName}은 이미 존재합니다.");
                continue;
            }

            // 새 오브젝트 생성
            GameObject infoBlock = new GameObject(blockName);
            infoBlock.transform.SetParent(contentTransform, false);

            // RectTransform 추가
            RectTransform rect = infoBlock.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 100);

            // Icon 자식 생성
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(infoBlock.transform, false);
            icon.AddComponent<UnityEngine.UI.Image>();

            // Name 자식 생성
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(infoBlock.transform, false);
            TMPro.TextMeshProUGUI nameText = nameObj.AddComponent<TMPro.TextMeshProUGUI>();
            nameText.text = "Name";

            // TooltipDescription 자식 생성
            GameObject tooltipObj = new GameObject("TooltipDescription");
            tooltipObj.transform.SetParent(infoBlock.transform, false);
            TMPro.TextMeshProUGUI tooltipText = tooltipObj.AddComponent<TMPro.TextMeshProUGUI>();
            tooltipText.text = "Description";

            Debug.Log($"[CardInfoPanelManager] {blockName} 생성 완료");
        }
    }
#endif
}
