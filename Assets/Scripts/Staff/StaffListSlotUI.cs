using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StaffListSlotUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image profileImage;      // StaffImage
    [SerializeField] private TextMeshProUGUI nameText;       // TextGroup > Name
    [SerializeField] private TextMeshProUGUI phoneText;      // TextGroup > CallNumber
    [SerializeField] private TextMeshProUGUI jobText;        // TextGroup > Roll (역할)
    [SerializeField] private Button clickBtn;         // 버튼 (프리팹 전체 혹은 투명버튼)

    private StaffData currentStaff;

    // 데이터를 받아서 UI를 갱신하는 함수
    public void Setup(StaffData staff, System.Action<StaffData> onClick)
    {
        currentStaff = staff;

        nameText.text = $"이름: {staff.name}";
        phoneText.text = $"전화: {staff.phone}";

        // jobType을 한글로 예쁘게 변환
        if (staff.jobType == "Kitchen") jobText.text = "주방 담당";
        else if (staff.jobType == "Delivery") jobText.text = "배달 담당";
        else jobText.text = staff.jobType; // 그 외

        // 버튼 클릭 시 콜백 호출
        if (clickBtn != null)
        {
            clickBtn.onClick.RemoveAllListeners();
            clickBtn.onClick.AddListener(() => onClick?.Invoke(currentStaff));
        }
    }
}