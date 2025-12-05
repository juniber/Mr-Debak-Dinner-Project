using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database; // Firebase 사용
using System.Text;       // StringBuilder 사용

public class StaffOrderConfirmUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI detailText; // 상세 내역 텍스트
    [SerializeField] private Button backSpaceBtn;        // 뒤로가기 버튼

    [Header("Action Buttons")]
    [SerializeField] private GameObject buttonGroup;     // 버튼들을 감싸는 부모 (Btns)
    [SerializeField] private Button confirmBtn;          // 수락 버튼
    [SerializeField] private Button cancelBtn;           // 취소 버튼
    [SerializeField] private Button cookFinishBtn;       // 조리 완료(상태변경) 버튼
    [SerializeField] private TextMeshProUGUI cookBtnText;// 조리 버튼 텍스트 (조리시작/조리완료)

    private Order currentOrder;
    private DatabaseReference dbReference;

    private void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        // 버튼 리스너 연결
        backSpaceBtn.onClick.AddListener(ClosePanel);
        confirmBtn.onClick.AddListener(OnConfirmClicked);
        cancelBtn.onClick.AddListener(OnCancelClicked);
        cookFinishBtn.onClick.AddListener(OnCookClicked);
    }

    public void Open(Order order)
    {
        currentOrder = order;
        gameObject.SetActive(true);
        // 1. UI 내용 채우기
        UpdateDetailText();

        // 2. 상태에 따라 버튼 보여주기/숨기기
        UpdateButtonsState();
    }

    private void ClosePanel()
    {
        UIManager.Instance.ShowPanel("SOrderManagePanel");
    }

    // --- 1. 상세 텍스트 생성 로직 ---
    private void UpdateDetailText()
    {
        StringBuilder sb = new StringBuilder();

        // 상단: 주문 기본 정보
        sb.AppendLine($"<size=120%>주문 상세 정보</size>");
        sb.AppendLine($"------------------------------");
        sb.AppendLine($"주문 시간: {System.DateTimeOffset.FromUnixTimeSeconds(currentOrder.orderTimestamp).LocalDateTime:MM/dd HH:mm}");
        sb.AppendLine($"배달 예정: <color=yellow>{currentOrder.deliveryDate}</color>");

        string typeStr = currentOrder.isReservation ? "<color=cyan>[예약 주문]</color>" : "<color=red>[즉시 주문]</color>";
        sb.AppendLine($"주문 유형: {typeStr}");
        sb.AppendLine($"------------------------------\n");

        // 중단: 메뉴 상세 내역
        sb.AppendLine($"<size=110%>[ 메뉴 목록 ]</size>");

        foreach (var group in currentOrder.courseGroups)
        {
            string menuName = MenuData.GetMenuName(group.courseType);
            sb.AppendLine($"\n● {menuName} (x{group.details.Count})");

            // 각 코스별 옵션(스타일, 추가/제외) 나열
            for (int i = 0; i < group.details.Count; i++)
            {
                var detail = group.details[i];
                sb.AppendLine($"  [{i + 1}] 스타일: {detail.style}");

                // 추가된 항목
                if (detail.addedItems.Count > 0)
                {
                    sb.Append("     <color=green>+ 추가:</color> ");
                    foreach (var item in detail.addedItems)
                        sb.Append($"{MenuData.GetAddonName(item)}, ");
                    sb.Length -= 2; // 마지막 콤마 제거
                    sb.AppendLine();
                }

                // 제외된 항목
                if (detail.removedItems.Count > 0)
                {
                    sb.Append("     <color=red>- 제외:</color> ");
                    foreach (var item in detail.removedItems)
                        sb.Append($"{MenuData.GetAddonName(item)}, ");
                    sb.Length -= 2;
                    sb.AppendLine();
                }
            }
        }

        // 하단: 요청사항 및 가격
        sb.AppendLine($"\n------------------------------");
        if (!string.IsNullOrEmpty(currentOrder.globalRequests))
        {
            sb.AppendLine($"요청사항: {currentOrder.globalRequests}");
        }
        sb.AppendLine($"총 금액: <size=120%>{currentOrder.totalPrice:N0}원</size>");

        detailText.text = sb.ToString();
    }

    // --- 2. 버튼 상태 관리 로직 ---
    private void UpdateButtonsState()
    {
        // 일단 모든 버튼 숨김
        confirmBtn.gameObject.SetActive(false);
        cancelBtn.gameObject.SetActive(false);
        cookFinishBtn.gameObject.SetActive(false);

        switch (currentOrder.status)
        {
            // [예약 대기] 상태 -> 수락 / 취소 가능
            case OrderStatus.Reserved:
                confirmBtn.gameObject.SetActive(true);
                cancelBtn.gameObject.SetActive(true);
                break;

            // [주문 확정] 상태 -> 조리 시작 가능
            case OrderStatus.Confirmed:
                cookFinishBtn.gameObject.SetActive(true);
                cookBtnText.text = "조리 시작";
                break;

            // [조리 중] 상태 -> 조리 완료(배달 시작) 가능
            case OrderStatus.Cooking:
                cookFinishBtn.gameObject.SetActive(true);
                cookBtnText.text = "조리 완료";
                break;

            // 그 외(배달중, 완료, 취소) -> 버튼 없음 (확인용)
            default:
                break;
        }
    }

    // --- 3. 버튼 클릭 이벤트 핸들러 ---

    // [수락] 버튼: Reserved -> Confirmed
    private void OnConfirmClicked()
    {
        UpdateOrderStatus(OrderStatus.Confirmed);
        ClosePanel();
    }

    // [취소] 버튼: -> Canceled
    private void OnCancelClicked()
    {
        UpdateOrderStatus(OrderStatus.Canceled);
        ClosePanel();
    }

    // [조리] 버튼: Confirmed -> Cooking -> Delivering
    private void OnCookClicked()
    {
        if (currentOrder.status == OrderStatus.Confirmed)
        {
            // 조리 시작
            UpdateOrderStatus(OrderStatus.Cooking);
            ClosePanel();
        }
        else if (currentOrder.status == OrderStatus.Cooking)
        {
            // 조리 완료 (배달 시작)
            UpdateOrderStatus(OrderStatus.Delivering);
            ClosePanel();
        }
    }

    private void UpdateOrderStatus(OrderStatus newStatus)
    {
        if (currentOrder == null)
        {
            Debug.LogError("오류: 선택된 주문 객체(currentOrder)가 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(currentOrder.orderId))
        {
            Debug.LogError("오류: 주문 ID(orderId)가 비어있습니다. DB 경로를 찾을 수 없습니다.");
            return;
        }

        // 로컬 데이터 먼저 업데이트
        currentOrder.status = newStatus;

        // DB 경로 확인용 로그 
        string path = $"orders/{currentOrder.orderId}/status";
        Debug.Log($"업데이트 시도 중... 경로: {path}, 값: {(int)newStatus}");

        // Firebase 업데이트
        dbReference.Child("orders").Child(currentOrder.orderId).Child("status").SetValueAsync((int)newStatus)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Firebase 업데이트 실패: {task.Exception}");
                }
                else if (task.IsCanceled)
                {
                    Debug.LogError("Firebase 업데이트 취소됨");
                }
                else if (task.IsCompleted)
                {
                    Debug.Log($"주문 상태 DB 변경 성공! (상태: {newStatus})");
                }
            });
    }
}