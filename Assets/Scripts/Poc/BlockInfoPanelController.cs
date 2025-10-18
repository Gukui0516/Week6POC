using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

// 블록 정보 패널 컨트롤러 - 스크롤 및 잠금 시스템 관리
public class BlockInfoPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject[] infoBlocks; // Info_A ~ Info_J (10개)

    [Header("Lock System")]
    [SerializeField] private GameObject lockOverlay; // 잠금 화면 오버레이
    [SerializeField] private TextMeshProUGUI lockStatusText;    // 잠금 상태 표시 텍스트

    // 에디터에서 접근 가능한 public 프로퍼티들
    public ScrollRect ScrollRect { get => scrollRect; set => scrollRect = value; }
    public Transform ContentParent { get => contentParent; set => contentParent = value; }
    public GameObject[] InfoBlocks { get => infoBlocks; set => infoBlocks = value; }
    public GameObject LockOverlay { get => lockOverlay; set => lockOverlay = value; }
    public TextMeshProUGUI LockStatusText { get => lockStatusText; set => lockStatusText = value; }

    [Header("Unlock Settings")]
    public int initialUnlockedCount = 2; // 처음에 해금된 블록 수

    // 해금 상태 관리
    private List<bool> unlockedStates = new List<bool>();
    private string unlockSequence = "12345678";
    private int currentSequenceIndex = 0;

    // 블록 정보 데이터 (A~J)
    private readonly string[] blockInfoTexts = {
        "<b>A 기본 2점 / A가 2개만 붙이기</b>\n(-1) : A블록이 3개 이상 인접하면",
        "<b>B 기본 1점 / 다른 종류 블록과 인접</b>\n(+1) : B가 아닌 블록이 2개 이상 인접하면",
        "<b>C 기본 0점 / C 최대한 인접하게</b>\n(+1) : 인접한 C 블록 갯수 만큼",
        "<b>D 기본 1점 / D블록 정확히 2개 붙이기</b>\n(+1) : D 블록이 1개 or 2개 인접하면\n(-1) : D 블록이 3개 이상 인접하면",
        "<b>E 기본 4점 / 최대한 채우기</b>\n(-1) : 다른 E 블록과 빈칸 갯수 만큼",
        "<b>F 기본 0점 / 최대한 다양한 블록 종류</b>\n(+1) : F가 아닌 블록 종류 만큼\n(-1) : 다른 F 블록 갯수 만큼",
        "<b>G 기본 5점 / 각 끝에 배치하기</b>\n(-1) : 인접한 블록 갯수 만큼\n(-2) : 인접한 빈칸 갯수 만큼",
        "<b>H 기본 3점 / 대각선 배치</b>\n(+2) : 대각선에 H 블록이 있으면\n(-1) : 수직/수평 인접 블록 갯수 만큼",
        "<b>I 기본 1점 / 라인 완성</b>\n(+3) : 같은 행/열에 3개 배치시\n(+1) : 같은 행/열에 2개 배치시",
        "<b>J 기본 6점 / 홀로 배치</b>\n(-2) : 인접한 모든 블록 갯수 만큼"
    };

    private readonly Color[] blockColors = {
        new Color(1.0f, 0.8f, 0.8f, 1.0f), // A - 연분홍
        new Color(0.8f, 1.0f, 0.8f, 1.0f), // B - 연녹색
        new Color(0.8f, 0.8f, 1.0f, 1.0f), // C - 연파랑
        new Color(1.0f, 1.0f, 0.8f, 1.0f), // D - 연노랑
        new Color(1.0f, 0.8f, 1.0f, 1.0f), // E - 연보라
        new Color(0.8f, 1.0f, 1.0f, 1.0f), // F - 연청록
        new Color(1.0f, 0.9f, 0.8f, 1.0f), // G - 연주황
        new Color(0.9f, 0.9f, 0.9f, 1.0f), // H - 연회색
        new Color(1.0f, 1.0f, 0.9f, 1.0f), // I - 크림색
        new Color(0.9f, 0.8f, 1.0f, 1.0f)  // J - 연라벤더
    };

    private void Start()
    {
        InitializeUnlockSystem();
        SetupInfoBlocks();
        UpdateLockDisplay();
    }

    private void Update()
    {
        HandleUnlockInput();
    }

    private void InitializeUnlockSystem()
    {
        // 초기 해금 상태 설정 (처음 2개만 해금)
        unlockedStates.Clear();
        for (int i = 0; i < 10; i++)
        {
            unlockedStates.Add(i < initialUnlockedCount);
        }
    }

    private void SetupInfoBlocks()
    {
        // 스크롤뷰 설정
        if (scrollRect != null)
        {
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        // 각 정보 블록 설정
        for (int i = 0; i < infoBlocks.Length && i < 10; i++)
        {
            if (infoBlocks[i] != null)
            {
                // 블록 색상 설정
                var image = infoBlocks[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = blockColors[i];
                }

                // 초기 텍스트는 UpdateLockDisplay에서 설정됨
                // 여기서는 색상만 설정
            }
        }
    }

    private void HandleUnlockInput()
    {
        if (Input.inputString.Length > 0)
        {
            foreach (char c in Input.inputString)
            {
                if (char.IsDigit(c))
                {
                    ProcessUnlockInput(c);
                }
            }
        }
    }

    private void ProcessUnlockInput(char digit)
    {
        // 현재 시퀀스 위치의 숫자가 맞는지 확인
        if (currentSequenceIndex < unlockSequence.Length &&
            digit == unlockSequence[currentSequenceIndex])
        {
            currentSequenceIndex++;

            // 새로운 블록 해금
            UnlockNextBlock();

            Debug.Log($"올바른 입력: {digit}. 진행률: {currentSequenceIndex}/{unlockSequence.Length}");

            // 시퀀스 완료 확인
            if (currentSequenceIndex >= unlockSequence.Length)
            {
                Debug.Log("모든 블록이 해금되었습니다!");
                currentSequenceIndex = 0; // 리셋 (재사용 가능)
            }
        }
        else
        {
            // 잘못된 입력시 리셋
            currentSequenceIndex = 0;
            Debug.Log($"잘못된 입력: {digit}. 시퀀스가 리셋되었습니다.");
        }

        UpdateLockDisplay();
    }

    private void UnlockNextBlock()
    {
        // 다음 잠긴 블록 찾아서 해금
        for (int i = 0; i < unlockedStates.Count; i++)
        {
            if (!unlockedStates[i])
            {
                unlockedStates[i] = true;
                Debug.Log($"블록 {(char)('A' + i)} 해금됨!");
                break;
            }
        }
    }

    private void UpdateLockDisplay()
    {
        // 각 블록의 잠금 상태 업데이트
        for (int i = 0; i < infoBlocks.Length && i < unlockedStates.Count; i++)
        {
            if (infoBlocks[i] != null)
            {
                // 잠금된 블록은 어둡게 표시
                var canvasGroup = infoBlocks[i].GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = infoBlocks[i].AddComponent<CanvasGroup>();
                }

                if (unlockedStates[i])
                {
                    canvasGroup.alpha = 1.0f;
                    canvasGroup.interactable = true;

                    // 해금된 블록은 원래 텍스트로 복원
                    var textComponent = infoBlocks[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        textComponent.text = blockInfoTexts[i];
                    }
                }
                else
                {
                    canvasGroup.alpha = 0.3f;
                    canvasGroup.interactable = false;

                    // 잠긴 블록의 텍스트를 "???"로 변경 - TextMeshProUGUI 사용
                    var textComponent = infoBlocks[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        textComponent.text = "<b>??? 잠김</b>\n입력하여 해금하세요...";
                    }
                }
            }
        }

        // 잠금 상태 텍스트 업데이트
        if (lockStatusText != null)
        {
            int unlockedCount = unlockedStates.Count(x => x);
            lockStatusText.text = $"해금된 블록: {unlockedCount}/10\n진행률: {currentSequenceIndex}/{unlockSequence.Length}";
        }

        // 전체 잠금 오버레이는 사용하지 않음 (개별 블록 alpha로 처리)
        // 모든 블록이 해금되면 상태 텍스트를 숨김
        if (lockOverlay != null)
        {
            int unlockedCount = unlockedStates.Count(x => x);
            // LockOverlay를 항상 비활성화 (개별 블록의 CanvasGroup으로 잠금 표시)
            lockOverlay.SetActive(false);
        }
    }

    // 테스트용 메소드들
    [ContextMenu("모든 블록 해금")]
    public void UnlockAllBlocks()
    {
        for (int i = 0; i < unlockedStates.Count; i++)
        {
            unlockedStates[i] = true;
        }
        UpdateLockDisplay();
    }

    [ContextMenu("초기 상태로 리셋")]
    public void ResetToInitialState()
    {
        InitializeUnlockSystem();
        currentSequenceIndex = 0;
        UpdateLockDisplay();
    }

    // 외부에서 해금 상태 확인용
    public bool IsBlockUnlocked(int blockIndex)
    {
        return blockIndex >= 0 && blockIndex < unlockedStates.Count && unlockedStates[blockIndex];
    }

    // 특정 블록 강제 해금 (치트용)
    public void ForceUnlockBlock(int blockIndex)
    {
        if (blockIndex >= 0 && blockIndex < unlockedStates.Count)
        {
            unlockedStates[blockIndex] = true;
            UpdateLockDisplay();
        }
    }
}