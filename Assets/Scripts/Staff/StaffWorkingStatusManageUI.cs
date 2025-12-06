using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SWorkingStatusPanel : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private Transform kitchenContainer;
    [SerializeField] private Transform deliveryContainer;
    [SerializeField] private GameObject staffListPrefab; // StaffListPrefab 연결 확인!
    [SerializeField] private Button backspaceBtn;

    private DatabaseReference dbReference;

    private void Awake()
    {
        if (backspaceBtn) backspaceBtn.onClick.AddListener(OnBackClicked);
    }

    private void OnEnable()
    {
        if (dbReference == null) dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        LoadStaffList();
    }

    private async void LoadStaffList()
    {
        ClearContainer(kitchenContainer);
        ClearContainer(deliveryContainer);

        List<StaffData> staffList = await FetchAllStaffSafe();

        foreach (var staff in staffList)
        {
            // 직무에 따라 분류해서 생성
            if (staff.jobType == "Delivery")
            {
                CreateSlot(staff, deliveryContainer);
            }
            else
            {
                // Kitchen 이거나, jobType이 없는 경우 기본적으로 주방에 배치
                CreateSlot(staff, kitchenContainer);
            }
        }
    }

    // ★ [수정됨] 모든 유저를 가져와서 Staff/Master만 골라내는 안전한 함수
    private async Task<List<StaffData>> FetchAllStaffSafe()
    {
        var list = new List<StaffData>();

        // users 노드 전체 로드
        var snapshot = await dbReference.Child("users").GetValueAsync();

        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
            {
                // 데이터 파싱
                string role = ParseString(child, "role", "");

                // ★ 여기서 필터링: Staff 이거나 Master 인 사람만 리스트에 넣음
                if (role == "Staff" || role == "Master")
                {
                    list.Add(new StaffData
                    {
                        id = child.Key,
                        name = ParseString(child, "name", "이름없음"),
                        phone = ParseString(child, "phone", "번호없음"),
                        role = role,
                        jobType = ParseString(child, "jobType", "Kitchen"), // 없으면 Kitchen
                        status = ParseString(child, "status", "근무중")
                    });
                }
            }
        }
        return list;
    }

    private void CreateSlot(StaffData staff, Transform parent)
    {
        GameObject go = Instantiate(staffListPrefab, parent);
        StaffListSlotUI slot = go.GetComponent<StaffListSlotUI>();

        if (slot != null)
        {
            slot.Setup(staff, (selectedStaff) =>
            {
                // 클릭 시 관리 패널로 이동
                UIManager.Instance.ShowPanel("SWorkerManagePanel", selectedStaff);
            });
        }
    }

    private void OnBackClicked()
    {
        UIManager.Instance.ShowPanel("StaffMainPanel");
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container) Destroy(child.gameObject);
    }

    private string ParseString(DataSnapshot s, string key, string defaultValue)
    {
        if (s.HasChild(key) && s.Child(key).Value != null)
            return s.Child(key).Value.ToString();
        return defaultValue;
    }
}