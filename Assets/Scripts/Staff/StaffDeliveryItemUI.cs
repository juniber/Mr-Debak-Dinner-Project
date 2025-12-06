using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaffDeliveryItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI addressText;
    [SerializeField] private Button actionBtn;       // 수락 or 완료 버튼
    [SerializeField] private TextMeshProUGUI btnText; // 버튼 안의 텍스트

    private Order currentOrder;
    // 콜백 함수: (주문객체, "Accept"인지 "Complete"인지 여부)
    private System.Action<Order, bool> onActionCallback;
    private bool isMyOrder = false;

    public void Setup(Order order, bool isMine, System.Action<Order, bool> onAction)
    {
        currentOrder = order;
        isMyOrder = isMine;
        onActionCallback = onAction;

        // 1. 주소 표시
        addressText.text = string.IsNullOrEmpty(order.deliveryAddress) ?
                           "주소 정보 없음" : $"주소 : {order.deliveryAddress}";

        // 2. 상태에 따른 UI 변경
        if (isMyOrder)
        {
            // [내 배달 목록] - 배달 완료 버튼 표시
            statusText.text = "<color=green>현재 배달 중입니다!</color>";
            btnText.text = "배달 완료";
            actionBtn.interactable = true;

            // 버튼 색상 변경 (초록색 등) - 선택사항
            actionBtn.image.color = new Color(0.3f, 0.8f, 0.3f);
        }
        else
        {
            // [대기 주문 목록] - 수락 버튼 표시
            if (!string.IsNullOrEmpty(order.riderId))
            {
                statusText.text = "이미 배차된 주문";
                btnText.text = "수락 불가";
                actionBtn.interactable = false;
                actionBtn.image.color = Color.gray;
            }
            else
            {
                statusText.text = "배달 기사 기다리는 중...";
                btnText.text = "주문 수락";
                actionBtn.interactable = true;
                actionBtn.image.color = Color.white;
            }
        }

        // 3. 버튼 리스너
        actionBtn.onClick.RemoveAllListeners();
        actionBtn.onClick.AddListener(() =>
        {
            // isMyOrder가 true면 '완료' 로직, false면 '수락' 로직 실행
            onActionCallback?.Invoke(currentOrder, isMyOrder);
        });
    }
}