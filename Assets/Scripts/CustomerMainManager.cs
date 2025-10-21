using UnityEngine;

public class CustomerMainManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject orderTypePanel;    // OrderTypePanel
    public GameObject backgroundBlocker; // �� ��� ��ġ ������ �г�

    private void Start()
    {
        if (orderTypePanel != null) { orderTypePanel.SetActive(false); }
        if (backgroundBlocker != null) { backgroundBlocker.SetActive(false); }
    }

    // �ֹ� ��� ���� â�� �޹�� ���Ŀ Ȱ��ȭ
    public void ShowOrderTypePanel()
    {
        if (orderTypePanel != null) { orderTypePanel.SetActive(true); }
        if (backgroundBlocker != null) { backgroundBlocker.SetActive(true); }
    }

    // �ֹ� ��� ���� â�� �޹�� ���Ŀ�� ��Ȱ��ȭ
    public void HideOrderTypePanel()
    {
        if (orderTypePanel != null) { orderTypePanel.SetActive(false); }
        if (backgroundBlocker != null) { backgroundBlocker.SetActive(false); }
    }
}
