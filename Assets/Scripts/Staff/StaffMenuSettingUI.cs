using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections.Generic;
using System;

public class SMenuSettingPanel : MonoBehaviour
{
    [Header("Containers (ContentPanel)")]
    [SerializeField] private Transform addonsContainer;   // AddonsPanel > ContentPanel
    [SerializeField] private Transform coursesContainer;  // CoursesPanel > ContentPanel
    [SerializeField] private Transform stylesContainer;   // StylesPanel > ContentPanel

    [Header("Prefab")]
    [SerializeField] private GameObject menuSlotPrefab;   // StaffMenuPrefab

    [Header("Buttons")]
    [SerializeField] private Button backspaceBtn;

    private DatabaseReference dbReference;
    // DB에서 가져온 가격표를 임시 저장 (Key: 메뉴키, Value: 가격)
    private Dictionary<string, int> currentPriceList = new Dictionary<string, int>();

    private void Awake()
    {
        if (backspaceBtn) backspaceBtn.onClick.AddListener(OnBackClicked);
    }

    private void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    private void OnEnable()
    {
        if (dbReference == null) dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        LoadMenuPrices();
    }

    // --- 1. 가격 데이터 가져오기 & 리스트 생성 ---
    private async void LoadMenuPrices()
    {
        // 1. Firebase에서 priceList 전체 가져오기
        var snapshot = await dbReference.Child("priceList").GetValueAsync();

        currentPriceList.Clear();
        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
            {
                int price = 0;
                if (child.Value != null) int.TryParse(child.Value.ToString(), out price);
                currentPriceList[child.Key] = price;
            }
        }

        // 2. UI 초기화 (기존 목록 삭제)
        ClearContainer(addonsContainer);
        ClearContainer(coursesContainer);
        ClearContainer(stylesContainer);

        // 3. 카테고리별로 슬롯 생성
        CreateCourseSlots();
        CreateStyleSlots();
        CreateAddonSlots();
    }

    // [코스] 목록 생성
    private void CreateCourseSlots()
    {
        // Enum 순회
        foreach (CourseType type in Enum.GetValues(typeof(CourseType)))
        {
            string key = type.ToString();
            string name = MenuData.GetMenuName(type); // 한글 이름
            int price = GetPriceFromCache(key);

            CreateSlot(key, name, price, coursesContainer);
        }
    }

    // [스타일] 목록 생성
    private void CreateStyleSlots()
    {
        foreach (StyleType type in Enum.GetValues(typeof(StyleType)))
        {
            if (type == StyleType.None) continue; // None은 제외

            string key = type.ToString();
            string name = key; // 스타일은 별도 한글 변환 없으면 영문 그대로
            int price = GetPriceFromCache(key);

            CreateSlot(key, name, price, stylesContainer);
        }
    }

    // [추가옵션] 목록 생성
    private void CreateAddonSlots()
    {
        // MenuData.GetAddonCosts()의 키들을 가져옴
        var addonDict = MenuData.GetAddonCosts();
        foreach (var kvp in addonDict)
        {
            string key = kvp.Key;
            string name = MenuData.GetAddonName(key); // 한글 이름
            int price = GetPriceFromCache(key);

            CreateSlot(key, name, price, addonsContainer);
        }
    }

    // 공통 슬롯 생성 함수
    private void CreateSlot(string key, string name, int price, Transform parent)
    {
        GameObject go = Instantiate(menuSlotPrefab, parent);
        var slotUI = go.GetComponent<StaffMenuSlotUI>();
        slotUI.Setup(key, name, price, OnPriceUpdate);
    }

    // --- 2. 가격 변경 (Firebase 저장) ---
    private void OnPriceUpdate(string key, int newPrice)
    {
        Debug.Log($"가격 변경 시도: {key} -> {newPrice:N0}원");

        // priceList/{key} 경로에 값 저장
        dbReference.Child("priceList").Child(key).SetValueAsync(newPrice).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log("가격 변경 완료");
                    UIManager.Instance.ShowTemporaryStatus("가격이 변경되었습니다.", 1f);

                    // 로컬 캐시 업데이트 후 UI 갱신 (전체 리로드 대신 해당 슬롯만 갱신하면 좋지만, 
                    // 간단하게 전체 리로드로 처리)
                    LoadMenuPrices();
                });
            }
            else
            {
                Debug.LogError($"가격 변경 실패: {task.Exception}");
            }
        });
    }

    // 캐시에서 가격 찾기 (없으면 0)
    private int GetPriceFromCache(string key)
    {
        return currentPriceList.ContainsKey(key) ? currentPriceList[key] : 0;
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container) Destroy(child.gameObject);
    }

    private void OnBackClicked()
    {
        UIManager.Instance.ShowPanel("StaffMainPanel");
    }
}