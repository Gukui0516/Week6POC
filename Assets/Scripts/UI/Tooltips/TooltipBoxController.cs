using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 고정된 위치에서 텍스트만 변경되는 간단한 툴박스 컨트롤러
/// 마우스를 따라가지 않고 지정된 위치에 고정
/// </summary>
public class ToolBoxController : MonoBehaviour
{
    #region Singleton
    private static ToolBoxController instance;
    private static bool isQuitting = false;

    public static ToolBoxController Instance
    {
        get
        {
            if (isQuitting)
            {
                return null;
            }

            if (instance == null)
            {
                instance = FindFirstObjectByType<ToolBoxController>();

                if (instance == null)
                {
                    GameObject toolboxObj = new GameObject("ToolBoxController");
                    instance = toolboxObj.AddComponent<ToolBoxController>();
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("ToolBox UI References")]
    [SerializeField] private GameObject toolboxPanel;
    [SerializeField] private TextMeshProUGUI toolboxText;

    private Canvas canvas;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeToolBox();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void InitializeToolBox()
    {
     

        if (toolboxPanel != null)
        {
            toolboxText = toolboxPanel.GetComponentInChildren<TextMeshProUGUI>();
            //HideToolBox();
        }
    }

 

    /// <summary>
    /// 툴박스에 텍스트 표시
    /// </summary>
    public void ShowToolBox(string text)
    {
        if (toolboxPanel == null || toolboxText == null) return;

        toolboxText.text = text;
        toolboxPanel.SetActive(true);
    }

    /// <summary>
    /// 툴박스 숨기기
    /// </summary>
    public void HideToolBox()
    {
        if (toolboxPanel != null)
        {
            toolboxPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 툴박스가 현재 표시 중인지 여부
    /// </summary>
    public bool IsShowing()
    {
        return toolboxPanel != null && toolboxPanel.activeSelf;
    }

    /// <summary>
    /// 툴박스 위치 설정 (선택 사항)
    /// </summary>
    public void SetPosition(Vector2 anchoredPosition)
    {
        if (toolboxPanel != null)
        {
            var rect = toolboxPanel.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;
        }
    }

    /// <summary>
    /// 툴박스 크기 설정 (선택 사항)
    /// </summary>
    public void SetSize(Vector2 sizeDelta)
    {
        if (toolboxPanel != null)
        {
            var rect = toolboxPanel.GetComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
        }
    }
}