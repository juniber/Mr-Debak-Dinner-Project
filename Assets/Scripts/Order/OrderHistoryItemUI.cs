using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class OrderHistoryItemUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text dateText;                // "2025.12.09 오후 06:30"
    public TMP_Text statusText;              // "배달 완료"
    public TMP_Text menuNameAndPriceText;    // "프렌치 디너 외 2건"
    public Button detailButton;

    private Order currentOrder;

    // Order 객체를 받아 UI를 갱신
    public void Setup(Order order)
    {
        currentOrder = order;

        // 1. 날짜 표시 (Unix Timestamp -> DateTime 변환)
        DateTime orderTime = DateTimeOffset.FromUnixTimeSeconds(order.orderTimestamp).LocalDateTime;

        // 예약 주문인 경우 배달 예정일을 강조, 아니면 주문 시간 표시
        if (order.isReservation)
        {
            dateText.text = $"{order.deliveryDate} (예약)";
        }
        else
        {
            dateText.text = orderTime.ToString("yyyy.MM.dd tt hh:mm");
        }

        // 2. 상태 표시 (Enum -> 한글 변환)
        statusText.text = GetStatusString(order.status);

        // 상태에 따른 색상 (완료: 검정, 진행 중: 파랑)
        if (order.status == OrderStatus.Completed)
            statusText.color = Color.black;
        else
            statusText.color = new Color(0f, 0.5f, 1f); // 파란색

        // 3. 메뉴 이름 요약 ("메인메뉴 외 N건")
        string menuSummary = "메뉴 정보 없음";

        if (order.courseGroups != null && order.courseGroups.Count > 0)
        {
            // 첫 번째 메뉴 이름 가져오기
            string firstMenuKey = order.courseGroups[0].courseType;
            string firstMenuName = MenuData.GetMenuName(firstMenuKey);

            // 총 코스 개수 계산
            int totalCount = order.GetTotalCourseCount();

            if (totalCount > 1)
                menuSummary = $"{firstMenuName} 외 {totalCount - 1}건";
            else
                menuSummary = firstMenuName;
        }
        // 4. 가격 표시 (천 단위 콤마)
        menuNameAndPriceText.text = $"{menuSummary}\n{order.totalPrice:N0}원";

        // 5. 상세 버튼 클릭 시 상세 패널 열기
        if (detailButton != null)
        {
            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(() => {

                // Unity 6 권장 API 사용 (비활성화된 객체 포함 검색)
                var detailPanel = FindFirstObjectByType<DetailOrderHistoryManager>(FindObjectsInactive.Include);
                // UIManager를 통해 패널을 열고 데이터 전달 (UIManager 수정 필요 없음, Manager끼리 통신)
                // DetailOrderHistoryManager를 찾아서 Setup 호출
                if (detailPanel != null)
                {
                    UIManager.Instance.ShowPanel("DetailOrderHistoryPanel");
                    detailPanel.Setup(currentOrder);
                }
                else
                {
                    Debug.LogError("DetailOrderHistoryManager를 찾을 수 없습니다.");
                }
            });
        }
    }

    // 상태 Enum을 한글 문자열로 변환
    private string GetStatusString(OrderStatus status)
    {
        switch (status)
        {
            case OrderStatus.Pending: return "주문 대기";
            case OrderStatus.Reserved: return "예약 확정";
            case OrderStatus.Confirmed: return "주문 접수";
            case OrderStatus.Cooking: return "조리 중";
            case OrderStatus.Delivering: return "배달 중";
            case OrderStatus.Completed: return "배달 완료";
            default: return "";
        }
    }
}
