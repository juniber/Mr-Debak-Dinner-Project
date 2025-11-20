using UnityEngine;
using UnityEngine.UI;

public class CustomerMainManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject orderTypePanel;    // OrderTypePanel
    public GameObject backgroundBlocker; // 뒷 배경 터치 감지용 패널

    // 음성 주문 패널 연결
    public GameObject voiceOrderPanel;

    [Header("Main Buttons")]
    public Button voiceOrderButton; // 음성 주문 버튼

    private void Start()
    {
        if (orderTypePanel != null) { orderTypePanel.SetActive(false); }
        if (backgroundBlocker != null) { backgroundBlocker.SetActive(false); }
        if (voiceOrderPanel != null) voiceOrderPanel.SetActive(false);

        // 리스너 연결
        if (voiceOrderButton != null)
        {
            voiceOrderButton.onClick.AddListener(ShowVoiceOrderPanel);
        }
    }

    private void OnEnable()
    {
        if (orderTypePanel != null) { orderTypePanel.SetActive(false); }
        if (backgroundBlocker != null) { backgroundBlocker.SetActive(false); }
    }

    // 주문 방식 선택 창과 뒷배경 블로커 활성화
    public void ShowOrderTypePanel()
    {
        if (orderTypePanel != null) { orderTypePanel.SetActive(true); }
        if (backgroundBlocker != null) { backgroundBlocker.SetActive(true); }
    }

    // 주문 방식 선택 창과 뒷배경 블로커를 비활성화
    public void HideOrderTypePanel()
    {
        if (orderTypePanel != null) { orderTypePanel.SetActive(false); }
        if (backgroundBlocker != null) { backgroundBlocker.SetActive(false); }
    }

    // 음성 주문 패널 열기
    private void ShowVoiceOrderPanel()
    {
        // 주문 방식 선택 팝업은 닫고, 음성 주문 패널을 연다.
        HideOrderTypePanel();

        UIManager.Instance.ShowPanel("VoiceOrderPanel");
        // 패널이 켜지면 VoiceOrderManager의 OnEnable이 호출되어
        // 자동으로 AI와의 대화(인사말)가 시작될 것
    }
}
