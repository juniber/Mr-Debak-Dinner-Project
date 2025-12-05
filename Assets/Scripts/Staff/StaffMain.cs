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

    [Header("3. 근무 현황 패널 (계층 구조 반영)")]
    [SerializeField] private Transform contentPanel;        // 세로로 줄(Row)들이 쌓일 부모 (Vertical Layout Group)
    [SerializeField] private GameObject rowPrefab;          // 가로 한 줄 프리팹 (Horizontal Layout Group)
    [SerializeField] private StaffMain_SlotUI slotPrefab;        // 직원 개별 슬롯 프리팹

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
    }

    private void UpdateStoreInfo(StoreStatusData data)
    {
        statusText.text = data.isOpen ? "영업중" : "영업종료";
        timeText.text = $"영업 시간 : {data.openTime} - {data.closeTime}";
        completionRateText.text = $"주문 완료율 : {data.completionRate}%";
        salesText.text = $"매출액 : {data.totalSales:N0}원";
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

            return new StoreStatusData
            {
                isOpen = isOpen,
                openTime = openTime,
                closeTime = closeTime,
                completionRate = completionRate,
                totalSales = totalSales
            };
        }
        return null;
    }

    // 2. 직원 목록 가져오기
    private async Task<List<StaffData>> GetStaffListFromFirebase()
    {
        var list = new List<StaffData>();

        // role이 "Staff"인 사용자만 쿼리
        var query = dbReference.Child("users").OrderByChild("role").EqualTo("Staff");
        var snapshot = await query.GetValueAsync();

        Console.WriteLine("오예 난 이걸 찍어봐야겠어.");

        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
            {
                // 각 필드값 직접 가져오기 (null 체크 포함)
                string name = child.Child("name").Value?.ToString() ?? "이름없음";
                string role = child.Child("role").Value?.ToString() ?? "Staff";

                // status는 DB에 없을 수도 있으므로 체크
                string status = "근무중";
                if (child.HasChild("status"))
                {
                    status = child.Child("status").Value.ToString();
                }

                list.Add(new StaffData
                {
                    id = child.Key, // UID
                    name = name,
                    status = status,
                    role = role
                });
            }
        }
        return list;
    }
}