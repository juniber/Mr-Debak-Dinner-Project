using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 주문 세션을 관리하는 싱글톤
// 사용자가 앱을 실행하는 동안 생성 중인 'Order' 객체를 보관
public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    // 현재 사용자가 생성 중인 주문 (장바구니)
    public Order CurrentOrder { get; private set; }

    // 현재 DinnerDetailPanel에서 수정 중인 특정 CourseDetail
    private CourseDetail _editingCourseDetail;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
    }

    // 새 코스를 현재 주문에 추가
    public void AddCourseToOrder(CourseType type)
    {
        // 현재 주문이 없다면 새로 생성
        if (CurrentOrder == null)
        {
            // 로그인한 사용자 ID로 새 주문 생성
            FirebaseUser user = auth.CurrentUser;
            if (user == null)
            {
                Debug.LogError("로그인한 사용자 정보가 없습니다.");
                return;
            }
            // 로그인한 사용자 ID로 새 Order 객체 생성
            CurrentOrder = new Order(user.UserId);
        }

        // Order 객체에 새 코스 추가 (새 CourseDetail 객체가 생성됨)
        CurrentOrder.AddCourse(type);
        // 방금 추가한 새 CourseDetail을 '수정 대상'으로 자동 설정
        _editingCourseDetail = CurrentOrder.GetLastAddedCourseDetail();
    }

    // "옵션 변경" 시, 수정할 대상을 명시적으로 설정
    public void SetCourseDetailForEditing(CourseDetail detail)
    {
        _editingCourseDetail = detail;
    }

    // DinnerDetailManager가 현재 수정해야 할 CourseDetail을 반환
    public CourseDetail GetCurrentCourseDetailForEditing()
    {
        if (CurrentOrder == null)
        {
            Debug.LogError("CurrentOrder가 null입니다. AddCourseToOrder가 먼저 호출되어야 합니다.");
            _editingCourseDetail = CurrentOrder?.GetLastAddedCourseDetail();
        }
        // Order 객체 내의 헬퍼 함수를 호출
        return _editingCourseDetail;
    }

    // 장바구니(CurrentOrder)를 비운다. 
    public void ClearOrder()
    {
        CurrentOrder = null;
        _editingCourseDetail = null;
    }

    // 현재 주문을 DB에 저장하고 세션을 종료
    // 'isReservation' 플래그를 받아 즉시 주문과 예약 주문을 구분
    public async Task FinalizeAndSubmitOrder(bool isReservation)
    {
        if (CurrentOrder == null)
        {
            Debug.LogWarning("전송할 주문이 없습니다.");
            return;
        }

        // 1. 주문 객체 상태 확정
        PriceManager.Instance.CalculateTotalPrice(CurrentOrder);

        // 예약 여부에 따라 상태를 다르게 설정
        if (isReservation)
        {
            CurrentOrder.status = OrderStatus.Reserved; // (Enum 값 1)
        }
        else
        {
            CurrentOrder.status = OrderStatus.Confirmed; // (Enum 값 2)
        }
        CurrentOrder.orderTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CurrentOrder.isReservation = isReservation;

        // 2. 재고 차감 (즉시 주문일 경우에만)
        try
        {
            // (수정) 재고 차감 로직을 실제 구현 함수로 변경
            await SubtractInventory(CurrentOrder);
        }
        catch (Exception ex)
        {
            // 재고 차감 트랜잭션 실패 (예: 동시 접속으로 재고 부족)
            Debug.LogError($"재고 차감 실패: {ex}");
            // 사용자에게 오류 메시지를 표시하고 주문 전송을 중단
            UIManager.Instance.ShowTemporaryStatus("죄송합니다. 주문 중 재고가 소진되었습니다.", 3f, 1);
            throw; // 예외를 다시 던져서 ConfirmOrderManager의 catch 블록이 실행되도록 함
        }

        // 3. Order 객체를 JSON으로 변환
        string json = JsonUtility.ToJson(CurrentOrder, true);
        Debug.Log("--- 최종 주문 DB 전송 ---");
        Debug.Log(json);

        // 4. (수정) 주문 경로 분리
        string orderPath = isReservation ? "scheduledOrders" : "orders";

        await dbReference.Child(orderPath).Child(CurrentOrder.orderId).SetRawJsonValueAsync(json);

        Debug.Log($"주문이 [{orderPath}] 경로로 전송되었습니다.");

        // 5. 주문 완료 후 장바구니(CurrentOrder) 비우기
        ClearOrder();
    }

    // 주문에 사용된 모든 재료를 계산하여 DB에서 차감
    private async Task SubtractInventory(Order order)
    {
        Debug.Log("주문 확정: 재고 차감을 시작합니다...");

        // 0. 인증 상태 확인 (가장 의심되는 부분)
        if (auth.CurrentUser == null)
        {
            Debug.LogError("[치명적 오류] 재고 차감 시점에 로그인이 되어있지 않습니다!");
            throw new Exception("User not authenticated");
        }
        else
        {
            Debug.Log($"[인증 확인] 사용자 ID: {auth.CurrentUser.UserId}");
        }

        var totalCost = new Dictionary<string, long>();
        var addonCosts = MenuData.GetAddonCosts(); // 중앙 데이터 가져오기

        // 1. 장바구니의 모든 재료 소모량 계산
        foreach (var group in order.courseGroups)
        {
            if (string.IsNullOrEmpty(group.courseType)) continue; // 안전 장치

            // Enum 파싱 시도
            if (!System.Enum.TryParse(group.courseType, out CourseType type))
            {
                Debug.LogWarning($"알 수 없는 코스 타입: {group.courseType}");
                continue;
            }
            var baseReqs = MenuData.GetCourseBaseRequirements(type);

            foreach (var detail in group.details)
            {
                // 기본 재료
                foreach (var req in baseReqs)
                {
                    if (!totalCost.ContainsKey(req.Key)) totalCost[req.Key] = 0;
                    totalCost[req.Key] += req.Value;
                }

                // 추가 재료
                foreach (string addonKey in detail.addedItems)
                {
                    if (addonCosts.TryGetValue(addonKey, out var costInfo))
                    {
                        if (!totalCost.ContainsKey(costInfo.InventoryKey)) totalCost[costInfo.InventoryKey] = 0;
                        totalCost[costInfo.InventoryKey] += costInfo.Amount;
                    }
                    else
                    {
                        Debug.LogWarning($"알 수 없는 추가 옵션 키: {addonKey}");
                    }
                }

                // 제외 재료 (환불)
                foreach (string removedKey in detail.removedItems)
                {
                    var refund = MenuData.GetRefundInfo(type, removedKey);
                    if (refund != null)
                    {
                        if (!totalCost.ContainsKey(refund.InventoryKey)) totalCost[refund.InventoryKey] = 0;
                        totalCost[refund.InventoryKey] -= refund.Amount;
                    }
                }
            }
        }

        // 2. 계산된 총 소모량을 기반으로 DB 트랜잭션 실행
        List<Task> transactionTasks = new List<Task>();
        foreach (var item in totalCost)
        {
            string itemKey = item.Key;
            long amountToSubtract = item.Value;

            if (amountToSubtract <= 0) continue; // 0 이하면 차감할 필요 없음

            Debug.Log($"[Transaction] {itemKey} 재고 {amountToSubtract} 차감 시도...");

            DatabaseReference itemRef = dbReference.Child("inventory").Child(itemKey);

            // 각 재료 항목에 대해 개별 트랜잭션 실행
            Task transactionTask = itemRef.RunTransaction(data =>
            {
                if (data.Value == null)
                {
                    // DB에 해당 재고 항목이 없음. 오류로 중단.
                    Debug.LogWarning($"재고 차감 실패: {itemKey} 항목이 DB에 없습니다.");
                    return TransactionResult.Success(data); // 멈추지 않고 넘어감
                }

                long currentStock = Convert.ToInt64(data.Value);
                try
                {
                    currentStock = Convert.ToInt64(data.Value);
                }
                catch
                {
                    Debug.LogError($"[형변환 오류] {itemKey} 값: {data.Value}");
                    return TransactionResult.Abort();
                }

                if (currentStock < amountToSubtract)
                {
                    Debug.LogWarning($"[재고 부족] {itemKey} (남음: {currentStock}, 필요: {amountToSubtract})");
                    return TransactionResult.Abort();
                }

                // 재고 차감
                data.Value = currentStock - amountToSubtract;
                return TransactionResult.Success(data);
            });

            transactionTasks.Add(transactionTask);
        }

        // 3. 모든 재고 차감 트랜잭션이 완료될 때까지 기다림
        // 만약 하나라도 Abort()되면, Task.WhenAll이 예외(DatabaseException)를 던집니다.
        try
        {
            await Task.WhenAll(transactionTasks);
            Debug.Log("모든 재고 차감 완료.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[트랜잭션 실패] 상세 내용: {ex.ToString()}");
            throw;
        }
    }
}
