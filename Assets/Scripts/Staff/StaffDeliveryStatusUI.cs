using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

public class StaffDeliveryStatusUI : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private Transform availableContainer;   // 대기 주문 (DeliveryContainer)
    [SerializeField] private Transform myDeliveryContainer;  // 내 배달 주문 (새로 만든 것)
    [SerializeField] private GameObject deliveryItemPrefab;

    [Header("Rider Status Buttons")]
    [SerializeField] private Button onDeliveryBtn;
    [SerializeField] private Button onFreeBtn;
    [SerializeField] private Button backspaceBtn;

    private DatabaseReference dbReference;
    private string myUid;

    private void Awake()
    {
        if (onDeliveryBtn) onDeliveryBtn.onClick.AddListener(() => SetRiderStatus("배달중"));
        if (onFreeBtn) onFreeBtn.onClick.AddListener(() => SetRiderStatus("대기중"));
        if (backspaceBtn) backspaceBtn.onClick.AddListener(OnBackClicked);
    }

    private void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        var user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null) myUid = user.UserId;
    }

    private void OnEnable()
    {
        if (dbReference == null) dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        LoadDeliveryOrders();
    }

    // --- 1. 주문 목록 불러오기 (쿼리 2번 실행) ---
    private async void LoadDeliveryOrders()
    {
        // 1. 기존 목록 초기화
        ClearContainer(availableContainer);
        ClearContainer(myDeliveryContainer);

        // 2. orders 노드 통째로 가져오기 (쿼리 조건 없음)
        var snapshot = await dbReference.Child("orders").GetValueAsync();

        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
            {
                // 데이터 파싱
                string orderId = child.Key;
                string riderId = ParseString(child, "riderId", "");
                string address = ParseString(child, "deliveryAddress", "주소 미입력");

                // 상태값 가져오기 (없으면 -1)
                int status = -1;
                if (child.HasChild("status"))
                    int.TryParse(child.Child("status").Value.ToString(), out status);

                long discountPrice = 0;
                if (child.HasChild("totalDiscountPrice"))
                    long.TryParse(child.Child("totalDiscountPrice").Value.ToString(), out discountPrice);


                // 주문 객체 생성
                Order order = new Order("");
                order.orderId = orderId;
                order.riderId = riderId;
                order.deliveryAddress = address;
                order.status = (OrderStatus)status;
                order.totalDiscountPrice = discountPrice;

                // ==========================================================
                // ★ [분류 로직] 상태와 라이더 ID를 보고 어디에 넣을지 결정
                // ==========================================================

                // Case A: [배차 대기] -> 상태가 'Prepared(5)' 이고, 라이더가 없는 경우
                if (status == (int)OrderStatus.Prepared && string.IsNullOrEmpty(riderId))
                {
                    CreateDeliveryItem(order, availableContainer, false); // isMine = false
                }

                // Case B: [내 배달] -> 상태가 'Delivering(6)' 이고, 라이더가 '나'인 경우
                else if (status == (int)OrderStatus.Delivering && riderId == myUid)
                {
                    CreateDeliveryItem(order, myDeliveryContainer, true); // isMine = true
                }

                // 그 외(완료된 주문, 취소된 주문, 남이 배달중인 주문)는 그냥 무시(Pass)
            }
        }
    }

    private void CreateDeliveryItem(Order order, Transform parent, bool isMine)
    {
        GameObject go = Instantiate(deliveryItemPrefab, parent);
        var itemUI = go.GetComponent<StaffDeliveryItemUI>();
        itemUI.Setup(order, isMine, OnItemActionClicked);
    }

    // --- 버튼 클릭 통합 핸들러 ---
    private void OnItemActionClicked(Order order, bool isMine)
    {
        if (isMine) CompleteDelivery(order); // 배달 완료 처리
        else AcceptOrder(order);             // 주문 수락 처리
    }

    // --- 2. [핵심] 주문 수락 트랜잭션 (검증 강화) ---
    private void AcceptOrder(Order order)
    {
        Debug.Log($"주문 수락 시도: {order.orderId}");
        DatabaseReference orderRef = dbReference.Child("orders").Child(order.orderId);

        orderRef.RunTransaction(mutableData =>
        {
            var data = mutableData.Value as Dictionary<string, object>;
            if (data == null) return TransactionResult.Success(mutableData);

            // [검증 1] 이미 배차된 주문인지 확인 (riderId 존재 여부)
            if (data.ContainsKey("riderId") && !string.IsNullOrEmpty(data["riderId"].ToString()))
            {
                return TransactionResult.Abort(); // 이미 누가 가져감 -> 실패
            }

            // [검증 2] 주문 상태가 여전히 'Prepared(4)'인지 확인
            // (만약 직원이 취소했거나 상태가 바뀌었다면 수락 불가)
            int currentStatus = -1;
            if (data.ContainsKey("status"))
            {
                int.TryParse(data["status"].ToString(), out currentStatus);
            }

            if (currentStatus != (int)OrderStatus.Prepared)
            {
                return TransactionResult.Abort(); // 상태가 변함 -> 실패
            }

            // [성공 시 데이터 변경]
            // 1. 내 아이디 등록
            data["riderId"] = myUid;
            // 2. 상태를 'Delivering(5)'으로 변경
            data["status"] = (int)OrderStatus.Delivering;

            mutableData.Value = data;
            return TransactionResult.Success(mutableData);
        })
        .ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"트랜잭션 오류: {task.Exception}");
                return;
            }

            if (task.IsCompleted)
            {
                // 결과 확인: riderId가 내 것으로 바뀌었는지 확인
                DataSnapshot snapshot = task.Result;
                string winnerId = snapshot.HasChild("riderId") ? snapshot.Child("riderId").Value.ToString() : "";

                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (winnerId == myUid)
                    {
                        Debug.Log("배차 성공!");
                        SetRiderStatus("배달중");
                        LoadDeliveryOrders(); // 목록 새로고침
                    }
                    else
                    {
                        // 실패 (다른 기사가 가져감 or 상태 변경됨)
                        UIManager.Instance.ShowTemporaryStatus("이미 배차되었거나 상태가 변경된 주문입니다.", 2f);
                        LoadDeliveryOrders();
                    }
                });
            }
        });
    }

    // --- 3. 배달 완료 처리 ---
    private void CompleteDelivery(Order order)
    {
        Debug.Log($"배달 완료 처리: {order.orderId}");

        // 1. 주문 상태 변경 (Delivering -> Completed)
        var updates = new Dictionary<string, object>();
        updates[$"orders/{order.orderId}/status"] = (int)OrderStatus.Completed;

        dbReference.UpdateChildrenAsync(updates).ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                // ★ [추가] 매출액 및 완료 횟수 집계 (store_info)
                // 요청하신 대로 'totalDiscountPrice'를 넘겨줍니다.
                UpdateStoreSalesInfo(order.totalDiscountPrice);

                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    UIManager.Instance.ShowTemporaryStatus("배달 완료! 수고하셨습니다.", 2f);
                    SetRiderStatus("대기중");
                    LoadDeliveryOrders();
                });
            }
            else
            {
                Debug.LogError($"상태 변경 실패: {task.Exception}");
            }
        });
    }

    private void UpdateStoreSalesInfo(long finalPrice)
    {
        DatabaseReference storeRef = dbReference.Child("store_info");

        storeRef.RunTransaction(mutableData =>
        {
            var data = mutableData.Value as Dictionary<string, object>;
            if (data == null) data = new Dictionary<string, object>();

            // A. 총 매출 더하기 (totalSales)
            long currentSales = 0;
            if (data.ContainsKey("totalSales"))
            {
                // 안전한 형변환
                long.TryParse(data["totalSales"].ToString(), out currentSales);
            }

            data["totalSales"] = currentSales + finalPrice;

            // B. 완료된 주문 수 +1 (completedOrderCount)
            long currentCompleted = 0;
            if (data.ContainsKey("completedOrderCount"))
            {
                long.TryParse(data["completedOrderCount"].ToString(), out currentCompleted);
            }

            data["completedOrderCount"] = currentCompleted + 1;

            // 변경된 데이터 저장
            mutableData.Value = data;
            return TransactionResult.Success(mutableData);
        });
    }

    // --- 기타 헬퍼 함수들 ---
    private void SetRiderStatus(string status)
    {
        if (!string.IsNullOrEmpty(myUid))
        {
            dbReference.Child("users").Child(myUid).Child("workStatus").SetValueAsync(status);
            UpdateRiderStatusUI(status);
        }
    }

    private void UpdateRiderStatusUI(string status)
    {
        Color active = Color.green;
        Color inactive = Color.white;
        if (onDeliveryBtn) onDeliveryBtn.image.color = (status == "배달중") ? active : inactive;
        if (onFreeBtn) onFreeBtn.image.color = (status == "대기중") ? active : inactive;
    }

    private void OnBackClicked() { UIManager.Instance.ShowPanel("StaffMainPanel"); }

    private void ClearContainer(Transform t) { foreach (Transform child in t) Destroy(child.gameObject); }

    // 데이터 파싱 헬퍼
    private Order CreateOrderObject(DataSnapshot child)
    {
        Order order = new Order("");
        order.orderId = child.Key;
        order.riderId = ParseString(child, "riderId", "");
        order.deliveryAddress = ParseString(child, "deliveryAddress", "주소 미입력");
        return order;
    }

    private string ParseString(DataSnapshot s, string key, string def)
    {
        if (s.HasChild(key) && s.Child(key).Value != null) return s.Child(key).Value.ToString();
        return def;
    }
}