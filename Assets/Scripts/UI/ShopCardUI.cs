using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopCardUI : MonoBehaviour
{
    [SerializeField] private ShopManager shopManager;
    [SerializeField] private Toggle toggle;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI baseScore;
    [SerializeField] private CardType cardType;

    // 색상 설정 추가
    [Header("Toggle Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.green;

    private Image toggleBackground; // Toggle의 배경 이미지

    // 외부에서 Set 해줄 인덱스 (ShopManager가 할당)
    public int Index { get; set; }

    private void Start()
    {
        // Toggle의 배경 이미지 가져오기
        if (toggle != null)
        {
            toggleBackground = toggle.targetGraphic as Image;
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            UpdateToggleColor(toggle.isOn); // 초기 색상 설정
        }
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        UpdateToggleColor(isOn);
        if (isOn) SelectType();
    }

    // 색상 업데이트 메서드 추가
    private void UpdateToggleColor(bool isOn)
    {
        if (toggleBackground != null)
        {
            toggleBackground.color = isOn ? selectedColor : normalColor;
        }
    }

    public void SetCardUI(CardType type)
    {
        var cardData = CardDataLoader.GetData(type);
        cardType = type;
        icon.sprite = cardData.iconSprite;
        cardName.text = cardData.cardName;
        description.text = cardData.description;
        baseScore.text = cardData.baseScore.ToString();

        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(false);
            UpdateToggleColor(false); // 색상도 초기화
        }
    }

    public void SelectType()
    {
        shopManager.SelectShop(Index, cardType);
    }

    public void Deselect()
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(false);
            UpdateToggleColor(false); // 색상도 초기화
        }
    }
}