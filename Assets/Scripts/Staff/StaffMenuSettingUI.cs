using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class SMenuSettingPanel : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private Transform addonsContainer;
    [SerializeField] private Transform coursesContainer;
    [SerializeField] private Transform stylesContainer;

    [Header("Prefab")]
    [SerializeField] private GameObject menuSlotPrefab;

    [Header("Buttons")]
    [SerializeField] private Button backspaceBtn;

    private DatabaseReference dbReference;
    // 가격 캐싱 (Key: 메뉴키, Value: 가격)
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

    // --- 1. 가격 데이터 가져오기 (폴더 구조 대응) ---
    private async void LoadMenuPrices()
    {
        var snapshot = await dbReference.Child("priceList").GetValueAsync();

        currentPriceList.Clear();

        if (snapshot.Exists)
        {
            foreach (var category in snapshot.Children)
            {
                // DB 구조가 priceList -> addons -> Key:Value 형태라면
                // category.Key는 "addons", "courses" 등이 됨

                // 1. 폴더 안에 있는 아이템들 순회
                if (category.HasChildren)
                {
                    foreach (var item in category.Children)
                    {
                        int price = 0;
                        if (item.Value != null) int.TryParse(item.Value.ToString(), out price);
                        currentPriceList[item.Key] = price;

                        // 디버깅용 로그
                        // Debug.Log($"[가격 로드] {category.Key}/{item.Key} : {price}");
                    }
                }
                // 2. 혹시 폴더 없이 바로 들어있는 아이템이 있다면 처리
                else
                {
                    int price = 0;
                    if (category.Value != null) int.TryParse(category.Value.ToString(), out price);
                    currentPriceList[category.Key] = price;
                }
            }
        }

        // UI 갱신
        ClearContainer(addonsContainer);
        ClearContainer(coursesContainer);
        ClearContainer(stylesContainer);

        CreateCourseSlots();
        CreateStyleSlots();
        CreateAddonSlots();
    }

    // --- 2. 목록 생성 로직 (기존과 동일) ---
    private void CreateCourseSlots()
    {
        foreach (CourseType type in Enum.GetValues(typeof(CourseType)))
        {
            string key = type.ToString();
            string name = MenuData.GetMenuName(type);
            int price = GetPriceFromCache(key);
            CreateSlot(key, name, price, coursesContainer, "courses"); // 카테고리 전달
        }
    }

    private void CreateStyleSlots()
    {
        foreach (StyleType type in Enum.GetValues(typeof(StyleType)))
        {
            if (type == StyleType.None) continue;
            string key = type.ToString();
            string name = key;
            int price = GetPriceFromCache(key);
            CreateSlot(key, name, price, stylesContainer, "styles"); // 카테고리 전달
        }
    }

    private void CreateAddonSlots()
    {
        var addonDict = MenuData.GetAddonCosts();
        foreach (var kvp in addonDict)
        {
            string key = kvp.Key;
            string name = MenuData.GetAddonName(key);
            int price = GetPriceFromCache(key);
            CreateSlot(key, name, price, addonsContainer, "addons"); // 카테고리 전달
        }
    }

    // 슬롯 생성 (카테고리 정보를 람다식에 포함)
    private void CreateSlot(string key, string name, int price, Transform parent, string category)
    {
        GameObject go = Instantiate(menuSlotPrefab, parent);
        var slotUI = go.GetComponent<StaffMenuSlotUI>();

        // 변경 시 category 정보도 함께 넘겨서 올바른 폴더에 저장하게 함
        slotUI.Setup(key, name, price, (targetKey, newPrice) =>
        {
            UpdatePriceInFirebase(category, targetKey, newPrice);
        });
    }

    // --- 3. 가격 저장 (올바른 폴더 위치에 저장) ---
    private void UpdatePriceInFirebase(string category, string key, int newPrice)
    {
        Debug.Log($"가격 저장: priceList/{category}/{key} -> {newPrice}");

        // 예: priceList/addons/AddBacon18g 경로에 저장
        dbReference.Child("priceList").Child(category).Child(key).SetValueAsync(newPrice)
            .ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        UIManager.Instance.ShowTemporaryStatus("가격이 변경되었습니다.", 1f);
                        // 캐시 즉시 업데이트 (리로드 없이 반영)
                        currentPriceList[key] = newPrice;
                        // 필요하면 LoadMenuPrices() 호출해서 전체 갱신
                        // LoadMenuPrices(); 
                    });
                }
            });
    }

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