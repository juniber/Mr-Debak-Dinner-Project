using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth; // 로그아웃/탈퇴용
using System.Threading.Tasks; // 비동기 처리용

public class StaffSelfService : MonoBehaviour
{
    [Header("1. 매장 관리 버튼")]
    [SerializeField] private Button inventoryBtn;      // 재고관리
    [SerializeField] private Button menuSettingBtn;    // 메뉴설정

    [Header("2. 업무 화면 이동 버튼")]
    [SerializeField] private Button kitchenBtn;        // 주방 (조리 현황 등)
    [SerializeField] private Button deliveryBtn;       // 배달 (배달 현황 등)

    [Header("3. 계정 관리 버튼")]
    [SerializeField] private Button logoutBtn;         // 로그아웃
    [SerializeField] private Button deleteAccountBtn;  // 회원탈퇴

    private void Start()
    {
        // 버튼 리스너 연결
        if (inventoryBtn) inventoryBtn.onClick.AddListener(OnInventoryClicked);
        if (menuSettingBtn) menuSettingBtn.onClick.AddListener(OnMenuSettingClicked);
        if (kitchenBtn) kitchenBtn.onClick.AddListener(OnKitchenClicked);
        if (deliveryBtn) deliveryBtn.onClick.AddListener(OnDeliveryClicked);
        if (logoutBtn) logoutBtn.onClick.AddListener(OnLogoutClicked);
        if (deleteAccountBtn) deleteAccountBtn.onClick.AddListener(OnDeleteAccountClicked);
    }

    // --- 이벤트 핸들러 ---

    private void OnInventoryClicked()
    {
        // 아직 안 만드셨다면 패널 이름만 정해두세요 (예: SInventoryPanel)
        UIManager.Instance.ShowPanel("SInventoryPanel");
    }

    private void OnMenuSettingClicked()
    {
        UIManager.Instance.ShowPanel("SMenuSettingPanel");
    }

    private void OnKitchenClicked()
    {
        // 주방 화면이 따로 있다면 거기로, 없다면 주문 처리 화면으로
        UIManager.Instance.ShowPanel("SKitchenPanel");
    }

    private void OnDeliveryClicked()
    {
        // 배달 현황 화면으로 이동
        UIManager.Instance.ShowPanel("SDeliveryStatusPanel");
    }

    // [로그아웃]
    private void OnLogoutClicked()
    {
        Debug.Log("로그아웃 시도...");
        FirebaseAuth.DefaultInstance.SignOut(); // Firebase 로그아웃

        // 로그인 화면으로 강제 이동
        UIManager.Instance.ShowPanel("LoginPanel");
        Debug.Log("로그아웃 완료. 로그인 화면으로 이동합니다.");
    }

    // [회원탈퇴] - 주의: 실제 앱에서는 "정말 탈퇴하시겠습니까?" 팝업을 띄워야 합니다.
    private void OnDeleteAccountClicked()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null)
        {
            // 계정 삭제 비동기 호출
            user.DeleteAsync().ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("회원탈퇴 취소됨");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError($"회원탈퇴 실패: {task.Exception}");
                    // 오래된 로그인 세션이면 "재로그인 필요" 에러가 날 수 있음
                    return;
                }

                // 성공 시 메인 스레드에서 로그인 화면으로 이동해야 함 (유니티 UI 제약)
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    Debug.Log("회원탈퇴 성공! 안녕히 가세요.");
                    UIManager.Instance.ShowPanel("LoginPanel");
                });
            });
        }
    }
}