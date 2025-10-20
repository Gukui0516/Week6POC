using UnityEngine;
using UnityEngine.UI;

// 게임 컨트롤 버튼 컴포넌트 (New Game, End Turn) 
public class GameUIButton : MonoBehaviour
{
    public enum ButtonType { NewGame, EndTurn }
    public ButtonType buttonType;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (GameManager.Instance == null) return;

        switch (buttonType)
        {
            case ButtonType.NewGame:
                GameManager.Instance.StartNewGame();
                break;
            case ButtonType.EndTurn:
                GameManager.Instance.EndTurn();
                break;
        }
    }
}