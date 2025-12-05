using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class StaffMain_SlotUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image profileImg;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button clickButton;

    private StaffData currentData;
    private System.Action<StaffData> onClickCallback;

    // 데이터를 받아서 UI를 갱신하는 함수
    public void Setup(StaffData data, System.Action<StaffData> onClick)
    {
        currentData = data;
        onClickCallback = onClick;

        // UI 갱신
        nameText.text = data.name;

        // 버튼 클릭 이벤트 연결
        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => onClickCallback?.Invoke(currentData));
    }
}
