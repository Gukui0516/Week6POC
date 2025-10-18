using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;


//UI 만들기
public class CardGameUIGenerator : EditorWindow
{
    [MenuItem("Tools/Card Game UI/Generate UI")]
    public static void GenerateUI()
    {
        // 기존 Canvas 확인 및 생성
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("CardGameCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // 메인 컨테이너 생성
        GameObject mainContainer = CreateUIObject("MainContainer", canvas.transform);
        RectTransform mainRect = mainContainer.GetComponent<RectTransform>();
        SetFullScreen(mainRect);

        HorizontalLayoutGroup mainLayout = mainContainer.AddComponent<HorizontalLayoutGroup>();
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = true;
        mainLayout.childForceExpandWidth = true;
        mainLayout.childForceExpandHeight = true;
        mainLayout.spacing = 20;
        mainLayout.padding = new RectOffset(20, 20, 20, 100);

        // 왼쪽 패널 (상점) 생성
        GameObject leftPanel = CreatePanel("ShopPanel", mainContainer.transform);
        CreateShopCards(leftPanel.transform);

        // 오른쪽 패널 (인벤토리) 생성
        GameObject rightPanel = CreatePanel("InventoryPanel", mainContainer.transform);
        CreateInventoryCards(rightPanel.transform);

        // 하단 버튼 생성
        CreateBottomButton(canvas.transform);

        Debug.Log("카드 게임 UI가 성공적으로 생성되었습니다!");
        Selection.activeGameObject = canvas.gameObject;
    }

    [MenuItem("Tools/Card Game UI/Delete UI")]
    public static void DeleteUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            DestroyImmediate(canvas.gameObject);
            Debug.Log("카드 게임 UI가 삭제되었습니다.");
        }
        else
        {
            Debug.LogWarning("삭제할 Canvas를 찾을 수 없습니다.");
        }
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        return obj;
    }

    private static GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = CreateUIObject(name, parent);
        RectTransform rect = panel.GetComponent<RectTransform>();

        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        if (name == "ShopPanel")
        {
            HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 15;
            layout.padding = new RectOffset(30, 30, 30, 30);
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
        else if (name == "InventoryPanel")
        {
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 10;
            layout.padding = new RectOffset(30, 30, 30, 30);
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        return panel;
    }

    private static void CreateShopCards(Transform parent)
    {
        Color blueColor = new Color(0.2f, 0.5f, 1f, 1f);

        for (int i = 0; i < 3; i++)
        {
            CreateCard($"ShopCard_{i + 1}", parent, blueColor, 150, 200);
        }
    }

    private static void CreateInventoryCards(Transform parent)
    {
        Color[] colors = {
            new Color(1f, 0.3f, 0.3f, 1f),    // 빨강
            new Color(0.3f, 1f, 0.3f, 1f),    // 초록
            new Color(1f, 1f, 0.3f, 1f),      // 노랑
            new Color(1f, 0.5f, 0.3f, 1f),    // 주황
            new Color(0.7f, 0.3f, 1f, 1f),    // 보라
            new Color(0.3f, 1f, 1f, 1f),      // 청록
            new Color(1f, 0.7f, 0.8f, 1f)     // 핑크
        };

        for (int i = 0; i < 7; i++)
        {
            CreateCard($"InventoryCard_{i + 1}", parent, colors[i], 120, 160);
        }
    }

    private static void CreateCard(string name, Transform parent, Color color, float width, float height)
    {
        GameObject card = CreateUIObject(name, parent);
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);

        Button btn = card.AddComponent<Button>();
        Image img = card.AddComponent<Image>();
        img.color = color;

        // 검은색 테두리 추가
        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, -3);

        // 클릭 이벤트 설정
        string cardName = name;
        btn.onClick.AddListener(() => OnCardClick(cardName));

        // 카드 텍스트 추가 (TextMeshPro 사용)
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(card.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        SetFullScreen(textRect);

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        text.text = name.Replace("_", " ");
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 18;
        text.fontStyle = FontStyles.Bold;

        // TMP 내장 아웃라인 효과
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color(0, 0, 0, 0.8f);
    }

    private static void CreateBottomButton(Transform parent)
    {
        GameObject buttonContainer = CreateUIObject("BottomButtonContainer", parent);
        RectTransform containerRect = buttonContainer.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0);
        containerRect.anchorMax = new Vector2(0.5f, 0);
        containerRect.pivot = new Vector2(0.5f, 0);
        containerRect.anchoredPosition = new Vector2(0, 20);
        containerRect.sizeDelta = new Vector2(200, 60);

        Button btn = buttonContainer.AddComponent<Button>();
        Image img = buttonContainer.AddComponent<Image>();
        img.color = new Color(0.3f, 0.7f, 0.3f, 1f);

        Outline outline = buttonContainer.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3, -3);

        btn.onClick.AddListener(() => OnNextButtonClick());

        // 버튼 텍스트 추가 (TextMeshPro 사용)
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(buttonContainer.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        SetFullScreen(textRect);

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        text.text = "넘기기";
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.fontSize = 28;
        text.fontStyle = FontStyles.Bold;

        // TMP 내장 아웃라인 효과
        text.outlineWidth = 0.2f;
        text.outlineColor = new Color(0, 0, 0, 0.8f);
    }

    private static void SetFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void OnCardClick(string cardName)
    {
        Debug.Log($"카드 클릭: {cardName}");
    }

    private static void OnNextButtonClick()
    {
        Debug.Log("넘기기 버튼 클릭!");
    }
}