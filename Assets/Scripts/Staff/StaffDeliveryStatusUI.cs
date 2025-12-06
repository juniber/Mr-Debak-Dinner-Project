using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SDeliveryStatusPanel : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private Transform deliveryContainer;
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

    private async void LoadDeliveryOrders()
    {
        ClearContainer();

        var query = dbReference.Child("orders").OrderByChild("status").EqualTo((int)OrderStatus.Delivering);
        var snapshot = await query.GetValueAsync();

        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
            {
                // 수동 파싱 (GetRawJson 오류 방지)
                string orderId = child.Key;
                string riderId = ParseString(child, "riderId", "");
                string address = ParseString(child, "deliveryAddress", "주소 미입력");

                // 아직 배차되지 않은(riderId가 없는) 주문만 표시
                if (string.IsNullOrEmpty(riderId))
                {
                    // UI 생성을 위한 임시 객체
                    Order order = new Order("");
                    order.orderId = orderId;
                    order.riderId = riderId;
                    order.deliveryAddress = address;

                    CreateDeliveryItem(order);
                }
            }
        }
    }

    private void CreateDeliveryItem(Order order)
    {
        GameObject go = Instantiate(deliveryItemPrefab, deliveryContainer);
        var itemUI = go.GetComponent<StaffDeliveryItemUI>();
        itemUI.Setup(order, OnAcceptOrderClicked);
    }

    // ★ [핵심 수정] 트랜잭션 로직 변경
    private void OnAcceptOrderClicked(Order order)
    {
        Debug.Log($"주문 수락 시도: {order.orderId}");
        DatabaseReference orderRef = dbReference.Child("orders").Child(order.orderId);

        orderRef.RunTransaction(mutableData =>
        {
            var data = mutableData.Value as Dictionary<string, object>;
            if (data == null) return TransactionResult.Success(mutableData);

            // 1. 이미 누가 가져갔는지 확인
            if (data.ContainsKey("riderId") && !string.IsNullOrEmpty(data["riderId"].ToString()))
            {
                // 이미 선점됨 -> 그냥 현재 상태 그대로 성공 처리 (값 변경 안 함)
                // (Abort를 하면 에러 처리가 복잡해지므로, Success로 반환하되 내가 먹었는지 나중에 확인)
                return TransactionResult.Success(mutableData);
            }

            // 2. 비어있다면 내 UID 등록 (내가 먹음)
            data["riderId"] = myUid;
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
                // ★ 수정된 부분: task.Result는 DataSnapshot입니다. IsCommitted가 없습니다.
                // 대신 결과 데이터(Snapshot)를 확인해서 riderId가 '나'인지 확인합니다.
                DataSnapshot snapshot = task.Result;
                string winnerId = "";

                if (snapshot.HasChild("riderId"))
                {
                    winnerId = snapshot.Child("riderId").Value.ToString();
                }

                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (winnerId == myUid)
                    {
                        // 성공: 내가 배차 받음
                        Debug.Log("배차 성공! 내가 배달합니다.");
                        LoadDeliveryOrders();
                        SetRiderStatus("배달중");
                    }
                    else
                    {
                        // 실패: 다른 사람이 먼저 채감
                        Debug.LogWarning("이미 다른 기사님이 수락했습니다.");
                        UIManager.Instance.ShowTemporaryStatus("이미 배차된 주문입니다.", 2f);
                        LoadDeliveryOrders(); // 목록 갱신
                    }
                });
            }
        });
    }

    private void SetRiderStatus(string status)
    {
        if (string.IsNullOrEmpty(myUid)) return;
        dbReference.Child("users").Child(myUid).Child("workStatus").SetValueAsync(status);
        UpdateRiderStatusUI(status);
    }

    private void UpdateRiderStatusUI(string status)
    {
        Color active = Color.green;
        Color inactive = Color.white;
        if (onDeliveryBtn) onDeliveryBtn.image.color = (status == "배달중") ? active : inactive;
        if (onFreeBtn) onFreeBtn.image.color = (status == "대기중") ? active : inactive;
    }

    private void OnBackClicked() { UIManager.Instance.ShowPanel("StaffMainPanel"); }
    private void ClearContainer() { foreach (Transform child in deliveryContainer) Destroy(child.gameObject); }

    private string ParseString(DataSnapshot s, string key, string def)
    {
        if (s.HasChild(key) && s.Child(key).Value != null) return s.Child(key).Value.ToString();
        return def;
    }
}