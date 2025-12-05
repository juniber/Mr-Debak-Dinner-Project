using UnityEngine;
using UnityEngine.UI;

public class StaffMenubar : MonoBehaviour
{
    [Header("Navigation Buttons")]
    [SerializeField] private Button homeBtn;       // 홈 버튼
    [SerializeField] private Button orderBtn;      // 주문관리 버튼
    [SerializeField] private Button selfBtn;       // 셀프 버튼

    [SerializeField] private static string currentPanel = "StaffMain";
    [SerializeField] private static string prevPanel = "";

    private void Start()
    {
        // 버튼 리스너 연결
        if (homeBtn != null)
            homeBtn.onClick.AddListener(OnHomeClicked);

        if (orderBtn != null)
            orderBtn.onClick.AddListener(OnOrderClicked);

        if (selfBtn != null)
            selfBtn.onClick.AddListener(OnSelfClicked);
    }

    // --- 클릭 이벤트 핸들러 ---

    private void OnHomeClicked()
    {
        currentPanel = "StaffMainPanel";
        prevPanel = currentPanel;
        UIManager.Instance.ShowPanel("StaffMainPanel");
    }

    private void OnOrderClicked()
    {
        currentPanel = "SOrderManagePanel";
        prevPanel = currentPanel;
        UIManager.Instance.ShowPanel("SOrderManagePanel");
    }

    private void OnSelfClicked()
    {
        currentPanel = "SSelfServicePanel";
        prevPanel = currentPanel;
        UIManager.Instance.ShowPanel("SSelfServicePanel");
    }

    public static void RecordCurrent(string currrent)
    {
        prevPanel = currentPanel;
        currentPanel = currrent;
    }
}
