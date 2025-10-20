using UnityEngine;

public class EndingUI : MonoBehaviour
{
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    public void ShowVictoryUI(bool isCleared)
    {
        victoryPanel.SetActive(isCleared);
        defeatPanel.SetActive(!isCleared);
    }
}