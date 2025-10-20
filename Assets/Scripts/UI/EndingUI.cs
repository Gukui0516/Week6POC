using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingUI : MonoBehaviour
{
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    public void ShowVictoryUI(bool isCleared)
    {
        victoryPanel.SetActive(isCleared);
        defeatPanel.SetActive(!isCleared);
    }

    public static void ReloadCurrentScene()
    {
        // 혹시 게임 일시정지 중이면 복원
        Time.timeScale = 1f;

        // 현재 활성화된 씬을 그대로 다시 로드
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }
}