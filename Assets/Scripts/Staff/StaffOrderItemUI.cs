using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text; // StringBuilder 사용

public class StaffOrderItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI dateText;        // 주문 접수 시간
    [SerializeField] private TextMeshProUGUI deliveryDateText;// 배달 희망일 (중요!)
    [SerializeField] private TextMeshProUGUI summaryText;     // 메뉴 요약 (예: 발렌타인 디너 x2)
    [SerializeField] private TextMeshProUGUI statusText;      // 현재 상태 텍스트
    [SerializeField] private TextMeshProUGUI priceText;       // 총 가격
    [SerializeField] private Button itemBtn;                  // 상세 보기 버튼

    private Order currentOrder;
    private Action<Order> onClickCallback;

    public void Setup(Order order, Action<Order> onClick)
    {
        currentOrder = order;
        onClickCallback = onClick;

        // 1. 주문 시간 (Unix Timestamp -> DateTime)
        DateTime orderTime = DateTimeOffset.FromUnixTimeSeconds(order.orderTimestamp).LocalDateTime;
        dateText.text = $"접수: {orderTime:MM/dd HH:mm}";

        // 2. 배달 희망일 강조 (예약 주문인 경우 '예약' 표시)
        string reservationTag = order.isReservation ? " <color=orange>[예약]</color>" : "";
        deliveryDateText.text = $"<color=yellow>배달일: {order.deliveryDate}</color>{reservationTag}";

        // 3. 메뉴 요약 텍스트 생성 (팀원분의 MenuData 활용)
        summaryText.text = GetOrderSummary(order);

        // 4. 가격 표시
        priceText.text = $"{order.totalPrice:N0}원";

        // 5. 상태 텍스트 및 색상
        UpdateStatusUI(order.status);

        // 6. 버튼 클릭 연결
        itemBtn.onClick.RemoveAllListeners();
        itemBtn.onClick.AddListener(() => onClickCallback?.Invoke(currentOrder));
    }

    // 복잡한 주문 객체를 한눈에 보기 좋게 요약하는 함수
    private string GetOrderSummary(Order order)
    {
        StringBuilder sb = new StringBuilder();

        // CourseGroup을 순회하며 "메뉴이름 x개수" 형태로 만듦
        foreach (var group in order.courseGroups)
        {
            // MenuData 클래스의 static 메서드 활용
            string menuName = MenuData.GetMenuName(group.courseType);
            int count = group.details.Count;

            if (count > 0)
            {
                sb.AppendLine($"- {menuName} x{count}");
            }
        }

        // 전역 요청사항이 있으면 표시
        if (!string.IsNullOrEmpty(order.globalRequests))
        {
            sb.AppendLine($"<color=#AAAAAA>(요청: {order.globalRequests})</color>");
        }

        return sb.ToString();
    }

    private void UpdateStatusUI(OrderStatus status)
    {
        switch (status)
        {
            case OrderStatus.Reserved:
                statusText.text = "<color=cyan>확인 대기</color>";
                break;
            case OrderStatus.Confirmed:
                statusText.text = "<color=red>조리 대기</color>"; // 즉시 주문은 빨간색으로 강조
                break;
            case OrderStatus.Cooking:
                statusText.text = "<color=green>조리중</color>";
                break;
            case OrderStatus.Delivering:
                statusText.text = "<color=blue>배달대기/배달중</color>";
                break;
            default:
                statusText.text = status.ToString();
                break;
        }
    }
}