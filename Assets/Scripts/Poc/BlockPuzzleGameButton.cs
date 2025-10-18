using UnityEngine;
using UnityEngine.UI;

// 게임 컨트롤 버튼 컴포넌트 (New Game, End Turn)
public class BlockPuzzleGameButton : MonoBehaviour
{
    public enum ButtonType { NewGame, EndTurn }
    public ButtonType buttonType;

    private GameManager gameManager;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    public void SetGameManager(GameManager gm)
    {
        gameManager = gm;
    }

    private void OnClick()
    {
        if (gameManager == null) return;

        switch (buttonType)
        {
            case ButtonType.NewGame:
                gameManager.StartNewGame();
                break;
            case ButtonType.EndTurn:
                gameManager.EndTurn();
                break;
        }
    }
}