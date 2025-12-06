using UnityEngine;
using System.Collections.Generic;
using Firebase.Database;
using System.Threading.Tasks;
using UnityEngine.UI;

public class StaffOrderManagerUI : MonoBehaviour
{
    [Header("Scroll View Containers")]
    [SerializeField] private Transform newOrderContainer;    // 미수락(신규) 주문이 쌓일 곳
    [SerializeField] private Transform processingContainer;  // 진행중(조리/배달) 주문이 쌓일 곳
    [SerializeField] private GameObject orderItemPrefab;     // OrderItemUI 프리팹

    [SerializeField] private StaffOrderConfirmUI detailPanel;

    [SerializeField] private Button backBtn;

    private DatabaseReference dbReference;

    private void OnEnable()
    {
        // 패널이 켜질 때마다 새로고침
        if (dbReference != null) RefreshOrders();

        backBtn.onClick.RemoveAllListeners();
        backBtn.onClick.AddListener(OnEnterPanelBackSpaceBtn);
    }

    private void OnEnterPanelBackSpaceBtn()
    {
        UIManager.Instance.ShowPanel(StaffMenubar.GetPrev());
    }

    private void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        RefreshOrders();
    }

    public async void RefreshOrders()
    {
        // 1. 기존 UI 청소
        ClearContainer(newOrderContainer);
        ClearContainer(processingContainer);

        // 2. Firebase에서 데이터 가져오기
        List<Order> orderList = await FetchOrdersFromFirebase();

        if (orderList == null) return;

        // 3. 상태별로 분류해서 UI 생성
        foreach (Order order in orderList)
        {
            switch (order.status)
            {
                // [신규 주문 패널] 예약(1) 혹은 즉시주문확정(2)
                case OrderStatus.Reserved:
                case OrderStatus.Placed:
                    CreateOrderItem(order, newOrderContainer);
                    break;

                // [진행중 패널] 조리중(3) 혹은 배달중(4)
                case OrderStatus.Confirmed:
                case OrderStatus.Cooking:
                case OrderStatus.Prepared:
                case OrderStatus.Delivering:
                    CreateOrderItem(order, processingContainer);
                    break;

                // 장바구니(Pending)나 완료(Completed)는 표시하지 않음
                default:
                    break;
            }
        }
    }

    // 아이템 생성 헬퍼 함수
    private void CreateOrderItem(Order order, Transform parent)
    {
        GameObject go = Instantiate(orderItemPrefab, parent);
        StaffOrderItemUI itemUI = go.GetComponent<StaffOrderItemUI>();

        // 클릭 시 실행할 동작 (상세 팝업 띄우기 등)
        itemUI.Setup(order, OnOrderClicked);
    }

    // 주문 클릭 이벤트
    private void OnOrderClicked(Order order)
    {
        Debug.Log($"주문 선택됨: {order.orderId}");

        // 상세 패널 열기
        if (detailPanel != null)
        {
            UIManager.Instance.ShowPanel("SOrderConfirmPanel", order);
        }
        else
        {
            Debug.LogError("Detail Panel이 연결되지 않았습니다!");
        }
    }

    // Firebase 데이터 파싱 로직
    private async Task<List<Order>> FetchOrdersFromFirebase()
    {
        var list = new List<Order>();

        // 데이터 가져오기
        var snapshot = await dbReference.Child("orders").GetValueAsync();

        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
            {
                try
                {
                    // 수동 파싱 함수 호출
                    Order order = ParseOrder(child);
                    if (order != null)
                    {
                        list.Add(order);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"주문 파싱 실패 (ID: {child.Key}): {e.Message}");
                }
            }
        }

        // 정렬: 주문 시간 순서대로
        list.Sort((a, b) => a.orderTimestamp.CompareTo(b.orderTimestamp));

        return list;
    }

    private Order ParseOrder(DataSnapshot snapshot)
    {
        // 1. 필수값인 UserId 먼저 가져오기
        string userId = ParseString(snapshot, "userId");

        // 생성자로 객체 만들기
        Order order = new Order(userId);

        // 2. 기본 정보 채우기
        order.orderId = snapshot.Key;
        // Enum 변환 (int -> Enum)
        order.status = (OrderStatus)ParseInt(snapshot, "status");
        order.totalPrice = ParseLong(snapshot, "totalPrice");
        order.orderTimestamp = ParseLong(snapshot, "orderTimestamp");
        order.deliveryDate = ParseString(snapshot, "deliveryDate");
        order.isReservation = ParseBool(snapshot, "isReservation");
        order.globalRequests = ParseString(snapshot, "globalRequests");

        // 3. 중첩된 리스트 (CourseGroups) 파싱
        if (snapshot.HasChild("courseGroups"))
        {
            foreach (var groupSnap in snapshot.Child("courseGroups").Children)
            {
                // CourseGroup 생성
                string cType = ParseString(groupSnap, "courseType");
                CourseGroup group = new CourseGroup(cType);

                // Group 안의 Details 리스트 파싱
                if (groupSnap.HasChild("details"))
                {
                    foreach (var detailSnap in groupSnap.Child("details").Children)
                    {
                        CourseDetail detail = new CourseDetail();

                        // 스타일 (Enum)
                        int styleInt = ParseInt(detailSnap, "style");
                        detail.style = (StyleType)styleInt;

                        // 추가된 항목 (List<string>)
                        if (detailSnap.HasChild("addedItems"))
                        {
                            foreach (var item in detailSnap.Child("addedItems").Children)
                                detail.addedItems.Add(item.Value.ToString());
                        }

                        // 제외된 항목 (List<string>)
                        if (detailSnap.HasChild("removedItems"))
                        {
                            foreach (var item in detailSnap.Child("removedItems").Children)
                                detail.removedItems.Add(item.Value.ToString());
                        }

                        group.details.Add(detail);
                    }
                }
                order.courseGroups.Add(group);
            }
        }

        return order;
    }

    private void ClearContainer(Transform container)
    {
        foreach (Transform child in container) Destroy(child.gameObject);
    }

    private string ParseString(DataSnapshot s, string key) => (s.HasChild(key) && s.Child(key).Value != null) ? s.Child(key).Value.ToString() : "";
    private int ParseInt(DataSnapshot s, string key) => (s.HasChild(key) && int.TryParse(s.Child(key).Value.ToString(), out int v)) ? v : 0;
    private long ParseLong(DataSnapshot s, string key) => (s.HasChild(key) && long.TryParse(s.Child(key).Value.ToString(), out long v)) ? v : 0;
    private bool ParseBool(DataSnapshot s, string key) => (s.HasChild(key) && bool.TryParse(s.Child(key).Value.ToString(), out bool v)) ? v : false;
}