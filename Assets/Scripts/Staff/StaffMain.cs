using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StaffMain : MonoBehaviour
{
    [Header("1. 영업 시간 패널")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Button changeSettingsBtn;

    [Header("2. 오늘 현황 패널")]
    [SerializeField] private Button statsPanelBtn;
    [SerializeField] private TextMeshProUGUI completionRateText;
    [SerializeField] private TextMeshProUGUI salesText;

    [Header("3. 근무 현황 패널")]
    [SerializeField] private Transform contentPanel;       
    [SerializeField] private GameObject rowPrefab;          
    [SerializeField] private StaffMain_SlotUI slotPrefab;       

    // 최대 표시 인원수 제한
    private const int MAX_STAFF_DISPLAY = 6;
    private const int ITEMS_PER_ROW = 3;

    [SerializeField] private GameObject SMenuPanel;

    private void OnEnable()
    {
        if(dbReference != null)
            RefreshUI();

        if(SMenuPanel.activeSelf != true)
            SMenuPanel.SetActive(true);

        StaffMenubar.RecordCurrent("StaffMainPanel");
    }

    private void UpdateStoreInfo(StoreStatusData data)
    {
        statusText.text = data.isOpen ? "영업중" : "영업종료";
        timeText.text = $"영업 시간 : {data.openTime} - {data.closeTime}";
        salesText.text = $"매출액 : {data.totalSales:N0}원";

        float rate = 0f;
        if (data.totalOrderCount > 0)
        {
            rate = (float)data.completedOrderCount / data.totalOrderCount * 100f;
        }

        completionRateText.text = $"주문 완료율 : {rate:F1}%";
    }

    private void UpdateStaffList(List<StaffData> staffList)
    {
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        int count = Mathf.Min(staffList.Count, MAX_STAFF_DISPLAY);

        Transform currentRowTransform = null;

        for (int i = 0; i < count; i++)
        {
            if (i % ITEMS_PER_ROW == 0)
            {
                GameObject newRow = Instantiate(rowPrefab, contentPanel);
                currentRowTransform = newRow.transform;
            }

            if (currentRowTransform != null)
            {
                StaffMain_SlotUI slot = Instantiate(slotPrefab, currentRowTransform);
                slot.Setup(staffList[i], OnStaffClicked);
            }
        }
    }

    // --- 이벤트 핸들러 (기존과 동일) ---
    private void OnSettingsClicked() {
        StaffMenubar.RecordCurrent("SServiceTimeManagerPanel");
        UIManager.Instance.ShowPanel("SServiceTimeManagerPanel");
    }
    private void OnStatsPanelClicked() { Debug.Log("매출 상세 이동"); }
    private void OnStaffClicked(StaffData staff) {
        StaffMenubar.RecordCurrent("SWorkingStatusPanel"); 
        UIManager.Instance.ShowPanel("SWorkingStatusPanel"); 
    }

    // Firebase 레퍼런스
    private DatabaseReference dbReference;

    private void Start()
    {
        // Firebase 초기화
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // 버튼 리스너 연결 (기존 코드)
        changeSettingsBtn.onClick.AddListener(OnSettingsClicked);
        statsPanelBtn.onClick.AddListener(OnStatsPanelClicked);

        // 시작하자마자 UI 갱신 (비동기 함수 호출)
        RefreshUI();
    }

    // 기존 RefreshUI를 async로 변경
    public async void RefreshUI()
    {
        Console.WriteLine("RefreshUI - start");
        // 1. 가게 정보 가져오기 (비동기 대기)
        StoreStatusData storeData = await GetStoreInfoFromFirebase();
        if (storeData != null)
        {
            UpdateStoreInfo(storeData);
        }

        // 2. 직원 목록 가져오기 (비동기 대기)
        List<StaffData> staffList = await GetStaffListFromFirebase();
        if (staffList != null)
        {
            UpdateStaffList(staffList);
        }
    }

    private async Task<StoreStatusData> GetStoreInfoFromFirebase()
    {
        var snapshot = await dbReference.Child("store_info").GetValueAsync();

        if (snapshot.Exists && snapshot.HasChildren)
        {
            bool isOpen = false;
            if (snapshot.Child("isOpen").Value != null)
                isOpen = bool.Parse(snapshot.Child("isOpen").Value.ToString());

            string openTime = snapshot.Child("openTime").Value?.ToString() ?? "09:00";
            string closeTime = snapshot.Child("closeTime").Value?.ToString() ?? "22:00";

            float completionRate = 0f;
            if (snapshot.Child("completionRate").Value != null)
                float.TryParse(snapshot.Child("completionRate").Value.ToString(), out completionRate);

            int totalSales = 0;
            if (snapshot.Child("totalSales").Value != null)
                int.TryParse(snapshot.Child("totalSales").Value.ToString(), out totalSales);

            long totalOrders = 0;
            if (snapshot.Child("totalOrderCount").Value != null)
                long.TryParse(snapshot.Child("totalOrderCount").Value.ToString(), out totalOrders);

            long completedOrders = 0;
            if (snapshot.Child("completedOrderCount").Value != null)
                long.TryParse(snapshot.Child("completedOrderCount").Value.ToString(), out completedOrders);

            return new StoreStatusData
            {
                isOpen = isOpen,
                openTime = openTime,
                closeTime = closeTime,
                completionRate = completionRate,
                totalSales = totalSales,
                totalOrderCount = totalOrders,
                completedOrderCount = completedOrders
            };
        }
        return null;
    }

    // 2. 직원 목록 가져오기
    private async Task<List<StaffData>> GetStaffListFromFirebase()
    {
        var list = new List<StaffData>();

        // 필터링 없이 일단 'users' 전체를 다 가져옵니다!
        Debug.Log(" --- [데이터 전수 조사 시작] ---");
        var snapshot = await dbReference.Child("users").GetValueAsync();

        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
            {
                string uid = child.Key;
                string name = ParseString(child, "name", "[(이름없음)]");
                string role = ParseString(child, "role", "[(권한없음)]");

                // 로그 출력 (이걸 확인하세요!)
                Debug.Log($" UID: {uid} | 이름: {name} | 권한: '{role}'");

                // 여기서 "Staff"인 사람만 리스트에 담습니다.
                // (주의: "Staff " 처럼 공백이 있어도 안 담깁니다!)
                if (role == "Staff")
                {
                    list.Add(new StaffData { id = uid, name = name, role = role, status = "근무중" });
                }
            }
        }
        Debug.Log(" --- [조사 끝] ---");

        return list;
    }

    private string ParseString(DataSnapshot s, string key, string defaultValue)
    {
        if (s.HasChild(key) && s.Child(key).Value != null)
        {
            return s.Child(key).Value.ToString();
        }
        return defaultValue;
    }
}