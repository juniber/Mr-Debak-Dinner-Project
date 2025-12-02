using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

public class OrderHistoryManager : MonoBehaviour
{
    [Header("UI Components")]
    public Transform contentTransform;      // ScrollView의 Content
    public GameObject orderItemPrefab;      // OrderHistoryItem 프리팹
    public Button backButton;               // 뒤로가기(X) 버튼
    public GameObject statusText;           // 주문 내역이 없습니다 텍스트

    private DatabaseReference dbReference;
    private FirebaseAuth auth;

    private void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                UIManager.Instance.ShowPanel("CustomerMainPanel");
            });
        }
    }

    private void OnEnable()
    {
        statusText.SetActive(false);
        LoadOrderHistory();
    }

    private void LoadOrderHistory()
    {
        // 1. 기존 아이템 삭제 (UI 초기화)
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        if (auth.CurrentUser == null)
        {
            Debug.LogError("로그인 정보가 없습니다.");
            return;
        }

        string userId = auth.CurrentUser.UserId;

        // 2. 두 경로(orders, scheduledOrders)에서 동시에 데이터 가져오기
        var task1 = GetOrdersFromPath("orders", userId);
        var task2 = GetOrdersFromPath("scheduledOrders", userId);

        Task.WhenAll(task1, task2).ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("주문 내역 로드 실패: " + task.Exception);
                return;
            }

            // 3. 결과 리스트 병합
            List<Order> allOrders = new List<Order>();
            allOrders.AddRange(task1.Result);
            allOrders.AddRange(task2.Result);

            // 4. 정렬 및 필터링 (최신순 10개)
            var sortedOrders = allOrders
                .OrderByDescending(o => o.orderTimestamp)
                .Take(10)
                .ToList();

            // 5. UI 아이템 생성
            if (sortedOrders.Count == 0)
            {
                Debug.Log("주문 내역이 없습니다.");
                statusText.SetActive(true);
            }
            else
            {
                foreach (var order in sortedOrders)
                {
                    CreateOrderItem(order);
                }
            }
        });
    }

    // 특정 경로(path)에서 내 ID로 된 주문만 가져오는 헬퍼 함수
    private async Task<List<Order>> GetOrdersFromPath(string path, string userId)
    {
        List<Order> orders = new List<Order>();

        // Firebase Query: userId가 일치하는 데이터만 필터링
        var query = dbReference.Child(path).OrderByChild("userId").EqualTo(userId);

        try
        {
            var snapshot = await query.GetValueAsync();
            if (snapshot.Exists)
            {
                foreach (var child in snapshot.Children)
                {
                    string json = child.GetRawJsonValue();
                    if (!string.IsNullOrEmpty(json))
                    {
                        // JSON -> Order 객체 변환
                        Order order = JsonUtility.FromJson<Order>(json);
                        orders.Add(order);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[{path}] 데이터 로드 중 예외 발생 (데이터가 없을 수 있음): {ex.Message}");
        }

        return orders;
    }

    // 프리팹 인스턴스화 및 데이터 주입
    private void CreateOrderItem(Order order)
    {
        GameObject itemGO = Instantiate(orderItemPrefab, contentTransform);
        OrderHistoryItemUI itemUI = itemGO.GetComponent<OrderHistoryItemUI>();

        if (itemUI != null)
        {
            itemUI.Setup(order);
        }
    }
}
