using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections.Generic;
using System.Threading.Tasks;

public class StaffInventoryManageUI : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private Transform contentContainer; // VerticalContentPanel 연결
    [SerializeField] private GameObject itemPrefab;      // InventoryItemPrefab 연결

    [Header("Buttons")]
    [SerializeField] private Button backspaceBtn;

    private DatabaseReference dbReference;

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
        LoadInventory();
    }

    // --- 1. 재고 목록 불러오기 ---
    private async void LoadInventory()
    {
        // 기존 목록 삭제
        foreach (Transform child in contentContainer) Destroy(child.gameObject);

        // inventory 노드 전체 가져오기
        var snapshot = await dbReference.Child("inventory").GetValueAsync();

        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
            {
                string key = child.Key;
                long amount = 0;

                if (child.Value != null)
                    long.TryParse(child.Value.ToString(), out amount);

                CreateInventoryItem(key, amount);
            }
        }
        else
        {
            Debug.Log("재고 데이터가 없습니다.");
        }
    }

    private void CreateInventoryItem(string key, long amount)
    {
        GameObject go = Instantiate(itemPrefab, contentContainer);
        var itemUI = go.GetComponent<StaffInventoryItemUI>();

        // 아이템 세팅 (콜백으로 OnAddStock 연결)
        itemUI.Setup(key, amount, OnAddStock);
    }

    // --- 2. 재고 추가 (트랜잭션) ---
    private void OnAddStock(string key, int addAmount)
    {
        Debug.Log($"[{key}] 재고 추가 시도: +{addAmount}");

        DatabaseReference itemRef = dbReference.Child("inventory").Child(key);

        itemRef.RunTransaction(mutableData =>
        {
            long currentStock = 0;
            if (mutableData.Value != null)
            {
                long.TryParse(mutableData.Value.ToString(), out currentStock);
            }

            // 현재 값 + 추가할 값
            mutableData.Value = currentStock + addAmount;

            return TransactionResult.Success(mutableData);
        })
        .ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                // 성공 시 UI 갱신 (전체를 다시 불러와서 최신 상태 유지)
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log("재고 추가 완료");
                    UIManager.Instance.ShowTemporaryStatus($"{addAmount}개 추가되었습니다.", 1f);
                    LoadInventory(); // 목록 새로고침
                });
            }
            else
            {
                Debug.LogError($"재고 추가 실패: {task.Exception}");
            }
        });
    }

    private void OnBackClicked()
    {
        UIManager.Instance.ShowPanel("StaffMainPanel");
    }
}