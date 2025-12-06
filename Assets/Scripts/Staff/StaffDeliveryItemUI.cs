using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaffDeliveryItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI statusText;  // "배달 대기중..."
    [SerializeField] private TextMeshProUGUI addressText; // 주소 : 서울시 ...
    [SerializeField] private Button acceptBtn;            // 수락 버튼

    private Order currentOrder;
    private System.Action<Order> onAcceptCallback;

    public void Setup(Order order, System.Action<Order> onAccept)
    {
        currentOrder = order;
        onAcceptCallback = onAccept;

        // 1. 주소 표시
        if (string.IsNullOrEmpty(order.deliveryAddress))
        {
            addressText.text = "주소 : (주소 정보 없음)";
        }
        else
        {
            addressText.text = $"주소 : {order.deliveryAddress}";
        }

        // 2. 상태 표시 및 버튼 활성화 여부
        // 이미 riderId가 있다면(누가 채갔다면) 버튼 잠금
        if (!string.IsNullOrEmpty(order.riderId))
        {
            statusText.text = "이미 배차된 주문";
            acceptBtn.interactable = false;
        }
        else
        {
            statusText.text = "배달 대기중...";
            acceptBtn.interactable = true;
        }

        // 3. 버튼 리스너 연결
        acceptBtn.onClick.RemoveAllListeners();
        acceptBtn.onClick.AddListener(() => onAcceptCallback?.Invoke(currentOrder));
    }
}