using UnityEngine;

public class CustomerMainManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject orderTypePanel;    // OrderTypePanel
    public GameObject backgroundBlocker; // 뒷 배경 터치 감지용 패널

    private void Start()
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
}
